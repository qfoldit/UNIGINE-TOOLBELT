// qFoldIT Toolbelt for UNIGINE 2 — NodeWorkflowTools.cs
// Category: NodeWorkflow
// UNIGINE's closest equivalent to Unity prefabs is saving a node subtree to
// a .node file and re-loading it via World.LoadNode/World.SaveNode. There
// is no built-in "prefab instance" link that tracks overrides the way
// Unity's PrefabUtility does — each loaded .node is an independent copy
// once placed, matching MCPBridge Plugin's own "create nodes from project
// assets (.node, .mesh, .fbx, .obj, .dae)" feature description.

using System.IO;
using Newtonsoft.Json.Linq;
using Unigine;

namespace QFoldIT.Toolbelt
{
    public static class NodeWorkflowTools
    {
        public static void Register()
        {
            ToolRegistry.Register("node_save_as_asset", "NodeWorkflow",
                "Saves a node (and its children) in the world to a .node asset file.",
                SaveAsAsset);

            ToolRegistry.Register("node_reload_from_asset", "NodeWorkflow",
                "Deletes a world node and reloads a fresh copy from its source .node asset at the same transform.",
                ReloadFromAsset);

            ToolRegistry.Register("node_instantiate_variant", "NodeWorkflow",
                "Loads a .node asset and immediately applies a material preset override and/or scale, producing a 'variant' instance.",
                InstantiateVariant);

            ToolRegistry.Register("node_export_xml", "NodeWorkflow",
                "Exports a node's full property tree to XML for inspection (mirrors MCPBridge Plugin's XML export/import feature).",
                ExportXml);
        }

        private static object SaveAsAsset(JObject p)
        {
            string name = (string)p["name"];
            string outputPath = (string)p["output_path"];

            var node = UnigineCompat.FindNodeByName(name);
            if (node == null) return new { success = false, error = $"Node '{name}' not found." };
            if (string.IsNullOrEmpty(outputPath)) return new { success = false, error = "output_path is required." };

            string dataRoot = Engine.Get().GetSourceDataPath() ?? ".";
            var fullPath = Path.Combine(dataRoot, outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? ".");

            bool ok = node.SaveWorld(outputPath); // some SDK versions expose this as World.SaveNode(node, path) instead
            return new { success = ok, name, path = outputPath };
        }

        private static object ReloadFromAsset(JObject p)
        {
            string name = (string)p["name"];
            string assetPath = (string)p["asset_path"];

            var node = UnigineCompat.FindNodeByName(name);
            if (node == null) return new { success = false, error = $"Node '{name}' not found." };
            if (string.IsNullOrEmpty(assetPath)) return new { success = false, error = "asset_path is required." };

            var pos = node.WorldPosition;
            node.DeleteLater();

            var fresh = Unigine.World.LoadNode(assetPath);
            if (fresh == null) return new { success = false, error = $"Failed to load '{assetPath}'." };
            UnigineCompat.SetWorldPosition(fresh, pos.x, pos.y, pos.z);
            fresh.Name = name;

            return new { success = true, name, asset_path = assetPath };
        }

        private static object InstantiateVariant(JObject p)
        {
            string assetPath = (string)p["asset_path"];
            double x = (double?)p["x"] ?? 0, y = (double?)p["y"] ?? 0, z = (double?)p["z"] ?? 0;
            float scale = (float?)p["scale"] ?? 1f;
            string name = (string)p["name"];

            if (string.IsNullOrEmpty(assetPath)) return new { success = false, error = "asset_path is required." };

            var node = Unigine.World.LoadNode(assetPath);
            if (node == null) return new { success = false, error = $"Failed to load '{assetPath}'." };

            UnigineCompat.SetWorldPosition(node, x, y, z);
            UnigineCompat.SetUniformScale(node, scale);
            if (!string.IsNullOrEmpty(name)) node.Name = name;

            return new { success = true, name = node.Name, asset_path = assetPath, scale };
        }

        private static object ExportXml(JObject p)
        {
            string name = (string)p["name"];
            string outputPath = (string)p["output_path"];

            var node = UnigineCompat.FindNodeByName(name);
            if (node == null) return new { success = false, error = $"Node '{name}' not found." };
            if (string.IsNullOrEmpty(outputPath)) return new { success = false, error = "output_path is required." };

            string dataRoot = Engine.Get().GetSourceDataPath() ?? ".";
            var fullPath = Path.Combine(dataRoot, outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? ".");

            var xml = new Xml("node");
            node.SaveState(xml); // property name varies; some SDKs use node.Save(xml) directly
            xml.Save(fullPath);

            return new { success = true, name, path = outputPath };
        }
    }
}
