// qFoldIT Toolbelt for UNIGINE 2 — UAGBridgeTools.cs
// Category: UAGBridge
//
// The piece that connects UNIGINE-TOOLBELT to the rest of the qFoldIT
// stack (SOS -> SKG -> SEM -> UAG -> UWI -> MCP), mirroring UNITY-TOOLBELT's
// UAGBridgeTools.cs and UEFN-TOOLBELT's unreal-world-builder skill:
// validate first, realize the graph purely by calling this toolbelt's own
// already-registered tools (never touching Unigine.* directly here), and
// report gaps explicitly.
//
// Architectural note: unlike the Unity version (which calls other tool
// files' public static methods directly, in-process), this file dispatches
// through ToolRegistry.Dispatch(name, JObject) — the exact same path an
// external MCP client uses via mcp_server.py's run_toolbelt_tool. That
// keeps this adapter honest in a stronger sense: it has no special access
// any other caller lacks.

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using QFoldIT.Toolbelt.Uag;

namespace QFoldIT.Toolbelt
{
    public static class UAGBridgeTools
    {
        public static void Register()
        {
            ToolRegistry.Register("uag_validate", "UAGBridge",
                "Validates a UAG v0.1 graph against this engine's adapter: dangling id references, parent_child cycles, and which node/constraint/interaction types this adapter can and cannot realize. Makes no changes to the world.",
                UagValidateTool);

            ToolRegistry.Register("uag_apply", "UAGBridge",
                "Realizes a validated UAG v0.1 graph in the loaded world by calling this toolbelt's own tools (spawn_primitive, light_create, parent_node, physics_add_shape, etc.). Aborts with no world changes if validation fails.",
                UagApply);
        }

        private static object UagValidateTool(JObject p)
        {
            string uagJson = (string)p["uag_json"];
            UagGraph graph;
            try { graph = UagGraph.Parse(uagJson); }
            catch (System.Exception ex) { return new { success = false, error = $"Could not parse UAG JSON: {ex.Message}" }; }

            var result = UagValidator.Validate(graph);
            return new
            {
                success = true,
                is_valid = result.IsValid,
                errors = result.Errors,
                unmapped_node_types = result.UnmappedNodeTypes,
                unmapped_constraint_types = result.UnmappedConstraintTypes,
                unmapped_interactions = result.UnmappedInteractions.Select(i => new { i.Id, i.Trigger, i.TargetNode, i.Action }),
                node_count = graph.Nodes.Count,
                connection_count = graph.Connections.Count,
                constraint_count = graph.Constraints.Count,
                interaction_count = graph.Interactions.Count
            };
        }

        private static object UagApply(JObject p)
        {
            string uagJson = (string)p["uag_json"];
            bool generateInteractionStub = (bool?)p["generate_interaction_stub"] ?? true;
            string stubOutputPath = (string)p["stub_output_path"] ?? "logic/generated/uag_interaction_handlers.cs";

            UagGraph graph;
            try { graph = UagGraph.Parse(uagJson); }
            catch (System.Exception ex) { return new { success = false, error = $"Could not parse UAG JSON: {ex.Message}" }; }

            var validation = UagValidator.Validate(graph);
            if (!validation.IsValid)
                return new { success = false, error = "Validation failed — no changes made.", validation_errors = validation.Errors };

            var idMap = new Dictionary<string, string>();
            var nodeFailures = new List<object>();
            var unrealizedNodeIds = new HashSet<string>();

            // ── Pass 1: create every node ──
            foreach (var node in graph.Nodes)
            {
                if (!UagValidator.MappedNodeTypes.Contains(node.Type))
                {
                    unrealizedNodeIds.Add(node.Id);
                    continue;
                }

                var createResult = CreateNode(node);
                bool ok = GetBoolField(createResult, "success");
                if (ok)
                {
                    idMap[node.Id] = node.Id;
                    ApplyTransform(node);
                }
                else
                {
                    unrealizedNodeIds.Add(node.Id);
                    string reason = GetStringField(createResult, "error") ?? "unknown error";
                    nodeFailures.Add(new { node_id = node.Id, type = node.Type, error = reason });
                }
            }

            // ── Pass 2: parent_id hierarchy ──
            int reparented = 0;
            foreach (var node in graph.Nodes)
            {
                if (string.IsNullOrEmpty(node.ParentId) || unrealizedNodeIds.Contains(node.Id) || unrealizedNodeIds.Contains(node.ParentId))
                    continue;
                ToolRegistry.Dispatch("parent_node", new JObject { ["child"] = node.Id, ["parent"] = node.ParentId });
                reparented++;
            }

            // ── Pass 3: connections ──
            int connectionsApplied = 0;
            var unmappedConnectionTypes = new HashSet<string>();
            foreach (var conn in graph.Connections)
            {
                if (unrealizedNodeIds.Contains(conn.FromNode) || unrealizedNodeIds.Contains(conn.ToNode)) continue;

                switch (conn.Type)
                {
                    case "parent_child":
                        ToolRegistry.Dispatch("parent_node", new JObject { ["child"] = conn.FromNode, ["parent"] = conn.ToNode });
                        connectionsApplied++;
                        break;
                    case "joint_fixed":
                    case "joint_hinge":
                    case "joint_slider":
                        string jointType = conn.Type.Substring("joint_".Length);
                        ToolRegistry.Dispatch("physics_add_joint", new JObject { ["name"] = conn.FromNode, ["joint_type"] = jointType, ["connected_body"] = conn.ToNode });
                        connectionsApplied++;
                        break;
                    default:
                        unmappedConnectionTypes.Add(conn.Type); // e.g. data_link — no UNIGINE primitive
                        break;
                }
            }

            // ── Pass 4: constraints ──
            int constraintsApplied = 0;
            var interactionNodeIds = new HashSet<string>();
            foreach (var constraint in graph.Constraints)
            {
                var validTargets = constraint.TargetNodes.Where(t => !unrealizedNodeIds.Contains(t)).ToList();
                if (constraint.Type == "physics_collision")
                {
                    foreach (var target in validTargets)
                    {
                        string shape = (string)constraint.Properties["shape"] ?? "box";
                        ToolRegistry.Dispatch("physics_add_shape", new JObject { ["name"] = target, ["shape"] = shape, ["is_trigger"] = false });
                        ToolRegistry.Dispatch("physics_add_body", new JObject { ["name"] = target, ["mass"] = 1f });
                        constraintsApplied++;
                    }
                }
                else
                {
                    foreach (var t in validTargets) interactionNodeIds.Add(t);
                }
            }
            foreach (var interaction in graph.Interactions)
                if (!unrealizedNodeIds.Contains(interaction.TargetNode))
                    interactionNodeIds.Add(interaction.TargetNode);

            // ── Optional codegen stub for everything not live-realizable ──
            string stubPath = null;
            if (generateInteractionStub && interactionNodeIds.Count > 0)
            {
                ToolRegistry.Dispatch("codegen_node_component", new JObject
                {
                    ["class_name"] = "UagInteractionHandlers",
                    ["node_names"] = string.Join(",", interactionNodeIds),
                    ["output_path"] = stubOutputPath,
                    ["namespace"] = "QFoldIT.Generated"
                });
                stubPath = stubOutputPath;
            }

            return new
            {
                success = true,
                nodes_created = idMap.Count,
                node_failures = nodeFailures,
                unmapped_node_types = validation.UnmappedNodeTypes,
                nodes_reparented = reparented,
                connections_applied = connectionsApplied,
                unmapped_connection_types = unmappedConnectionTypes,
                constraints_applied = constraintsApplied,
                unmapped_constraint_types = validation.UnmappedConstraintTypes,
                unmapped_interactions = validation.UnmappedInteractions.Select(i => new { i.Id, i.Trigger, i.TargetNode, i.Action }),
                interaction_stub_path = stubPath,
                id_map = idMap
            };
        }

        // ── Node type -> existing-tool dispatch ────────────────────────
        private static object CreateNode(UagNode node)
        {
            var pos = node.Transform?.Position ?? new float[] { 0, 0, 0 };
            double x = pos.Length > 0 ? pos[0] : 0, y = pos.Length > 1 ? pos[1] : 0, z = pos.Length > 2 ? pos[2] : 0;

            switch (node.Type)
            {
                case "mesh":
                    var meshPath = (string)node.Properties["mesh_ref"];
                    if (!string.IsNullOrEmpty(meshPath))
                        return ToolRegistry.Dispatch("asset_instantiate_node", new JObject { ["node_path"] = meshPath, ["x"] = x, ["y"] = y, ["z"] = z, ["name"] = node.Id });
                    return ToolRegistry.Dispatch("spawn_primitive", new JObject { ["type"] = (string)node.Properties["primitive"] ?? "box", ["name"] = node.Id, ["x"] = x, ["y"] = y, ["z"] = z });

                case "light":
                    return ToolRegistry.Dispatch("light_create", new JObject
                    {
                        ["type"] = (string)node.Properties["light_type"] ?? "point",
                        ["name"] = node.Id, ["x"] = x, ["y"] = y, ["z"] = z,
                        ["color_hex"] = (string)node.Properties["color_hex"] ?? "FFFFFF",
                        ["intensity"] = (float?)node.Properties["intensity"] ?? 1f
                    });

                case "camera":
                    return ToolRegistry.Dispatch("camera_create", new JObject { ["name"] = node.Id, ["x"] = x, ["y"] = y, ["z"] = z, ["fov"] = (float?)node.Properties["fov"] ?? 60f });

                case "audio_source":
                    {
                        ToolRegistry.Dispatch("spawn_group_node", new JObject { ["name"] = node.Id, ["x"] = x, ["y"] = y, ["z"] = z });
                        var soundPath = (string)node.Properties["sound_path"];
                        if (string.IsNullOrEmpty(soundPath))
                            return new { success = false, error = "audio_source node has no properties.sound_path — UNIGINE has no default sound asset." };
                        return ToolRegistry.Dispatch("audio_add_source", new JObject { ["name"] = node.Id, ["sound_path"] = soundPath, ["x"] = x, ["y"] = y, ["z"] = z, ["loop"] = (bool?)node.Properties["loop"] ?? false });
                    }

                case "particle_emitter":
                    {
                        var assetPath = (string)node.Properties["asset_ref"];
                        if (string.IsNullOrEmpty(assetPath))
                            return new { success = false, error = "particle_emitter node has no properties.asset_ref — UNIGINE has no built-in generic particle preset (unlike the Unity adapter)." };
                        return ToolRegistry.Dispatch("particles_spawn_from_asset", new JObject { ["name"] = node.Id, ["asset_path"] = assetPath, ["x"] = x, ["y"] = y, ["z"] = z });
                    }

                case "ui_panel":
                    return ToolRegistry.Dispatch("ui_create_panel", new JObject { ["name"] = node.Id, ["x"] = (int)x, ["y"] = (int)y });

                case "trigger_volume":
                    {
                        var r1 = ToolRegistry.Dispatch("spawn_primitive", new JObject { ["type"] = "box", ["name"] = node.Id, ["x"] = x, ["y"] = y, ["z"] = z });
                        ToolRegistry.Dispatch("physics_add_shape", new JObject { ["name"] = node.Id, ["shape"] = "box", ["is_trigger"] = true });
                        return r1;
                    }

                case "group":
                    return ToolRegistry.Dispatch("spawn_group_node", new JObject { ["name"] = node.Id, ["x"] = x, ["y"] = y, ["z"] = z });

                default:
                    return new { success = false, error = $"No creation handler for node type '{node.Type}'." };
            }
        }

        private static void ApplyTransform(UagNode node)
        {
            var rot = node.Transform?.RotationEulerDeg ?? new float[] { 0, 0, 0 };
            var scl = node.Transform?.Scale ?? new float[] { 1, 1, 1 };
            ToolRegistry.Dispatch("transform_node", new JObject
            {
                ["name"] = node.Id,
                ["rot_x"] = rot.Length > 0 ? rot[0] : 0f,
                ["rot_y"] = rot.Length > 1 ? rot[1] : 0f,
                ["rot_z"] = rot.Length > 2 ? rot[2] : 0f,
                ["scale"] = scl.Length > 0 ? scl[0] : 1f // uniform-scale only; non-uniform UAG scale approximated by its X component
            });
        }

        // Every tool in this repo returns an anonymous object with at least
        // a `success` bool (see ToolRegistry.Dispatch's own contract). These
        // helpers read that shape via reflection instead of `dynamic`, which
        // needs the Microsoft.CSharp assembly — not guaranteed present in
        // every UNIGINE C# runtime configuration.
        private static bool GetBoolField(object obj, string fieldName)
        {
            if (obj == null) return false;
            var prop = obj.GetType().GetProperty(fieldName);
            if (prop == null) return false;
            var value = prop.GetValue(obj);
            return value is bool b && b;
        }

        private static string GetStringField(object obj, string fieldName)
        {
            if (obj == null) return null;
            var prop = obj.GetType().GetProperty(fieldName);
            return prop?.GetValue(obj)?.ToString();
        }
    }
}
