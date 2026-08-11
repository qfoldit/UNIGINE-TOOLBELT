using System;
using System.IO;
using Newtonsoft.Json.Linq;
using QFoldIT.Toolbelt.Uag;

class ParseTestV2
{
    static int failures = 0;
    static void Check(string label, bool cond)
    {
        if (cond) Console.WriteLine($"  PASS: {label}");
        else { Console.WriteLine($"  FAIL: {label}"); failures++; }
    }

    static void Main()
    {
        Console.WriteLine("=== Parsing spec/examples/protein-folding.uag.json (hand-authored example) ===");
        {
            string json = File.ReadAllText("protein-folding.uag.json");
            var g = UagGraph.Parse(json);

            Check("schema == qfoldit.uag/0.1", g.Schema == "qfoldit.uag/0.1");
            Check("scene.id == protein-folding-demo", g.Scene.Id == "protein-folding-demo");
            Check("2 nodes parsed", g.Nodes.Count == 2);
            Check("node[0].id == protein", g.Nodes[0].Id == "protein");
            Check("node[0].type == molecular_structure", g.Nodes[0].Type == "molecular_structure");
            Check("node[1].parent == protein", g.Nodes[1].Parent == "protein");
            Check("node[1].type == interaction_zone", g.Nodes[1].Type == "interaction_zone");
            Check("1 interaction parsed", g.Interactions.Count == 1);
            Check("interaction.target == binding-site", g.Interactions[0].Target == "binding-site");
            Check("interaction.type == selection", g.Interactions[0].Type == "selection");
            Check("1 binding parsed", g.Bindings.Count == 1);
            Check("binding.source == scientific-state://energy-trace", g.Bindings[0].Source == "scientific-state://energy-trace");
            Check("binding.target == protein", g.Bindings[0].Target == "protein");
            Check("0 constraints (empty array present)", g.Constraints.Count == 0);
        }

        Console.WriteLine();
        Console.WriteLine("=== Parsing REAL reference/compiler.py output (compiler_output_unity.json) ===");
        {
            string json = File.ReadAllText("compiler_output_unigine.json");
            var g = UagGraph.Parse(json);

            Check("schema == qfoldit.uag/0.1", g.Schema == "qfoldit.uag/0.1");
            Check("scene.id == protein_folding_construction-unigine", g.Scene.Id == "protein_folding_construction-unigine");
            Check("scene.metadata.mechanic == construction", (string)g.Scene.Metadata["mechanic"] == "construction");
            Check("1 node parsed", g.Nodes.Count == 1);
            Check("node.id == subject", g.Nodes[0].Id == "subject");
            Check("node.type == scientific_subject/construction", g.Nodes[0].Type == "scientific_subject/construction");
            Check("node.properties.source is the scientific-state URI",
                (string)g.Nodes[0].Properties["source"] == "scientific-state://protein_design_mcp/protein_folding_construction");
            Check("node.parent is null (root node)", g.Nodes[0].Parent == null);
            Check("1 interaction parsed", g.Interactions.Count == 1);
            Check("interaction.type == construction (the mechanic name)", g.Interactions[0].Type == "construction");
            Check("interaction.target == subject", g.Interactions[0].Target == "subject");
            Check("1 binding parsed", g.Bindings.Count == 1);
            Check("binding.source == binding.target's node source URI", g.Bindings[0].Source == (string)g.Nodes[0].Properties["source"]);
            Check("top-level metadata.presentation_theme == molecular_construction_lab",
                (string)g.Metadata["presentation_theme"] == "molecular_construction_lab");
        }

        Console.WriteLine();
        Console.WriteLine("=== Round-trip sanity: node.Transform helpers on a minimal doc with no transform block ===");
        {
            var g = UagGraph.Parse(@"{""schema"":""qfoldit.uag/0.1"",""scene"":{""id"":""x""},""nodes"":[{""id"":""a"",""type"":""group""}]}");
            Check("default Position is [0,0,0]", g.Nodes[0].Position[0] == 0 && g.Nodes[0].Position[1] == 0 && g.Nodes[0].Position[2] == 0);
            Check("default Scale is [1,1,1]", g.Nodes[0].Scale[0] == 1 && g.Nodes[0].Scale[1] == 1 && g.Nodes[0].Scale[2] == 1);
        }
        {
            var g = UagGraph.Parse(@"{""schema"":""qfoldit.uag/0.1"",""scene"":{""id"":""x""},""nodes"":[
                {""id"":""a"",""type"":""mesh"",""transform"":{""position"":[1.5,-2,3],""scale"":[2,2,2]}}]}");
            Check("explicit position parses correctly", g.Nodes[0].Position[0] == 1.5f && g.Nodes[0].Position[1] == -2f && g.Nodes[0].Position[2] == 3f);
            Check("explicit scale parses correctly", g.Nodes[0].Scale[0] == 2f);
        }

        Console.WriteLine();
        if (failures == 0) { Console.WriteLine("ALL V2 SCHEMA PARSE TESTS PASSED"); Environment.Exit(0); }
        else { Console.WriteLine($"{failures} CHECK(S) FAILED"); Environment.Exit(1); }
    }
}
