// qFoldIT Toolbelt for UNIGINE 2 — MeasurementTools.cs
// Category: Measurement

using Newtonsoft.Json.Linq;
using Unigine;

namespace QFoldIT.Toolbelt
{
    public static class MeasurementTools
    {
        public static void Register()
        {
            ToolRegistry.Register("measure_distance", "Measurement",
                "Reports the world-space distance between two named nodes.",
                MeasureDistance);

            ToolRegistry.Register("measure_bounds", "Measurement",
                "Reports a node's world bounding box (center + size) via its BoundBox.",
                MeasureBounds);

            ToolRegistry.Register("measure_world_bounds", "Measurement",
                "Reports the combined world bounding box of every node in the loaded world.",
                MeasureWorldBounds);
        }

        private static object MeasureDistance(JObject p)
        {
            string a = (string)p["object_a"];
            string b = (string)p["object_b"];

            var nodeA = UnigineCompat.FindNodeByName(a);
            var nodeB = UnigineCompat.FindNodeByName(b);
            if (nodeA == null) return new { success = false, error = $"Node '{a}' not found." };
            if (nodeB == null) return new { success = false, error = $"Node '{b}' not found." };

            double dist = Unigine.Math.dvec3.Distance(nodeA.WorldPosition, nodeB.WorldPosition);
            return new { success = true, distance = dist };
        }

        private static object MeasureBounds(JObject p)
        {
            string name = (string)p["name"];
            var node = UnigineCompat.FindNodeByName(name);
            if (node == null) return new { success = false, error = $"Node '{name}' not found." };

            var bb = node.WorldBoundBox;
            return new
            {
                success = true,
                center = new[] { bb.Center.x, bb.Center.y, bb.Center.z },
                size = new[] { bb.Size.x, bb.Size.y, bb.Size.z }
            };
        }

        private static object MeasureWorldBounds(JObject p)
        {
            var nodes = UnigineCompat.GetAllWorldNodes();
            if (nodes.Length == 0) return new { success = false, error = "World has no nodes." };

            var combined = nodes[0].WorldBoundBox;
            for (int i = 1; i < nodes.Length; i++)
                combined.Expand(nodes[i].WorldBoundBox);

            return new
            {
                success = true,
                node_count = nodes.Length,
                center = new[] { combined.Center.x, combined.Center.y, combined.Center.z },
                size = new[] { combined.Size.x, combined.Size.y, combined.Size.z }
            };
        }
    }
}
