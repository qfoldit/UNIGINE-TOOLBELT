// qFoldIT Toolbelt for UNIGINE 2 — UAGBridgeMechanics.cs
//
// Shared interaction-type vocabulary — identical to UNITY-TOOLBELT's
// Editor/Core/UAGBridgeMechanics.cs by design. See that file's header for
// the full rationale (the 10 gameplay-pattern mechanics from
// qfoldit-scientific-gameplay-framework-v0.1 plus legacy trigger names).

using System.Collections.Generic;

namespace QFoldIT.Toolbelt
{
    public static class UAGBridgeMechanics
    {
        public static readonly HashSet<string> GameplayMechanics = new HashSet<string>
        {
            "construction", "optimization", "pattern_matching", "rhythm",
            "survival_defense", "racing_tuning", "spatial_puzzle",
            "portal_exploration", "investigation_annotation", "competitive_microtasks"
        };

        public static readonly HashSet<string> LegacyTriggers = new HashSet<string>
        {
            "on_grab", "on_proximity", "on_gaze", "on_click", "on_timer", "selection"
        };

        public static readonly HashSet<string> MappedInteractionTypes =
            new HashSet<string>(System.Linq.Enumerable.Concat(GameplayMechanics, LegacyTriggers));
    }
}
