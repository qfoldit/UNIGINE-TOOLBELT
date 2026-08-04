// qFoldIT Toolbelt for UNIGINE 2 — PhysicsTools.cs
// Category: Physics
// ⚠ Unigine's physics API centers on BodyRigid/BodyDummy attached to a Node,
// plus Shape* classes (ShapeBox, ShapeSphere, ShapeCapsule) added to the
// body. Exact constructor argument order varies by SDK version — verify
// against your installed SDK's API docs.

using Newtonsoft.Json.Linq;
using Unigine;

namespace QFoldIT.Toolbelt
{
    public static class PhysicsTools
    {
        public static void Register()
        {
            ToolRegistry.Register("physics_add_body", "Physics",
                "Adds a BodyRigid to a node with the given mass; makes it dynamic under gravity.",
                AddBody);

            ToolRegistry.Register("physics_add_shape", "Physics",
                "Adds a collision Shape (box/sphere/capsule) to a node's physics body.",
                AddShape);

            ToolRegistry.Register("physics_set_material", "Physics",
                "Sets friction and restitution (bounciness) on a node's physics body/shape.",
                SetMaterial);

            ToolRegistry.Register("physics_raycast_query", "Physics",
                "Casts a physics ray and reports the first hit node, point, and distance.",
                RaycastQuery);

            ToolRegistry.Register("physics_set_gravity", "Physics",
                "Sets the world's global gravity vector via console variable.",
                SetGravity);
        }

        private static object AddBody(JObject p)
        {
            string name = (string)p["name"];
            float mass = (float?)p["mass"] ?? 1f;

            var node = UnigineCompat.FindNodeByName(name);
            if (node == null) return new { success = false, error = $"Node '{name}' not found." };

            var body = new BodyRigid(node);
            body.Mass = mass;
            body.Freeze();
            body.Enable();

            return new { success = true, name, mass };
        }

        private static object AddShape(JObject p)
        {
            string name = (string)p["name"];
            string shapeType = ((string)p["shape"] ?? "box").ToLowerInvariant();
            bool isTrigger = (bool?)p["is_trigger"] ?? false;

            var node = UnigineCompat.FindNodeByName(name);
            if (node == null) return new { success = false, error = $"Node '{name}' not found." };
            var body = Body.GetBody(node) ?? new BodyRigid(node);

            Shape shape = shapeType switch
            {
                "box" => new ShapeBox(body, new Unigine.Math.vec3(1, 1, 1)),
                "sphere" => new ShapeSphere(body, 0.5f),
                "capsule" => new ShapeCapsule(body, 0.5f, 1f),
                _ => new ShapeBox(body, new Unigine.Math.vec3(1, 1, 1))
            };
            shape.Trigger = isTrigger;

            return new { success = true, name, shape = shapeType, is_trigger = isTrigger };
        }

        private static object SetMaterial(JObject p)
        {
            string name = (string)p["name"];
            float friction = (float?)p["friction"] ?? 0.6f;
            float restitution = (float?)p["restitution"] ?? 0f;

            var node = UnigineCompat.FindNodeByName(name);
            if (node == null) return new { success = false, error = $"Node '{name}' not found." };
            var body = Body.GetBody(node);
            if (body == null) return new { success = false, error = $"'{name}' has no physics body — call physics_add_body first." };

            for (int i = 0; i < body.NumShapes; i++)
            {
                var shape = body.GetShape(i);
                shape.Friction = friction;
                shape.Restitution = restitution;
            }

            return new { success = true, name, friction, restitution };
        }

        private static object RaycastQuery(JObject p)
        {
            double ox = (double?)p["origin_x"] ?? 0, oy = (double?)p["origin_y"] ?? 0, oz = (double?)p["origin_z"] ?? 0;
            double dx = (double?)p["dir_x"] ?? 0, dy = (double?)p["dir_y"] ?? -1, dz = (double?)p["dir_z"] ?? 0;
            float maxDistance = (float?)p["max_distance"] ?? 100f;

            var origin = new Unigine.Math.dvec3(ox, oy, oz);
            var dirNorm = Unigine.Math.dvec3.Normalize(new Unigine.Math.dvec3(dx, dy, dz));
            var end = origin + dirNorm * maxDistance;

            var intersection = new WorldIntersectionNormal();
            var hitNode = World.GetIntersection(origin, end, 1, intersection);

            if (hitNode == null) return new { success = true, hit = false };

            return new
            {
                success = true,
                hit = true,
                node_name = hitNode.Name,
                point = new[] { intersection.Point.x, intersection.Point.y, intersection.Point.z }
            };
        }

        private static object SetGravity(JObject p)
        {
            float x = (float?)p["x"] ?? 0f, y = (float?)p["y"] ?? -9.81f, z = (float?)p["z"] ?? 0f;
            Console.Run($"physics_gravity \"{x} {y} {z}\"");
            return new { success = true, gravity = new[] { x, y, z } };
        }
    }
}
