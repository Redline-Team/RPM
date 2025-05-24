using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Redline.Editor
{
    /// <summary>
    /// Utility class for managing custom Unity Editor themes
    /// </summary>
    public static class ThemesUtility
    {
        // File paths
        public const string CustomThemesPath = "Packages/dev.redline-team.rpm/Redline/Scripts/Editor/Modules/Themer/Resources/Themes/";
        private const string UssFilePath = "Packages/dev.redline-team.rpm/Redline/Scripts/Editor/Modules/Themer/Resources/StyleSheets/Extensions/";
        public const string PresetsPath = "Packages/dev.redline-team.rpm/Redline/Scripts/Editor/Modules/Themer/Resources/CreatePresets/";
        private const string Version = "v0.67"; // Updated version number
        public const string Enc = ".json";

        // Currently active theme path
        public static string CurrentTheme;
        
        /// <summary>
        /// Converts HTML color code to Unity Color
        /// </summary>
        public static Color HtmlToRgb(string htmlColor)
        {
            ColorUtility.TryParseHtmlString(htmlColor, out var color);
            return color;
        }

        /// <summary>
        /// Opens the Edit Theme window for a given theme
        /// </summary>
        public static void OpenEditTheme(CustomTheme theme)
        {
            EditThemeWindow.Ct = theme;
            var window = EditorWindow.GetWindow<EditThemeWindow>(false, "Edit Theme");
            window.Show();
        }
        
        /// <summary>
        /// Loads a CustomTheme from a JSON file
        /// </summary>
        public static CustomTheme GetCustomThemeFromJson(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogError($"Theme file not found at path: {path}");
                return null;
            }
            
            var json = File.ReadAllText(path);
            return JsonUtility.FromJson<CustomTheme>(json);
        }

        /// <summary>
        /// Gets the full path for a theme by name
        /// </summary>
        public static string GetPathForTheme(string themeName)
        {
            return CustomThemesPath + themeName + Enc;
        }
        
        /// <summary>
        /// Deletes a file and its .meta file if they exist
        /// </summary>
        public static void DeleteFileWithMeta(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                
                // Delete meta file if it exists
                string metaPath = path + ".meta";
                if (File.Exists(metaPath))
                {
                    File.Delete(metaPath);
                }
            }
            else 
            {
                Debug.LogWarning($"Path does not exist: {path}");
            }
        }

        /// <summary>
        /// Generates USS string from a CustomTheme
        /// </summary>
        private static string GenerateUssString(CustomTheme theme)
        {
            var ussText = new System.Text.StringBuilder();
            ussText.AppendLine("/* ========== Editor Themes Plugin ==========*/");
            ussText.AppendLine("/*            Auto Generated Code            */");
            ussText.AppendLine("/*@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@*/");
            ussText.AppendLine($"/* {Version} */");

            foreach (var item in theme.Items)
            {
                ussText.Append(UssBlock(item.Name, item.Color));
            }
            
            return ussText.ToString();
        }

        /// <summary>
        /// Generates a USS block for a given UI element name and color
        /// </summary>
        private static string UssBlock(string name, Color color)
        {
            Color32 color32 = color;
            string alpha = color.a.ToString().Replace(",", ".");

            string colorValue = $"rgba({color32.r}, {color32.g}, {color32.b}, {alpha})";

            return $"\n\n.{name}\n{{\n\tbackground-color: {colorValue};\n}}";
        }

        /// <summary>
        /// Saves a CustomTheme to a JSON file and loads it
        /// </summary>
        public static void SaveJsonFileForTheme(CustomTheme theme)
        {
            theme.Version = Version;
            string json = JsonUtility.ToJson(theme, true); // Pretty print for better readability

            string path = GetPathForTheme(theme.Name);
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.WriteAllText(path, json);
            LoadUssFileForTheme(theme.Name);
        }
        
        /// <summary>
        /// Loads a theme by name
        /// </summary>
        public static void LoadUssFileForTheme(string themeName)
        {
            LoadUssFileForThemeUsingPath(GetPathForTheme(themeName));
        }

        /// <summary>
        /// Loads a theme from a specific path
        /// </summary>
        private static void LoadUssFileForThemeUsingPath(string path)
        {
            var theme = GetCustomThemeFromJson(path);
            if (theme == null) return;

            // Switch between dark and light skin if needed
            if ((EditorGUIUtility.isProSkin && theme.unityTheme == CustomTheme.UnityTheme.Light) || 
                (!EditorGUIUtility.isProSkin && theme.unityTheme == CustomTheme.UnityTheme.Dark))
            {
                InternalEditorUtility.SwitchSkinAndRepaintAllViews();
            }

            string ussText = GenerateUssString(theme);
            WriteUss(ussText);

            CurrentTheme = path;
        }

        /// <summary>
        /// Writes USS content to theme files and refreshes the AssetDatabase
        /// </summary>
        private static void WriteUss(string ussText)
        {
            string darkPath = UssFilePath + "dark.uss";
            DeleteFileWithMeta(darkPath);
            File.WriteAllText(darkPath, ussText);

            string lightPath = UssFilePath + "light.uss";
            DeleteFileWithMeta(lightPath);
            File.WriteAllText(lightPath, ussText);

            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Gets a list of UI element names by category index
        /// </summary>
        public static List<string> GetColorListByInt(int categoryIndex)
        {
            var colorList = new List<string>();

            switch (categoryIndex)
            {
                case 0: // Base
                    colorList.Add("TabWindowBackground");
                    colorList.Add("ScrollViewAlt");
                    colorList.Add("label");
                    colorList.Add("ProjectBrowserTopBarBg");
                    colorList.Add("ProjectBrowserBottomBarBg");
                    colorList.Add("ScrollViewAlt");
                    colorList.Add("label");
                    colorList.Add("ProjectBrowserTopBarBg");
                    colorList.Add("ProjectBrowserBottomBarBg");
                    break;
                    
                case 1: // Accent
                    colorList.Add("dockHeader");
                    colorList.Add("TV LineBold");
                    break;
                    
                case 2: // Secondary
                    colorList.Add("ToolbarDropDownToogleRight");
                    colorList.Add("ToolbarPopupLeft");
                    colorList.Add("ToolbarPopup");
                    colorList.Add("toolbarbutton");
                    colorList.Add("PreToolbar");
                    colorList.Add("GameViewBackground");
                    colorList.Add("CN EntryInfoSmall");
                    colorList.Add("Toolbar");
                    colorList.Add("toolbarbutton");
                    colorList.Add("toolbarbuttonRight");
                    colorList.Add("ProjectBrowserIconAreaBg");
                    // Note: dragTab is the currently clicked tab, needs a different color than other tabs
                    break;
                    
                case 3: // Tab
                    colorList.Add("dragtab-label"); // This overrides dragTab and dragtab first
                    break;
                    
                case 4: // Button
                    colorList.Add("AppCommandLeft");
                    colorList.Add("AppCommandMid");
                    colorList.Add("AppCommand");
                    colorList.Add("AppToolbarButtonLeft");
                    colorList.Add("AppToolbarButtonRight");
                    colorList.Add("AppCommand");
                    colorList.Add("AppToolbarButtonLeft");
                    colorList.Add("AppToolbarButtonRight");
                    colorList.Add("DropDown");
                    break;
                    
                case 5: // Additional UI Elements
                    colorList.Add("SceneTopBarBg");
                    colorList.Add("MiniPopup");
                    colorList.Add("TV Selection");
                    colorList.Add("ExposablePopupMenu");
                    colorList.Add("minibutton");
                    colorList.Add("ToolbarSearchTextField");
                    break;


            }
            return colorList;

        }


    }

}
