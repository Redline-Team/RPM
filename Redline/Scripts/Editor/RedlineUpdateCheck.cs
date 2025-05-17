using System.IO;
using System.Net.Http;
using RedlineUpdater.Editor;
using UnityEditor;
using UnityEngine;

namespace Redline.Scripts.Editor {
    public class RedlineUpdateCheck: MonoBehaviour {
        [InitializeOnLoad]
        public class Startup {
            private
                const string VersionURL = "https://redline.arch-linux.pro/API/version.txt";

            private static readonly string CurrentVersion =
                File.ReadAllText("Packages/dev.redline-team.rpm/RedlineUpdater/Editor/RedlineVersion.txt");

            static Startup() {
                Check();
            }

            private static async void Check() {
                var httpClient = new HttpClient();
                var result = await httpClient.GetAsync(VersionURL);
                var strServerVersion = await result.Content.ReadAsStringAsync();

                var thisVersion = CurrentVersion;

                if (strServerVersion != thisVersion) {
                    RedlineAutomaticUpdateAndInstall.AutomaticRedlineInstaller();
                }
            }
        }
    }
}