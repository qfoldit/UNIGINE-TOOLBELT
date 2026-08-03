// qFoldIT Toolbelt for UNIGINE 2 — ToolRegistry.cs
//
// A minimal, explicit dispatch table: tool name -> delegate. Unlike Unity's
// McpToolRegistry (which discovers [McpTool]-attributed methods reflectively
// at startup), UNIGINE's MCPBridge Plugin does not currently document a
// public extension API, so qFoldIT tools register themselves explicitly
// here at plugin load time instead of relying on reflection.
//
// Each Tools/*.cs file calls ToolRegistry.Register(...) for every tool it
// exposes, from a static constructor or an explicit RegisterAll() call
// invoked once from your WorldLogic.Init().

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace QFoldIT.Toolbelt
{
    public readonly struct ToolInfo
    {
        public readonly string Name;
        public readonly string Category;
        public readonly string Description;
        public readonly Func<JObject, object> Handler;

        public ToolInfo(string name, string category, string description, Func<JObject, object> handler)
        {
            Name = name;
            Category = category;
            Description = description;
            Handler = handler;
        }
    }

    public static class ToolRegistry
    {
        private static readonly Dictionary<string, ToolInfo> _tools = new Dictionary<string, ToolInfo>();

        public static void Register(string name, string category, string description, Func<JObject, object> handler)
        {
            if (_tools.ContainsKey(name))
                throw new InvalidOperationException($"qFoldIT Toolbelt: duplicate tool name '{name}'.");
            _tools[name] = new ToolInfo(name, category, description, handler);
        }

        public static object Dispatch(string name, JObject parameters)
        {
            if (string.IsNullOrEmpty(name))
                return new { success = false, error = "No tool name provided." };

            if (!_tools.TryGetValue(name, out var tool))
                return new { success = false, error = $"Unknown tool '{name}'. Call list_toolbelt_tools to see what's available." };

            try
            {
                return tool.Handler(parameters ?? new JObject());
            }
            catch (Exception ex)
            {
                return new { success = false, error = $"Tool '{name}' threw: {ex.Message}" };
            }
        }

        public static List<object> ListTools()
        {
            var list = new List<object>();
            foreach (var kv in _tools)
                list.Add(new { name = kv.Value.Name, category = kv.Value.Category, description = kv.Value.Description });
            return list;
        }

        /// <summary>
        /// Call once at startup, after all Tools/*.cs static constructors
        /// have had a chance to run. Explicit RegisterAll() calls per file
        /// keep load order predictable (static-constructor timing in C#
        /// is otherwise lazy and easy to get wrong for a plugin entry point).
        /// </summary>
        public static void RegisterAll()
        {
            SceneTools.Register();
            MaterialTools.Register();
            ProceduralPlacementTools.Register();
            StampTools.Register();
            ProjectSetupTools.Register();
            WorldStateExportTools.Register();
            NodeCodeGenTools.Register();
            AssetTools.Register();
            ConsoleTools.Register();
        }
    }
}
