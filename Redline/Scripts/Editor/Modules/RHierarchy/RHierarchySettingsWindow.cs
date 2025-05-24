using UnityEditor;
using UnityEngine;

namespace Redline.RPM.Editor.RHierarchy
{
    /// <summary>
    /// Settings window for the Redline Hierarchy
    /// </summary>
    public class RHierarchySettingsWindow : EditorWindow
    {
        private RHierarchySettings _settings;
        private SerializedObject _serializedObject;
        private Vector2 _scrollPosition;
        
        /// <summary>
        /// Open the settings window
        /// </summary>
        [MenuItem("Redline/Modules/RHierarchy/Settings", false, 1000)]
        public static void OpenWindow()
        {
            RHierarchySettingsWindow window = GetWindow<RHierarchySettingsWindow>(true, "Redline Hierarchy Settings", true);
            window.minSize = new Vector2(350, 450);
            window.Show();
        }
        
        private void OnEnable()
        {
            // Load settings
            _settings = RHierarchySettings.GetOrCreateSettings();
            _serializedObject = new SerializedObject(_settings);
        }
        
        private void OnGUI()
        {
            if (_settings == null || _serializedObject == null)
            {
                _settings = RHierarchySettings.GetOrCreateSettings();
                _serializedObject = new SerializedObject(_settings);
            }
            
            // Begin scroll view
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            // Update the serialized object
            _serializedObject.Update();
            
            // Draw header
            EditorGUILayout.Space(10);
            GUILayout.Label("Redline Hierarchy Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // Draw horizontal line
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space(5);
            
            // Draw all properties
            SerializedProperty iterator = _serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                // Skip script property
                if (iterator.propertyPath == "m_Script") continue;
                
                EditorGUILayout.PropertyField(iterator, true);
                enterChildren = false;
            }
            
            // Apply changes
            if (_serializedObject.hasModifiedProperties)
            {
                _serializedObject.ApplyModifiedProperties();
                
                // Force hierarchy window to repaint
                EditorApplication.RepaintHierarchyWindow();
            }
            
            EditorGUILayout.Space(10);
            
            // Add buttons at the bottom
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Reset to Defaults", GUILayout.Width(120)))
            {
                if (EditorUtility.DisplayDialog("Reset Settings", 
                    "Are you sure you want to reset all settings to their default values?", 
                    "Reset", "Cancel"))
                {
                    ResetToDefaults();
                }
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Close", GUILayout.Width(80)))
            {
                Close();
            }
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // End scroll view
            EditorGUILayout.EndScrollView();
        }
        
        /// <summary>
        /// Reset settings to default values
        /// </summary>
        private void ResetToDefaults()
        {
            // Create a new settings instance with default values
            RHierarchySettings defaultSettings = CreateInstance<RHierarchySettings>();
            
            // Copy default values to current settings
            _settings.enhancedHierarchyEnabled = defaultSettings.enhancedHierarchyEnabled;
            _settings.showComponentIcons = defaultSettings.showComponentIcons;
            _settings.showToggleIcons = defaultSettings.showToggleIcons;
            _settings.showCustomPrefix = defaultSettings.showCustomPrefix;
            _settings.showFolderIcons = defaultSettings.showFolderIcons;
            _settings.boldFolderLabels = defaultSettings.boldFolderLabels;
            _settings.colorByNestLevel = defaultSettings.colorByNestLevel;
            _settings.highlightPrefabs = defaultSettings.highlightPrefabs;
            _settings.highlightByTag = defaultSettings.highlightByTag;
            _settings.enableContextMenuExtensions = defaultSettings.enableContextMenuExtensions;
            _settings.enableDragAndDropEnhancements = defaultSettings.enableDragAndDropEnhancements;
            
            // Update serialized object
            _serializedObject.Update();
            
            // Force hierarchy window to repaint
            EditorApplication.RepaintHierarchyWindow();
            
            // Destroy temporary instance
            DestroyImmediate(defaultSettings);
        }
    }
}
