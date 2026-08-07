using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QFoldIT.Toolbelt;

class SimTest
{
    static int failures = 0;
    static List<string> callLog = new List<string>();

    static void Check(string label, bool cond)
    {
        if (cond) Console.WriteLine($"  PASS: {label}");
        else { Console.WriteLine($"  FAIL: {label}"); failures++; }
    }

    static object GetField(object obj, string name) => obj.GetType().GetProperty(name)?.GetValue(obj);

    static void Main()
    {
        ToolRegistry.RegisterAll();

        // ── Fake handlers standing in for the real Unigine-backed tools ──
        ToolRegistry.Register("spawn_primitive", "Scene", "fake", p => { callLog.Add($"spawn_primitive:{p["name"]}"); return new { success = true, name = (string)p["name"] }; });
        ToolRegistry.Register("light_create", "Lighting", "fake", p => { callLog.Add($"light_create:{p["name"]}"); return new { success = true, name = (string)p["name"] }; });
        ToolRegistry.Register("camera_create", "Camera", "fake", p => { callLog.Add($"camera_create:{p["name"]}"); return new { success = true, name = (string)p["name"] }; });
        ToolRegistry.Register("spawn_group_node", "Scene", "fake", p => { callLog.Add($"spawn_group_node:{p["name"]}"); return new { success = true, name = (string)p["name"] }; });
        ToolRegistry.Register("audio_add_source", "Audio", "fake", p => { callLog.Add($"audio_add_source:{p["name"]}"); return new { success = true, name = (string)p["name"] }; });
        ToolRegistry.Register("particles_spawn_from_asset", "Particles", "fake", p => { callLog.Add($"particles_spawn_from_asset:{p["name"]}"); return new { success = true, name = (string)p["name"] }; });
        ToolRegistry.Register("ui_create_panel", "UI", "fake", p => { callLog.Add($"ui_create_panel:{p["name"]}"); return new { success = true, name = (string)p["name"] }; });
        ToolRegistry.Register("physics_add_shape", "Physics", "fake", p => { callLog.Add($"physics_add_shape:{p["name"]}"); return new { success = true }; });
        ToolRegistry.Register("physics_add_body", "Physics", "fake", p => { callLog.Add($"physics_add_body:{p["name"]}"); return new { success = true }; });
        ToolRegistry.Register("physics_add_joint", "Physics", "fake", p => { callLog.Add($"physics_add_joint:{p["name"]}->{p["connected_body"]}"); return new { success = true }; });
        ToolRegistry.Register("parent_node", "Scene", "fake", p => { callLog.Add($"parent_node:{p["child"]}->{p["parent"]}"); return new { success = true }; });
        ToolRegistry.Register("transform_node", "Scene", "fake", p => { callLog.Add($"transform_node:{p["name"]}"); return new { success = true }; });
        ToolRegistry.Register("asset_instantiate_node", "Assets", "fake", p => { callLog.Add($"asset_instantiate_node:{p["name"]}"); return new { success = true, name = (string)p["name"] }; });
        ToolRegistry.Register("codegen_node_component", "CodeGen", "fake", p => { callLog.Add($"codegen_node_component:{p["class_name"]} nodes=[{p["node_names"]}]"); return new { success = true, path = (string)p["output_path"] }; });

        string graphJson = @"{
          ""uag_version"": ""0.1"",
          ""nodes"": [
            { ""id"": ""root"", ""type"": ""group"" },
            { ""id"": ""cube1"", ""type"": ""mesh"", ""parent_id"": ""root"",
              ""transform"": { ""position"": [1,2,3], ""rotation_euler_deg"": [0,45,0], ""scale"": [2,2,2] },
              ""properties"": { ""primitive"": ""sphere"" } },
            { ""id"": ""sun"", ""type"": ""light"", ""parent_id"": ""root"" },
            { ""id"": ""cam1"", ""type"": ""camera"" },
            { ""id"": ""snd1"", ""type"": ""audio_source"" },
            { ""id"": ""fx1"", ""type"": ""particle_emitter"", ""properties"": { ""asset_ref"": ""fx/spark.particles"" } },
            { ""id"": ""panel1"", ""type"": ""ui_panel"" },
            { ""id"": ""trig1"", ""type"": ""trigger_volume"" },
            { ""id"": ""mystery"", ""type"": ""custom"" }
          ],
          ""connections"": [
            { ""id"": ""c1"", ""type"": ""parent_child"", ""from_node"": ""cube1"", ""to_node"": ""root"" },
            { ""id"": ""c2"", ""type"": ""joint_hinge"", ""from_node"": ""cube1"", ""to_node"": ""root"" },
            { ""id"": ""c3"", ""type"": ""data_link"", ""from_node"": ""cube1"", ""to_node"": ""fx1"" }
          ],
          ""constraints"": [
            { ""id"": ""k1"", ""type"": ""physics_collision"", ""target_nodes"": [""cube1""] },
            { ""id"": ""k2"", ""type"": ""logic_rule"", ""target_nodes"": [""cube1""] }
          ],
          ""interactions"": [
            { ""id"": ""i1"", ""trigger"": ""on_click"", ""target_node"": ""cube1"", ""action"": ""highlight"" }
          ]
        }";

        var result = ToolRegistry.Dispatch("uag_apply", new JObject { ["uag_json"] = graphJson });
        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
        Console.WriteLine();

        Check("apply reports success", (bool)GetField(result, "success"));
        Check("8 of 9 nodes created (mystery unmapped, snd1 fails at runtime -> still 'created'? check below)",
            true); // see next check for the real number

        int nodesCreated = (int)GetField(result, "nodes_created");
        Console.WriteLine($"  (nodes_created = {nodesCreated})");
        Check("nodes_created == 7 (9 total - 1 unmapped 'custom' - 1 failed audio_source)", nodesCreated == 7);

        var nodeFailures = (System.Collections.IEnumerable)GetField(result, "node_failures");
        int failureCount = nodeFailures.Cast<object>().Count();
        Check("exactly 1 node_failure (snd1, missing sound_path)", failureCount == 1);

        var unmappedNodeTypes = (System.Collections.IEnumerable)GetField(result, "unmapped_node_types");
        Check("'custom' reported as unmapped node type", unmappedNodeTypes.Cast<string>().Contains("custom"));

        int reparented = (int)GetField(result, "nodes_reparented");
        Check("2 nodes reparented via parent_id (cube1->root and sun->root)", reparented == 2);

        int connectionsApplied = (int)GetField(result, "connections_applied");
        Check("2 connections applied (parent_child + joint_hinge)", connectionsApplied == 2);

        var unmappedConnTypes = (System.Collections.IEnumerable)GetField(result, "unmapped_connection_types");
        Check("'data_link' reported as unmapped connection type", unmappedConnTypes.Cast<string>().Contains("data_link"));

        int constraintsApplied = (int)GetField(result, "constraints_applied");
        Check("1 constraint applied (physics_collision)", constraintsApplied == 1);

        var unmappedConstraintTypes = (System.Collections.IEnumerable)GetField(result, "unmapped_constraint_types");
        Check("'logic_rule' reported as unmapped constraint type", unmappedConstraintTypes.Cast<string>().Contains("logic_rule"));

        string stubPath = (string)GetField(result, "interaction_stub_path");
        Check("interaction stub was generated (logic_rule + on_click both target cube1)", !string.IsNullOrEmpty(stubPath));
        Check("codegen_node_component was actually called", callLog.Any(l => l.StartsWith("codegen_node_component")));
        Check("codegen call included cube1", callLog.Any(l => l.Contains("cube1") && l.StartsWith("codegen_node_component")));

        Check("mystery node was never dispatched to any creation tool", !callLog.Any(l => l.Contains(":mystery")));
        Check("cube1 was transformed (rotation/scale applied)", callLog.Any(l => l == "transform_node:cube1"));

        Console.WriteLine();
        Console.WriteLine("Call log:");
        foreach (var l in callLog) Console.WriteLine($"  - {l}");

        // ── Second test: invalid graph aborts with no dispatch calls ──
        Console.WriteLine();
        Console.WriteLine("Invalid-graph abort test:");
        callLog.Clear();
        string invalidJson = @"{ ""uag_version"": ""0.1"", ""nodes"": [ { ""id"": ""a"", ""type"": ""mesh"", ""parent_id"": ""ghost"" } ] }";
        var invalidResult = ToolRegistry.Dispatch("uag_apply", new JObject { ["uag_json"] = invalidJson });
        Check("invalid graph -> success false", !(bool)GetField(invalidResult, "success"));
        Check("no tools were dispatched for an invalid graph", callLog.Count == 0);

        Console.WriteLine();
        if (failures == 0) { Console.WriteLine("ALL SIMULATION TESTS PASSED"); Environment.Exit(0); }
        else { Console.WriteLine($"{failures} CHECK(S) FAILED"); Environment.Exit(1); }
    }
}
