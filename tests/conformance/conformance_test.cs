using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QFoldIT.Toolbelt;
using QFoldIT.Toolbelt.Uag;

class ConformanceTest
{
    static int failures = 0;
    static void Check(string label, bool cond)
    {
        if (cond) Console.WriteLine($"  PASS: {label}");
        else { Console.WriteLine($"  FAIL: {label}"); failures++; }
    }

    static void Main()
    {
        string vectorsJson = File.ReadAllText("test_vectors.json");
        var doc = JObject.Parse(vectorsJson);
        var vectors = (JArray)doc["vectors"];

        Console.WriteLine($"Running {vectors.Count} REAL conformance vectors from qfoldit-engine-adapter-spec-v0.1/conformance/test_vectors.json against the actual (unmodified) UagValidator.cs:\n");

        foreach (var v in vectors)
        {
            string id = (string)v["id"];
            string inputJson = v["input"].ToString();
            var graph = UagGraph.Parse(inputJson);
            var result = UagValidator.Validate(graph);

            bool expectedValid = (bool)v["expected"]["valid"];
            Check($"[{id}] valid == {expectedValid}", result.IsValid == expectedValid);

            var expectedCode = (string)v["expected"]["error_code"];
            if (expectedCode != null)
            {
                bool hasCode = result.Errors.Any(e => e.Code == expectedCode);
                Check($"[{id}] errors contain code '{expectedCode}'", hasCode);
            }
        }

        Console.WriteLine();
        Console.WriteLine("Extended coverage (this adapter's own additions, beyond the 3 official vectors):\n");

        {
            var g = UagGraph.Parse(@"{""schema"":""qfoldit.uag/0.1"",""scene"":{""id"":""x""},""nodes"":[{""id"":""a"",""type"":""mesh"",""parent"":""a""}]}");
            var r = UagValidator.Validate(g);
            Check("[self-cycle] invalid", !r.IsValid);
            Check("[self-cycle] HIERARCHY_CYCLE code present", r.Errors.Any(e => e.Code == "HIERARCHY_CYCLE"));
        }
        {
            var g = UagGraph.Parse(@"{""schema"":""qfoldit.uag/9.9"",""scene"":{""id"":""x""},""nodes"":[]}");
            var r = UagValidator.Validate(g);
            Check("[wrong-schema] invalid", !r.IsValid);
            Check("[wrong-schema] INVALID_SCHEMA code present", r.Errors.Any(e => e.Code == "INVALID_SCHEMA"));
        }
        {
            var g = UagGraph.Parse(@"{""schema"":""qfoldit.uag/0.1"",""scene"":{""id"":""x""},""nodes"":[{""id"":""a"",""type"":""quantum_circuit""}]}");
            var r = UagValidator.Validate(g);
            Check("[unknown-node-type] still valid (gap, not error)", r.IsValid);
            Check("[unknown-node-type] reported as unmapped", r.UnmappedNodeTypes.Contains("quantum_circuit"));
        }
        {
            // The exact node type reference/compiler.py emits for every
            // themed pattern — must NOT be a gap.
            var g = UagGraph.Parse(@"{""schema"":""qfoldit.uag/0.1"",""scene"":{""id"":""x""},""nodes"":[{""id"":""subject"",""type"":""scientific_subject/construction""}],""interactions"":[{""id"":""i1"",""type"":""construction"",""target"":""subject""}]}");
            var r = UagValidator.Validate(g);
            Check("[scientific_subject/construction] mapped, no gap", r.UnmappedNodeTypes.Count == 0);
            Check("[interaction type=construction] mapped, no gap", r.UnmappedInteractions.Count == 0);
        }

        Console.WriteLine();
        if (failures == 0) { Console.WriteLine($"ALL CHECKS PASSED ({vectors.Count} official conformance vectors + 4 extended scenarios)"); Environment.Exit(0); }
        else { Console.WriteLine($"{failures} CHECK(S) FAILED"); Environment.Exit(1); }
    }
}
