using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Redline.Editor
{
    /// <summary>
    /// Editor window for managing and selecting themes
    /// </summary>
    public class ThemeSettings : EditorWindow
    {
        private Vector2 _scrollPosition;
        
        [MenuItem("Redline/Modules/Themer/Select Themes")]
        public static void ShowWindow()
        {
            GetWindow<ThemeSettings>("Theme Settings");
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawCreateThemeButton();
            DrawThemeCategories();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                margin = new RectOffset(0, 0, 10, 5)
            };
            
            EditorGUILayout.LabelField("Unity Theme Manager", titleStyle);
            
            string currentThemeName = "None";
            if (!string.IsNullOrEmpty(ThemesUtility.CurrentTheme))
            {
                currentThemeName = Path.GetFileNameWithoutExtension(ThemesUtility.CurrentTheme);
            }
            
            GUIStyle subtitleStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Italic
            };
            
            EditorGUILayout.LabelField($"Currently Using: {currentThemeName}", subtitleStyle);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
        }

        private void DrawCreateThemeButton()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                fixedHeight = 30,
                margin = new RectOffset(10, 10, 5, 5)
            };
            
            if (GUILayout.Button("Create New Theme", buttonStyle, GUILayout.Width(180)))
            {
                var window = GetWindow<CreateThemeWindow>(false, "Create Theme");
                window.Show();
            }
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("Available Themes", labelStyle);
            EditorGUILayout.Space(5);
        }

        private void DrawThemeCategories()
        {
            var darkThemes = new List<CustomTheme>();
            var lightThemes = new List<CustomTheme>();
            var bothThemes = new List<CustomTheme>();

            // Get all theme files and categorize them
            if (Directory.Exists(ThemesUtility.CustomThemesPath))
            {
                foreach (var themePath in Directory.GetFiles(ThemesUtility.CustomThemesPath, "*" + ThemesUtility.Enc))
                {
                    var theme = ThemesUtility.GetCustomThemeFromJson(themePath);
                    if (theme == null) continue;
                    
                    switch (theme.unityTheme)
                    {
                        case CustomTheme.UnityTheme.Dark:
                            darkThemes.Add(theme);
                            break;
                        case CustomTheme.UnityTheme.Light:
                            lightThemes.Add(theme);
                            break;
                        case CustomTheme.UnityTheme.Both:
                            bothThemes.Add(theme);
                            break;
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Themes directory not found. Please ensure the package is properly installed.", MessageType.Warning);
                return;
            }
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            // Display themes by category
            EditorGUILayout.Space();
            DrawThemeCategory("Dark & Light Themes:", bothThemes);
            
            EditorGUILayout.Space();
            DrawThemeCategory("Dark Themes:", darkThemes);
            
            EditorGUILayout.Space();
            DrawThemeCategory("Light Themes:", lightThemes);

            EditorGUILayout.EndScrollView();
        }

        private void DrawThemeCategory(string categoryTitle, List<CustomTheme> themes)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUIStyle categoryStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                margin = new RectOffset(5, 5, 8, 8)
            };
            
            EditorGUILayout.LabelField(categoryTitle, categoryStyle);
            EditorGUILayout.Space(2);
            
            if (themes.Count == 0)
            {
                GUIStyle emptyStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Italic
                };
                
                EditorGUILayout.LabelField("No themes in this category", emptyStyle);
            }
            else
            {
                foreach (var theme in themes)
                {
                    DisplayThemeItem(theme);
                }
            }
            
            EditorGUILayout.Space(5);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(8);
        }

        private static void DisplayThemeItem(CustomTheme theme)
        {
            // Create a box for each theme item
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Theme name with background color preview
            EditorGUILayout.BeginHorizontal();
            
            // Small color preview box
            if (theme.Items.Count > 0)
            {
                // Use the first color as a preview
                Color previewColor = theme.Items[0].Color;
                EditorGUI.DrawRect(GUILayoutUtility.GetRect(16, 16), previewColor);
            }
            else
            {
                // Default preview if no colors defined
                EditorGUI.DrawRect(GUILayoutUtility.GetRect(16, 16), Color.gray);
            }
            
            GUILayout.Space(5);
            
            // Theme name with custom style
            GUIStyle nameStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                margin = new RectOffset(0, 0, 2, 2)
            };
            EditorGUILayout.LabelField(theme.Name, nameStyle);
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Action buttons
            EditorGUILayout.BeginHorizontal();
            
            // Apply button style
            GUIStyle applyButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold
            };
            
            // Action button style
            GUIStyle actionButtonStyle = new GUIStyle(GUI.skin.button)
            {
                margin = new RectOffset(2, 2, 0, 0)
            };
            
            // Select theme button
            if (GUILayout.Button("Apply", applyButtonStyle))
            {
                ThemesUtility.LoadUssFileForTheme(theme.Name);
            }

            GUILayout.FlexibleSpace();

            // Edit theme button (if allowed)
            if (!theme.IsUnEditable && GUILayout.Button("Edit", actionButtonStyle, GUILayout.Width(60)))
            {
                ThemesUtility.OpenEditTheme(theme);
            }
            
            // Delete theme button (if allowed)
            if (!theme.IsUnDeletable && GUILayout.Button("Delete", actionButtonStyle, GUILayout.Width(60)))
            {
                if (EditorUtility.DisplayDialog(
                    $"Delete {theme.Name}?", 
                    $"Do you want to permanently delete the theme {theme.Name}? This cannot be undone.", 
                    "Delete", "Cancel"))
                {
                    ThemesUtility.DeleteFileWithMeta(ThemesUtility.GetPathForTheme(theme.Name));
                    ThemesUtility.LoadUssFileForTheme("_default");
                    GUIUtility.ExitGUI(); // Prevent errors from deleted objects
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(4);
        }
    }
}