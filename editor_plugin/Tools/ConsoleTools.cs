// qFoldIT Toolbelt for UNIGINE 2 — ConsoleTools.cs
// Category: BuildConsole

using Newtonsoft.Json.Linq;
using Unigine;

namespace QFoldIT.Toolbelt
{
    public static class ConsoleTools
    {
        public static void Register()
        {
            ToolRegistry.Register("console_run_command", "BuildConsole",
                "Runs a UNIGINE console command string (e.g. 'render_show_grid 1') — a generic escape hatch for actions not covered by a dedicated tool.",
                RunCommand);

            ToolRegistry.Register("console_get_variable", "BuildConsole",
                "Reads the current value of a UNIGINE console variable.",
                GetVariable);

            ToolRegistry.Register("world_save", "BuildConsole",
                "Saves the currently loaded world to disk, optionally to a new path.",
                WorldSave);
        }

        private static object RunCommand(JObject p)
        {
            string command = (string)p["command"];
            if (string.IsNullOrEmpty(command)) return new { success = false, error = "command is required." };

            Console.Run(command);
            return new { success = true, command };
        }

        private static object GetVariable(JObject p)
        {
            string variable = (string)p["variable"];
            if (string.IsNullOrEmpty(variable)) return new { success = false, error = "variable is required." };

            // ConsoleVariable read-back API varies slightly by SDK version;
            // Console.GetString / Console.GetFloat cover the common cases.
            string value = Console.GetString(variable);
            return new { success = true, variable, value };
        }

        private static object WorldSave(JObject p)
        {
            string path = (string)p["path"]; // null/empty = save to current world path
            bool ok = string.IsNullOrEmpty(path) ? World.Save() : World.Save(path);
            return new { success = ok, path = string.IsNullOrEmpty(path) ? World.GetName() : path };
        }
    }
}
