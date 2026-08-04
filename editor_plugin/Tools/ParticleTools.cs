// qFoldIT Toolbelt for UNIGINE 2 — ParticleTools.cs
// Category: Particles
// ⚠ UNIGINE's ObjectParticles is normally driven by a .particles asset
// authored in the Particle Editor rather than fully procedural setup like
// Unity's ParticleSystem. These tools create an ObjectParticles node and
// apply what runtime-settable properties are exposed (color, emission
// rate) — for full presets (fire/smoke/etc.), pairing this with a
// pre-authored .particles asset per preset is the recommended real-world
// path; verify property names against your SDK's ObjectParticles API.

using Newtonsoft.Json.Linq;
using Unigine;

namespace QFoldIT.Toolbelt
{
    public static class ParticleTools
    {
        public static void Register()
        {
            ToolRegistry.Register("particles_spawn_from_asset", "Particles",
                "Creates an ObjectParticles node at a world position from a pre-authored .particles asset.",
                SpawnFromAsset);

            ToolRegistry.Register("particles_set_emission_rate", "Particles",
                "Sets the spawn rate (particles/sec-equivalent NumSpawnedParticles) on an existing ObjectParticles node.",
                SetEmissionRate);

            ToolRegistry.Register("particles_set_color", "Particles",
                "Sets the base color/tint on an existing ObjectParticles node.",
                SetColor);

            ToolRegistry.Register("particles_stop", "Particles",
                "Stops emission on an existing ObjectParticles node without destroying it (lets existing particles finish their lifetime).",
                Stop);
        }

        private static object SpawnFromAsset(JObject p)
        {
            string name = (string)p["name"] ?? "Particles";
            string assetPath = (string)p["asset_path"];
            double x = (double?)p["x"] ?? 0, y = (double?)p["y"] ?? 0, z = (double?)p["z"] ?? 0;

            if (string.IsNullOrEmpty(assetPath)) return new { success = false, error = "asset_path is required (a .particles asset)." };

            var node = Unigine.World.LoadNode(assetPath) as ObjectParticles;
            if (node == null) return new { success = false, error = $"'{assetPath}' did not load as an ObjectParticles node." };

            UnigineCompat.SetWorldPosition(node, x, y, z);
            node.Name = name;
            node.Enabled = true;

            return new { success = true, name, asset_path = assetPath };
        }

        private static object SetEmissionRate(JObject p)
        {
            string name = (string)p["name"];
            int rate = (int?)p["rate"] ?? 10;

            var node = UnigineCompat.FindNodeByName(name) as ObjectParticles;
            if (node == null) return new { success = false, error = $"ObjectParticles '{name}' not found." };

            node.NumSpawnedParticles = rate;
            return new { success = true, name, rate };
        }

        private static object SetColor(JObject p)
        {
            string name = (string)p["name"];
            string hex = (string)p["color_hex"] ?? "FFFFFF";

            var node = UnigineCompat.FindNodeByName(name) as ObjectParticles;
            if (node == null) return new { success = false, error = $"ObjectParticles '{name}' not found." };

            hex = hex.TrimStart('#');
            if (hex.Length == 6)
            {
                int r = System.Convert.ToInt32(hex.Substring(0, 2), 16);
                int g = System.Convert.ToInt32(hex.Substring(2, 2), 16);
                int b = System.Convert.ToInt32(hex.Substring(4, 2), 16);
                node.SetMaterialParameterFloat4("albedo_color", new Unigine.Math.vec4(r / 255f, g / 255f, b / 255f, 1f), 0);
            }

            return new { success = true, name, color_hex = hex };
        }

        private static object Stop(JObject p)
        {
            string name = (string)p["name"];
            var node = UnigineCompat.FindNodeByName(name) as ObjectParticles;
            if (node == null) return new { success = false, error = $"ObjectParticles '{name}' not found." };

            node.NumSpawnedParticles = 0;
            return new { success = true, name };
        }
    }
}
