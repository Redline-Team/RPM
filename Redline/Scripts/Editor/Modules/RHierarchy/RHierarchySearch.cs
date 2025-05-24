using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Redline.RPM.Editor.RHierarchy
{
    /// <summary>
    /// Enhances Unity's built-in hierarchy search functionality
    /// </summary>
    [InitializeOnLoad]
    public static class RHierarchySearch
    {
        // Settings
        private static RHierarchySettings _settings;
        
        // Search enhancement state
        private static bool _enhancedSearchActive = false;
        private static string _lastSearchFilter = string.Empty;
        private static SearchMode _searchMode = SearchMode.Name;
        private static Dictionary<int, bool> _searchMatchCache = new Dictionary<int, bool>();
        
        // Unity's SceneHierarchyWindow reflection info
        private static Type _sceneHierarchyWindowType;
        private static EditorWindow _hierarchyWindow;
        private static MethodInfo _setSearchFilterMethod;
        private static PropertyInfo _searchFilterProperty;
        
        /// <summary>
        /// Search modes for enhanced search
        /// </summary>
        public enum SearchMode
        {
            Name,
            Tag,
            Component,
            Layer,
            Path
        }
        
        /// <summary>
        /// Static constructor
        /// </summary>
        static RHierarchySearch()
        {
            try
            {
                // Load settings
                _settings = RHierarchySettings.GetOrCreateSettings();
                
                // Get Unity's hierarchy window through reflection
                _sceneHierarchyWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
                if (_sceneHierarchyWindowType != null)
                {
                    // Get methods and properties for interacting with Unity's search
                    _setSearchFilterMethod = _sceneHierarchyWindowType.GetMethod("SetSearchFilter", 
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    _searchFilterProperty = _sceneHierarchyWindowType.GetProperty("searchFilter", 
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                }
                
                // Register event handlers
                EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyItemGUI;
                EditorApplication.update += OnEditorUpdate;
                
                // Add menu items for search modes
                AddSearchMenuItems();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error initializing RHierarchySearch: {e.Message}\n{e.StackTrace}");
            }
        }
        
        /// <summary>
        /// Add menu items for enhanced search modes
        /// </summary>
        private static void AddSearchMenuItems()
        {
            // Menu items are added using Unity's MenuItem attribute in the MenuItems class
            // No need to do anything here as the static methods with MenuItem attributes are registered automatically
        }
        
        /// <summary>
        /// Menu items for enhanced search
        /// </summary>
        private static class MenuItems
        {
            [MenuItem("Redline/Modules/RHierarchy/Enhanced Search/By Name", false, 49)]
            private static void SearchByName()
            {
                SetSearchMode(SearchMode.Name);
            }
            
            [MenuItem("Redline/Modules/RHierarchy/Enhanced Search/By Tag", false, 50)]
            private static void SearchByTag()
            {
                SetSearchMode(SearchMode.Tag);
            }
            
            [MenuItem("Redline/Modules/RHierarchy/Enhanced Search/By Component", false, 51)]
            private static void SearchByComponent()
            {
                SetSearchMode(SearchMode.Component);
            }
            
            [MenuItem("Redline/Modules/RHierarchy/Enhanced Search/By Layer", false, 52)]
            private static void SearchByLayer()
            {
                SetSearchMode(SearchMode.Layer);
            }
            
            [MenuItem("Redline/Modules/RHierarchy/Enhanced Search/By Path", false, 53)]
            private static void SearchByPath()
            {
                SetSearchMode(SearchMode.Path);
            }
            
            [MenuItem("Redline/Modules/RHierarchy/Enhanced Search/Disable Enhanced Search", false, 54)]
            private static void DisableSearch()
            {
                DisableEnhancedSearch();
            }
        }
        
        /// <summary>
        /// Called when the editor updates
        /// </summary>
        private static void OnEditorUpdate()
        {
            if (!_enhancedSearchActive)
                return;
                
            try
            {
                // Get the current hierarchy window if we don't have it yet
                if (_hierarchyWindow == null)
                {
                    _hierarchyWindow = EditorWindow.GetWindow(_sceneHierarchyWindowType);
                }
                
                // Get the current search filter from Unity's hierarchy
                if (_hierarchyWindow != null && _searchFilterProperty != null)
                {
                    string currentFilter = (string)_searchFilterProperty.GetValue(_hierarchyWindow, null);
                    
                    // If the search filter has changed, update our search
                    if (currentFilter != _lastSearchFilter)
                    {
                        _lastSearchFilter = currentFilter;
                        _searchMatchCache.Clear();
                        
                        // Only repaint if there's an active search
                        if (!string.IsNullOrEmpty(currentFilter))
                        {
                            EditorApplication.RepaintHierarchyWindow();
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Silently handle any errors to prevent editor crashes
                _enhancedSearchActive = false;
            }
        }
        
        /// <summary>
        /// Called for each hierarchy item
        /// </summary>
        private static void OnHierarchyItemGUI(int instanceID, Rect selectionRect)
        {
            // Skip if enhanced search is not active or there's no search filter
            if (!_enhancedSearchActive || string.IsNullOrEmpty(_lastSearchFilter))
                return;
                
            try
            {
                // Get the GameObject for this hierarchy item
                GameObject gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
                if (gameObject == null)
                    return;
                    
                // Check if this object matches our enhanced search criteria
                bool isMatch;
                if (_searchMatchCache.TryGetValue(instanceID, out isMatch))
                {
                    // Use cached result
                }
                else
                {
                    // Determine if this object matches based on search mode
                    isMatch = IsMatchForSearchMode(gameObject, _lastSearchFilter, _searchMode);
                    _searchMatchCache[instanceID] = isMatch;
                }
                
                // If Unity's search already filtered this object, don't interfere
                // We only want to enhance the results, not override Unity's filtering
                
                // Add visual indicator for objects that match our enhanced criteria
                if (isMatch)
                {
                    // Draw a very subtle highlight for matching objects with higher transparency
                    Color highlightColor = new Color(0.2f, 0.6f, 1f, 0.05f); // Reduced alpha to 0.05 for better readability
                    EditorGUI.DrawRect(selectionRect, highlightColor);
                    
                    // Draw a small indicator dot on the right side to show enhanced match
                    // Position it far to the right to avoid interfering with any text
                    float dotSize = 4f;
                    Rect dotRect = new Rect(
                        EditorGUIUtility.currentViewWidth - 20f,
                        selectionRect.y + (selectionRect.height - dotSize) * 0.5f,
                        dotSize,
                        dotSize
                    );
                    
                    // Use a simple colored dot as indicator (no text)
                    EditorGUI.DrawRect(dotRect, new Color(0f, 1f, 1f, 0.8f));
                }
            }
            catch (Exception)
            {
                // Silently handle errors to prevent editor crashes
                _enhancedSearchActive = false;
            }
        }
        
        /// <summary>
        /// Set the search mode and enable enhanced search
        /// </summary>
        private static void SetSearchMode(SearchMode mode)
        {
            _searchMode = mode;
            _enhancedSearchActive = true;
            _searchMatchCache.Clear();
            
            // Get the current search filter from Unity's hierarchy
            if (_hierarchyWindow == null)
            {
                _hierarchyWindow = EditorWindow.GetWindow(_sceneHierarchyWindowType);
            }
            
            if (_hierarchyWindow != null && _searchFilterProperty != null)
            {
                string currentFilter = (string)_searchFilterProperty.GetValue(_hierarchyWindow, null);
                _lastSearchFilter = currentFilter;
                
                // If there's no current search, suggest to the user to enter a search term
                if (string.IsNullOrEmpty(currentFilter))
                {
                    // Focus the hierarchy window's search field
                    _hierarchyWindow.Focus();
                    EditorGUIUtility.keyboardControl = 0;
                    
                    // Show a notification about the enhanced search mode
                    string modeName = Enum.GetName(typeof(SearchMode), _searchMode);
                    EditorWindow.focusedWindow.ShowNotification(
                        new GUIContent($"Enhanced Search: {modeName}\nEnter a search term in the hierarchy search field"));
                }
                else
                {
                    // There's already a search term, so just repaint to show enhanced results
                    EditorApplication.RepaintHierarchyWindow();
                }
            }
        }
        
        /// <summary>
        /// Disable enhanced search and return to Unity's standard search
        /// </summary>
        private static void DisableEnhancedSearch()
        {
            _enhancedSearchActive = false;
            _searchMatchCache.Clear();
            EditorApplication.RepaintHierarchyWindow();
        }
        
        /// <summary>
        /// Check if a GameObject matches the search criteria based on the current search mode
        /// </summary>
        private static bool IsMatchForSearchMode(GameObject gameObject, string searchText, SearchMode mode)
        {
            if (gameObject == null || string.IsNullOrEmpty(searchText))
                return false;
                
            switch (mode)
            {
                case SearchMode.Name:
                    return gameObject.name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                    
                case SearchMode.Tag:
                    return !string.IsNullOrEmpty(gameObject.tag) && 
                           gameObject.tag.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                    
                case SearchMode.Component:
                    // Search for components by type name
                    Component[] components = gameObject.GetComponents<Component>();
                    foreach (Component component in components)
                    {
                        if (component != null && 
                            component.GetType().Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return true;
                        }
                    }
                    return false;
                    
                case SearchMode.Layer:
                    // Search by layer name
                    string layerName = LayerMask.LayerToName(gameObject.layer);
                    return layerName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                    
                case SearchMode.Path:
                    // Search by full path
                    string path = GetFullPath(gameObject);
                    return path.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                    
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Get the full hierarchy path of a GameObject
        /// </summary>
        private static string GetFullPath(GameObject gameObject)
        {
            if (gameObject == null)
                return string.Empty;
                
            string path = gameObject.name;
            Transform parent = gameObject.transform.parent;
            
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            
            return path;
        }
    }
}
