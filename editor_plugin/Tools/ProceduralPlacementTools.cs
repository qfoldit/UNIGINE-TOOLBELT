// qFoldIT Toolbelt for UNIGINE 2 — ProceduralPlacementTools.cs
// Category: Procedural / Arena

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Unigine;

namespace QFoldIT.Toolbelt
{
    public static class ProceduralPlacementTools
    {
        public static void Register()
        {
            ToolRegistry.Register("procedural_place", "Procedural",
                "Places N copies of a primitive (or a loaded .node asset) using one of 8 geometric patterns: grid, circle, arc, spiral, line, wave, helix, radial.",
                ProceduralPlace);

            ToolRegistry.Register("arena_generate", "Procedural",
                "Generates a symmetrical Red-vs-Blue competitive arena: floor plane, boundary, and spawn markers, auto-split by team color.",
                ArenaGenerate);
        }

        private static object ProceduralPlace(JObject p)
        {
            string nodePath = (string)p["node_path"];   // optional .node asset; falls back to a box primitive
            string pattern = ((string)p["pattern"] ?? "grid").ToLowerInvariant();
            int count = (int?)p["count"] ?? 12;
            double radius = (double?)p["radius"] ?? 5;
            double cx = (double?)p["center_x"] ?? 0;
            double cy = (double?)p["center_y"] ?? 0;
            double cz = (double?)p["center_z"] ?? 0;
            string prefix = (string)p["name_prefix"] ?? "PropPattern";

            var positions = ComputePositions(pattern, Math.Max(1, count), radius, cx, cy, cz);
            var created = new List<string>();

            for (int i = 0; i < positions.Count; i++)
            {
                var (x, y, z) = positions[i];
                Node node;
                if (!string.IsNullOrEmpty(nodePath))
                {
                    node = Unigine.World.LoadNode(nodePath);
                    UnigineCompat.SetWorldPosition(node, x, y, z);
                }
                else
                {
                    node = UnigineCompat.CreatePrimitive("box", x, y, z, 1f);
                }
                node.Name = $"{prefix}_{i:D3}";
                created.Add(node.Name);
            }

            return new { success = true, pattern, placed_count = created.Count, names = created };
        }

        private static List<(double, double, double)> ComputePositions(string pattern, int count, double radius, double cx, double cy, double cz)
        {
            var result = new List<(double, double, double)>(count);
            switch (pattern)
            {
                case "grid":
                    int cols = (int)Math.Ceiling(Math.Sqrt(count));
                    for (int i = 0; i < count; i++)
                    {
                        int row = i / cols, col = i % cols;
                        result.Add((cx + col * radius, cy, cz + row * radius));
                    }
                    break;

                case "circle":
                    for (int i = 0; i < count; i++)
                    {
                        double a = 2 * Math.PI * i / count;
                        result.Add((cx + Math.Cos(a) * radius, cy, cz + Math.Sin(a) * radius));
                    }
                    break;

                case "arc":
                    for (int i = 0; i < count; i++)
                    {
                        double a = Math.PI * i / Math.Max(1, count - 1);
                        result.Add((cx + Math.Cos(a) * radius, cy, cz + Math.Sin(a) * radius));
                    }
                    break;

                case "spiral":
                    for (int i = 0; i < count; i++)
                    {
                        double t = i / (double)count;
                        double a = t * Math.PI * 6;
                        double r = t * radius;
                        result.Add((cx + Math.Cos(a) * r, cy, cz + Math.Sin(a) * r));
                    }
                    break;

                case "line":
                    for (int i = 0; i < count; i++)
                        result.Add((cx + i * radius, cy, cz));
                    break;

                case "wave":
                    for (int i = 0; i < count; i++)
                        result.Add((cx + i * radius, cy + Math.Sin(i * 0.6) * radius * 0.5, cz));
                    break;

                case "helix":
                    for (int i = 0; i < count; i++)
                    {
                        double a = i * 0.6;
                        result.Add((cx + Math.Cos(a) * radius, cy + i * (radius * 0.25), cz + Math.Sin(a) * radius));
                    }
                    break;

                case "radial":
                    int rings = Math.Max(1, (int)Math.Ceiling(count / 8.0));
                    int idx = 0;
                    for (int ring = 1; ring <= rings && idx < count; ring++)
                    {
                        int perRing = Math.Min(8 * ring, count - idx);
                        for (int i = 0; i < perRing; i++)
                        {
                            double a = 2 * Math.PI * i / perRing;
                            result.Add((cx + Math.Cos(a) * radius * ring, cy, cz + Math.Sin(a) * radius * ring));
                            idx++;
                        }
                    }
                    break;

                default:
                    throw new ArgumentException($"Unknown pattern '{pattern}'. Valid: grid, circle, arc, spiral, line, wave, helix, radial.");
            }
            return result;
        }

        private static object ArenaGenerate(JObject p)
        {
            string size = ((string)p["size"] ?? "medium").ToLowerInvariant();
            double cx = (double?)p["center_x"] ?? 0;
            double cz = (double?)p["center_z"] ?? 0;

            double halfExtent = size switch { "small" => 15, "medium" => 25, "large" => 40, _ => 25 };
            int spawnsPerTeam = size switch { "small" => 3, "medium" => 6, "large" => 10, _ => 6 };

            var floor = UnigineCompat.CreatePrimitive("plane", cx, 0, cz, (float)(halfExtent / 5), $"Arena_Floor_{size}");

            int total = 0;
            for (int i = 0; i < spawnsPerTeam; i++)
            {
                double t = (i + 0.5) / spawnsPerTeam;
                double x = cx + Lerp(-halfExtent * 0.8, halfExtent * 0.8, t);

                var red = UnigineCompat.CreatePrimitive("cylinder", x, 0.1, cz - halfExtent * 0.85, 1f, $"RedSpawn_{i:D2}");
                UnigineCompat.ApplyMaterialColor((ObjectMeshStatic)red, 0, 0.85f, 0.15f, 0.1f, 1f, false, 0f);

                var blue = UnigineCompat.CreatePrimitive("cylinder", x, 0.1, cz + halfExtent * 0.85, 1f, $"BlueSpawn_{i:D2}");
                UnigineCompat.ApplyMaterialColor((ObjectMeshStatic)blue, 0, 0.1f, 0.4f, 0.85f, 1f, false, 0f);

                total += 2;
            }

            return new { success = true, size, spawns_created = total, half_extent = halfExtent, floor = floor.Name };
        }

        private static double Lerp(double a, double b, double t) => a + (b - a) * t;
    }
}
