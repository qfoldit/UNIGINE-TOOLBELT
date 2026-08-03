// qFoldIT Toolbelt for UNIGINE 2 — MaterialTools.cs
// Category: Materials

using System.Linq;
using Newtonsoft.Json.Linq;
using Unigine;

namespace QFoldIT.Toolbelt
{
    public static class MaterialTools
    {
        private struct Preset { public float R, G, B, A; public bool Emissive; public float EmissiveStrength; }

        private static readonly System.Collections.Generic.Dictionary<string, Preset> Presets =
            new System.Collections.Generic.Dictionary<string, Preset>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "chrome",   new Preset { R = 0.9f, G = 0.9f, B = 0.95f, A = 1f } },
            { "neon",     new Preset { R = 0.1f, G = 0.9f, B = 1f,    A = 1f, Emissive = true, EmissiveStrength = 3f } },
            { "hologram", new Preset { R = 0.3f, G = 0.9f, B = 1f,    A = 0.35f, Emissive = true, EmissiveStrength = 1.5f } },
            { "lava",     new Preset { R = 1f,   G = 0.25f,B = 0f,    A = 1f, Emissive = true, EmissiveStrength = 2f } },
            { "ice",      new Preset { R = 0.7f, G = 0.9f, B = 1f,    A = 0.6f } },
            { "glass",    new Preset { R = 1f,   G = 1f,   B = 1f,    A = 0.15f } },
            { "emissive", new Preset { R = 1f,   G = 1f,   B = 1f,    A = 1f, Emissive = true, EmissiveStrength = 4f } },
            { "matte",    new Preset { R = 0.5f, G = 0.5f, B = 0.5f,  A = 1f } },
            { "rubber",   new Preset { R = 0.1f, G = 0.1f, B = 0.1f,  A = 1f } },
            { "gold",     new Preset { R = 1f,   G = 0.84f,B = 0.2f,  A = 1f } },
            { "toxic",    new Preset { R = 0.5f, G = 1f,   B = 0f,    A = 1f, Emissive = true, EmissiveStrength = 1.5f } },
            { "ghost",    new Preset { R = 0.8f, G = 0.85f,B = 1f,    A = 0.2f } },
        };

        public static void Register()
        {
            ToolRegistry.Register("material_apply_preset", "Materials",
                "Applies one of 12 built-in material presets (chrome, neon, hologram, lava, ice, glass, emissive, matte, rubber, gold, toxic, ghost) to a node.",
                ApplyPreset);

            ToolRegistry.Register("material_bulk_swap", "Materials",
                "Applies a material preset to every node whose name contains the given substring.",
                BulkSwap);

            ToolRegistry.Register("material_team_color_split", "Materials",
                "Colors two groups of nodes (matched by name substring) in two distinct team colors.",
                TeamColorSplit);

            ToolRegistry.Register("material_list_presets", "Materials",
                "Lists all available material preset names.",
                ListPresets);
        }

        private static object ApplyPreset(JObject p)
        {
            string name = (string)p["name"];
            string presetName = (string)p["preset"];

            var node = UnigineCompat.FindNodeByName(name);
            if (node == null) return new { success = false, error = $"Node '{name}' not found." };
            if (!(node is ObjectMeshStatic mesh)) return new { success = false, error = $"'{name}' is not an ObjectMeshStatic (materials apply to mesh objects)." };
            if (!Presets.TryGetValue(presetName ?? "", out var preset)) return new { success = false, error = $"Unknown preset '{presetName}'." };

            UnigineCompat.ApplyMaterialColor(mesh, 0, preset.R, preset.G, preset.B, preset.A, preset.Emissive, preset.EmissiveStrength);
            return new { success = true, name, preset = presetName };
        }

        private static object BulkSwap(JObject p)
        {
            string contains = ((string)p["name_contains"] ?? "").ToLowerInvariant();
            string presetName = (string)p["preset"];
            if (!Presets.TryGetValue(presetName ?? "", out var preset)) return new { success = false, error = $"Unknown preset '{presetName}'." };

            var matched = UnigineCompat.GetAllWorldNodes()
                .OfType<ObjectMeshStatic>()
                .Where(n => n.Name != null && n.Name.ToLowerInvariant().Contains(contains))
                .ToList();

            foreach (var m in matched)
                UnigineCompat.ApplyMaterialColor(m, 0, preset.R, preset.G, preset.B, preset.A, preset.Emissive, preset.EmissiveStrength);

            return new { success = true, preset = presetName, nodes_updated = matched.Count };
        }

        private static object TeamColorSplit(JObject p)
        {
            string aContains = ((string)p["team_a_contains"] ?? "").ToLowerInvariant();
            string bContains = ((string)p["team_b_contains"] ?? "").ToLowerInvariant();
            string aHex = (string)p["team_a_color"] ?? "FF3B30";
            string bHex = (string)p["team_b_color"] ?? "0A84FF";

            int UpdateGroup(string contains, string hex)
            {
                var (r, g, b) = HexToRgb(hex);
                var matched = UnigineCompat.GetAllWorldNodes()
                    .OfType<ObjectMeshStatic>()
                    .Where(n => n.Name != null && n.Name.ToLowerInvariant().Contains(contains))
                    .ToList();
                foreach (var m in matched)
                    UnigineCompat.ApplyMaterialColor(m, 0, r, g, b, 1f, false, 0f);
                return matched.Count;
            }

            int a = UpdateGroup(aContains, aHex);
            int b = UpdateGroup(bContains, bHex);
            return new { success = true, team_a_updated = a, team_b_updated = b };
        }

        private static object ListPresets(JObject p)
        {
            return new { success = true, presets = Presets.Keys.ToArray() };
        }

        private static (float, float, float) HexToRgb(string hex)
        {
            hex = hex.TrimStart('#');
            if (hex.Length != 6) return (1f, 1f, 1f);
            int r = System.Convert.ToInt32(hex.Substring(0, 2), 16);
            int g = System.Convert.ToInt32(hex.Substring(2, 2), 16);
            int b = System.Convert.ToInt32(hex.Substring(4, 2), 16);
            return (r / 255f, g / 255f, b / 255f);
        }
    }
}
