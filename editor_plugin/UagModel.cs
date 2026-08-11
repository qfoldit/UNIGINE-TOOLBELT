// qFoldIT Toolbelt for UNIGINE 2 — UagModel.cs
//
// Data model for qFoldIT's Universal Assembly Graph, conforming to the
// FORMAL, normative schema shipped in qfoldit-engine-adapter-spec-v0.1
// (schemas/uag.schema.json) — supersedes the informal markdown-derived
// model this file was originally built against in Phase 1. Identical
// shape to UNITY-TOOLBELT's Editor/Core/UagModel.cs by design, so a
// single UAG document is byte-for-byte interchangeable between the two
// engine adapters. Key differences from the earlier Phase-1 shape:
//
//   - Top-level "schema" (const "qfoldit.uag/0.1"), not "uag_version".
//   - A required "scene" object ({id, name?, metadata?}), not absent.
//   - "nodes[].parent" (a single string/null), not "parent_id".
//   - No "connections[]" array at all — hierarchy is expressed purely via
//     node.parent.
//   - A new "bindings[]" array: {id, source, target}, binding a node to a
//     live scientific-state URI.
//
// interactions[]/constraints[]/bindings[] internal shape is NOT mandated
// by schemas/uag.schema.json (only that they're arrays of objects). This
// file implements the same interpretation as the Unity adapter, informed
// by qfoldit-scientific-gameplay-framework-v0.1's reference/compiler.py:
//   interactions: [{id, type, target}]      (single "target")
//   bindings:     [{id, source, target}]
//   constraints:  {id, type, target_nodes[], properties} for
//                 physics-flavoured constraints.

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QFoldIT.Toolbelt.Uag
{
    public class UagScene
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("metadata")] public JObject Metadata { get; set; } = new JObject();
    }

    public class UagNode
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("type")] public string Type { get; set; }
        [JsonProperty("parent")] public string Parent { get; set; }
        [JsonProperty("transform")] public JObject Transform { get; set; } = new JObject();
        [JsonProperty("properties")] public JObject Properties { get; set; } = new JObject();
        [JsonProperty("metadata")] public JObject Metadata { get; set; } = new JObject();

        public float[] Position => ReadFloatArray("position", new float[] { 0, 0, 0 });
        public float[] RotationEulerDeg => ReadFloatArray("rotation_euler_deg", new float[] { 0, 0, 0 });
        public float[] Scale => ReadFloatArray("scale", new float[] { 1, 1, 1 });

        private float[] ReadFloatArray(string key, float[] fallback)
        {
            if (Transform == null || !Transform.TryGetValue(key, out var token) || token.Type != JTokenType.Array)
                return fallback;
            var arr = (JArray)token;
            var result = new float[arr.Count];
            for (int i = 0; i < arr.Count; i++) result[i] = (float)arr[i];
            return result;
        }
    }

    public class UagConstraint
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("type")] public string Type { get; set; }
        [JsonProperty("target_nodes")] public List<string> TargetNodes { get; set; } = new List<string>();
        [JsonProperty("properties")] public JObject Properties { get; set; } = new JObject();
    }

    public class UagInteraction
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("type")] public string Type { get; set; }
        [JsonProperty("target")] public string Target { get; set; }
        [JsonProperty("properties")] public JObject Properties { get; set; } = new JObject();
    }

    public class UagBinding
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("source")] public string Source { get; set; }
        [JsonProperty("target")] public string Target { get; set; }
        [JsonProperty("properties")] public JObject Properties { get; set; } = new JObject();
    }

    public class UagGraph
    {
        [JsonProperty("schema")] public string Schema { get; set; }
        [JsonProperty("scene")] public UagScene Scene { get; set; } = new UagScene();
        [JsonProperty("nodes")] public List<UagNode> Nodes { get; set; } = new List<UagNode>();
        [JsonProperty("constraints")] public List<UagConstraint> Constraints { get; set; } = new List<UagConstraint>();
        [JsonProperty("interactions")] public List<UagInteraction> Interactions { get; set; } = new List<UagInteraction>();
        [JsonProperty("bindings")] public List<UagBinding> Bindings { get; set; } = new List<UagBinding>();
        [JsonProperty("metadata")] public JObject Metadata { get; set; } = new JObject();

        public const string SupportedSchema = "qfoldit.uag/0.1";

        public static UagGraph Parse(string json) =>
            JsonConvert.DeserializeObject<UagGraph>(json) ?? new UagGraph();
    }
}
