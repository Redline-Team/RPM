using System;
using System.Collections.Generic;
using System.IO;
using RedlineUpdater.Editor;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Redline.Editor.VPM;

namespace Redline.Scripts.Editor {
    public class RedlinePackageManager : EditorWindow {
        private const string Url = "https://github.com/Redline";
        private const string Url1 = "https://arch-linux.pro/";

        private static GUIStyle _redlineHeader;
        private static readonly Dictionary<string, string> Assets = new();
        private static Vector2 _changeLogScroll;

        // Initialize the package manager window
        [MenuItem("Redline/Package Manager", false, 501)]
        private static void Init() {
            var window = (RedlinePackageManager)GetWindow(typeof(RedlinePackageManager));
            window.Show();
        }

        // OnEnable initializes the window with necessary settings and checks
        public void OnEnable() {
            titleContent = new GUIContent("Redline Package Manager");
            minSize = new Vector2(400, 600);
            RedlineImportManager.CheckForConfigUpdate();
            LoadJson();

            // Header style setup
            _redlineHeader = new GUIStyle
            {
                normal = {
                    background = Resources.Load<Texture2D>("RedlinePMHeader"),
                    textColor = Color.white
                }
            };
        }

        // Loads the JSON configuration for the assets
        public static void LoadJson() {
            Assets.Clear();

            var configPath = Path.Combine(RedlineSettings.ProjectConfigPath, RedlineImportManager.ConfigName);
            if (!File.Exists(configPath)) {
                Debug.LogError($"[Redline] Config file not found at: {configPath}");
                return;
            }

            try {
                // Read and parse the configuration JSON file
                dynamic configJson = JObject.Parse(File.ReadAllText(configPath));
                Debug.Log("Server Asset Url is: " + configJson["config"]["serverUrl"]);
                RedlineImportManager.ServerUrl = configJson["config"]["serverUrl"].ToString();

                // Populate the assets dictionary from the JSON
                foreach (JProperty assetProperty in configJson["assets"]) {
                    var buttonName = "";
                    var file = "";

                    foreach (var assetDetail in assetProperty.Value) {
                        var detail = (JProperty)assetDetail;
                        switch (detail.Name)
                        {
	                        case "name":
		                        buttonName = detail.Value.ToString();
		                        break;
	                        case "file":
		                        file = detail.Value.ToString();
		                        break;
                        }
                    }

                    Assets[buttonName] = file;
                }
            }
            catch (Exception ex) {
                Debug.LogError($"[Redline] Error loading config file: {ex.Message}");
            }
        }

        // OnGUI creates the editor window interface
        public void OnGUI() {
            // Header UI with dynamic height to maintain aspect ratio
            if (_redlineHeader != null && _redlineHeader.normal.background != null)
            {
                Texture2D headerTexture = _redlineHeader.normal.background;
                // Original aspect ratio is 1024:217
                float aspectRatio = 1024f / 217f;
                // Calculate height based on current window width to maintain aspect ratio
                float width = EditorGUIUtility.currentViewWidth;
                float height = width / aspectRatio;
                
                // Draw the banner with calculated height
                Rect bannerRect = GUILayoutUtility.GetRect(width, height);
                GUI.Box(bannerRect, "", _redlineHeader);
            }
            GUILayout.Space(4);
            
            // DEPRECATION NOTICE
            GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f); // Darker red for deprecation
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("⚠️ DEPRECATION NOTICE ⚠️", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This Package Manager is deprecated and may be removed in a future update. " +
                                  "Please use the New VPM Manager instead for all package management needs.", 
                                  MessageType.Warning);
            
            // Button to open VPM Manager
            if (GUILayout.Button("Open VPM Manager", GUILayout.Height(30))) {
                VPM.OpenVPMManager();
            }
            EditorGUILayout.EndVertical();
            GUILayout.Space(8);
            
            // Version 3.0.0 Warning Banner
            GUI.backgroundColor = Color.red;
            EditorGUILayout.HelpBox("WARNING: Version 3.0.0 is not backwards compatible with 2.2.1. " +
                                  "You may have to manually remove the old package folder (dev.runaxr.redline) to import this update. " +
                                  "We have attempted to automate this process but cannot guarantee it will work in all cases.", 
                                  MessageType.Warning);
            GUILayout.Space(4);

            // Buttons for various actions
            CreateButton("Check for Updates", RedlineAutomaticUpdateAndInstall.AutomaticRedlineInstaller);
            CreateButton("Redline", () => Application.OpenURL(Url));
            CreateButton("arch-linux.pro", () => Application.OpenURL(Url1));

            GUILayout.Space(4);
            CreateButton("Update Config", RedlineImportManager.UpdateConfig);
            GUILayout.Space(4);

            // Display assets in a scrollable view
            _changeLogScroll = GUILayout.BeginScrollView(_changeLogScroll, GUILayout.Width(0));

            // Display asset buttons (Import/Download and Delete)
            foreach (var asset in Assets) {
                DisplayAsset(asset);
            }

            GUILayout.EndScrollView();
        }

        // Helper method to create a button with a specific action
        private static void CreateButton(string label, Action action) {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(label)) {
                action();
            }
            GUILayout.EndHorizontal();
        }

        // Displays asset buttons with their corresponding actions (Download/Import and Delete)
        private static void DisplayAsset(KeyValuePair<string, string> asset) {
            GUILayout.BeginHorizontal();
            var assetPath = Path.Combine(RedlineSettings.GetAssetPath(), asset.Value);

            if (asset.Value == "") {
                GUILayout.FlexibleSpace();
                GUILayout.Label(asset.Key);
                GUILayout.FlexibleSpace();
            } else {
                var buttonLabel = File.Exists(assetPath) ? "Import" : "Download";
                if (GUILayout.Button($"{buttonLabel} {asset.Key}")) {
                    RedlineImportManager.DownloadAndImportAssetFromServer(asset.Value);
                }

                if (GUILayout.Button("Del", GUILayout.Width(40))) {
                    RedlineImportManager.DeleteAsset(asset.Value);
                }
            }
            GUILayout.EndHorizontal();
        }
    }
}
