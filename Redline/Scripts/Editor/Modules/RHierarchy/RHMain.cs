using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Redline.RPM.Editor.RHierarchy
{
    /// <summary>
    /// RHMain - Redline Hierarchy Main Class
    /// Enhances Unity's built-in hierarchy with additional features
    /// </summary>
    [InitializeOnLoad]
    public static class RHMain
    {
        #region Variables

        // Settings
        private static bool _initialized = false;
        private static RHierarchySettings _settings;
        
        // Cached data
        private static Dictionary<int, GameObject> _gameObjectCache = new Dictionary<int, GameObject>();
        private static Dictionary<int, Color> _hierarchyRowColors = new Dictionary<int, Color>();
        
        // Style cache
        private static GUIStyle _iconStyle;
        private static GUIStyle _labelStyle;
        private static GUIStyle _boldLabelStyle;
        
        // Icons
        private static Texture2D _folderIcon;
        private static Texture2D _lockIcon;
        private static Texture2D _visibilityOnIcon;
        private static Texture2D _visibilityOffIcon;
        
        #endregion

        #region Constructor

        /// <summary>
        /// Static constructor called by Unity when the editor starts
        /// </summary>
        static RHMain()
        {
            Initialize();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the enhanced hierarchy
        /// </summary>
        private static void Initialize()
        {
            if (_initialized)
                return;

            try
            {
                // Load settings
                _settings = RHierarchySettings.GetOrCreateSettings();
                
                // Load icons
                LoadIcons();
                
                // Subscribe to hierarchy window events
                EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyItemGUI;
                EditorApplication.hierarchyChanged += OnHierarchyChanged;
                
                // Delay style setup until first hierarchy GUI call
                // This ensures EditorStyles are fully initialized
                _initialized = true;
                
                Debug.Log("Redline Hierarchy initialized");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error initializing Redline Hierarchy: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// Load all required icons
        /// </summary>
        private static void LoadIcons()
        {
            _folderIcon = EditorGUIUtility.FindTexture("Folder Icon");
            _lockIcon = EditorGUIUtility.FindTexture("LockIcon");
            _visibilityOnIcon = EditorGUIUtility.FindTexture("d_scenevis_visible_hover");
            _visibilityOffIcon = EditorGUIUtility.FindTexture("d_scenevis_hidden_hover");
        }

        /// <summary>
        /// Setup GUI styles
        /// </summary>
        private static void SetupStyles()
        {
            // Create a basic style that doesn't depend on EditorStyles
            _iconStyle = new GUIStyle(GUIStyle.none)
            {
                padding = new RectOffset(2, 2, 2, 2)
            };
            
            // Check if EditorStyles is initialized
            // This can be null during early initialization
            if (EditorStyles.label != null)
            {
                _labelStyle = new GUIStyle(EditorStyles.label);
                _boldLabelStyle = new GUIStyle(EditorStyles.boldLabel);
            }
            else
            {
                // Create fallback styles if EditorStyles isn't available yet
                _labelStyle = new GUIStyle();
                _boldLabelStyle = new GUIStyle();
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Called when the hierarchy window needs to be redrawn
        /// </summary>
        private static void OnHierarchyItemGUI(int instanceID, Rect selectionRect)
        {
            // Make sure settings are loaded
            if (_settings == null)
            {
                try
                {
                    _settings = RHierarchySettings.GetOrCreateSettings();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to load RHierarchy settings: {e.Message}");
                    return;
                }
            }

            // Ensure styles are initialized
            if (_labelStyle == null || _boldLabelStyle == null)
            {
                try
                {
                    SetupStyles();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to setup RHierarchy styles: {e.Message}");
                    return;
                }
            }

            // Check if hierarchy enhancement is enabled
            if (!_settings.enhancedHierarchyEnabled)
                return;

            try
            {
                // Get the GameObject for this hierarchy item
                GameObject gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
                if (gameObject == null)
                    return;

                // Cache the GameObject
                _gameObjectCache[instanceID] = gameObject;

                // Calculate rects for different elements
                Rect fullRect = new Rect(selectionRect);
                fullRect.width = Screen.width;
                
                // Draw background color if needed
                DrawRowBackground(instanceID, fullRect, gameObject);

                // Draw custom elements
                if (_settings.showComponentIcons)
                    DrawComponentIcons(gameObject, selectionRect);
                    
                if (_settings.showToggleIcons)
                    DrawToggleIcons(gameObject, selectionRect);
                    
                if (_settings.showCustomPrefix)
                    DrawCustomPrefix(gameObject, selectionRect);
            }
            catch (Exception e)
            {
                // Prevent exceptions from breaking the hierarchy window
                Debug.LogError($"Error in RHierarchy OnHierarchyItemGUI: {e.Message}");
            }
        }

        /// <summary>
        /// Called when the hierarchy changes
        /// </summary>
        private static void OnHierarchyChanged()
        {
            // Clean up any deleted GameObjects from our cache
            List<int> keysToRemove = new List<int>();
            
            foreach (var kvp in _gameObjectCache)
            {
                if (kvp.Value == null)
                    keysToRemove.Add(kvp.Key);
            }
            
            foreach (int key in keysToRemove)
            {
                _gameObjectCache.Remove(key);
                _hierarchyRowColors.Remove(key);
            }
        }

        #endregion

        #region Drawing Methods

        /// <summary>
        /// Draw background color for hierarchy rows
        /// </summary>
        private static void DrawRowBackground(int instanceID, Rect rect, GameObject gameObject)
        {
            Color backgroundColor = Color.clear;
            
            // Check if we should color this row
            if (_settings.colorByNestLevel)
            {
                // Get the depth level of the GameObject in the hierarchy
                int depth = 0;
                Transform parent = gameObject.transform.parent;
                while (parent != null)
                {
                    depth++;
                    parent = parent.parent;
                }
                
                // Calculate color based on depth with very low opacity
                if (depth > 0)
                {
                    float hue = (depth * 0.05f) % 1.0f;
                    Color baseColor = Color.HSVToRGB(hue, 0.1f, 0.9f);
                    backgroundColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.05f); // Very low opacity
                }
            }
            
            // Check for prefab instances
            if (_settings.highlightPrefabs && PrefabUtility.IsPartOfAnyPrefab(gameObject))
            {
                backgroundColor = new Color(0.7f, 0.9f, 0.7f, 0.05f); // Reduced opacity for better readability
            }
            
            // Check for specific tags
            if (_settings.highlightByTag && !string.IsNullOrEmpty(gameObject.tag) && gameObject.tag != "Untagged")
            {
                switch (gameObject.tag)
                {
                    case "Player":
                        backgroundColor = new Color(0.2f, 0.6f, 1f, 0.05f); // Reduced opacity for better readability
                        break;
                    case "MainCamera":
                        backgroundColor = new Color(1f, 0.7f, 0.3f, 0.05f); // Reduced opacity for better readability
                        break;
                    // Add more tag-based colors as needed
                }
            }
            
            // Draw the background if we have a color
            if (backgroundColor != Color.clear)
            {
                _hierarchyRowColors[instanceID] = backgroundColor;
                EditorGUI.DrawRect(rect, backgroundColor);
            }
        }

        /// <summary>
        /// Draw component icons for the GameObject
        /// </summary>
        private static void DrawComponentIcons(GameObject gameObject, Rect selectionRect)
        {
            // Get important components to display
            Component[] components = gameObject.GetComponents<Component>();
            
            // Skip Transform which every GameObject has
            List<Component> filteredComponents = components
                .Where(c => c != null && !(c is Transform))
                .Take(4) // Limit to first 4 components to avoid cluttering
                .ToList();
                
            if (filteredComponents.Count == 0)
                return;
                
            // Calculate rect for component icons
            float iconSize = 16f;
            float padding = 2f;
            float totalWidth = (iconSize + padding) * filteredComponents.Count;
            float visibilityIconWidth = 20f; // Width of visibility icon plus padding
            float scrollbarOffset = 8f; // Offset to account for scrollbar width
            
            // Position component icons to the left of the visibility toggle
            // Adjust position to account for scrollbar when present
            Rect iconsRect = new Rect(
                EditorGUIUtility.currentViewWidth - totalWidth - visibilityIconWidth - 4f - scrollbarOffset,
                selectionRect.y,
                totalWidth,
                selectionRect.height
            );
            
            // Draw each component icon
            for (int i = 0; i < filteredComponents.Count; i++)
            {
                Component component = filteredComponents[i];
                
                // Get component icon
                Texture2D icon = AssetPreview.GetMiniThumbnail(component);
                
                if (icon != null)
                {
                    Rect iconRect = new Rect(
                        iconsRect.x + (i * (iconSize + padding)),
                        iconsRect.y + (iconsRect.height - iconSize) * 0.5f,
                        iconSize,
                        iconSize
                    );
                    
                    // Draw icon only (tooltip only, no text overlay)
                    GUI.Label(iconRect, new GUIContent(icon, component.GetType().Name), _iconStyle);
                }
            }
        }

        /// <summary>
        /// Draw visibility toggle icon on the absolute right side of the hierarchy
        /// </summary>
        private static void DrawToggleIcons(GameObject gameObject, Rect selectionRect)
        {
            float iconSize = 16f;
            float padding = 2f;
            float scrollbarOffset = 8f; // Offset to account for scrollbar width
            
            // Calculate rect for visibility toggle icon on the absolute right side
            // Adjust position to account for scrollbar when present
            Rect visibilityRect = new Rect(
                EditorGUIUtility.currentViewWidth - iconSize - padding - 4f - scrollbarOffset, // Position adjusted for scrollbar
                selectionRect.y + (selectionRect.height - iconSize) * 0.5f,
                iconSize,
                iconSize
            );
            
            // Get current active state
            bool isActive = gameObject.activeInHierarchy;
            
            // Draw visibility toggle
            EditorGUI.BeginChangeCheck();
            bool newIsActive = GUI.Toggle(visibilityRect, isActive, new GUIContent(isActive ? _visibilityOnIcon : _visibilityOffIcon), _iconStyle);
            if (EditorGUI.EndChangeCheck() && newIsActive != isActive)
            {
                Undo.RecordObject(gameObject, "Toggle GameObject Active State");
                gameObject.SetActive(newIsActive);
            }
            
            // Lock toggle removed as requested
        }

        /// <summary>
        /// Draw custom prefix for special GameObjects
        /// </summary>
        private static void DrawCustomPrefix(GameObject gameObject, Rect selectionRect)
        {
            // Check if this is an empty GameObject (only has Transform component)
            bool isEmpty = gameObject.GetComponents<Component>().Length <= 1;
            
            // Check if this is acting like a folder (empty with children)
            bool isFolder = isEmpty && gameObject.transform.childCount > 0;
            
            if (isFolder && _settings.showFolderIcons)
            {
                // Draw folder icon on the right side (similar to component icons)
                float iconSize = 16f;
                float padding = 2f;
                float visibilityIconWidth = 20f; // Width of visibility icon plus padding
                
                // Position folder icon to the left of the visibility toggle
                // Adjust position to account for scrollbar when present
                float scrollbarOffset = 8f; // Offset to account for scrollbar width
                Rect iconRect = new Rect(
                    EditorGUIUtility.currentViewWidth - visibilityIconWidth - iconSize - padding - 4f - scrollbarOffset,
                    selectionRect.y + (selectionRect.height - iconSize) * 0.5f,
                    iconSize,
                    iconSize
                );
                
                GUI.Label(iconRect, new GUIContent(_folderIcon, "Folder"), _iconStyle);
                
                // We'll use a different approach for folder styling to avoid text overlay issues
                // Instead of drawing a bold label (which can cause glitchy text), we'll just
                // draw a very subtle background to indicate folders
                if (_settings.boldFolderLabels)
                {
                    // Draw a subtle background behind folder items
                    Color folderColor = new Color(1f, 1f, 0.8f, 0.05f); // Very light yellow with low alpha
                    Rect fullRect = new Rect(selectionRect);
                    fullRect.width = Screen.width - selectionRect.x;
                    EditorGUI.DrawRect(fullRect, folderColor);
                }
            }
        }

        #endregion
    }
}