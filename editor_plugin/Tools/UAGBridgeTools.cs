// qFoldIT Toolbelt for UNIGINE 2 — UAGBridgeTools.cs
// Category: UAGBridge
//
// Adapted to qfoldit-engine-adapter-spec-v0.1's formal contract — see
// UNITY-TOOLBELT's version of this file for the full rationale (identical
// schema, identical error-code/execution-report shape). Architectural
// note carried over from Phase 1: this file dispatches through
// ToolRegistry.Dispatch(name, JObject) for every tool call, the exact
// same path an external MCP client uses — this adapter has no special
// access any other caller lacks.

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using QFoldIT.Toolbelt.Uag;

namespace QFoldIT.Toolbelt
{
    public static class UAGBridgeTools
    {
        public const string AdapterId = "qfoldit-unigine-toolbelt";
        public const string AdapterVersion = "0.2.0";
        public const string EngineId = "unigine2";

        public static void Register()
        {
            ToolRegistry.Register("uag_validate", "UAGBridge",
                "Validates a UAG document against this engine's adapter: schema id, duplicate/dangling references, hierarchy cycles, and which node/constraint/interaction types this adapter can and cannot realize. Makes no changes to the world. Errors are {code, message} objects matching qfoldit-engine-adapter-spec-v0.1's conformance vectors.",
                UagValidateTool);

            ToolRegistry.Register("uag_apply", "UAGBridge",
                "Realizes a validated UAG document in the loaded world by dispatching to this toolbelt's own registered tools. Returns a structured execution report (status/created/updated/skipped/gaps/warnings/errors) matching schemas/execution-report.schema.json. Aborts with no world changes if validation fails.",
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
                errors = result.Errors.Select(e => new { code = e.Code, message = e.Message }),
                unmapped_node_types = result.UnmappedNodeTypes,
                unmapped_constraint_types = result.UnmappedConstraintTypes,
                unmapped_interactions = result.UnmappedInteractions.Select(i => new { i.Id, i.Type, i.Target }),
                node_count = graph.Nodes.Count,
                constraint_count = graph.Constraints.Count,
                interaction_count = graph.Interactions.Count,
                binding_count = graph.Bindings.Count
            };
        }

        private static object UagApply(JObject p)
        {
            string uagJson = (string)p["uag_json"];
            UagGraph graph;
            try { graph = UagGraph.Parse(uagJson); }
            catch (System.Exception ex)
            {
                return Report("failed", errors: new object[] { new { code = "PARSE_ERROR", message = ex.Message } });
            }

            var validation = UagValidator.Validate(graph);
            if (!validation.IsValid)
                return Report("failed", errors: validation.Errors.Select(e => (object)new { code = e.Code, message = e.Message }), provenance: Provenance(graph));

            var created = new List<string>();
            var updated = new List<string>();
            var skipped = new List<string>();
            var gaps = new List<object>();
            var warnings = new List<object>();
            var errors = new List<object>();
            var unrealizedNodeIds = new HashSet<string>();

            // ── Pass 1: create every node ──
            foreach (var node in graph.Nodes)
            {
                if (!UagValidator.IsMappedNodeType(node.Type))
                {
                    unrealizedNodeIds.Add(node.Id);
                    skipped.Add(node.Id);
                    gaps.Add(new { element = "node", id = node.Id, type = node.Type, reason = "unmapped node type" });
                    continue;
                }

                var createResult = CreateNode(node);
                if (GetBoolField(createResult, "success"))
                {
                    created.Add(node.Id);
                    ApplyTransform(node);
                }
                else
                {
                    unrealizedNodeIds.Add(node.Id);
                    skipped.Add(node.Id);
                    errors.Add(new { code = "NODE_CREATE_FAILED", node_id = node.Id, type = node.Type, message = GetStringField(createResult, "error") ?? "unknown error" });
                }
            }

            // ── Pass 2: parent hierarchy ──
            foreach (var node in graph.Nodes)
            {
                if (string.IsNullOrEmpty(node.Parent) || unrealizedNodeIds.Contains(node.Id) || unrealizedNodeIds.Contains(node.Parent))
                    continue;
                ToolRegistry.Dispatch("parent_node", new JObject { ["child"] = node.Id, ["parent"] = node.Parent });
                if (!updated.Contains(node.Id)) updated.Add(node.Id);
            }

            // ── Pass 3: constraints ──
            foreach (var constraint in graph.Constraints)
            {
                var validTargets = constraint.TargetNodes.Where(t => !unrealizedNodeIds.Contains(t)).ToList();
                switch (constraint.Type)
                {
                    case "physics_collision":
                    case "physics.collision":
                        foreach (var target in validTargets)
                        {
                            string shape = (string)constraint.Properties["shape"] ?? "box";
                            ToolRegistry.Dispatch("physics_add_shape", new JObject { ["name"] = target, ["shape"] = shape, ["is_trigger"] = false });
                            ToolRegistry.Dispatch("physics_add_body", new JObject { ["name"] = target, ["mass"] = 1f });
                            if (!updated.Contains(target)) updated.Add(target);
                        }
                        break;
                    case "physics.joint":
                        if (validTargets.Count >= 1)
                        {
                            string jointType = (string)constraint.Properties["joint_type"] ?? "fixed";
                            string connected = validTargets.Count >= 2 ? validTargets[1] : null;
                            ToolRegistry.Dispatch("physics_add_joint", new JObject { ["name"] = validTargets[0], ["joint_type"] = jointType, ["connected_body"] = connected });
                            if (!updated.Contains(validTargets[0])) updated.Add(validTargets[0]);
                        }
                        break;
                    default:
                        gaps.Add(new { element = "constraint", id = constraint.Id, type = constraint.Type, reason = "unmapped constraint type" });
                        break;
                }
            }

            // ── Pass 4: interactions — REAL realization via InteractionTools ──
            foreach (var interaction in graph.Interactions)
            {
                if (string.IsNullOrEmpty(interaction.Target) || unrealizedNodeIds.Contains(interaction.Target))
                {
                    gaps.Add(new { element = "interaction", id = interaction.Id, type = interaction.Type, reason = "target node was not realized" });
                    continue;
                }
                if (!UAGBridgeMechanics.MappedInteractionTypes.Contains(interaction.Type))
                {
                    gaps.Add(new { element = "interaction", id = interaction.Id, type = interaction.Type, reason = "unmapped interaction type" });
                    continue;
                }

                ToolRegistry.Dispatch("interaction_create", new JObject { ["name"] = interaction.Target, ["interaction_type"] = interaction.Type });
                if (!updated.Contains(interaction.Target)) updated.Add(interaction.Target);

                if (UAGBridgeMechanics.GameplayMechanics.Contains(interaction.Type))
                {
                    warnings.Add(new
                    {
                        code = "INTERACTABLE_REGISTERED_NOT_GAMEPLAY_COMPLETE",
                        interaction_id = interaction.Id,
                        message = $"'{interaction.Target}' now has a real physics shape and a persisted interaction_type='{interaction.Type}' registry entry (interaction_get), but this does NOT wire a live click-to-callback the way the Unity adapter's QFoldITInteractable does — see InteractionTools.cs's header for why, and full '{interaction.Type}' gameplay logic remains out of scope for a generic adapter regardless."
                    });
                }
            }

            // ── Pass 5: bindings — REAL realization via ScientificVisualizationTools ──
            foreach (var binding in graph.Bindings)
            {
                if (string.IsNullOrEmpty(binding.Target) || unrealizedNodeIds.Contains(binding.Target))
                {
                    gaps.Add(new { element = "binding", id = binding.Id, reason = "target node was not realized" });
                    continue;
                }
                ToolRegistry.Dispatch("scientific_binding_create", new JObject { ["name"] = binding.Target, ["binding_id"] = binding.Id, ["source_uri"] = binding.Source });
                if (!updated.Contains(binding.Target)) updated.Add(binding.Target);
            }

            string status = errors.Count > 0 && created.Count == 0 ? "failed"
                : (gaps.Count > 0 || warnings.Count > 0 || errors.Count > 0) ? "partial"
                : "success";

            return Report(status, created, updated, skipped, gaps, warnings, errors, Provenance(graph));
        }

        // ── Node type -> existing-tool dispatch ────────────────────────
        private static object CreateNode(UagNode node)
        {
            var pos = node.Position;
            double x = pos[0], y = pos[1], z = pos[2];

            if (node.Type.StartsWith("scientific_subject/"))
            {
                string mechanic = node.Type.Substring("scientific_subject/".Length);
                return ToolRegistry.Dispatch("scientific_visualization_create", new JObject
                {
                    ["name"] = node.Id, ["mechanic"] = mechanic, ["x"] = x, ["y"] = y, ["z"] = z,
                    ["source_uri"] = (string)node.Properties["source"] ?? ""
                });
            }

            switch (node.Type)
            {
                case "mesh":
                    var meshPath = (string)node.Properties["mesh_ref"];
                    if (!string.IsNullOrEmpty(meshPath))
                        return ToolRegistry.Dispatch("asset_instantiate_node", new JObject { ["node_path"] = meshPath, ["x"] = x, ["y"] = y, ["z"] = z, ["name"] = node.Id });
                    return ToolRegistry.Dispatch("spawn_primitive", new JObject { ["type"] = (string)node.Properties["primitive"] ?? "box", ["name"] = node.Id, ["x"] = x, ["y"] = y, ["z"] = z });

                case "molecular_structure":
                    return ToolRegistry.Dispatch("scientific_visualization_create", new JObject
                    {
                        ["name"] = node.Id, ["mechanic"] = "", ["x"] = x, ["y"] = y, ["z"] = z,
                        ["source_uri"] = (string)node.Properties["source"] ?? ""
                    });

                case "interaction_zone":
                    {
                        var r1 = ToolRegistry.Dispatch("spawn_primitive", new JObject { ["type"] = "box", ["name"] = node.Id, ["x"] = x, ["y"] = y, ["z"] = z });
                        if (!GetBoolField(r1, "success")) return r1;
                        ToolRegistry.Dispatch("physics_add_shape", new JObject { ["name"] = node.Id, ["shape"] = "box", ["is_trigger"] = true });
                        ToolRegistry.Dispatch("interaction_create", new JObject { ["name"] = node.Id, ["interaction_type"] = (string)node.Properties["interaction"] ?? "selection" });
                        return r1;
                    }

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
                        var soundPath = (string)node.Properties["sound_path"];
                        if (string.IsNullOrEmpty(soundPath))
                            return new { success = false, error = "audio_source node has no properties.sound_path — UNIGINE has no default sound asset (nothing was created for this node)." };
                        ToolRegistry.Dispatch("spawn_group_node", new JObject { ["name"] = node.Id, ["x"] = x, ["y"] = y, ["z"] = z });
                        return ToolRegistry.Dispatch("audio_add_source", new JObject { ["name"] = node.Id, ["sound_path"] = soundPath, ["x"] = x, ["y"] = y, ["z"] = z, ["loop"] = (bool?)node.Properties["loop"] ?? false });
                    }

                case "particle_emitter":
                    {
                        var assetPath = (string)node.Properties["asset_ref"];
                        if (string.IsNullOrEmpty(assetPath))
                            return new { success = false, error = "particle_emitter node has no properties.asset_ref — UNIGINE has no built-in generic particle preset." };
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
            var rot = node.RotationEulerDeg;
            var scl = node.Scale;
            ToolRegistry.Dispatch("transform_node", new JObject
            {
                ["name"] = node.Id,
                ["rot_x"] = rot[0], ["rot_y"] = rot[1], ["rot_z"] = rot[2],
                ["scale_x"] = scl[0], ["scale_y"] = scl[1], ["scale_z"] = scl[2]
            });
        }

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

        private static object Provenance(UagGraph graph) => new
        {
            schema = graph.Schema,
            scene_id = graph.Scene?.Id,
            compiler = graph.Metadata != null && graph.Metadata["compiler"] != null ? (string)graph.Metadata["compiler"] : null
        };

        private static object Report(string status,
            IEnumerable<string> created = null, IEnumerable<string> updated = null, IEnumerable<string> skipped = null,
            IEnumerable<object> gaps = null, IEnumerable<object> warnings = null, IEnumerable<object> errors = null,
            object provenance = null) => new
        {
            success = status != "failed",
            status,
            engine = EngineId,
            adapter = AdapterId,
            adapter_version = AdapterVersion,
            created = created ?? System.Array.Empty<string>(),
            updated = updated ?? System.Array.Empty<string>(),
            skipped = skipped ?? System.Array.Empty<string>(),
            gaps = gaps ?? System.Array.Empty<object>(),
            warnings = warnings ?? System.Array.Empty<object>(),
            errors = errors ?? System.Array.Empty<object>(),
            provenance
        };
    }
}
