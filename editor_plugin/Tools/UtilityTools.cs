// qFoldIT Toolbelt for UNIGINE 2 — UtilityTools.cs
// Category: Utility

using System.Linq;
using Newtonsoft.Json.Linq;
using Unigine;

namespace QFoldIT.Toolbelt
{
    public static class UtilityTools
    {
        public static void Register()
        {
            ToolRegistry.Register("batch_rename", "Utility",
                "Renames every node whose name contains a substring to a common prefix with an incrementing index.",
                BatchRename);

            ToolRegistry.Register("editor_get_engine_info", "Utility",
                "Reports basic Engine info (version, current world) — useful as a first sanity-check call after connecting.",
                GetEngineInfo);
        }

        private static object BatchRename(JObject p)
        {
            string nameContains = ((string)p["name_contains"] ?? "").ToLowerInvariant();
            string newPrefix = (string)p["new_prefix"];

            var matched = UnigineCompat.GetAllWorldNodes()
                .Where(n => n.Name != null && n.Name.ToLowerInvariant().Contains(nameContains))
                .ToList();

            for (int i = 0; i < matched.Count; i++)
                matched[i].Name = $"{newPrefix}_{i:D3}";

            return new { success = true, renamed_count = matched.Count };
        }

        private static object GetEngineInfo(JObject p)
        {
            return new
            {
                success = true,
                engine_version = Engine.Get()?.GetVersion() ?? "unknown",
                world = Unigine.World.GetName() ?? "unsaved",
                node_count = UnigineCompat.GetAllWorldNodes().Length
            };
        }
    }
}
