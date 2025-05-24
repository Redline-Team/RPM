using System;
using System.Collections.Generic;
using UnityEngine;

namespace Redline.Editor
{
    [Serializable]
    public class CustomTheme
    {
        public string Name;
        
        public enum UnityTheme { Dark, Light, Both }
        
        public UnityTheme unityTheme;
        public bool IsUnDeletable;
        public bool IsUnEditable;
        public string Version;
        public List<UIItem> Items;
        
        [Serializable]
        public class UIItem
        {
            public string Name;
            public Color Color;
        }
    }
}
