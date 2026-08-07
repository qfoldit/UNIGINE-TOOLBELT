// qFoldIT Toolbelt for UNIGINE 2 — UagValidator.cs
//
// Same three checks as UNITY-TOOLBELT's validator (dangling references,
// parent_child cycles, gap reporting) — the algorithm was verified
// standalone via mcs/mono against 10 scenarios (24 assertions) before this
// file was written; only the per-engine MappedNodeTypes/MappedConstraintTypes
// sets below differ between the two toolbelts.

using System.Collections.Generic;
using System.Linq;
using QFoldIT.Toolbelt.Uag;

namespace QFoldIT.Toolbelt
{
    public class UagValidationResult
    {
        public bool IsValid => Errors.Count == 0;
        public List<string> Errors { get; } = new List<string>();
        public List<string> UnmappedNodeTypes { get; } = new List<string>();
        public List<string> UnmappedConstraintTypes { get; } = new List<string>();
        public List<UagInteraction> UnmappedInteractions { get; } = new List<UagInteraction>();
    }

    public static class UagValidator
    {
        // What UAGBridgeTools.cs actually knows how to realize in UNIGINE 2 today.
        public static readonly HashSet<string> MappedNodeTypes = new HashSet<string>
        {
            "mesh", "light", "camera", "trigger_volume", "ui_panel", "particle_emitter", "audio_source", "group"
            // Note: audio_source and particle_emitter require an explicit
            // properties.sound_path / properties.asset_ref respectively —
            // UNIGINE has no built-in generic preset for either the way
            // Unity's ParticleSystem/AudioSource defaults do. A node of
            // this type with no asset reference will fail at apply time
            // (surfaced in node_failures), even though the type itself is
            // "mapped" here.
        };

        public static readonly HashSet<string> MappedConstraintTypes = new HashSet<string>
        {
            "physics_collision"
        };

        public static UagValidationResult Validate(UagGraph graph)
        {
            var result = new UagValidationResult();
            var nodeIds = new HashSet<string>(graph.Nodes.Select(n => n.Id));

            var duplicateIds = graph.Nodes.GroupBy(n => n.Id).Where(g => g.Count() > 1).Select(g => g.Key);
            foreach (var dup in duplicateIds)
                result.Errors.Add($"Duplicate node id '{dup}'.");

            foreach (var node in graph.Nodes)
                if (!string.IsNullOrEmpty(node.ParentId) && !nodeIds.Contains(node.ParentId))
                    result.Errors.Add($"Node '{node.Id}' has parent_id '{node.ParentId}' which does not exist.");

            foreach (var conn in graph.Connections)
            {
                if (!nodeIds.Contains(conn.FromNode))
                    result.Errors.Add($"Connection '{conn.Id}' from_node '{conn.FromNode}' does not exist.");
                if (!nodeIds.Contains(conn.ToNode))
                    result.Errors.Add($"Connection '{conn.Id}' to_node '{conn.ToNode}' does not exist.");
            }

            foreach (var constraint in graph.Constraints)
                foreach (var target in constraint.TargetNodes)
                    if (!nodeIds.Contains(target))
                        result.Errors.Add($"Constraint '{constraint.Id}' target_node '{target}' does not exist.");

            foreach (var interaction in graph.Interactions)
                if (!nodeIds.Contains(interaction.TargetNode))
                    result.Errors.Add($"Interaction '{interaction.Id}' target_node '{interaction.TargetNode}' does not exist.");

            var parentOf = graph.Nodes.Where(n => !string.IsNullOrEmpty(n.ParentId) && nodeIds.Contains(n.ParentId))
                                       .ToDictionary(n => n.Id, n => n.ParentId);
            foreach (var start in nodeIds)
            {
                var visited = new HashSet<string> { start };
                var current = start;
                while (parentOf.TryGetValue(current, out var parent))
                {
                    if (!visited.Add(parent))
                    {
                        result.Errors.Add($"Cycle detected in parent_child hierarchy involving node '{start}'.");
                        break;
                    }
                    current = parent;
                }
            }

            foreach (var type in graph.Nodes.Select(n => n.Type).Distinct())
                if (!MappedNodeTypes.Contains(type))
                    result.UnmappedNodeTypes.Add(type);

            foreach (var type in graph.Constraints.Select(c => c.Type).Distinct())
                if (!MappedConstraintTypes.Contains(type))
                    result.UnmappedConstraintTypes.Add(type);

            result.UnmappedInteractions.AddRange(graph.Interactions);

            return result;
        }
    }
}
