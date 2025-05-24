using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Redline.RPM.Editor.RHierarchy
{
    /// <summary>
    /// Context menu extensions for the Redline Hierarchy
    /// </summary>
    [InitializeOnLoad]
    public static class RHierarchyContextMenu
    {
        private static RHierarchySettings _settings;
        
        /// <summary>
        /// Static constructor
        /// </summary>
        static RHierarchyContextMenu()
        {
            _settings = RHierarchySettings.GetOrCreateSettings();
            
            // Register context menu handlers
            EditorApplication.contextualPropertyMenu += OnHierarchyContextMenu;
        }
        
        /// <summary>
        /// Called when the hierarchy context menu is opened
        /// </summary>
        private static void OnHierarchyContextMenu(GenericMenu menu, SerializedProperty property)
        {
            if (!_settings.enableContextMenuExtensions)
                return;
                
            // Add custom menu items to the hierarchy context menu
            if (Selection.gameObjects.Length > 0)
            {
                // Add separator
                menu.AddSeparator("");
                
                // Add Redline Hierarchy submenu
                menu.AddItem(new GUIContent("Redline Hierarchy/Create Empty Folder"), false, CreateEmptyFolder);
                menu.AddItem(new GUIContent("Redline Hierarchy/Group Selected Objects"), false, GroupSelectedObjects);
                menu.AddSeparator("Redline Hierarchy/");
                menu.AddItem(new GUIContent("Redline Hierarchy/Expand All Children"), false, ExpandAllChildren);
                menu.AddItem(new GUIContent("Redline Hierarchy/Collapse All Children"), false, CollapseAllChildren);
                menu.AddSeparator("Redline Hierarchy/");
                menu.AddItem(new GUIContent("Redline Hierarchy/Copy Full Path"), false, CopyFullPath);
                menu.AddItem(new GUIContent("Redline Hierarchy/Copy GameObject Name"), false, CopyGameObjectName);
                
                // Add separator
                menu.AddSeparator("");
            }
        }
        
        #region Menu Actions
        
        /// <summary>
        /// Create an empty folder GameObject
        /// </summary>
        private static void CreateEmptyFolder()
        {
            GameObject selectedObject = Selection.activeGameObject;
            Transform parent = selectedObject != null ? selectedObject.transform.parent : null;
            
            GameObject folder = RHierarchyUtility.CreateFolder("New Folder", parent);
            Selection.activeGameObject = folder;
            
            // Begin rename
            EditorApplication.delayCall += () =>
            {
                var sceneHierarchyWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
                var sceneHierarchyWindow = EditorWindow.GetWindow(sceneHierarchyWindowType);
                
                var methodInfo = sceneHierarchyWindowType.GetMethod("RenameGO", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                if (methodInfo != null)
                {
                    methodInfo.Invoke(sceneHierarchyWindow, null);
                }
            };
        }
        
        /// <summary>
        /// Group selected objects under a new parent
        /// </summary>
        private static void GroupSelectedObjects()
        {
            RHierarchyUtility.GroupSelection();
        }
        
        /// <summary>
        /// Expand all children of the selected GameObject
        /// </summary>
        private static void ExpandAllChildren()
        {
            foreach (GameObject obj in Selection.gameObjects)
            {
                RHierarchyUtility.SetExpandedRecursive(obj, true);
            }
        }
        
        /// <summary>
        /// Collapse all children of the selected GameObject
        /// </summary>
        private static void CollapseAllChildren()
        {
            foreach (GameObject obj in Selection.gameObjects)
            {
                RHierarchyUtility.SetExpandedRecursive(obj, false);
            }
        }
        
        /// <summary>
        /// Copy the full path of the selected GameObject to the clipboard
        /// </summary>
        private static void CopyFullPath()
        {
            if (Selection.activeGameObject != null)
            {
                string path = RHierarchyUtility.GetFullPath(Selection.activeGameObject);
                EditorGUIUtility.systemCopyBuffer = path;
            }
        }
        
        /// <summary>
        /// Copy the name of the selected GameObject to the clipboard
        /// </summary>
        private static void CopyGameObjectName()
        {
            if (Selection.activeGameObject != null)
            {
                EditorGUIUtility.systemCopyBuffer = Selection.activeGameObject.name;
            }
        }
        
        #endregion
    }
}
