using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Redline.RPM.Editor.RHierarchy
{
    /// <summary>
    /// Handles custom icons and visual indicators for the Redline Hierarchy
    /// </summary>
    [InitializeOnLoad]
    public static class RHierarchyIcons
    {
        private static RHierarchySettings _settings;
        private static Dictionary<Type, Texture2D> _componentIconCache = new Dictionary<Type, Texture2D>();
        
        // Custom icons
        private static Texture2D _warningIcon;
        private static Texture2D _errorIcon;
        private static Texture2D _infoIcon;
        private static Texture2D _scriptIcon;
        
        /// <summary>
        /// Static constructor
        /// </summary>
        static RHierarchyIcons()
        {
            _settings = RHierarchySettings.GetOrCreateSettings();
            LoadIcons();
        }
        
        /// <summary>
        /// Load all required icons
        /// </summary>
        private static void LoadIcons()
        {
            _warningIcon = EditorGUIUtility.FindTexture("console.warnicon");
            _errorIcon = EditorGUIUtility.FindTexture("console.erroricon");
            _infoIcon = EditorGUIUtility.FindTexture("console.infoicon");
            _scriptIcon = EditorGUIUtility.FindTexture("cs Script Icon");
        }
        
        /// <summary>
        /// Get an icon for a specific component type
        /// </summary>
        public static Texture2D GetComponentIcon(Type componentType)
        {
            if (componentType == null)
                return null;
                
            // Check cache first
            if (_componentIconCache.TryGetValue(componentType, out Texture2D cachedIcon))
                return cachedIcon;
                
            // Get icon for this component type
            Texture2D icon = null;
            
            // Try to get built-in icon
            MonoScript script = MonoScript.FromScriptableObject(ScriptableObject.CreateInstance(componentType.Name));
            if (script != null)
            {
                icon = AssetPreview.GetMiniThumbnail(script);
            }
            
            // Fallback to default script icon
            if (icon == null)
            {
                icon = _scriptIcon;
            }
            
            // Cache the icon
            _componentIconCache[componentType] = icon;
            
            return icon;
        }
        
        /// <summary>
        /// Get status icon for a GameObject based on its components
        /// </summary>
        public static Texture2D GetStatusIcon(GameObject gameObject, out string tooltip)
        {
            tooltip = string.Empty;
            
            if (gameObject == null)
                return null;
                
            // Check for missing scripts
            Component[] components = gameObject.GetComponents<Component>();
            foreach (Component component in components)
            {
                if (component == null)
                {
                    tooltip = "Missing Script";
                    return _errorIcon;
                }
            }
            
            // Check for missing required components
            foreach (Component component in components)
            {
                if (component == null)
                    continue;
                    
                Type componentType = component.GetType();
                
                // Check for RequireComponent attributes
                object[] attributes = componentType.GetCustomAttributes(typeof(RequireComponent), true);
                foreach (RequireComponent attribute in attributes)
                {
                    Type requiredType = attribute.m_Type0;
                    if (requiredType != null && gameObject.GetComponent(requiredType) == null)
                    {
                        tooltip = $"Missing required component: {requiredType.Name}";
                        return _warningIcon;
                    }
                }
            }
            
            // Check for disabled components
            Behaviour[] behaviours = gameObject.GetComponents<Behaviour>();
            foreach (Behaviour behaviour in behaviours)
            {
                if (behaviour != null && !behaviour.enabled)
                {
                    tooltip = "Has disabled components";
                    return _infoIcon;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Draw component icons for a GameObject
        /// </summary>
        public static void DrawComponentIcons(GameObject gameObject, Rect selectionRect, int maxIcons = 3)
        {
            if (gameObject == null || maxIcons <= 0)
                return;
                
            // Get important components to display
            Component[] components = gameObject.GetComponents<Component>();
            
            // Skip Transform which every GameObject has
            List<Component> filteredComponents = new List<Component>();
            foreach (Component component in components)
            {
                if (component != null && !(component is Transform))
                {
                    filteredComponents.Add(component);
                    
                    if (filteredComponents.Count >= maxIcons)
                        break;
                }
            }
            
            if (filteredComponents.Count == 0)
                return;
                
            // Calculate rect for component icons
            float iconSize = 16f;
            float padding = 2f;
            float totalWidth = (iconSize + padding) * filteredComponents.Count;
            
            Rect iconsRect = new Rect(
                selectionRect.xMax - totalWidth - 4f,
                selectionRect.y,
                totalWidth,
                selectionRect.height
            );
            
            // Draw each component icon
            for (int i = 0; i < filteredComponents.Count; i++)
            {
                Component component = filteredComponents[i];
                Type componentType = component.GetType();
                
                // Get component icon
                Texture2D icon = GetComponentIcon(componentType);
                
                if (icon != null)
                {
                    Rect iconRect = new Rect(
                        iconsRect.x + (i * (iconSize + padding)),
                        iconsRect.y + (iconsRect.height - iconSize) * 0.5f,
                        iconSize,
                        iconSize
                    );
                    
                    // Draw icon with tooltip
                    GUI.Label(iconRect, new GUIContent(icon, componentType.Name));
                }
            }
        }
        
        /// <summary>
        /// Draw status icon for a GameObject
        /// </summary>
        public static void DrawStatusIcon(GameObject gameObject, Rect selectionRect)
        {
            if (gameObject == null)
                return;
                
            // Get status icon
            Texture2D icon = GetStatusIcon(gameObject, out string tooltip);
            
            if (icon != null)
            {
                // Calculate rect for status icon
                float iconSize = 16f;
                Rect iconRect = new Rect(
                    selectionRect.x - iconSize - 2f,
                    selectionRect.y + (selectionRect.height - iconSize) * 0.5f,
                    iconSize,
                    iconSize
                );
                
                // Draw icon with tooltip
                GUI.Label(iconRect, new GUIContent(icon, tooltip));
            }
        }
    }
}
