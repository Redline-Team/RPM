using System;
using UnityEditor;

namespace Redline.Editor
{
    /// <summary>
    /// Utility class for opening Unity's IMGUI Debugger window
    /// </summary>
    public static class IMGUIDebugger
    {
        private static readonly Type DebuggerWindowType = Type.GetType("UnityEditor.GUIViewDebuggerWindow,UnityEditor");

        [MenuItem("Redline/Modules/Themer/IMGUI Debugger")]
        public static void Open() => EditorWindow.GetWindow(DebuggerWindowType).Show();
    }
}