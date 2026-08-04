// qFoldIT Toolbelt for UNIGINE 2 — ComponentTools.cs
// Category: Components
// ⚠ UNIGINE 2's C# component system (Unigine.Component base class,
// [Component] attribute, node.AddComponent<T>()) is broadly analogous to
// Unity's MonoBehaviour components, but was introduced later in the 2.x
// line — confirm your SDK version supports it before relying on this file.
// Reflection is used here the same way as the Unity toolbelt's
// ComponentTools.cs, for the same reason: one generic tool set instead of
// per-type dedicated tools.

using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Unigine;

namespace QFoldIT.Toolbelt
{
    public static class ComponentTools
    {
        public static void Register()
        {
            ToolRegistry.Register("component_add", "Components",
                "Adds a C# Component to a node by type name (project-defined Unigine.Component subclasses).",
                Add);

            ToolRegistry.Register("component_remove", "Components",
                "Removes the first matching component of the given type name from a node.",
                Remove);

            ToolRegistry.Register("component_set_property", "Components",
                "Sets a public field or property on a component via reflection.",
                SetProperty);

            ToolRegistry.Register("component_get_property", "Components",
                "Reads a public field or property value from a component via reflection.",
                GetProperty);

            ToolRegistry.Register("component_list", "Components",
                "Lists every Component attached to a node by type name.",
                List);
        }

        private static System.Type ResolveComponentType(string typeName)
        {
            return System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return System.Type.EmptyTypes; } })
                .FirstOrDefault(t => t.Name == typeName && typeof(Component).IsAssignableFrom(t));
        }

        private static object Add(JObject p)
        {
            string name = (string)p["name"];
            string componentType = (string)p["component_type"];

            var node = UnigineCompat.FindNodeByName(name);
            if (node == null) return new { success = false, error = $"Node '{name}' not found." };

            var type = ResolveComponentType(componentType);
            if (type == null) return new { success = false, error = $"Component type '{componentType}' not found. Only project-defined Unigine.Component subclasses are discoverable this way." };

            var addMethod = typeof(Node).GetMethod("AddComponent", System.Type.EmptyTypes)?.MakeGenericMethod(type);
            if (addMethod == null) return new { success = false, error = "Node.AddComponent<T>() not found on this SDK version." };

            var comp = addMethod.Invoke(node, null);
            return new { success = comp != null, name, component_type = componentType };
        }

        private static object Remove(JObject p)
        {
            string name = (string)p["name"];
            string componentType = (string)p["component_type"];

            var node = UnigineCompat.FindNodeByName(name);
            if (node == null) return new { success = false, error = $"Node '{name}' not found." };

            var type = ResolveComponentType(componentType);
            if (type == null) return new { success = false, error = $"Component type '{componentType}' not found." };

            var getMethod = typeof(Node).GetMethod("GetComponent", System.Type.EmptyTypes)?.MakeGenericMethod(type);
            var comp = getMethod?.Invoke(node, null) as Component;
            if (comp == null) return new { success = false, error = $"'{name}' has no component of type '{componentType}'." };

            comp.DeleteForce();
            return new { success = true, name, removed = componentType };
        }

        private static object SetProperty(JObject p)
        {
            string name = (string)p["name"];
            string componentType = (string)p["component_type"];
            string fieldName = (string)p["field_name"];
            string value = (string)p["value"];

            var node = UnigineCompat.FindNodeByName(name);
            if (node == null) return new { success = false, error = $"Node '{name}' not found." };

            var type = ResolveComponentType(componentType);
            if (type == null) return new { success = false, error = $"Component type '{componentType}' not found." };

            var getMethod = typeof(Node).GetMethod("GetComponent", System.Type.EmptyTypes)?.MakeGenericMethod(type);
            var comp = getMethod?.Invoke(node, null);
            if (comp == null) return new { success = false, error = $"'{name}' has no component of type '{componentType}'." };

            var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (field != null) { field.SetValue(comp, ConvertValue(value, field.FieldType)); return new { success = true, name, field = fieldName, value }; }

            var prop = type.GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.CanWrite) { prop.SetValue(comp, ConvertValue(value, prop.PropertyType)); return new { success = true, name, field = fieldName, value }; }

            return new { success = false, error = $"No writable field/property '{fieldName}' on '{componentType}'." };
        }

        private static object GetProperty(JObject p)
        {
            string name = (string)p["name"];
            string componentType = (string)p["component_type"];
            string fieldName = (string)p["field_name"];

            var node = UnigineCompat.FindNodeByName(name);
            if (node == null) return new { success = false, error = $"Node '{name}' not found." };

            var type = ResolveComponentType(componentType);
            if (type == null) return new { success = false, error = $"Component type '{componentType}' not found." };

            var getMethod = typeof(Node).GetMethod("GetComponent", System.Type.EmptyTypes)?.MakeGenericMethod(type);
            var comp = getMethod?.Invoke(node, null);
            if (comp == null) return new { success = false, error = $"'{name}' has no component of type '{componentType}'." };

            var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (field != null) return new { success = true, value = field.GetValue(comp)?.ToString() };

            var prop = type.GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.CanRead) return new { success = true, value = prop.GetValue(comp)?.ToString() };

            return new { success = false, error = $"No readable field/property '{fieldName}' on '{componentType}'." };
        }

        private static object List(JObject p)
        {
            string name = (string)p["name"];
            var node = UnigineCompat.FindNodeByName(name);
            if (node == null) return new { success = false, error = $"Node '{name}' not found." };

            // Node.GetComponents() (no generic arg) — verify exact accessor
            // name for your SDK version if this differs.
            var listMethod = typeof(Node).GetMethod("GetComponents", System.Type.EmptyTypes);
            var components = listMethod?.Invoke(node, null) as System.Collections.IEnumerable;
            var names = components?.Cast<object>().Select(c => c.GetType().Name).ToArray() ?? new string[0];

            return new { success = true, name, components = names };
        }

        private static object ConvertValue(string raw, System.Type targetType)
        {
            if (targetType == typeof(float)) return float.Parse(raw);
            if (targetType == typeof(int)) return int.Parse(raw);
            if (targetType == typeof(bool)) return bool.Parse(raw);
            if (targetType == typeof(string)) return raw;
            return raw;
        }
    }
}
