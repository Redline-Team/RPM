using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Redline.Editor.RedlineUpdater;

namespace Redline.Editor {
    public class RedlineUpdateCheck: MonoBehaviour {
        [InitializeOnLoad]
        public class Startup {
            private const string VersionURL = "https://raw.githubusercontent.com/Redline-Team/RPM/refs/heads/main/latestrelease.txt";
            private const int RequestTimeoutSeconds = 10;
            private static readonly string CurrentVersion = RedlineVersionUtility.GetCurrentVersion();
            private static readonly HttpClient httpClient = new HttpClient { Timeout = System.TimeSpan.FromSeconds(RequestTimeoutSeconds) };

            static Startup() {
                Check();
            }

            private static async void Check() {
                try {
                    await CheckForUpdates();
                }
                catch (HttpRequestException ex) {
                    Debug.LogError($"[Redline] Network error while checking for updates: {ex.Message}");
                    EditorUtility.DisplayDialog(
                        "Redline Update Check Failed",
                        "Could not connect to the update server. Please check your internet connection and try again.",
                        "OK");
                }
                catch (TaskCanceledException) {
                    Debug.LogError("[Redline] Update check timed out");
                    EditorUtility.DisplayDialog(
                        "Redline Update Check Failed",
                        "The update check took too long to complete. Please try again later.",
                        "OK");
                }
                catch (System.Exception ex) {
                    Debug.LogError($"[Redline] Unexpected error while checking for updates: {ex.Message}");
                    EditorUtility.DisplayDialog(
                        "Redline Update Check Failed",
                        "An unexpected error occurred while checking for updates. Please try again later.",
                        "OK");
                }
            }

            private static async Task CheckForUpdates() {
                // Validate local version first
                var thisVersion = CurrentVersion;
                if (!RedlineVersionUtility.IsValidSemanticVersion(thisVersion)) {
                    Debug.LogError("[Redline] Invalid local version format detected. Please update to the latest version.");
                    EditorUtility.DisplayDialog(
                        "Redline Version Error",
                        "Your version of Redline has an unknown or invalid version number. Please install the latest version from the GitHub repository.",
                        "OK");
                    return;
                }
                
                // Get server version
                var result = await httpClient.GetAsync(VersionURL);
                result.EnsureSuccessStatusCode();
                var strServerVersion = await result.Content.ReadAsStringAsync();
                
                // Validate server version
                if (!RedlineVersionUtility.IsValidSemanticVersion(strServerVersion)) {
                    Debug.LogError($"[Redline] Invalid server version format: {strServerVersion}");
                    EditorUtility.DisplayDialog(
                        "Redline Update Check Failed",
                        "Received an invalid version number from the server. Please try again later.",
                        "OK");
                    return;
                }
                
                // Compare versions
                int comparisonResult = RedlineVersionUtility.CompareVersions(thisVersion, strServerVersion);
                
                if (comparisonResult < 0) {
                    // Local version is older than server version - update needed
                    RedlineAutomaticUpdateAndInstall.AutomaticRedlineInstaller();
                } 
                else if (comparisonResult > 0) {
                    // Local version is newer than server version - development build
                    Debug.Log($"[Redline] Development Build Version Assumed (Local: {thisVersion}, Server: {strServerVersion})");
                }
                else {
                    // Versions are equal - up to date
                    Debug.Log($"[Redline] Up to date (Version: {thisVersion})");
                }
            }
        }
    }
}