// qFoldIT Toolbelt for UNIGINE 2 — CameraTools.cs
// Category: Camera

using System.IO;
using Newtonsoft.Json.Linq;
using Unigine;

namespace QFoldIT.Toolbelt
{
    public static class CameraTools
    {
        public static void Register()
        {
            ToolRegistry.Register("camera_create", "Camera",
                "Creates a Player (camera) node at a world position with a given field of view.",
                CameraCreate);

            ToolRegistry.Register("camera_set_follow", "Camera",
                "Marks a camera node to follow a target with a fixed offset, tracked via a lightweight follow registry updated in ToolbeltBootstrap.Update().",
                SetFollow);

            ToolRegistry.Register("camera_set_clipping", "Camera",
                "Sets a camera's near/far clipping planes.",
                SetClipping);

            ToolRegistry.Register("camera_set_fov", "Camera",
                "Sets a camera's field of view in degrees.",
                SetFov);

            ToolRegistry.Register("camera_screenshot", "Camera",
                "Captures a screenshot of the current render output to a file.",
                Screenshot);
        }

        // Lightweight follow registry — call CameraFollowSystem.Update() each
        // frame from ToolbeltBootstrap.Update() to apply it (see note there).
        public static readonly System.Collections.Generic.Dictionary<string, (string target, Unigine.Math.dvec3 offset)> FollowRegistry =
            new System.Collections.Generic.Dictionary<string, (string, Unigine.Math.dvec3)>();

        private static object CameraCreate(JObject p)
        {
            string name = (string)p["name"] ?? "Camera";
            double x = (double?)p["x"] ?? 0, y = (double?)p["y"] ?? 2, z = (double?)p["z"] ?? -10;
            float fov = (float?)p["fov"] ?? 60f;

            var player = new PlayerDummy();
            UnigineCompat.SetWorldPosition(player, x, y, z);
            player.FovY = fov;
            player.Name = name;
            World.AddChild(player);

            return new { success = true, name, fov };
        }

        private static object SetFollow(JObject p)
        {
            string cameraName = (string)p["camera_name"];
            string targetName = (string)p["target_name"];
            double ox = (double?)p["offset_x"] ?? 0, oy = (double?)p["offset_y"] ?? 3, oz = (double?)p["offset_z"] ?? -8;

            if (UnigineCompat.FindNodeByName(cameraName) == null) return new { success = false, error = $"Camera '{cameraName}' not found." };
            if (UnigineCompat.FindNodeByName(targetName) == null) return new { success = false, error = $"Target '{targetName}' not found." };

            FollowRegistry[cameraName] = (targetName, new Unigine.Math.dvec3(ox, oy, oz));
            return new { success = true, camera = cameraName, target = targetName };
        }

        private static object SetClipping(JObject p)
        {
            string name = (string)p["name"];
            float near = (float?)p["near"] ?? 0.3f, far = (float?)p["far"] ?? 1000f;

            var node = UnigineCompat.FindNodeByName(name) as Player;
            if (node == null) return new { success = false, error = $"Camera '{name}' not found or not a Player node." };

            node.ZNear = near;
            node.ZFar = far;
            return new { success = true, name, near, far };
        }

        private static object SetFov(JObject p)
        {
            string name = (string)p["name"];
            float fov = (float?)p["fov"] ?? 60f;

            var node = UnigineCompat.FindNodeByName(name) as Player;
            if (node == null) return new { success = false, error = $"Camera '{name}' not found or not a Player node." };

            node.FovY = fov;
            return new { success = true, name, fov };
        }

        private static object Screenshot(JObject p)
        {
            string outputPath = (string)p["output_path"] ?? "screenshot.png";
            string dataRoot = Engine.Get().GetSourceDataPath() ?? ".";
            var fullPath = Path.Combine(dataRoot, outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? ".");

            // Console-based screenshot capture is the most version-stable
            // approach; the exact API also exists as Unigine.Render /
            // WindowManager screenshot calls in some SDK versions.
            Console.Run($"makeScreenshot \"{outputPath}\"");

            return new { success = true, path = outputPath };
        }
    }
}
