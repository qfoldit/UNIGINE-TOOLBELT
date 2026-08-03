// qFoldIT Toolbelt for UNIGINE 2 — StampTools.cs
// Category: Stamps
// NOTE: "current selection" in the Editor is exposed through the Editor
// scripting API (Unigine.Editor.SelectorSystem / EditorInterface, name
// varies by SDK version) rather than the runtime World API used elsewhere
// in this file. Verify the exact selection-access call for your installed
// SDK — this file isolates that lookup in GetEditorSelection() below so
// only one method needs adjusting.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unigine;

namespace QFoldIT.Toolbelt
{
    public static class StampTools
    {
        private class StampEntry
        {
            public string NodePath;   // asset path if the node came from a .node file, else null
            public double Px, Py, Pz; // position relative to pivot
            public float Rx, Ry, Rz;
            public float Scale;
        }

        private class StampFile
        {
            public string Name;
            public List<StampEntry> Entries = new List<StampEntry>();
        }

        private static string StampDir =>
            Path.Combine(Engine.Get().GetSourceDataPath() ?? ".", "Saved", "QFoldIT_Toolbelt", "stamps");

        public static void Register()
        {
            ToolRegistry.Register("stamp_save", "Stamps",
                "Saves the currently selected nodes in the Editor as a reusable stamp, positions stored relative to their combined pivot.",
                StampSave);

            ToolRegistry.Register("stamp_place", "Stamps",
                "Places a previously saved stamp at a world position, with an optional yaw rotation applied to the whole group.",
                StampPlace);

            ToolRegistry.Register("stamp_list", "Stamps",
                "Lists every stamp saved so far.",
                StampList);
        }

        /// <summary>
        /// Isolated so only this method needs adjusting per SDK version's
        /// Editor selection API.
        /// </summary>
        private static Node[] GetEditorSelection()
        {
            // Common shape across recent UNIGINE 2 Editor scripting APIs:
            // Unigine.Editor.SelectorSystem.Get().GetSelected() -> NodePtr[]
            // Adjust the namespace/type below if your SDK exposes it differently.
            return Unigine.Editor.SelectorSystem.Get().GetSelected();
        }

        private static object StampSave(JObject p)
        {
            string name = (string)p["name"];
            var selection = GetEditorSelection();
            if (selection == null || selection.Length == 0)
                return new { success = false, error = "Nothing selected in the Editor." };

            double px = 0, py = 0, pz = 0;
            foreach (var n in selection) { var w = n.WorldPosition; px += w.x; py += w.y; pz += w.z; }
            px /= selection.Length; py /= selection.Length; pz /= selection.Length;

            var stamp = new StampFile { Name = name };
            foreach (var n in selection)
            {
                var w = n.WorldPosition;
                stamp.Entries.Add(new StampEntry
                {
                    NodePath = null, // populated if you track source asset paths on your nodes
                    Px = w.x - px, Py = w.y - py, Pz = w.z - pz,
                    Rx = 0, Ry = 0, Rz = 0, // extend with actual rotation extraction from WorldTransform if needed
                    Scale = 1f
                });
            }

            Directory.CreateDirectory(StampDir);
            var path = Path.Combine(StampDir, $"{name}.json");
            File.WriteAllText(path, JsonConvert.SerializeObject(stamp, Formatting.Indented));

            return new { success = true, name, objects_saved = stamp.Entries.Count, path };
        }

        private static object StampPlace(JObject p)
        {
            string name = (string)p["name"];
            double x = (double?)p["x"] ?? 0;
            double y = (double?)p["y"] ?? 0;
            double z = (double?)p["z"] ?? 0;
            float yawOffset = (float?)p["yaw_offset"] ?? 0f;

            var path = Path.Combine(StampDir, $"{name}.json");
            if (!File.Exists(path)) return new { success = false, error = $"Stamp '{name}' not found." };

            var stamp = JsonConvert.DeserializeObject<StampFile>(File.ReadAllText(path));
            var created = new List<string>();
            double yawRad = yawOffset * Math.PI / 180.0;

            foreach (var e in stamp.Entries)
            {
                double rx = e.Px * Math.Cos(yawRad) - e.Pz * Math.Sin(yawRad);
                double rz = e.Px * Math.Sin(yawRad) + e.Pz * Math.Cos(yawRad);

                Node node = !string.IsNullOrEmpty(e.NodePath)
                    ? Unigine.World.LoadNode(e.NodePath)
                    : UnigineCompat.CreatePrimitive("box", 0, 0, 0, e.Scale);

                UnigineCompat.SetWorldPosition(node, x + rx, y + e.Py, z + rz);
                UnigineCompat.SetEulerRotation(node, e.Rx, e.Ry + yawOffset, e.Rz);
                node.Name = $"{stamp.Name}_{created.Count:D2}";
                created.Add(node.Name);
            }

            return new { success = true, name, placed_count = created.Count, names = created };
        }

        private static object StampList(JObject p)
        {
            if (!Directory.Exists(StampDir)) return new { success = true, stamps = Array.Empty<string>() };
            var stamps = Directory.GetFiles(StampDir, "*.json").Select(f => Path.GetFileNameWithoutExtension(f)).ToArray();
            return new { success = true, stamps };
        }
    }
}
