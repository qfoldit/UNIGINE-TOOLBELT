using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QFoldIT.Toolbelt;

class SimTestV2
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
        ToolRegistry.Register("interaction_create", "Interaction", "fake", p => { callLog.Add($"interaction_create:{p["name"]}:{p["interaction_type"]}"); return new { success = true }; });
        ToolRegistry.Register("scientific_visualization_create", "ScientificVisualization", "fake", p => { callLog.Add($"scientific_visualization_create:{p["name"]}:{p["mechanic"]}"); return new { success = true, name = (string)p["name"] }; });
        ToolRegistry.Register("scientific_binding_create", "ScientificVisualization", "fake", p => { callLog.Add($"scientific_binding_create:{p["name"]}:{p["source_uri"]}"); return new { success = true }; });

        // A graph exercising: scientific_subject/* node + matching mechanic
        // interaction (the exact shape reference/compiler.py emits), a
        // legacy molecular_structure + interaction_zone pair (the spec's
        // own hand-authored example shape), an audio_source missing its
        // required sound_path (should fail cleanly, no orphan), an
        // unmapped node type, an unmapped constraint type, a joint
        // connection expressed as a physics.joint constraint, and a
        // binding on the scientific subject.
        string graphJson = @"{
          ""schema"": ""qfoldit.uag/0.1"",
          ""scene"": { ""id"": ""sim-scene"" },
          ""nodes"": [
            { ""id"": ""subject"", ""type"": ""scientific_subject/construction"",
              ""properties"": { ""source"": ""scientific-state://protein_design_mcp/x"" } },
            { ""id"": ""protein"", ""type"": ""molecular_structure"" },
            { ""id"": ""zone"", ""type"": ""interaction_zone"", ""parent"": ""protein"",
              ""properties"": { ""interaction"": ""selection"" } },
            { ""id"": ""snd1"", ""type"": ""audio_source"" },
            { ""id"": ""a"", ""type"": ""mesh"" },
            { ""id"": ""b"", ""type"": ""mesh"" },
            { ""id"": ""mystery"", ""type"": ""quantum_circuit"" }
          ],
          ""constraints"": [
            { ""id"": ""k1"", ""type"": ""physics.joint"", ""target_nodes"": [""a"", ""b""], ""properties"": { ""joint_type"": ""hinge"" } },
            { ""id"": ""k2"", ""type"": ""logic_rule"", ""target_nodes"": [""a""] }
          ],
          ""interactions"": [
            { ""id"": ""i1"", ""type"": ""construction"", ""target"": ""subject"" }
          ],
          ""bindings"": [
            { ""id"": ""bnd1"", ""source"": ""scientific-state://protein_design_mcp/x"", ""target"": ""subject"" }
          ]
        }";

        var result = ToolRegistry.Dispatch("uag_apply", new JObject { ["uag_json"] = graphJson });
        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
        Console.WriteLine();

        string status = (string)GetField(result, "status");
        Check("status == partial (mystery node + logic_rule constraint are gaps)", status == "partial");

        var created = ((System.Collections.IEnumerable)GetField(result, "created")).Cast<string>().ToList();
        Check("5 of 7 nodes created (mystery unmapped, snd1 fails)", created.Count == 5);
        Check("subject created", created.Contains("subject"));
        Check("mystery NOT created", !created.Contains("mystery"));
        Check("snd1 NOT created (missing sound_path)", !created.Contains("snd1"));

        var skipped = ((System.Collections.IEnumerable)GetField(result, "skipped")).Cast<string>().ToList();
        Check("mystery + snd1 both skipped", skipped.Contains("mystery") && skipped.Contains("snd1"));

        var gaps = ((System.Collections.IEnumerable)GetField(result, "gaps")).Cast<object>().ToList();
        Check("gap recorded for mystery (unmapped node type)", gaps.Any(g => (string)GetField(g, "id") == "mystery"));
        Check("gap recorded for logic_rule constraint", gaps.Any(g => (string)GetField(g, "id") == "k2"));

        Check("scientific_visualization_create dispatched for 'subject' with mechanic=construction",
            callLog.Any(l => l == "scientific_visualization_create:subject:construction"));
        Check("scientific_visualization_create dispatched for 'protein' (molecular_structure, empty mechanic)",
            callLog.Any(l => l.StartsWith("scientific_visualization_create:protein:")));
        Check("interaction_create dispatched for 'subject' with type=construction (the mechanic name)",
            callLog.Any(l => l == "interaction_create:subject:construction"));
        Check("interaction_create dispatched for 'zone' with type=selection (interaction_zone's properties.interaction)",
            callLog.Any(l => l == "interaction_create:zone:selection"));
        Check("scientific_binding_create dispatched for 'subject'",
            callLog.Any(l => l.StartsWith("scientific_binding_create:subject:")));
        Check("physics_add_joint dispatched for the physics.joint constraint (a->b, hinge)",
            callLog.Any(l => l == "physics_add_joint:a->b"));
        Check("snd1 was never dispatched to any tool (no orphan)", !callLog.Any(l => l.Contains(":snd1")));
        Check("mystery was never dispatched to any tool", !callLog.Any(l => l.Contains(":mystery")));

        var warnings = ((System.Collections.IEnumerable)GetField(result, "warnings")).Cast<object>().ToList();
        Check("a warning was emitted for the gameplay-mechanic interaction (honesty about scope)", warnings.Count == 1);

        Console.WriteLine();
        Console.WriteLine("Call log:");
        foreach (var l in callLog) Console.WriteLine($"  - {l}");

        // ── Invalid graph: wrong schema aborts with zero dispatches ──
        Console.WriteLine();
        Console.WriteLine("Invalid-graph abort test (wrong schema):");
        callLog.Clear();
        string invalidJson = @"{ ""schema"": ""qfoldit.uag/9.9"", ""scene"": {""id"":""x""}, ""nodes"": [] }";
        var invalidResult = ToolRegistry.Dispatch("uag_apply", new JObject { ["uag_json"] = invalidJson });
        Check("invalid graph -> status failed", (string)GetField(invalidResult, "status") == "failed");
        Check("errors contain INVALID_SCHEMA", ((System.Collections.IEnumerable)GetField(invalidResult, "errors")).Cast<object>()
            .Any(e => (string)GetField(e, "code") == "INVALID_SCHEMA"));
        Check("no tools were dispatched for an invalid graph", callLog.Count == 0);

        Console.WriteLine();
        if (failures == 0) { Console.WriteLine("ALL V2 SIMULATION TESTS PASSED"); Environment.Exit(0); }
        else { Console.WriteLine($"{failures} CHECK(S) FAILED"); Environment.Exit(1); }
    }
}
