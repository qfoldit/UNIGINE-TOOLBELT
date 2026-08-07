using System;
using System.Collections.Generic;
using System.Linq;
using QFoldIT.Toolbelt.Uag;
using QFoldIT.Toolbelt;

class Program
{
    static int failures = 0;

    static void Check(string label, bool condition)
    {
        if (condition) { Console.WriteLine($"  PASS: {label}"); }
        else { Console.WriteLine($"  FAIL: {label}"); failures++; }
    }

    static UagNode Node(string id, string type, string parentId = null) =>
        new UagNode { Id = id, Type = type, ParentId = parentId };

    static void Main()
    {
        // ── Test 1: valid, fully-mapped graph ──
        Console.WriteLine("Test 1: valid graph, all types mapped");
        {
            var g = new UagGraph
            {
                Nodes = new List<UagNode> { Node("root", "group"), Node("cube1", "mesh", "root"), Node("sun", "light", "root") },
                Connections = new List<UagConnection> { new UagConnection { Id = "c1", Type = "parent_child", FromNode = "cube1", ToNode = "root" } },
                Constraints = new List<UagConstraint> { new UagConstraint { Id = "k1", Type = "physics_collision", TargetNodes = new List<string> { "cube1" } } },
            };
            var r = UagValidator.Validate(g);
            Check("is valid", r.IsValid);
            Check("no errors", r.Errors.Count == 0);
            Check("no unmapped node types", r.UnmappedNodeTypes.Count == 0);
            Check("no unmapped constraint types", r.UnmappedConstraintTypes.Count == 0);
        }

        // ── Test 2: dangling parent_id ──
        Console.WriteLine("Test 2: dangling parent_id reference");
        {
            var g = new UagGraph { Nodes = new List<UagNode> { Node("a", "mesh", "ghost") } };
            var r = UagValidator.Validate(g);
            Check("invalid", !r.IsValid);
            Check("error mentions ghost", r.Errors.Any(e => e.Contains("ghost")));
        }

        // ── Test 3: dangling connection reference ──
        Console.WriteLine("Test 3: dangling connection from_node/to_node");
        {
            var g = new UagGraph
            {
                Nodes = new List<UagNode> { Node("a", "mesh") },
                Connections = new List<UagConnection> { new UagConnection { Id = "c1", Type = "parent_child", FromNode = "a", ToNode = "missing" } }
            };
            var r = UagValidator.Validate(g);
            Check("invalid", !r.IsValid);
            Check("error mentions missing", r.Errors.Any(e => e.Contains("missing")));
        }

        // ── Test 4: direct cycle (A parent B, B parent A) ──
        Console.WriteLine("Test 4: two-node cycle");
        {
            var g = new UagGraph { Nodes = new List<UagNode> { Node("a", "mesh", "b"), Node("b", "mesh", "a") } };
            var r = UagValidator.Validate(g);
            Check("invalid", !r.IsValid);
            Check("error mentions cycle", r.Errors.Any(e => e.Contains("Cycle")));
        }

        // ── Test 5: self-referential parent (A parent A) ──
        Console.WriteLine("Test 5: self-referential parent");
        {
            var g = new UagGraph { Nodes = new List<UagNode> { Node("a", "mesh", "a") } };
            var r = UagValidator.Validate(g);
            Check("invalid", !r.IsValid);
            Check("error mentions cycle", r.Errors.Any(e => e.Contains("Cycle")));
        }

        // ── Test 6: three-node cycle (A->B->C->A) is caught, not just direct cycles ──
        Console.WriteLine("Test 6: three-node cycle");
        {
            var g = new UagGraph { Nodes = new List<UagNode> { Node("a", "mesh", "b"), Node("b", "mesh", "c"), Node("c", "mesh", "a") } };
            var r = UagValidator.Validate(g);
            Check("invalid", !r.IsValid);
            Check("error mentions cycle", r.Errors.Any(e => e.Contains("Cycle")));
        }

        // ── Test 7: long valid chain is NOT flagged as a cycle (no false positive) ──
        Console.WriteLine("Test 7: long valid non-cyclic chain");
        {
            var g = new UagGraph { Nodes = new List<UagNode> { Node("a", "mesh"), Node("b", "mesh", "a"), Node("c", "mesh", "b"), Node("d", "mesh", "c"), Node("e", "mesh", "d") } };
            var r = UagValidator.Validate(g);
            Check("valid (no false-positive cycle)", r.IsValid);
        }

        // ── Test 8: unmapped node/constraint types are reported as gaps, not errors ──
        Console.WriteLine("Test 8: unmapped types are gaps, graph stays valid");
        {
            var g = new UagGraph
            {
                Nodes = new List<UagNode> { Node("a", "custom") },
                Constraints = new List<UagConstraint> { new UagConstraint { Id = "k1", Type = "logic_rule", TargetNodes = new List<string> { "a" } } },
                Interactions = new List<UagInteraction> { new UagInteraction { Id = "i1", Trigger = "on_click", TargetNode = "a", Action = "toggle_light" } }
            };
            var r = UagValidator.Validate(g);
            Check("still valid (gaps aren't errors)", r.IsValid);
            Check("custom flagged unmapped", r.UnmappedNodeTypes.Contains("custom"));
            Check("logic_rule flagged unmapped", r.UnmappedConstraintTypes.Contains("logic_rule"));
            Check("interaction always surfaced", r.UnmappedInteractions.Count == 1);
        }

        // ── Test 9: duplicate node ids ──
        Console.WriteLine("Test 9: duplicate node ids");
        {
            var g = new UagGraph { Nodes = new List<UagNode> { Node("a", "mesh"), Node("a", "light") } };
            var r = UagValidator.Validate(g);
            Check("invalid", !r.IsValid);
            Check("error mentions duplicate", r.Errors.Any(e => e.Contains("Duplicate")));
        }

        // ── Test 10: dangling constraint / interaction targets ──
        Console.WriteLine("Test 10: dangling constraint and interaction targets");
        {
            var g = new UagGraph
            {
                Nodes = new List<UagNode> { Node("a", "mesh") },
                Constraints = new List<UagConstraint> { new UagConstraint { Id = "k1", Type = "physics_collision", TargetNodes = new List<string> { "a", "ghost" } } },
                Interactions = new List<UagInteraction> { new UagInteraction { Id = "i1", Trigger = "on_grab", TargetNode = "ghost2", Action = "x" } }
            };
            var r = UagValidator.Validate(g);
            Check("invalid", !r.IsValid);
            Check("error mentions ghost constraint target", r.Errors.Any(e => e.Contains("ghost") && e.Contains("Constraint")));
            Check("error mentions ghost2 interaction target", r.Errors.Any(e => e.Contains("ghost2")));
        }

        Console.WriteLine();
        if (failures == 0) { Console.WriteLine("ALL TESTS PASSED"); Environment.Exit(0); }
        else { Console.WriteLine($"{failures} CHECK(S) FAILED"); Environment.Exit(1); }
    }
}
