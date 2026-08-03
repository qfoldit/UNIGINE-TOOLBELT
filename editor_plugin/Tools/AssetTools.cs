// qFoldIT Toolbelt for UNIGINE 2 — AssetTools.cs
// Category: Assets

using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Unigine;

namespace QFoldIT.Toolbelt
{
    public static class AssetTools
    {
        public static void Register()
        {
            ToolRegistry.Register("asset_list", "Assets",
                "Lists project assets of a given extension (.node, .mesh, .mat, .dae, .fbx) under a folder, relative to the project data root.",
                AssetList);

            ToolRegistry.Register("asset_instantiate_node", "Assets",
                "Loads a .node asset and places it at a world position.",
                InstantiateNode);

            ToolRegistry.Register("asset_find_by_extension", "Assets",
                "Finds project assets of a given extension whose file name contains a substring.",
                FindByExtension);
        }

        private static object AssetList(JObject p)
        {
            string ext = (string)p["extension"] ?? "node";
            string folder = (string)p["folder"] ?? "";
            int maxResults = (int?)p["max_results"] ?? 100;

            string dataRoot = Engine.Get().GetSourceDataPath() ?? ".";
            string searchRoot = Path.Combine(dataRoot, folder);
            if (!Directory.Exists(searchRoot)) return new { success = true, extension = ext, folder, count = 0, assets = new string[0] };

            var files = Directory.GetFiles(searchRoot, $"*.{ext.TrimStart('.')}", SearchOption.AllDirectories)
                .Select(f => Path.GetRelativePath(dataRoot, f).Replace('\\', '/'))
                .Take(maxResults)
                .ToArray();

            return new { success = true, extension = ext, folder, count = files.Length, assets = files };
        }

        private static object InstantiateNode(JObject p)
        {
            string nodePath = (string)p["node_path"];
            double x = (double?)p["x"] ?? 0;
            double y = (double?)p["y"] ?? 0;
            double z = (double?)p["z"] ?? 0;
            string name = (string)p["name"];

            if (string.IsNullOrEmpty(nodePath)) return new { success = false, error = "node_path is required." };

            var node = Unigine.World.LoadNode(nodePath);
            if (node == null) return new { success = false, error = $"Failed to load node asset at '{nodePath}'." };

            UnigineCompat.SetWorldPosition(node, x, y, z);
            if (!string.IsNullOrEmpty(name)) node.Name = name;

            return new { success = true, name = node.Name, node_path = nodePath };
        }

        private static object FindByExtension(JObject p)
        {
            string ext = (string)p["extension"] ?? "node";
            string nameContains = ((string)p["name_contains"] ?? "").ToLowerInvariant();

            string dataRoot = Engine.Get().GetSourceDataPath() ?? ".";
            if (!Directory.Exists(dataRoot)) return new { success = true, matches = new string[0] };

            var matches = Directory.GetFiles(dataRoot, $"*.{ext.TrimStart('.')}", SearchOption.AllDirectories)
                .Where(f => Path.GetFileNameWithoutExtension(f).ToLowerInvariant().Contains(nameContains))
                .Select(f => Path.GetRelativePath(dataRoot, f).Replace('\\', '/'))
                .ToArray();

            return new { success = true, matches };
        }
    }
}
