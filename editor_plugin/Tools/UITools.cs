// qFoldIT Toolbelt for UNIGINE 2 — UITools.cs
// Category: UI
// ⚠ UNIGINE's UI system is Widget-based (WidgetButton, WidgetLabel,
// WidgetSprite, WidgetSlider, WidgetWindow) added to Unigine.Gui.Get(),
// not a Canvas/RectTransform model like Unity. Positions are 2D screen-
// space pixel coordinates. Verify exact widget constructor signatures for
// your SDK version.

using Newtonsoft.Json.Linq;
using Unigine;

namespace QFoldIT.Toolbelt
{
    public static class UITools
    {
        public static void Register()
        {
            ToolRegistry.Register("ui_create_button", "UI",
                "Creates a WidgetButton with a label at a 2D screen position.",
                CreateButton);

            ToolRegistry.Register("ui_create_text", "UI",
                "Creates a WidgetLabel at a 2D screen position.",
                CreateText);

            ToolRegistry.Register("ui_create_panel", "UI",
                "Creates a background WidgetSprite/window panel at a 2D screen position.",
                CreatePanel);

            ToolRegistry.Register("ui_create_slider", "UI",
                "Creates a WidgetSlider at a 2D screen position.",
                CreateSlider);

            ToolRegistry.Register("ui_set_position", "UI",
                "Sets a UI widget's screen position by widget variable name tracked in the toolbelt's widget registry.",
                SetPosition);
        }

        // A simple name -> Widget registry so subsequent calls (ui_set_position,
        // future enable/disable tools) can find widgets created via this file.
        // Unigine's Gui system does not itself index widgets by string name.
        private static readonly System.Collections.Generic.Dictionary<string, Widget> _widgets =
            new System.Collections.Generic.Dictionary<string, Widget>();

        private static object CreateButton(JObject p)
        {
            string name = (string)p["name"] ?? $"Button_{_widgets.Count}";
            string label = (string)p["label"] ?? "Button";
            int x = (int?)p["x"] ?? 0, y = (int?)p["y"] ?? 0;

            var button = new WidgetButton(Gui.Get(), label);
            button.SetPosition(x, y);
            Gui.Get().AddChild(button, Gui.ALIGN_OVERLAP);
            _widgets[name] = button;

            return new { success = true, name, label };
        }

        private static object CreateText(JObject p)
        {
            string name = (string)p["name"] ?? $"Label_{_widgets.Count}";
            string text = (string)p["text"] ?? "Text";
            int x = (int?)p["x"] ?? 0, y = (int?)p["y"] ?? 0;
            int fontSize = (int?)p["font_size"] ?? 16;

            var label = new WidgetLabel(Gui.Get(), text);
            label.FontSize = fontSize;
            label.SetPosition(x, y);
            Gui.Get().AddChild(label, Gui.ALIGN_OVERLAP);
            _widgets[name] = label;

            return new { success = true, name, text };
        }

        private static object CreatePanel(JObject p)
        {
            string name = (string)p["name"] ?? $"Panel_{_widgets.Count}";
            int x = (int?)p["x"] ?? 0, y = (int?)p["y"] ?? 0;
            int width = (int?)p["width"] ?? 400, height = (int?)p["height"] ?? 300;

            var window = new WidgetWindow(Gui.Get(), "");
            window.SetPosition(x, y);
            window.SetWidth(width);
            window.SetHeight(height);
            Gui.Get().AddChild(window, Gui.ALIGN_OVERLAP);
            _widgets[name] = window;

            return new { success = true, name };
        }

        private static object CreateSlider(JObject p)
        {
            string name = (string)p["name"] ?? $"Slider_{_widgets.Count}";
            int x = (int?)p["x"] ?? 0, y = (int?)p["y"] ?? 0;
            float min = (float?)p["min"] ?? 0f, max = (float?)p["max"] ?? 1f, value = (float?)p["value"] ?? 0.5f;

            var slider = new WidgetSlider(Gui.Get());
            slider.MinValue = min;
            slider.MaxValue = max;
            slider.Value = value;
            slider.SetPosition(x, y);
            Gui.Get().AddChild(slider, Gui.ALIGN_OVERLAP);
            _widgets[name] = slider;

            return new { success = true, name, value };
        }

        private static object SetPosition(JObject p)
        {
            string name = (string)p["name"];
            int x = (int?)p["x"] ?? 0, y = (int?)p["y"] ?? 0;

            if (!_widgets.TryGetValue(name, out var widget))
                return new { success = false, error = $"No tracked widget named '{name}' (only widgets created via this file's tools are tracked)." };

            widget.SetPosition(x, y);
            return new { success = true, name, x, y };
        }
    }
}
