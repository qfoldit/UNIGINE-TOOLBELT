// qFoldIT Toolbelt for UNIGINE 2 — ScientificVisualizationTools.cs
// Category: ScientificVisualization
//
// The concrete "adapter"-level realization behind the scientific.visualization
// capability. Maps a UAG "scientific_subject/<mechanic>" node — the exact
// shape qfoldit-scientific-gameplay-framework-v0.1's reference/compiler.py
// emits for every themed pattern — to a real, visible, mechanic-differentiated
// primitive, plus a real, persisted bindings[] realization.
//
// Honest scope, explicitly NOT claimed: unlike UNITY-TOOLBELT's version of
// this file (which adds an optional floating world-space text label via
// a WorldSpace Canvas), this file does not attempt a 3D text label —
// UNIGINE's 3D text/billboard API needs SDK-version verification this
// adapter doesn't have access to (see UnigineCompat.cs's header caveat).
// Shape + material differentiation and a real, queryable binding registry
// are delivered; a floating label is not, rather than guessed at.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QFoldIT.Toolbelt
{
    public static class ScientificVisualizationTools
    {
        private static (string primitive, string preset) SchemeFor(string mechanic) => mechanic switch
        {
            "construction" => ("box", "matte"),
            "optimization" => ("sphere", "neon"),
            "pattern_matching" => ("cylinder", "chrome"),
            "rhythm" => ("sphere", "toxic"),
            "survival_defense" => ("capsule", "rubber"),
            "racing_tuning" => ("cylinder", "gold"),
            "spatial_puzzle" => ("box", "ice"),
            "portal_exploration" => ("sphere", "hologram"),
            "investigation_annotation" => ("capsule", "glass"),
            "competitive_microtasks" => ("box", "emissive"),
            _ => ("sphere", "matte"),
        };

        private class BindingRegistry
        {
            public Dictionary<string, BindingEntry> NodeBindings { get; set; } = new Dictionary<string, BindingEntry>();
        }

        private class BindingEntry
        {
            public string BindingId { get; set; }
            public string SourceUri { get; set; }
        }

        private static string RegistryPath => Path.Combine(UnigineCompat.SavedDataDir, "scientific_bindings.json");

        private static BindingRegistry LoadRegistry()
        {
            if (!File.Exists(RegistryPath)) return new BindingRegistry();
            return JsonConvert.DeserializeObject<BindingRegistry>(File.ReadAllText(RegistryPath)) ?? new BindingRegistry();
        }

        private static void SaveRegistry(BindingRegistry reg)
        {
            Directory.CreateDirectory(UnigineCompat.SavedDataDir);
            File.WriteAllText(RegistryPath, JsonConvert.SerializeObject(reg, Formatting.Indented));
        }

        public static void Register()
        {
            ToolRegistry.Register("scientific_visualization_create", "ScientificVisualization",
                "Creates a real, mechanic-differentiated visualization anchor for a UAG 'scientific_subject/<mechanic>' node: a shaped, colored primitive, plus a persisted binding registry entry if a source URI is given.",
                Create);

            ToolRegistry.Register("scientific_binding_create", "ScientificVisualization",
                "Records a UAG bindings[] entry (node -> scientific-state:// URI) in a persisted, queryable registry instead of accepting-and-discarding it.",
                Bind);

            ToolRegistry.Register("scientific_binding_get", "ScientificVisualization",
                "Reads back the binding recorded for a node by scientific_binding_create, if any.",
                Get);
        }

        private static object Create(JObject p)
        {
            string name = (string)p["name"];
            string mechanic = (string)p["mechanic"] ?? "";
            double x = (double?)p["x"] ?? 0, y = (double?)p["y"] ?? 0, z = (double?)p["z"] ?? 0;
            string sourceUri = (string)p["source_uri"];

            if (string.IsNullOrEmpty(name)) return new { success = false, error = "name is required." };

            var (primitive, preset) = SchemeFor(mechanic);

            var spawnResult = ToolRegistry.Dispatch("spawn_primitive", new JObject { ["type"] = primitive, ["name"] = name, ["x"] = x, ["y"] = y, ["z"] = z });
            ToolRegistry.Dispatch("material_apply_preset", new JObject { ["name"] = name, ["preset"] = preset });

            bool bound = false;
            if (!string.IsNullOrEmpty(sourceUri))
            {
                Bind(new JObject { ["name"] = name, ["binding_id"] = $"{name}-binding", ["source_uri"] = sourceUri });
                bound = true;
            }

            return new { success = true, name, mechanic, primitive, material_preset = preset, bound };
        }

        private static object Bind(JObject p)
        {
            string name = (string)p["name"];
            string bindingId = (string)p["binding_id"];
            string sourceUri = (string)p["source_uri"];
            if (string.IsNullOrEmpty(name)) return new { success = false, error = "name is required." };
            if (string.IsNullOrEmpty(sourceUri)) return new { success = false, error = "source_uri is required." };

            var reg = LoadRegistry();
            reg.NodeBindings[name] = new BindingEntry { BindingId = bindingId, SourceUri = sourceUri };
            SaveRegistry(reg);

            return new { success = true, name, binding_id = bindingId, source_uri = sourceUri };
        }

        private static object Get(JObject p)
        {
            string name = (string)p["name"];
            var reg = LoadRegistry();
            if (!reg.NodeBindings.TryGetValue(name, out var entry))
                return new { success = false, error = $"No binding recorded for '{name}'." };
            return new { success = true, name, binding_id = entry.BindingId, source_uri = entry.SourceUri };
        }
    }
}
