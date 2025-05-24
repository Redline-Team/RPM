using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Redline.RPM.Editor.RHierarchy
{
    /// <summary>
    /// Utility methods for the Redline Hierarchy
    /// </summary>
    public static class RHierarchyUtility
    {
        /// <summary>
        /// Get the full hierarchy path of a GameObject
        /// </summary>
        public static string GetFullPath(GameObject gameObject)
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
        
        /// <summary>
        /// Get the hierarchy depth of a GameObject
        /// </summary>
        public static int GetHierarchyDepth(GameObject gameObject)
        {
            if (gameObject == null)
                return 0;
                
            int depth = 0;
            Transform parent = gameObject.transform.parent;
            
            while (parent != null)
            {
                depth++;
                parent = parent.parent;
            }
            
            return depth;
        }
        
        /// <summary>
        /// Check if a GameObject is acting as a folder (empty with children)
        /// </summary>
        public static bool IsFolder(GameObject gameObject)
        {
            if (gameObject == null)
                return false;
                
            // Check if it only has a Transform component
            bool isEmpty = gameObject.GetComponents<Component>().Length <= 1;
            
            // Check if it has children
            bool hasChildren = gameObject.transform.childCount > 0;
            
            return isEmpty && hasChildren;
        }
        
        /// <summary>
        /// Create a new folder GameObject at the specified path
        /// </summary>
        public static GameObject CreateFolder(string name, Transform parent = null)
        {
            GameObject folder = new GameObject(name);
            
            if (parent != null)
                folder.transform.SetParent(parent);
                
            Undo.RegisterCreatedObjectUndo(folder, "Create Folder");
            
            return folder;
        }
        
        /// <summary>
        /// Group selected GameObjects under a new parent
        /// </summary>
        public static GameObject GroupSelection(string groupName = "Group")
        {
            // Get current selection
            GameObject[] selection = Selection.gameObjects;
            
            if (selection.Length == 0)
                return null;
                
            // Find common parent
            Transform commonParent = FindCommonParent(selection);
            
            // Create group GameObject
            GameObject groupObject = new GameObject(groupName);
            Undo.RegisterCreatedObjectUndo(groupObject, "Group Objects");
            
            // Set parent
            if (commonParent != null)
            {
                Undo.SetTransformParent(groupObject.transform, commonParent, "Set Group Parent");
            }
            
            // Move selected objects under the group
            foreach (GameObject obj in selection)
            {
                Undo.SetTransformParent(obj.transform, groupObject.transform, "Group Objects");
            }
            
            // Select the new group
            Selection.activeGameObject = groupObject;
            
            return groupObject;
        }
        
        /// <summary>
        /// Find the common parent of multiple GameObjects
        /// </summary>
        private static Transform FindCommonParent(GameObject[] objects)
        {
            if (objects.Length == 0)
                return null;
                
            if (objects.Length == 1)
                return objects[0].transform.parent;
                
            // Get all parents of the first object
            List<Transform> parentsOfFirst = new List<Transform>();
            Transform parent = objects[0].transform.parent;
            
            while (parent != null)
            {
                parentsOfFirst.Add(parent);
                parent = parent.parent;
            }
            
            // Find the deepest common parent
            for (int i = 1; i < objects.Length; i++)
            {
                parent = objects[i].transform.parent;
                
                // If any object has no parent, there's no common parent
                if (parent == null)
                    return null;
                    
                // Check each parent of the current object
                bool foundCommon = false;
                while (parent != null)
                {
                    if (parentsOfFirst.Contains(parent))
                    {
                        // Keep only parents that are common to all objects
                        int startIndex = parentsOfFirst.IndexOf(parent);
                        parentsOfFirst.RemoveRange(0, startIndex);
                        foundCommon = true;
                        break;
                    }
                    
                    parent = parent.parent;
                }
                
                // If no common parent found, return null
                if (!foundCommon)
                    return null;
            }
            
            // Return the deepest common parent
            return parentsOfFirst.Count > 0 ? parentsOfFirst[0] : null;
        }
        
        /// <summary>
        /// Expand or collapse all children of a GameObject in the hierarchy
        /// </summary>
        public static void SetExpandedRecursive(GameObject gameObject, bool expanded)
        {
            if (gameObject == null)
                return;
                
            // Get the instance ID
            int instanceID = gameObject.GetInstanceID();
            
            // Set expanded state
            EditorGUIUtility.SetIconSize(Vector2.one * 16);
            
            // Use reflection to access internal SetExpanded method
            var sceneHierarchyWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
            var sceneHierarchyWindow = EditorWindow.GetWindow(sceneHierarchyWindowType);
            
            var methodInfo = sceneHierarchyWindowType.GetMethod("SetExpandedRecursive", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            if (methodInfo != null)
            {
                methodInfo.Invoke(sceneHierarchyWindow, new object[] { instanceID, expanded });
            }
        }
    }
}
