// qFoldIT Toolbelt for UNIGINE 2 — SceneTools.cs
// Category: Scene

using System.Linq;
using Newtonsoft.Json.Linq;
using Unigine;

namespace QFoldIT.Toolbelt
{
    public static class SceneTools
    {
        public static void Register()
        {
            ToolRegistry.Register("spawn_primitive", "Scene",
                "Creates a primitive node (box, sphere, cylinder, capsule, plane, cone, torus) at a world position.",
                SpawnPrimitive);

            ToolRegistry.Register("transform_node", "Scene",
                "Sets position/rotation/scale on an existing node found by name.",
                TransformNode);

            ToolRegistry.Register("clone_node", "Scene",
                "Duplicates a node N times with an incremental world-space offset per copy.",
                CloneNode);

            ToolRegistry.Register("delete_node", "Scene",
                "Deletes a node from the world by name.",
                DeleteNode);

            ToolRegistry.Register("parent_node", "Scene",
                "Reparents one node under another, or un-parents it.",
                ParentNode);

            ToolRegistry.Register("world_list_nodes", "Scene",
                "Lists every node in the currently loaded world with name, type, and position.",
                ListNodes);

            ToolRegistry.Register("world_find_by_name", "Scene",
                "Finds all nodes whose name contains the given substring.",
                FindByName);

            ToolRegistry.Register("spawn_group_node", "Scene",
                "Creates an empty NodeDummy — a pure transform container for grouping children, with no visible geometry (maps UAG's 'group' node type).",
                SpawnGroupNode);
        }

        private static object SpawnPrimitive(JObject p)
        {
            string type = (string)p["type"] ?? "box";
            double x = (double?)p["x"] ?? 0;
            double y = (double?)p["y"] ?? 0;
            double z = (double?)p["z"] ?? 0;
            float scale = (float?)p["scale"] ?? 1f;
            string name = (string)p["name"];

            var node = UnigineCompat.CreatePrimitive(type, x, y, z, scale, name);
            return new { success = true, name = node.Name, node_id = node.ID, position = new[] { x, y, z } };
        }

        private static object TransformNode(JObject p)
        {
            string name = (string)p["name"];
            var node = UnigineCompat.FindNodeByName(name);
            if (node == null) return new { success = false, error = $"Node '{name}' not found." };

            var pos = node.WorldPosition;
            double x = (double?)p["x"] ?? pos.x;
            double y = (double?)p["y"] ?? pos.y;
            double z = (double?)p["z"] ?? pos.z;
            UnigineCompat.SetWorldPosition(node, x, y, z);

            if (p["rot_x"] != null || p["rot_y"] != null || p["rot_z"] != null)
            {
                float rx = (float?)p["rot_x"] ?? 0f;
                float ry = (float?)p["rot_y"] ?? 0f;
                float rz = (float?)p["rot_z"] ?? 0f;
                UnigineCompat.SetEulerRotation(node, rx, ry, rz);
            }

            if (p["scale"] != null)
                UnigineCompat.SetUniformScale(node, (float)p["scale"]);

            return new { success = true, name = node.Name, position = new[] { x, y, z } };
        }

        private static object CloneNode(JObject p)
        {
            string name = (string)p["name"];
            int count = (int?)p["count"] ?? 1;
            double offsetX = (double?)p["offset_x"] ?? 1;
            double offsetY = (double?)p["offset_y"] ?? 0;
            double offsetZ = (double?)p["offset_z"] ?? 0;

            var src = UnigineCompat.FindNodeByName(name);
            if (src == null) return new { success = false, error = $"Node '{name}' not found." };

            var created = new System.Collections.Generic.List<string>();
            for (int i = 1; i <= System.Math.Max(1, count); i++)
            {
                var copy = src.Clone(Node.CLONE.ALL);
                var basePos = src.WorldPosition;
                UnigineCompat.SetWorldPosition(copy, basePos.x + offsetX * i, basePos.y + offsetY * i, basePos.z + offsetZ * i);
                copy.Name = $"{src.Name}_{i}";
                World.AddChild(copy);
                created.Add(copy.Name);
            }

            return new { success = true, created_count = created.Count, created_names = created };
        }

        private static object DeleteNode(JObject p)
        {
            string name = (string)p["name"];
            var node = UnigineCompat.FindNodeByName(name);
            if (node == null) return new { success = false, error = $"Node '{name}' not found." };
            node.DeleteLater();
            return new { success = true, deleted = name };
        }

        private static object ParentNode(JObject p)
        {
            string childName = (string)p["child"];
            string parentName = (string)p["parent"];

            var child = UnigineCompat.FindNodeByName(childName);
            if (child == null) return new { success = false, error = $"Child '{childName}' not found." };

            if (string.IsNullOrEmpty(parentName))
            {
                World.AddChild(child); // un-parent to world root
                return new { success = true, child = childName, parent = (string)null };
            }

            var parent = UnigineCompat.FindNodeByName(parentName);
            if (parent == null) return new { success = false, error = $"Parent '{parentName}' not found." };

            parent.AddChild(child);
            return new { success = true, child = childName, parent = parentName };
        }

        private static object ListNodes(JObject p)
        {
            var all = UnigineCompat.GetAllWorldNodes();
            var result = all.Select(n => new
            {
                name = n.Name,
                type = n.GetType().Name,
                position = new[] { n.WorldPosition.x, n.WorldPosition.y, n.WorldPosition.z },
                child_count = n.NumChilds
            }).ToList();
            return new { success = true, node_count = result.Count, nodes = result };
        }

        private static object FindByName(JObject p)
        {
            string query = ((string)p["query"] ?? "").ToLowerInvariant();
            var matches = UnigineCompat.GetAllWorldNodes()
                .Where(n => n.Name != null && n.Name.ToLowerInvariant().Contains(query))
                .Select(n => n.Name)
                .ToList();
            return new { success = true, query, matches };
        }

        private static object SpawnGroupNode(JObject p)
        {
            string name = (string)p["name"];
            double x = (double?)p["x"] ?? 0, y = (double?)p["y"] ?? 0, z = (double?)p["z"] ?? 0;
            if (string.IsNullOrEmpty(name)) return new { success = false, error = "name is required." };

            var node = new NodeDummy();
            UnigineCompat.SetWorldPosition(node, x, y, z);
            node.Name = name;
            World.AddChild(node);

            return new { success = true, name = node.Name };
        }
    }
}
