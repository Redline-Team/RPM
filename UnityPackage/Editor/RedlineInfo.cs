using UnityEditor;
using UnityEngine;

namespace Redline.Editor {
    [InitializeOnLoad]
    public class RedlineInfo : EditorWindow {
        private const string Url = "https://github.com/Redline-Team";
        private const string Url1 = "https://arch-linux.pro/";
        private const string Link1 = "https://status.arch-linux.pro";
        private static Vector2 _changeLogScroll;
        private static Vector2 _targetScroll;
        private static GUIStyle _toolkitHeader;
        private static GUIStyle _redlineBottomHeader;
        private static GUIStyle _redlineHeaderLearnMoreButton;
        private static GUIStyle _redlineBottomHeaderLearnMoreButton;
        private const float ScrollSpeed = 0.1f;

        static RedlineInfo() {
            // Ensure that the splash screen displays correctly
            EditorApplication.update -= DoSplashScreen;
            EditorApplication.update += DoSplashScreen;
        }

        private static void DoSplashScreen() {
            EditorApplication.update -= DoSplashScreen;
            // Ensure "Redline_ShowInfoPanel" exists in EditorPrefs
            if (!EditorPrefs.HasKey("Redline_ShowInfoPanel")) {
                EditorPrefs.SetBool("Redline_ShowInfoPanel", true);
            }
        }

        [MenuItem("Redline/Info", false, 500)]
        private static void Init() {
            var window = (RedlineInfo)GetWindow(typeof(RedlineInfo));
            window.Show();
        }

        public void OnEnable() {
            titleContent = new GUIContent("Redline Info");
            minSize = new Vector2(500, 720);

            _redlineBottomHeader = new GUIStyle();
            _toolkitHeader = new GUIStyle
            {
                normal = {
                    background = Resources.Load("RedlinePMHeader") as Texture2D,
                    textColor = Color.white
                }
                // No fixed height - will be calculated dynamically to maintain aspect ratio
            };
        }

        public void OnGUI() {
            // Draw the banner with dynamic height to maintain aspect ratio
            if (_toolkitHeader != null && _toolkitHeader.normal.background != null)
            {
                Texture2D headerTexture = _toolkitHeader.normal.background;
                // Original aspect ratio is 1024:217
                float aspectRatio = 1024f / 217f;
                // Calculate height based on current window width to maintain aspect ratio
                float width = position.width;
                float height = width / aspectRatio;
                
                // Draw the banner with calculated height
                Rect bannerRect = GUILayoutUtility.GetRect(width, height);
                GUI.Box(bannerRect, "", _toolkitHeader);
            }
            SetupButtonStyle();

            // Displaying URLs
            DisplayLinkButton("Redline Package Manager", Url);
            DisplayLinkButton("Arch-Linux.Pro", Url1);
            DisplayLinkButton("Status", Link1);

            GUILayout.Space(4);
            GUILayout.Label($"Redline Version {RedlineVersionUtility.GetCurrentVersion()}");
            GUILayout.Label("Redline imported correctly if you are seeing this");

            // Smooth scrolling implementation
            _targetScroll = GUILayout.BeginScrollView(_changeLogScroll, GUILayout.Width(position.width));
            _changeLogScroll = Vector2.Lerp(_changeLogScroll, _targetScroll, ScrollSpeed);
            GUILayout.Label(
                @"
== Redline Package Manager ==

A powerful VRChat Package Manager for Unity!

---------------------------------------------------------
∞∞∞∞∞∞∞∞∞∞∞∞Features∞∞∞∞∞∞∞∞∞∞∞∞
• Manage VRChat packages directly in Unity
• Import repositories from VCC/ALCOM
• View and manage package dependencies
• Track installation history
• Compare package versions
• Backup and restore packages
• Advanced search and filtering
• Automatic dependency resolution
• Support for multiple repositories
• Clean and intuitive interface

---------------------------------------------------------
∞∞∞∞∞∞∞∞∞∞∞∞Information∞∞∞∞∞∞∞∞∞∞∞∞
Redline Package Manager (RPM) is a comprehensive tool for managing
VRChat packages in Unity. It provides a user-friendly interface for
installing, updating, and managing packages, with features like
dependency visualization, version comparison, and installation history.

The package is designed to work seamlessly with VRChat's package
ecosystem, supporting both official and community repositories.
It can import repositories from VCC/ALCOM and provides advanced
features for package management.

For issues, feature requests, or bug reports, please visit our
GitHub repository. Updates can be installed directly through
Unity or manually via the package manager.

---------------------------------------------------------
∞∞∞∞Contributors to Redline Package Manager∞∞∞∞
> Developer: LoonieLoonz
> Contributor: RileyKun
============================================
                ");
            GUILayout.EndScrollView();

            GUILayout.Space(4);
            GUILayout.Box("", _redlineBottomHeader);

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            EditorPrefs.SetBool("Redline_ShowInfoPanel", GUILayout.Toggle(EditorPrefs.GetBool("Redline_ShowInfoPanel"), "Show at startup"));
            GUILayout.EndHorizontal();

            // Force repaint to update smooth scrolling
            if (_changeLogScroll != _targetScroll) {
                Repaint();
            }
        }

        // Helper method to set up button styles
        private void SetupButtonStyle() {
            _redlineHeaderLearnMoreButton = new GUIStyle(EditorStyles.miniButton);
            _redlineHeaderLearnMoreButton.normal.textColor = Color.black;
            _redlineHeaderLearnMoreButton.fontSize = 12;
            _redlineHeaderLearnMoreButton.border = new RectOffset(10, 10, 10, 10);
            var texture = AssetDatabase.GetBuiltinExtraResource<Texture2D>("UI/Skin/UISprite.psd");
            _redlineHeaderLearnMoreButton.normal.background = texture;
            _redlineHeaderLearnMoreButton.active.background = texture;
        }

        // Helper method to display buttons that open URLs
        private static void DisplayLinkButton(string label, string url) {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(label)) {
                Application.OpenURL(url);
            }
            GUILayout.EndHorizontal();
        }
    }
}
