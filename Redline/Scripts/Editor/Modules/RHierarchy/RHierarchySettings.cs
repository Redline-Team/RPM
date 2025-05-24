using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Redline.RPM.Editor.RHierarchy
{
    /// <summary>
    /// Settings for the Redline Hierarchy
    /// </summary>
    [Serializable]
    public class RHierarchySettings : ScriptableObject
    {
        private const string SettingsPath = "Assets/Redline/Editor/RHierarchySettings.asset";
        
        #region Settings Properties
        
        [Header("General")]
        [Tooltip("Enable or disable the enhanced hierarchy")]
        public bool enhancedHierarchyEnabled = true;
        
        [Header("Visual Features")]
        [Tooltip("Show component icons next to GameObjects")]
        public bool showComponentIcons = true;
        
        [Tooltip("Show toggle icons for visibility and lock state")]
        public bool showToggleIcons = true;
        
        [Tooltip("Show custom prefix for special GameObjects")]
        public bool showCustomPrefix = true;
        
        [Tooltip("Show folder icons for empty GameObjects with children")]
        public bool showFolderIcons = true;
        
        [Tooltip("Make folder labels bold")]
        public bool boldFolderLabels = true;
        
        [Header("Coloring")]
        [Tooltip("Color rows based on nesting level")]
        public bool colorByNestLevel = true;
        
        [Tooltip("Highlight prefab instances")]
        public bool highlightPrefabs = true;
        
        [Tooltip("Highlight GameObjects by tag")]
        public bool highlightByTag = true;
        
        [Header("Advanced")]
        [Tooltip("Enable right-click context menu extensions")]
        public bool enableContextMenuExtensions = true;
        
        [Tooltip("Enable drag and drop enhancements")]
        public bool enableDragAndDropEnhancements = true;
        
        #endregion
        
        #region Static Methods
        
        /// <summary>
        /// Get the current settings or create a new settings asset if none exists
        /// </summary>
        public static RHierarchySettings GetOrCreateSettings()
        {
            // Try to load existing settings
            RHierarchySettings settings = AssetDatabase.LoadAssetAtPath<RHierarchySettings>(SettingsPath);
            
            // If settings don't exist, create them
            if (settings == null)
            {
                // Create settings instance
                settings = CreateInstance<RHierarchySettings>();
                
                // Ensure directory exists
                string directory = Path.GetDirectoryName(SettingsPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Create asset
                AssetDatabase.CreateAsset(settings, SettingsPath);
                AssetDatabase.SaveAssets();
            }
            
            return settings;
        }
        
        /// <summary>
        /// Show the settings in the Project Settings window
        /// </summary>
        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            var provider = new SettingsProvider("Project/Redline/Hierarchy", SettingsScope.Project)
            {
                label = "Hierarchy",
                guiHandler = (searchContext) =>
                {
                    var settings = GetOrCreateSettings();
                    var serializedObject = new SerializedObject(settings);
                    
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Redline Hierarchy Settings", EditorStyles.boldLabel);
                    EditorGUILayout.Space();
                    
                    // Draw all properties
                    SerializedProperty iterator = serializedObject.GetIterator();
                    bool enterChildren = true;
                    while (iterator.NextVisible(enterChildren))
                    {
                        // Skip script property
                        if (iterator.propertyPath == "m_Script") continue;
                        
                        EditorGUILayout.PropertyField(iterator, true);
                        enterChildren = false;
                    }
                    
                    // Apply changes
                    serializedObject.ApplyModifiedProperties();
                },
                keywords = new string[] { "Redline", "Hierarchy", "Enhanced", "Editor" }
            };
            
            return provider;
        }
        
        #endregion
    }
}
