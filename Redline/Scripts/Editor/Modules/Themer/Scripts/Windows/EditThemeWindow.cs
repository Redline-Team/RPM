using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Redline.Editor 
{
    /// <summary>
    /// Editor window for editing theme properties
    /// </summary>
    public class EditThemeWindow : EditorWindow
    {
        public static CustomTheme Ct;

        private string _name;
        private Vector2 _scrollPosition;
        private List<Color> _simpleColors = new();
        private List<Color> _lastSimpleColors = new();

        private enum CustomView { Simple, Advanced }
        private CustomView _customView;

        // Keyboard shortcut tracking for regeneration
        private bool _rKeyHeld;
        private bool _ctrlKeyHeld;
        
        /// <summary>
        /// Clean up when window is closed
        /// </summary>
        private void OnDestroy()
        {
            Ct = null;
        }

        /// <summary>
        /// Initialize window when opened
        /// </summary>
        private void Awake()
        {
            if (Ct == null) return;
            
            _simpleColors = CreateAverageColors();
            _lastSimpleColors = CreateAverageColors();
            _name = Ct.Name;
        }

        /// <summary>
        /// Draw the editor window GUI
        /// </summary>
        private void OnGUI()
        {
            if (Ct == null)
            {
                Close();
                return;
            }
            
            // Handle keyboard shortcuts and regeneration
            bool regenerate = HandleKeyboardShortcuts();
            if (regenerate)
            {
                RegenerateTheme();
            }

            // Header section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                margin = new RectOffset(0, 0, 8, 8)
            };
            
            EditorGUILayout.LabelField("Edit Theme", titleStyle);
            
            EditorGUILayout.Space(5);
            
            // Theme name field with label
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Theme Name:", GUILayout.Width(100));
            
            GUIStyle nameFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            
            _name = EditorGUILayout.TextField(_name, nameFieldStyle);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // View mode selection with better styling
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Edit Mode:", GUILayout.Width(100));
            _customView = (CustomView)EditorGUILayout.EnumPopup(_customView);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);

            // Draw appropriate view based on selection
            if (_customView == CustomView.Advanced)
            {
                DrawAdvancedView();
            }
            else
            {
                DrawSimpleView();
            }

            // Draw theme type selection and save/clone buttons
            DrawThemeTypeAndButtons();
        }

        /// <summary>
        /// Handle keyboard shortcuts for theme regeneration
        /// </summary>
        private bool HandleKeyboardShortcuts()
        {
            Event e = Event.current;
            
            // Handle R key
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.R)
            {
                _rKeyHeld = true;
            }
            else if (e.type == EventType.KeyUp && e.keyCode == KeyCode.R)
            {
                _rKeyHeld = false;
            }
            
            // Handle Ctrl key
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.LeftControl)
            {
                _ctrlKeyHeld = true;
            }
            else if (e.type == EventType.KeyUp && e.keyCode == KeyCode.LeftControl)
            {
                _ctrlKeyHeld = false;
            }

            // Check for Ctrl+R combination
            if (_rKeyHeld && _ctrlKeyHeld)
            {
                _rKeyHeld = false;
                _ctrlKeyHeld = false;
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Regenerate the theme with current colors
        /// </summary>
        private void RegenerateTheme()
        {
            if (EditorUtility.DisplayDialog(
                "Do you want to regenerate this Theme? (Make a Clone first!)", 
                "Regenerating is helpful when the Theme was made with an older version of the Plugin (but you might lose small amounts of data)", 
                "Continue", 
                "Cancel"))
            {
                Ct.Items = new List<CustomTheme.UIItem>();
                
                // Add items for each color category
                for (int i = 0; i < 6; i++)
                {
                    foreach (string itemName in ThemesUtility.GetColorListByInt(i))
                    {
                        Ct.Items.Add(new CustomTheme.UIItem
                        {
                            Name = itemName,
                            Color = _simpleColors[i]
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Draw the advanced theme editing view
        /// </summary>
        private void DrawAdvancedView()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter
            };
            
            EditorGUILayout.LabelField("Advanced Theme Editing", headerStyle);
            EditorGUILayout.Space(5);
            
            // Item count display
            GUIStyle countStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Italic
            };
            
            EditorGUILayout.LabelField($"Total Items: {Ct.Items.Count}", countStyle);
            EditorGUILayout.Space(5);
            
            // Table header
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUIStyle columnHeaderStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(5, 5, 2, 2)
            };
            
            EditorGUILayout.LabelField("UI Element Name", columnHeaderStyle, GUILayout.Width(200));
            EditorGUILayout.LabelField("Color", columnHeaderStyle, GUILayout.Width(200));
            EditorGUILayout.LabelField("Actions", columnHeaderStyle, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();
            
            // Scrollable list of items
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(300));

            // Clone the items list to avoid collection modification issues
            var itemsClone = new List<CustomTheme.UIItem>(Ct.Items);
            int index = 0;
            foreach (var item in itemsClone)
            {
                // Alternate row background
                EditorGUILayout.BeginHorizontal(index % 2 == 0 ? 
                    EditorStyles.helpBox : new GUIStyle(EditorStyles.helpBox) { margin = new RectOffset(0, 0, 2, 2) });
                
                // Name field with custom style
                GUIStyle textFieldStyle = new GUIStyle(GUI.skin.textField)
                {
                    margin = new RectOffset(5, 5, 2, 2)
                };
                item.Name = EditorGUILayout.TextField(item.Name, textFieldStyle, GUILayout.Width(190));
                
                // Color field with preview
                EditorGUILayout.BeginHorizontal(GUILayout.Width(200));
                
                // Small color preview
                Rect previewRect = GUILayoutUtility.GetRect(16, 16);
                previewRect.y += 2;
                EditorGUI.DrawRect(previewRect, item.Color);
                
                GUILayout.Space(5);
                item.Color = EditorGUILayout.ColorField(item.Color, GUILayout.Width(170));
                EditorGUILayout.EndHorizontal();
                
                // Delete button
                GUIStyle deleteButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    margin = new RectOffset(5, 5, 2, 2)
                };
                
                if (GUILayout.Button("×", deleteButtonStyle, GUILayout.Width(25), GUILayout.Height(18)))
                {
                    Ct.Items.Remove(item);
                }
                
                EditorGUILayout.EndHorizontal();
                index++;
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space(10);

            // Add/Remove buttons with better styling
            EditorGUILayout.BeginHorizontal();
            
            GUIStyle actionButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                fixedHeight = 30,
                margin = new RectOffset(5, 5, 5, 5)
            };
            
            if (GUILayout.Button("+ Add New Item", actionButtonStyle))
            {
                Ct.Items.Add(new CustomTheme.UIItem
                {
                    Name = "Enter Name",
                    Color = Color.white
                });
            }
            
            GUILayout.Space(10);
            
            GUI.enabled = Ct.Items.Count > 0;
            if (GUILayout.Button("- Remove Last Item", actionButtonStyle))
            {
                Ct.Items.RemoveAt(Ct.Items.Count - 1);
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draw the simple theme editing view with color categories
        /// </summary>
        private void DrawSimpleView()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter
            };
            
            EditorGUILayout.LabelField("Color Categories", headerStyle);
            EditorGUILayout.Space(5);
            
            // Color category fields with descriptions
            string[] colorLabels = {
                "Base Color", 
                "Accent Color", 
                "Secondary Base Color", 
                "Tab Color", 
                "Command Bar Color", 
                "Additional Color"
            };
            
            string[] colorDescriptions = {
                "Main background color for windows and panels",
                "Highlight color for important UI elements",
                "Secondary background color for toolbars and headers",
                "Color for tabs and tab headers",
                "Color for command bars and buttons",
                "Additional color for miscellaneous UI elements"
            };

            for (int i = 0; i < 6; i++)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                // Color label with custom style
                GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    margin = new RectOffset(5, 5, 5, 2)
                };
                EditorGUILayout.LabelField(colorLabels[i], labelStyle);
                
                // Description with custom style
                GUIStyle descStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontStyle = FontStyle.Italic,
                    wordWrap = true,
                    margin = new RectOffset(5, 5, 0, 5)
                };
                EditorGUILayout.LabelField(colorDescriptions[i], descStyle);
                
                // Color preview box
                Rect colorRect = GUILayoutUtility.GetRect(0, 20);
                colorRect.x += 5;
                colorRect.width -= 10;
                EditorGUI.DrawRect(colorRect, _simpleColors[i]);
                
                EditorGUILayout.Space(5);
                
                // Color field with custom layout
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(5);
                Color newColor = EditorGUILayout.ColorField(_simpleColors[i], GUILayout.Height(25));
                GUILayout.Space(5);
                EditorGUILayout.EndHorizontal();
                
                // Apply color changes when modified
                if (newColor != _simpleColors[i])
                {
                    _simpleColors[i] = newColor;
                    EditColor(i, newColor);
                }
                
                EditorGUILayout.Space(5);
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.Space(5);
            }
            
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draw theme type selection and save/clone buttons
        /// </summary>
        private void DrawThemeTypeAndButtons()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Theme type selection with better styling
            EditorGUILayout.BeginHorizontal();
            
            GUIStyle themeTypeLabel = new GUIStyle(EditorStyles.boldLabel)
            {
                margin = new RectOffset(5, 5, 10, 5)
            };
            
            EditorGUILayout.LabelField("Unity Theme Compatibility:", themeTypeLabel, GUILayout.Width(180));
            
            // Custom enum popup with better styling
            GUIStyle enumStyle = new GUIStyle(EditorStyles.popup)
            {
                fixedHeight = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            
            Ct.unityTheme = (CustomTheme.UnityTheme)EditorGUILayout.EnumPopup(Ct.unityTheme, enumStyle);
            EditorGUILayout.EndHorizontal();
            
            // Theme type description
            GUIStyle descriptionStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                wordWrap = true,
                fontStyle = FontStyle.Italic,
                margin = new RectOffset(10, 10, 0, 10)
            };
            
            string themeDescription = Ct.unityTheme switch
            {
                CustomTheme.UnityTheme.Dark => "This theme will only be applied when using Unity's Dark Editor theme.",
                CustomTheme.UnityTheme.Light => "This theme will only be applied when using Unity's Light Editor theme.",
                CustomTheme.UnityTheme.Both => "This theme will be applied regardless of Unity's Editor theme setting.",
                _ => string.Empty
            };
            
            EditorGUILayout.LabelField(themeDescription, descriptionStyle);
            
            EditorGUILayout.Space(10);
            
            // Action buttons with better styling
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            // Save button
            GUIStyle saveButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                fixedHeight = 30,
                fixedWidth = 120,
                margin = new RectOffset(10, 10, 5, 5)
            };
            
            if (GUILayout.Button("Save Theme", saveButtonStyle))
            {
                if (Ct.Name != _name)
                {
                    ThemesUtility.DeleteFileWithMeta(ThemesUtility.GetPathForTheme(Ct.Name));
                }

                Ct.Name = _name;
                ThemesUtility.SaveJsonFileForTheme(Ct);
                
                // Show success message
                ShowNotification(new GUIContent("Theme saved successfully!"));
            }

            // Clone button
            GUIStyle cloneButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = 30,
                fixedWidth = 120,
                margin = new RectOffset(10, 10, 5, 5)
            };
            
            if (GUILayout.Button("Clone Theme", cloneButtonStyle))
            {
                Ct.Name = _name + " - Copy";
                ThemesUtility.SaveJsonFileForTheme(Ct);
                
                // Show success message
                ShowNotification(new GUIContent("Theme cloned successfully!"));
            }
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Get a UI item by its name
        /// </summary>
        private static CustomTheme.UIItem GetItemByName(string name)
        {
            return Ct?.Items?.FirstOrDefault(item => item.Name == name);
        }
        
        /// <summary>
        /// Create a list of average colors for each category
        /// </summary>
        private List<Color> CreateAverageColors()
        {
            var colors = new List<Color>();

            for (int i = 0; i < 6; i++)
            {
                var colorObjects = ThemesUtility.GetColorListByInt(i);
                var colorList = new List<Color>();
                
                foreach (string itemName in colorObjects)
                {
                    var item = GetItemByName(itemName);
                    if (item != null)
                    {
                        colorList.Add(item.Color);
                    }
                }

                // Use default color if no colors found
                colors.Add(colorList.Count > 0 ? GetAverage(colorList) : ThemesUtility.HtmlToRgb("#9A7B6E"));
            }

            return colors;
        }

        /// <summary>
        /// Edit all UI items in a category with a new color
        /// </summary>
        private void EditColor(int categoryIndex, Color newColor)
        {
            var itemNames = ThemesUtility.GetColorListByInt(categoryIndex);

            foreach (string itemName in itemNames)
            {
                var item = GetItemByName(itemName);
                if (item != null)
                {
                    item.Color = newColor;
                }
            }

            _lastSimpleColors[categoryIndex] = _simpleColors[categoryIndex];
        }

        /// <summary>
        /// Calculate the average color from a list of colors
        /// </summary>
        private static Color GetAverage(List<Color> colors)
        {
            if (colors == null || colors.Count == 0)
            {
                return Color.gray;
            }

            float r = 0, g = 0, b = 0;
            int count = colors.Count;
            
            foreach (Color color in colors)
            {
                r += color.r;
                g += color.g;
                b += color.b;
            }

            return new Color(r / count, g / count, b / count);
        }
    }
}