using UnityEditor;
using UnityEngine;

namespace Redline.Scripts.Editor {
    [InitializeOnLoad]
    public class RedlineInfo : EditorWindow {
        private const string Url = "https://github.com/Redline-Team";
        private const string Url1 = "https://arch-linux.pro/";
        private const string Link1 = "https://status.arch-linux.pro";
        private static Vector2 _changeLogScroll;
        private static GUIStyle _toolkitHeader;
        private static GUIStyle _redlineBottomHeader;
        private static GUIStyle _redlineHeaderLearnMoreButton;
        private static GUIStyle _redlineBottomHeaderLearnMoreButton;

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
            minSize = new Vector2(400, 720);

            _redlineBottomHeader = new GUIStyle();
            _toolkitHeader = new GUIStyle
            {
                normal = {
                    background = Resources.Load("RedlinePMHeader") as Texture2D,
                    textColor = Color.white
                },
                fixedHeight = 200
            };
        }

        public void OnGUI() {
            GUILayout.Box("", _toolkitHeader);
            SetupButtonStyle();

            // Displaying URLs
            DisplayLinkButton("Redline Ultimate Toolbox", Url);
            DisplayLinkButton("Arch-Linux.Pro", Url1);
            DisplayLinkButton("Status", Link1);

            GUILayout.Space(4);
            GUILayout.Label("Redline Version 4.0.0");
            GUILayout.Label("Redline imported correctly if you are seeing this");

            // Changelog ScrollView
            _changeLogScroll = GUILayout.BeginScrollView(_changeLogScroll, GUILayout.Width(390));
            GUILayout.Label(
                @"
== Redline Package Manager ==

The Modularized Update!
We've come to know some people dont want it all at once
So now we're taking a new approach
Repurposing the old importer we've created a new system
Where you can choose what you want modules to import
This should make the import process faster and more efficient
And it'll let you import only the modules you want

---------------------------------------------------------
∞∞∞∞∞∞∞∞∞∞∞∞Information∞∞∞∞∞∞∞∞∞∞∞∞
This unity kit provides tools and scripts for you
I am not responsible for misuse of these tools and scripts
The goal is to become the main package anyone needs
If you have issues visit the github repository issues tab
Updates can be done from within unity itself (or manually)
Bugs/Issues can be reported via github issues

---------------------------------------------------------
∞∞∞∞Contributors to Redline Unity Kit∞∞∞∞
> Developer: AromaXR (PhoenixAceVFX)
> Contributor: RileyKun
- Contributor: WTFBlaze - Made the old import system
============================================
                ");
            GUILayout.EndScrollView();

            GUILayout.Space(4);
            GUILayout.Box("", _redlineBottomHeader);

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            EditorPrefs.SetBool("Redline_ShowInfoPanel", GUILayout.Toggle(EditorPrefs.GetBool("Redline_ShowInfoPanel"), "Show at startup"));
            GUILayout.EndHorizontal();
        }

        // Helper method to set up button styles
        private void SetupButtonStyle() {
            _redlineHeaderLearnMoreButton = EditorStyles.miniButton;
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
