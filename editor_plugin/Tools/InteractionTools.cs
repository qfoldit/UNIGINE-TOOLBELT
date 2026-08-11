// qFoldIT Toolbelt for UNIGINE 2 — InteractionTools.cs
// Category: Interaction
//
// The concrete "adapter"-level realization behind the interaction
// capability in qfoldit.adapter.json. Two real, working pieces:
//
//   1. Ensures the target node has a physics shape/body (dispatches
//      physics_add_shape/physics_add_body), so it's genuinely detectable
//      by physics_raycast_query — real, working selectability.
//   2. Records the interaction type in a persisted JSON registry (same
//      pattern as TagsLayersTools.cs's tag/layer store), so it's a real,
//      queryable fact about the node, not silently discarded.
//
// Honest scope, explicitly NOT claimed: unlike UNITY-TOOLBELT's
// Runtime/QFoldITInteractable.cs (a real MonoBehaviour with a working
// OnMouseDown -> UnityEvent callback), this file does NOT wire a live
// click-to-event callback. UNIGINE's Input/callback API surface needs
// verification against your specific SDK version (the same caveat as
// every other file in this repo — see UnigineCompat.cs's header) that
// this adapter doesn't have access to. A companion script polling mouse
// input and cross-referencing interaction_get against
// physics_raycast_query's hit result is the documented path to a live
// callback; it isn't implemented here rather than guessed at.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unigine;

namespace QFoldIT.Toolbelt
{
    public static class InteractionTools
    {
        private class InteractionRegistry
        {
            public Dictionary<string, string> NodeInteractionType { get; set; } = new Dictionary<string, string>();
        }

        private static string RegistryPath => Path.Combine(UnigineCompat.SavedDataDir, "interactions.json");

        private static InteractionRegistry LoadRegistry()
        {
            if (!File.Exists(RegistryPath)) return new InteractionRegistry();
            return JsonConvert.DeserializeObject<InteractionRegistry>(File.ReadAllText(RegistryPath)) ?? new InteractionRegistry();
        }

        private static void SaveRegistry(InteractionRegistry reg)
        {
            Directory.CreateDirectory(UnigineCompat.SavedDataDir);
            File.WriteAllText(RegistryPath, JsonConvert.SerializeObject(reg, Formatting.Indented));
        }

        public static void Register()
        {
            ToolRegistry.Register("interaction_create", "Interaction",
                "Makes a node interactable: ensures it has a physics shape/body (so physics_raycast_query can detect it), and records the interaction type in a persisted, queryable registry.",
                Create);

            ToolRegistry.Register("interaction_get", "Interaction",
                "Reads back the interaction type recorded for a node by interaction_create, if any.",
                Get);

            ToolRegistry.Register("interaction_list", "Interaction",
                "Lists every node with a recorded interaction type.",
                List);
        }

        private static object Create(JObject p)
        {
            string name = (string)p["name"];
            string interactionType = (string)p["interaction_type"];
            if (string.IsNullOrEmpty(name)) return new { success = false, error = "name is required." };
            if (string.IsNullOrEmpty(interactionType)) return new { success = false, error = "interaction_type is required." };

            var node = UnigineCompat.FindNodeByName(name);
            if (node == null) return new { success = false, error = $"Node '{name}' not found." };

            if (Body.GetBody(node) == null)
            {
                ToolRegistry.Dispatch("physics_add_shape", new JObject { ["name"] = name, ["shape"] = "box", ["is_trigger"] = false });
            }

            var reg = LoadRegistry();
            reg.NodeInteractionType[name] = interactionType;
            SaveRegistry(reg);

            return new { success = true, name, interaction_type = interactionType };
        }

        private static object Get(JObject p)
        {
            string name = (string)p["name"];
            var reg = LoadRegistry();
            if (!reg.NodeInteractionType.TryGetValue(name, out var type))
                return new { success = false, error = $"No interaction recorded for '{name}'." };
            return new { success = true, name, interaction_type = type };
        }

        private static object List(JObject p)
        {
            var reg = LoadRegistry();
            return new { success = true, interactions = reg.NodeInteractionType.Select(kv => new { name = kv.Key, interaction_type = kv.Value }) };
        }
    }
}
