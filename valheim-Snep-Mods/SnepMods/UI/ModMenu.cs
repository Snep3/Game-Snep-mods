using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace SnepMods.UI
{
    // A single menu (Shift+P) listing every mod in the pack, grouped by each
    // feature's config section. Picking a mod opens its own settings page.
    internal static class ModMenu
    {
        private static ConfigFile _config;
        private static bool _visible;
        private static string _selectedSection;
        private static ConfigEntry<KeyboardShortcut> _capturingEntry;
        private static Rect _windowRect = new Rect(20, 20, 380, 400);
        private static GUIStyle _titleStyle;

        public static void Init(ConfigFile config)
        {
            _config = config;
        }

        public static void Update()
        {
            bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (shiftHeld && Input.GetKeyDown(KeyCode.P))
            {
                SetVisible(!_visible);
            }

            if (_visible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (_visible && _capturingEntry != null && Input.GetKeyDown(KeyCode.Escape))
            {
                _capturingEntry.Value = KeyboardShortcut.Empty;
                _capturingEntry = null;
            }
            else if (_visible && _capturingEntry != null && Input.anyKeyDown)
            {
                foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
                {
                    if (!Input.GetKeyDown(keyCode) || IsModifierKey(keyCode))
                    {
                        continue;
                    }

                    _capturingEntry.Value = new KeyboardShortcut(keyCode);
                    _capturingEntry = null;
                    break;
                }
            }
        }

        public static void OnGUI()
        {
            if (!_visible)
            {
                return;
            }

            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 14 };
            }

            _windowRect = GUILayout.Window(0x53656E70, _windowRect, DrawWindow, "SnepMods");
        }

        private static void SetVisible(bool visible)
        {
            _visible = visible;
            _capturingEntry = null;
            if (visible)
            {
                _selectedSection = null;
            }

            Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = visible;
        }

        private static void DrawWindow(int id)
        {
            if (_selectedSection == null)
            {
                DrawModList();
            }
            else
            {
                DrawModConfig(_selectedSection);
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        private static void DrawModList()
        {
            GUILayout.Label("Installed mods", _titleStyle);
            GUILayout.Space(6);

            foreach (string section in GetSections())
            {
                if (GUILayout.Button(section))
                {
                    _selectedSection = section;
                }
            }
        }

        private static void DrawModConfig(string section)
        {
            if (GUILayout.Button("< Back"))
            {
                _selectedSection = null;
                return;
            }

            GUILayout.Label(section, _titleStyle);
            GUILayout.Space(6);

            foreach (ConfigDefinition definition in _config.Keys.Where(d => d.Section == section).ToList())
            {
                DrawEntry(_config[definition]);
            }
        }

        private static void DrawEntry(ConfigEntryBase entry)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(entry.Definition.Key, GUILayout.Width(140));

            if (entry.SettingType == typeof(bool))
            {
                var boolEntry = (ConfigEntry<bool>)entry;
                boolEntry.Value = GUILayout.Toggle(boolEntry.Value, boolEntry.Value ? "On" : "Off");
            }
            else if (entry.SettingType == typeof(float))
            {
                var floatEntry = (ConfigEntry<float>)entry;
                float min = 0f;
                float max = 1f;
                if (entry.Description.AcceptableValues is AcceptableValueRange<float> range)
                {
                    min = range.MinValue;
                    max = range.MaxValue;
                }

                floatEntry.Value = GUILayout.HorizontalSlider(floatEntry.Value, min, max, GUILayout.Width(120));
                GUILayout.Label(floatEntry.Value.ToString("0.00"), GUILayout.Width(40));
            }
            else if (entry.SettingType == typeof(KeyboardShortcut))
            {
                var keyEntry = (ConfigEntry<KeyboardShortcut>)entry;
                if (_capturingEntry == keyEntry)
                {
                    GUILayout.Label("press a key...");
                }
                else if (GUILayout.Button(keyEntry.Value.ToString()))
                {
                    _capturingEntry = keyEntry;
                }
            }
            else
            {
                GUILayout.Label(entry.BoxedValue?.ToString() ?? "");
            }

            GUILayout.EndHorizontal();
        }

        private static IEnumerable<string> GetSections()
        {
            var seen = new HashSet<string>();
            foreach (ConfigDefinition definition in _config.Keys)
            {
                if (seen.Add(definition.Section))
                {
                    yield return definition.Section;
                }
            }
        }

        private static bool IsModifierKey(KeyCode keyCode)
        {
            switch (keyCode)
            {
                case KeyCode.LeftShift:
                case KeyCode.RightShift:
                case KeyCode.LeftControl:
                case KeyCode.RightControl:
                case KeyCode.LeftAlt:
                case KeyCode.RightAlt:
                case KeyCode.Mouse0:
                    return true;
                default:
                    return false;
            }
        }
    }
}
