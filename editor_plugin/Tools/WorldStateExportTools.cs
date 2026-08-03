// qFoldIT Toolbelt for UNIGINE 2 — WorldStateExportTools.cs
// Category: WorldState

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unigine;

namespace QFoldIT.Toolbelt
{
    public static class WorldStateExportTools
    {
        private class ExportedNode
        {
            public string Name;
            public string Type;
            public double[] Position;
            public bool Enabled;
            public string Parent;
        }

        private class ExportedWorld
        {
            public string WorldName;
            public string ExportedAtUtc;
            public int NodeCount;
            public List<ExportedNode> Nodes = new List<ExportedNode>();
        }

        public static void Register()
        {
            ToolRegistry.Register("world_state_export", "WorldState",
                "Exports every node in the loaded world (name, type, transform, parent) to a JSON file an AI agent can read for full level context.",
                WorldStateExport);
        }

        private static object WorldStateExport(JObject p)
        {
            string outputPath = (string)p["output_path"] ?? "docs/world_state.json";

            var export = new ExportedWorld
            {
                WorldName = World.GetName() ?? "unsaved",
                ExportedAtUtc = DateTime.UtcNow.ToString("o")
            };

            foreach (var n in UnigineCompat.GetAllWorldNodes())
            {
                var w = n.WorldPosition;
                export.Nodes.Add(new ExportedNode
                {
                    Name = n.Name,
                    Type = n.GetType().Name,
                    Position = new[] { w.x, w.y, w.z },
                    Enabled = n.Enabled,
                    Parent = n.Parent != null ? n.Parent.Name : ""
                });
            }
            export.NodeCount = export.Nodes.Count;

            string dataRoot = Engine.Get().GetSourceDataPath() ?? ".";
            string fullPath = Path.Combine(dataRoot, outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? ".");
            File.WriteAllText(fullPath, JsonConvert.SerializeObject(export, Formatting.Indented));

            return new { success = true, world = export.WorldName, node_count = export.NodeCount, path = outputPath };
        }
    }
}
