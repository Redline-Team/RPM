using System.IO;
using UnityEditor;
using UnityEngine;

namespace Redline.Editor 
{
    /// <summary>
    /// Window for creating new editor themes
    /// </summary>
    public class CreateThemeWindow : EditorWindow
    {
        private enum UnityTheme { FullDark, FullLight, Dark, Light, Both }

        private UnityTheme _unityTheme;
        private string _name = "EnterName";

        [MenuItem("Redline/Modules/Themer/Create Theme")]
        public static void ShowWindow()
        {
            ThemeSettings.ShowWindow();
            GetWindow<CreateThemeWindow>("Theme Settings");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();

            _name = EditorGUILayout.TextField(_name, GUILayout.Width(200));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preset:");

            _unityTheme = (UnityTheme)EditorGUILayout.EnumPopup(_unityTheme, GUILayout.Width(100));
            string description = _unityTheme switch
            {
                UnityTheme.FullDark => "Everything you need for a Dark Theme",
                UnityTheme.FullLight => "Everything you need for a Light Theme",
                UnityTheme.Light => "Minimalistic Preset for a Light Theme",
                UnityTheme.Dark => "Minimalistic Preset for a Dark Theme",
                UnityTheme.Both => "Minimalistic Preset for a Light & Dark Theme",
                _ => string.Empty
            };

            EditorGUILayout.LabelField(description);
            EditorGUILayout.Space();

            bool create = false;
            
            // Handle Enter key press
            Event e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Return)
            {
                create = true;
            }
            
            // Create button
            if (GUILayout.Button("Create Custom Theme", GUILayout.Width(200)))
            {
                create = true;
            }

            if (!create) return;
            
            // Check if theme already exists
            string path = ThemesUtility.CustomThemesPath + _name + ThemesUtility.Enc;
            if (File.Exists(path))
            {
                if (!EditorUtility.DisplayDialog("This Theme already exists", 
                    "Do you want to override the old Theme?", "Yes", "Cancel"))
                {
                    return;
                }
            }

            // Get preset name based on selected theme type
            string presetName = _unityTheme switch
            {
                UnityTheme.FullDark => "FullDark",
                UnityTheme.FullLight => "FullLight",
                UnityTheme.Light => "Light",
                UnityTheme.Dark => "Dark",
                UnityTheme.Both => "Both",
                _ => string.Empty
            };

            // Create and save the theme
            CustomTheme theme = FetchTheme(presetName, _name);
            if (theme != null)
            {
                ThemesUtility.SaveJsonFileForTheme(theme);
                ThemesUtility.OpenEditTheme(theme);
                Close();
            }
            else
            {
                EditorUtility.DisplayDialog("Error", 
                    $"Could not load preset theme: {presetName}", "OK");
            }
        }

        /// <summary>
        /// Loads a preset theme and assigns it a new name
        /// </summary>
        private static CustomTheme FetchTheme(string presetName, string newName)
        {
            CustomTheme customTheme = ThemesUtility.GetCustomThemeFromJson(ThemesUtility.PresetsPath + presetName + ThemesUtility.Enc);
            if (customTheme != null)
            {
                customTheme.Name = newName;
            }
            
            return customTheme;
        }
    }
}
