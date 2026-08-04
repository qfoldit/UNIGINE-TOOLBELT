// qFoldIT Toolbelt for UNIGINE 2 — WorldManagementTools.cs
// Category: WorldManagement
// UNIGINE works with a single active .world at a time (no Unity-style
// multi-scene additive loading in the base engine), so this category
// covers the analogous single-world lifecycle: new/load/save-as/reload/info.

using Newtonsoft.Json.Linq;
using Unigine;

namespace QFoldIT.Toolbelt
{
    public static class WorldManagementTools
    {
        public static void Register()
        {
            ToolRegistry.Register("world_new", "WorldManagement",
                "Creates a new empty world, discarding the currently loaded one (unsaved changes are lost).",
                WorldNew);

            ToolRegistry.Register("world_load", "WorldManagement",
                "Loads a .world file, replacing the currently loaded world.",
                WorldLoad);

            ToolRegistry.Register("world_save_as", "WorldManagement",
                "Saves the currently loaded world to a new .world path.",
                WorldSaveAs);

            ToolRegistry.Register("world_reload", "WorldManagement",
                "Reloads the currently loaded world from disk, discarding unsaved changes.",
                WorldReload);

            ToolRegistry.Register("world_get_info", "WorldManagement",
                "Reports the current world's name/path and node count.",
                WorldGetInfo);
        }

        private static object WorldNew(JObject p)
        {
            Console.Run("world_new");
            return new { success = true };
        }

        private static object WorldLoad(JObject p)
        {
            string worldPath = (string)p["world_path"];
            if (string.IsNullOrEmpty(worldPath)) return new { success = false, error = "world_path is required." };

            bool ok = Unigine.World.Load(worldPath);
            return new { success = ok, world_path = worldPath };
        }

        private static object WorldSaveAs(JObject p)
        {
            string worldPath = (string)p["world_path"];
            if (string.IsNullOrEmpty(worldPath)) return new { success = false, error = "world_path is required." };

            bool ok = Unigine.World.Save(worldPath);
            return new { success = ok, world_path = worldPath };
        }

        private static object WorldReload(JObject p)
        {
            string currentName = Unigine.World.GetName();
            if (string.IsNullOrEmpty(currentName)) return new { success = false, error = "No world is currently loaded / world has no known path." };

            bool ok = Unigine.World.Load(currentName);
            return new { success = ok, world = currentName };
        }

        private static object WorldGetInfo(JObject p)
        {
            var nodes = UnigineCompat.GetAllWorldNodes();
            return new { success = true, world = Unigine.World.GetName() ?? "unsaved", node_count = nodes.Length };
        }
    }
}
