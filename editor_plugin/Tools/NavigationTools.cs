// qFoldIT Toolbelt for UNIGINE 2 — NavigationTools.cs
// Category: Navigation
// ⚠ Navigation (NavMesh baking, NavAgent, NavObstacle) in UNIGINE 2 ships
// as part of the AI/Navigation add-on rather than the base SDK on all
// editions. These tools assume Unigine.Navigation.* types are available;
// if your project doesn't have the add-on enabled, they'll fail cleanly
// with a clear error rather than crash the Editor.

using Newtonsoft.Json.Linq;
using Unigine;

namespace QFoldIT.Toolbelt
{
    public static class NavigationTools
    {
        public static void Register()
        {
            ToolRegistry.Register("nav_bake_navmesh", "Navigation",
                "Bakes a NavigationMesh over the current world geometry (requires the Navigation add-on).",
                BakeNavMesh);

            ToolRegistry.Register("nav_add_agent", "Navigation",
                "Adds a NavigationAgent component to a node for NavMesh-based pathing.",
                AddAgent);

            ToolRegistry.Register("nav_add_obstacle", "Navigation",
                "Adds a NavigationObstacle to a node so it's avoided by NavMesh pathing.",
                AddObstacle);

            ToolRegistry.Register("nav_set_destination", "Navigation",
                "Sets a NavigationAgent's move target (Play Mode / running simulation only).",
                SetDestination);
        }

        private static object BakeNavMesh(JObject p)
        {
            try
            {
                var mesh = new NavigationMesh();
                World.AddChild(mesh);
                mesh.Create();
                return new { success = true };
            }
            catch (System.Exception ex)
            {
                return new { success = false, error = $"Navigation add-on unavailable or bake failed: {ex.Message}" };
            }
        }

        private static object AddAgent(JObject p)
        {
            string name = (string)p["name"];
            float speed = (float?)p["speed"] ?? 3.5f;
            float radius = (float?)p["radius"] ?? 0.5f;

            var node = UnigineCompat.FindNodeByName(name);
            if (node == null) return new { success = false, error = $"Node '{name}' not found." };

            try
            {
                var agent = new NavigationAgent(node);
                agent.MaxSpeed = speed;
                agent.Radius = radius;
                return new { success = true, name, speed, radius };
            }
            catch (System.Exception ex)
            {
                return new { success = false, error = $"Navigation add-on unavailable: {ex.Message}" };
            }
        }

        private static object AddObstacle(JObject p)
        {
            string name = (string)p["name"];
            var node = UnigineCompat.FindNodeByName(name);
            if (node == null) return new { success = false, error = $"Node '{name}' not found." };

            try
            {
                var obstacle = new NavigationObstacle(node);
                return new { success = true, name };
            }
            catch (System.Exception ex)
            {
                return new { success = false, error = $"Navigation add-on unavailable: {ex.Message}" };
            }
        }

        private static object SetDestination(JObject p)
        {
            string name = (string)p["name"];
            double x = (double?)p["x"] ?? 0, y = (double?)p["y"] ?? 0, z = (double?)p["z"] ?? 0;

            var node = UnigineCompat.FindNodeByName(name);
            if (node == null) return new { success = false, error = $"Node '{name}' not found." };

            try
            {
                var agent = NavigationAgent.GetAgent(node);
                if (agent == null) return new { success = false, error = $"'{name}' has no NavigationAgent — call nav_add_agent first." };
                agent.SetTargetPosition(new Unigine.Math.dvec3(x, y, z));
                return new { success = true, name, destination = new[] { x, y, z } };
            }
            catch (System.Exception ex)
            {
                return new { success = false, error = $"Navigation add-on unavailable: {ex.Message}" };
            }
        }
    }
}
