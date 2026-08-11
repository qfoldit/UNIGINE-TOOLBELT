// qFoldIT Toolbelt for UNIGINE 2 — UagValidator.cs
//
// Validates against qfoldit-engine-adapter-spec-v0.1's normative rules,
// emitting the same structured error CODES as the spec's own reference
// validator (conformance/run_conformance.py), verified directly against
// conformance/test_vectors.json — see tests/conformance/README.md.
// Identical algorithm to UNITY-TOOLBELT's UagValidator.cs; only the
// per-engine MappedNodeTypes/MappedConstraintTypes sets differ.

using System.Collections.Generic;
using System.Linq;
using QFoldIT.Toolbelt.Uag;

namespace QFoldIT.Toolbelt
{
    public readonly struct UagError
    {
        public readonly string Code;
        public readonly string Message;
        public UagError(string code, string message) { Code = code; Message = message; }
    }

    public class UagValidationResult
    {
        public bool IsValid => Errors.Count == 0;
        public List<UagError> Errors { get; } = new List<UagError>();
        public List<string> UnmappedNodeTypes { get; } = new List<string>();
        public List<string> UnmappedConstraintTypes { get; } = new List<string>();
        public List<UagInteraction> UnmappedInteractions { get; } = new List<UagInteraction>();
    }

    public static class UagValidator
    {
        // What UAGBridgeTools.cs actually knows how to realize in UNIGINE 2 today.
        public static readonly HashSet<string> MappedNodeTypes = new HashSet<string>
        {
            "mesh", "light", "camera", "trigger_volume", "ui_panel", "particle_emitter",
            "audio_source", "group",
            "molecular_structure", "interaction_zone"
            // Note: audio_source/particle_emitter/molecular_structure require
            // an explicit asset/source reference in properties — a specific
            // instance missing one fails at apply time (node_failures), even
            // though the type itself is mapped here (see docs/UAG_BRIDGE.md).
        };

        public static bool IsMappedNodeType(string type) =>
            type != null && (MappedNodeTypes.Contains(type) || type.StartsWith("scientific_subject/"));

        public static readonly HashSet<string> MappedConstraintTypes = new HashSet<string>
        {
            "physics_collision", "physics.collision", "physics.joint"
        };

        public static UagValidationResult Validate(UagGraph graph)
        {
            var result = new UagValidationResult();

            if (graph.Schema != UagGraph.SupportedSchema)
                result.Errors.Add(new UagError("INVALID_SCHEMA", $"Expected schema '{UagGraph.SupportedSchema}', got '{graph.Schema ?? "(missing)"}'."));

            var nodeIds = new HashSet<string>(graph.Nodes.Select(n => n.Id));

            var duplicateIds = graph.Nodes.GroupBy(n => n.Id).Where(g => g.Count() > 1).Select(g => g.Key);
            foreach (var dup in duplicateIds)
                result.Errors.Add(new UagError("DUPLICATE_NODE_ID", $"Duplicate node id '{dup}'."));

            foreach (var node in graph.Nodes)
                if (!string.IsNullOrEmpty(node.Parent) && !nodeIds.Contains(node.Parent))
                    result.Errors.Add(new UagError("DANGLING_PARENT", $"Node '{node.Id}' has parent '{node.Parent}' which does not exist."));

            foreach (var constraint in graph.Constraints)
                foreach (var target in constraint.TargetNodes)
                    if (!nodeIds.Contains(target))
                        result.Errors.Add(new UagError("DANGLING_REFERENCE", $"Constraint '{constraint.Id}' target_node '{target}' does not exist."));

            foreach (var interaction in graph.Interactions)
                if (!string.IsNullOrEmpty(interaction.Target) && !nodeIds.Contains(interaction.Target))
                    result.Errors.Add(new UagError("DANGLING_REFERENCE", $"Interaction '{interaction.Id}' target '{interaction.Target}' does not exist."));

            foreach (var binding in graph.Bindings)
                if (!string.IsNullOrEmpty(binding.Target) && !nodeIds.Contains(binding.Target))
                    result.Errors.Add(new UagError("DANGLING_REFERENCE", $"Binding '{binding.Id}' target '{binding.Target}' does not exist."));

            var parentOf = graph.Nodes.Where(n => !string.IsNullOrEmpty(n.Parent) && nodeIds.Contains(n.Parent))
                                       .ToDictionary(n => n.Id, n => n.Parent);
            var cycleAlreadyReportedFor = new HashSet<string>();
            foreach (var start in nodeIds)
            {
                var visited = new HashSet<string> { start };
                var current = start;
                while (parentOf.TryGetValue(current, out var parent))
                {
                    if (!visited.Add(parent))
                    {
                        if (cycleAlreadyReportedFor.Add(start))
                            result.Errors.Add(new UagError("HIERARCHY_CYCLE", $"Cycle detected in parent hierarchy involving node '{start}'."));
                        break;
                    }
                    current = parent;
                }
            }

            foreach (var type in graph.Nodes.Select(n => n.Type).Distinct())
                if (!IsMappedNodeType(type))
                    result.UnmappedNodeTypes.Add(type);

            foreach (var type in graph.Constraints.Select(c => c.Type).Distinct())
                if (!MappedConstraintTypes.Contains(type))
                    result.UnmappedConstraintTypes.Add(type);

            foreach (var interaction in graph.Interactions)
                if (!UAGBridgeMechanics.MappedInteractionTypes.Contains(interaction.Type))
                    result.UnmappedInteractions.Add(interaction);

            return result;
        }
    }
}
