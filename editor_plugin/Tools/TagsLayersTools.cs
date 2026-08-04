// qFoldIT Toolbelt for UNIGINE 2 — TagsLayersTools.cs
// Category: TagsLayers
// UNIGINE has no built-in string "tag" manager like Unity's Tag Manager,
// so free-text tags are implemented here as a small JSON-backed registry
// (persisted under Saved/QFoldIT_Toolbelt/). "Layers" map to UNIGINE's
// real IntersectionMask bitmask (node.SetIntersectionMask), which is a
// genuine engine feature used for raycast/collision filtering — layer
// names are just a friendly label over specific mask bits, tracked in the
// same JSON registry.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unigine;

namespace QFoldIT.Toolbelt
{
    public static class TagsLayersTools
    {
        private class TagsLayersData
        {
            public Dictionary<string, List<string>> NodeTags = new Dictionary<string, List<string>>();
            public Dictionary<string, int> LayerBits = new Dictionary<string, int>(); // layer name -> bit index 0-31
        }

        private static string DataPath => Path.Combine(UnigineCompat.SavedDataDir, "tags_layers.json");

        private static TagsLayersData Load()
        {
            if (!File.Exists(DataPath)) return new TagsLayersData();
            return JsonConvert.DeserializeObject<TagsLayersData>(File.ReadAllText(DataPath)) ?? new TagsLayersData();
        }

        private static void Save(TagsLayersData data)
        {
            Directory.CreateDirectory(UnigineCompat.SavedDataDir);
            File.WriteAllText(DataPath, JsonConvert.SerializeObject(data, Formatting.Indented));
        }

        public static void Register()
        {
            ToolRegistry.Register("tag_assign", "TagsLayers",
                "Assigns a free-text tag to a node (tracked in a JSON registry, since UNIGINE has no built-in tag manager).",
                TagAssign);

            ToolRegistry.Register("tag_find_nodes", "TagsLayers",
                "Finds all node names that have a given tag assigned.",
                TagFindNodes);

            ToolRegistry.Register("layer_create", "TagsLayers",
                "Registers a named layer, mapped to a free bit (0-31) in UNIGINE's real IntersectionMask system.",
                LayerCreate);

            ToolRegistry.Register("layer_assign", "TagsLayers",
                "Sets a node's IntersectionMask to a previously registered layer's bit.",
                LayerAssign);
        }

        private static object TagAssign(JObject p)
        {
            string name = (string)p["name"];
            string tag = (string)p["tag"];

            if (UnigineCompat.FindNodeByName(name) == null) return new { success = false, error = $"Node '{name}' not found." };

            var data = Load();
            if (!data.NodeTags.TryGetValue(name, out var tags)) { tags = new List<string>(); data.NodeTags[name] = tags; }
            if (!tags.Contains(tag)) tags.Add(tag);
            Save(data);

            return new { success = true, name, tag };
        }

        private static object TagFindNodes(JObject p)
        {
            string tag = (string)p["tag"];
            var data = Load();
            var matches = data.NodeTags.Where(kv => kv.Value.Contains(tag)).Select(kv => kv.Key).ToArray();
            return new { success = true, tag, matches };
        }

        private static object LayerCreate(JObject p)
        {
            string layerName = (string)p["layer_name"];
            var data = Load();

            if (data.LayerBits.ContainsKey(layerName))
                return new { success = true, layer = layerName, bit = data.LayerBits[layerName], already_existed = true };

            for (int bit = 0; bit < 32; bit++)
            {
                if (!data.LayerBits.ContainsValue(bit))
                {
                    data.LayerBits[layerName] = bit;
                    Save(data);
                    return new { success = true, layer = layerName, bit, already_existed = false };
                }
            }
            return new { success = false, error = "No free IntersectionMask bits (0-31) available." };
        }

        private static object LayerAssign(JObject p)
        {
            string name = (string)p["name"];
            string layerName = (string)p["layer_name"];

            var node = UnigineCompat.FindNodeByName(name);
            if (node == null) return new { success = false, error = $"Node '{name}' not found." };

            var data = Load();
            if (!data.LayerBits.TryGetValue(layerName, out int bit))
                return new { success = false, error = $"Layer '{layerName}' does not exist. Call layer_create first." };

            node.SetIntersectionMask(1 << bit, 0); // second arg is the "value" per-bit set/clear index on most SDK versions
            return new { success = true, name, layer = layerName, bit };
        }
    }
}
