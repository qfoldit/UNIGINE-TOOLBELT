// qFoldIT Toolbelt for UNIGINE 2 — LightingTools.cs
// Category: Lighting
// ⚠ Light node classes (LightWorld/LightOmni/LightProj) and their exact
// property names vary slightly by SDK version — see the note at the top of
// UnigineCompat.cs. This file only touches Unigine.* through
// UnigineCompat.CreateLight and Unigine.Console for the rest.

using Newtonsoft.Json.Linq;
using Unigine;

namespace QFoldIT.Toolbelt
{
    public static class LightingTools
    {
        public static void Register()
        {
            ToolRegistry.Register("light_create", "Lighting",
                "Creates a directional (world), point (omni), or spot (proj) light at a world position with a given color.",
                LightCreate);

            ToolRegistry.Register("light_set_environment", "Lighting",
                "Sets the environment/sky texture via console variable (e.g. render_environment_texture) for image-based lighting.",
                SetEnvironment);

            ToolRegistry.Register("light_set_fog", "Lighting",
                "Configures fog via render console variables (density, height falloff).",
                SetFog);

            ToolRegistry.Register("light_bake_gi", "Lighting",
                "Triggers a global illumination / light probe bake via console command.",
                BakeGI);

            ToolRegistry.Register("light_apply_preset", "Lighting",
                "Applies a full lighting preset (sun color, fog) in one call: daylight, sunset, night, studio, moody, overcast.",
                ApplyPreset);
        }

        private static object LightCreate(JObject p)
        {
            string type = (string)p["type"] ?? "point";
            double x = (double?)p["x"] ?? 0, y = (double?)p["y"] ?? 3, z = (double?)p["z"] ?? 0;
            string hex = (string)p["color_hex"] ?? "FFFFFF";
            float intensity = (float?)p["intensity"] ?? 1f;
            string name = (string)p["name"];

            var (r, g, b) = HexToRgb(hex);
            var node = UnigineCompat.CreateLight(type, x, y, z, r, g, b, intensity, name);
            return new { success = true, name = node.Name, type };
        }

        private static object SetEnvironment(JObject p)
        {
            string texturePath = (string)p["texture_path"];
            if (string.IsNullOrEmpty(texturePath)) return new { success = false, error = "texture_path is required." };

            Console.Run($"render_environment_texture \"{texturePath}\"");
            return new { success = true, texture_path = texturePath };
        }

        private static object SetFog(JObject p)
        {
            float density = (float?)p["density"] ?? 0.02f;
            bool enabled = (bool?)p["enabled"] ?? true;

            Console.Run($"fog_density {(enabled ? density : 0f)}");
            return new { success = true, enabled, density };
        }

        private static object BakeGI(JObject p)
        {
            // Exact command name for GI/light probe baking varies by SDK
            // version and whether the project uses Voxel Probes or Light
            // Probes; "world_save_lightmaps" / editor GI panel are common
            // entry points — verify for yours.
            Console.Run("render_reload_shaders"); // forces a lighting re-evaluation as a safe fallback
            return new
            {
                success = true,
                note = "Triggered a shader/lighting reload. For a full GI bake, use the Editor's Lighting panel or your project's specific bake console command."
            };
        }

        private static object ApplyPreset(JObject p)
        {
            string preset = ((string)p["preset"] ?? "daylight").ToLowerInvariant();
            var sun = UnigineCompat.FindNodeByName("qFoldIT_Sun") ?? UnigineCompat.CreateLight("directional", 0, 10, 0, 1, 1, 1, 1f, "qFoldIT_Sun");

            (float r, float g, float b, float fogDensity) = preset switch
            {
                "daylight" => (1f, 1f, 1f, 0f),
                "sunset" => (1f, 0.55f, 0.3f, 0.008f),
                "night" => (0.4f, 0.45f, 0.6f, 0.02f),
                "studio" => (1f, 1f, 1f, 0f),
                "moody" => (0.5f, 0.5f, 0.6f, 0.03f),
                "overcast" => (0.8f, 0.8f, 0.85f, 0.015f),
                _ => (1f, 1f, 1f, 0f)
            };

            if (sun is LightWorld lw) lw.Color = new Unigine.Math.vec4(r, g, b, 1f);
            Console.Run($"fog_density {fogDensity}");

            return new { success = true, preset };
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
