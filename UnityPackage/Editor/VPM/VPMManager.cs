using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace Redline.Editor.VPM
{
    /// <summary>
    /// Manages VPM repositories and packages
    /// </summary>
    public static class VPMManager
    {
        // Use [InitializeOnLoad] to ensure repositories are reloaded after domain reload
        [InitializeOnLoadAttribute]
        private static class Initializer
        {
            static Initializer()
            {
                // This will run whenever scripts are reloaded
                EditorApplication.delayCall += () => Initialize();
            }
        }
        
        private static List<VPMRepository> _repositories = new List<VPMRepository>();
        private static bool _isInitialized = false;
        
        // Cache for directory paths
        private static string _vpmConfigDir;
        private static string _packagesDir;
        private static string _tempDir;
        private static string _backupDir;
        
        // Cache for repository data
        private static Dictionary<string, DateTime> _repositoryLastModified = new Dictionary<string, DateTime>();
        private static bool _needsRepositoryRefresh = true;

        /// <summary>
        /// Gets the directory where VPM configuration files are stored
        /// </summary>
        /// <returns>The absolute path to the VPM config directory</returns>
        public static string GetVPMConfigDirectory()
        {
            if (string.IsNullOrEmpty(_vpmConfigDir))
            {
                _vpmConfigDir = RedlineSettings.GetRepositoriesPath();
                
                // Ensure the directory exists
                if (!Directory.Exists(_vpmConfigDir))
                {
                    Debug.Log($"Creating VPM config directory: {_vpmConfigDir}");
                    Directory.CreateDirectory(_vpmConfigDir);
                }
            }
            
            return _vpmConfigDir;
        }

        /// <summary>
        /// Gets the directory where packages will be installed
        /// </summary>
        /// <returns>The absolute path to the Packages directory</returns>
        public static string GetPackagesDirectory()
        {
            if (string.IsNullOrEmpty(_packagesDir))
            {
                _packagesDir = Path.Combine(Application.dataPath, "..", "Packages");
            }
            return _packagesDir;
        }

        /// <summary>
        /// Gets a temporary directory for downloading and extracting packages
        /// </summary>
        /// <returns>The absolute path to the temp directory</returns>
        public static string GetTempDirectory()
        {
            if (string.IsNullOrEmpty(_tempDir))
            {
                _tempDir = Path.Combine(Path.GetTempPath(), "Redline", "VPM");
                if (!Directory.Exists(_tempDir))
                {
                    Directory.CreateDirectory(_tempDir);
                }
            }
            return _tempDir;
        }

        /// <summary>
        /// Gets the directory where package backups are stored
        /// </summary>
        public static string GetBackupDirectory()
        {
            if (string.IsNullOrEmpty(_backupDir))
            {
                _backupDir = Path.Combine(Application.dataPath, "..", "Packages", "VPM_Backups");
                if (!Directory.Exists(_backupDir))
                {
                    Directory.CreateDirectory(_backupDir);
                }
            }
            return _backupDir;
        }

        /// <summary>
        /// Gets the path to the VCC/ALCOM repositories directory based on the current platform
        /// </summary>
        /// <returns>The path to the VCC/ALCOM repositories directory, or null if not found</returns>
        public static string GetVCCRepositoriesPath()
        {
            // Try multiple possible repository paths and return the first one that exists
            List<string> possiblePaths = new List<string>();

            // Check platform and add possible paths accordingly
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                // Windows paths
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                // VCC paths
                possiblePaths.Add(Path.Combine(localAppData, "VRChatCreatorCompanion", "Repos"));
                // ALCOM paths
                possiblePaths.Add(Path.Combine(localAppData, "ALCOM", "Repos"));
                possiblePaths.Add(Path.Combine(localAppData, "ALCOM", "Repositories"));
            }
            else if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.LinuxEditor)
            {
                // macOS/Linux paths
                // Check XDG_DATA_HOME first
                string dataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
                string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                
                if (!string.IsNullOrEmpty(dataHome))
                {
                    // VCC paths
                    possiblePaths.Add(Path.Combine(dataHome, "VRChatCreatorCompanion", "Repos"));
                    // ALCOM paths
                    possiblePaths.Add(Path.Combine(dataHome, "ALCOM", "Repos"));
                    possiblePaths.Add(Path.Combine(dataHome, "ALCOM", "Repositories"));
                }
                
                // Fallback to ~/.local/share
                // VCC paths
                // VCC path
                possiblePaths.Add(Path.Combine(home, ".local", "share", "VRChatCreatorCompanion", "Repos"));
                // ALCOM paths
                possiblePaths.Add(Path.Combine(home, ".local", "share", "ALCOM", "Repos"));
                possiblePaths.Add(Path.Combine(home, ".local", "share", "ALCOM", "Repositories"));
                
                // Additional common ALCOM paths
                possiblePaths.Add(Path.Combine(home, "ALCOM", "Repos"));
                possiblePaths.Add(Path.Combine(home, "ALCOM", "Repositories"));
            }

            // Log all paths we're checking
            Debug.Log("Checking for VCC/ALCOM repositories in the following locations:");
            foreach (string path in possiblePaths)
            {
                Debug.Log($"- {path}");
            }

            // Return the first path that exists
            foreach (string path in possiblePaths)
            {
                if (Directory.Exists(path))
                {
                    Debug.Log($"Found VCC/ALCOM repositories at: {path}");
                    return path;
                }
            }

            Debug.Log("No VCC/ALCOM repositories directory found in any of the checked locations.");
            return null;
        }

        /// <summary>
        /// Imports repositories from VCC/ALCOM repositories directory
        /// </summary>
        /// <returns>Number of repositories successfully imported</returns>
        public static async Task<int> ImportVCCRepositories()
        {
            string vccReposPath = GetVCCRepositoriesPath();
            if (vccReposPath == null)
            {
                Debug.Log("VCC/ALCOM repositories directory not found. Make sure you have VCC or ALCOM installed and have added repositories to it.");
                return 0;
            }

            Debug.Log($"Importing repositories from VCC/ALCOM directory: {vccReposPath}");
            int importedCount = 0;

            try
            {
                // Get all JSON files in the exact directory (don't scan subdirectories)
                string[] jsonFiles = Directory.GetFiles(vccReposPath, "*.json", SearchOption.TopDirectoryOnly);
                Debug.Log($"Found {jsonFiles.Length} JSON files in {vccReposPath}");

                foreach (string jsonFile in jsonFiles)
                {
                    try
                    {
                        Debug.Log($"Processing JSON file: {Path.GetFileName(jsonFile)}");
                        string json = File.ReadAllText(jsonFile);
                        JObject repoJson = JObject.Parse(json);

                        // Extract repository URL from various possible locations
                        string url = null;
                        string name = null;
                        string id = null;

                        // Try to find repository info under the "repo" key first (VCC format)
                        JToken repoToken = repoJson["repo"];
                        if (repoToken != null && repoToken.Type == JTokenType.Object)
                        {
                            Debug.Log("Found 'repo' section in JSON");
                            url = repoToken["url"]?.ToString();
                            name = repoToken["name"]?.ToString();
                            id = repoToken["id"]?.ToString();
                            
                            if (!string.IsNullOrEmpty(url))
                            {
                                Debug.Log($"Found repository under 'repo' key - Name: {name}, Id: {id}, URL: {url}");
                            }
                        }

                        // If not found under "repo", try the "repositories" format
                        if (string.IsNullOrEmpty(url))
                        {
                            // Try to find repository URL in VCC repositories format
                            // Format: { "repositories": { "<repo-id>": { "url": "<url>" } } }
                            JToken repositories = repoJson["repositories"];
                            if (repositories != null && repositories.Type == JTokenType.Object)
                            {
                                Debug.Log("Found 'repositories' section in JSON");
                                
                                // Loop through all repositories in the file (usually just one)
                                foreach (JProperty repo in repositories.Children<JProperty>())
                                {
                                    string repoId = repo.Name;
                                    JToken repoData = repo.Value;
                                    
                                    // Get the URL from the repository data
                                    url = repoData["url"]?.ToString();
                                    if (!string.IsNullOrEmpty(url))
                                    {
                                        // Try to get name and id
                                        name = repoData["name"]?.ToString();
                                        id = repoId; // Use the property name as the ID
                                        
                                        Debug.Log($"Found repository in repositories format - Name: {name}, Id: {id}, URL: {url}");
                                        break; // Just take the first one with a URL
                                    }
                                }
                            }
                        }
                        
                        // If still not found, try the standard format (direct properties)
                        if (string.IsNullOrEmpty(url))
                        {
                            url = repoJson["url"]?.ToString();
                            name = repoJson["name"]?.ToString() ?? repoJson["Name"]?.ToString();
                            id = repoJson["id"]?.ToString() ?? repoJson["Id"]?.ToString();
                            
                            if (!string.IsNullOrEmpty(url))
                            {
                                Debug.Log($"Found repository in standard format - Name: {name}, Id: {id}, URL: {url}");
                            }
                        }

                        // Skip if URL is missing
                        if (string.IsNullOrEmpty(url))
                        {
                            Debug.LogWarning($"Skipping repository in {Path.GetFileName(jsonFile)} - could not find URL");
                            continue;
                        }

                        // Check if we already have this repository by comparing Name AND Id
                        bool alreadyExists = false;
                        foreach (VPMRepository existingRepo in _repositories)
                        {
                            // Only consider it a match if BOTH Name AND Id match (when both are provided)
                            bool nameMatches = !string.IsNullOrEmpty(name) && 
                                              !string.IsNullOrEmpty(existingRepo.Name) && 
                                              existingRepo.Name.Equals(name, StringComparison.OrdinalIgnoreCase);
                            
                            bool idMatches = !string.IsNullOrEmpty(id) && 
                                            !string.IsNullOrEmpty(existingRepo.Id) && 
                                            existingRepo.Id.Equals(id, StringComparison.OrdinalIgnoreCase);
                            
                            // If both name and id are provided and both match, it's the same repository
                            if (nameMatches && idMatches)
                            {
                                Debug.Log($"Repository already exists with matching Name AND Id: {name}, {id}");
                                alreadyExists = true;
                                break;
                            }
                            
                            // Also check URL as a fallback
                            if (!string.IsNullOrEmpty(existingRepo.Url) && 
                                existingRepo.Url.Equals(url, StringComparison.OrdinalIgnoreCase))
                            {
                                Debug.Log($"Repository already exists with matching URL: {url}");
                                alreadyExists = true;
                                break;
                            }
                        }

                        if (!alreadyExists)
                        {
                            // Add the repository using its URL
                            Debug.Log($"Importing repository: {url}");
                            bool success = await AddRepositoryFromUrl(url);
                            if (success)
                            {
                                Debug.Log($"Successfully imported repository from VCC/ALCOM: {url}");
                                importedCount++;
                            }
                            else
                            {
                                Debug.LogError($"Failed to import repository: {url}");
                            }
                        }
                        else
                        {
                            Debug.Log($"Skipping repository as it already exists: {url}");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to process JSON file {jsonFile}: {e.Message}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error scanning VCC/ALCOM directory: {e.Message}");
            }

            Debug.Log($"Imported {importedCount} repositories from VCC/ALCOM");
            return importedCount;
        }

        /// <summary>
        /// Initializes the VPM manager by loading all repositories
        /// </summary>
        public static void Initialize()
        {
            if (!_isInitialized)
            {
                LoadAllRepositories();
                _isInitialized = true;
            }
        }

        /// <summary>
        /// Loads all repositories from the VPM config directory
        /// </summary>
        public static void LoadAllRepositories()
        {
            if (!_needsRepositoryRefresh && _repositories.Count > 0)
            {
                return;
            }

            string vpmDirectory = GetVPMConfigDirectory();
            if (!Directory.Exists(vpmDirectory))
            {
                Directory.CreateDirectory(vpmDirectory);
                return;
            }

            // Clear existing repositories
            _repositories.Clear();
            _repositoryLastModified.Clear();

            // Read all repository files
            string[] repoFiles = Directory.GetFiles(vpmDirectory, "*.json");
            foreach (string repoFile in repoFiles)
            {
                try
                {
                    var lastModified = File.GetLastWriteTime(repoFile);
                    if (_repositoryLastModified.TryGetValue(repoFile, out var cachedModified) && 
                        cachedModified == lastModified)
                    {
                        continue; // Skip if file hasn't changed
                    }

                    VPMRepository repo = VPMRepository.LoadFromFile(repoFile);
                    if (repo != null)
                    {
                        _repositories.Add(repo);
                        _repositoryLastModified[repoFile] = lastModified;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to load repository from {repoFile}: {e.Message}");
                }
            }

            _needsRepositoryRefresh = false;
        }

        /// <summary>
        /// Gets all loaded repositories
        /// </summary>
        /// <returns>List of loaded repositories</returns>
        public static List<VPMRepository> GetRepositories()
        {
            if (!_isInitialized)
                Initialize();

            return _repositories;
        }

        /// <summary>
        /// Checks if a repository with the same Name and Id already exists
        /// </summary>
        /// <param name="name">Name of the repository to check</param>
        /// <param name="id">Id of the repository to check</param>
        /// <returns>True if a repository with the same Name and Id exists, false otherwise</returns>
        public static bool RepositoryExists(string name, string id)
        {
            if (!_isInitialized)
                Initialize();

            return _repositories.Exists(r => 
                !string.IsNullOrEmpty(r.Name) && !string.IsNullOrEmpty(r.Id) &&
                r.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && 
                r.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Adds a new repository from a URL if it doesn't already exist
        /// </summary>
        /// <param name="url">The URL to download the repository from</param>
        /// <returns>True if the repository was added successfully, false if it already exists or failed to add</returns>
        public static async Task<bool> AddRepositoryFromUrl(string url)
        {
            VPMRepository repo = await VPMRepository.DownloadFromUrl(url);
            if (repo != null)
            {
                // Check if a repository with the same Name and Id already exists
                if (RepositoryExists(repo.Name, repo.Id))
                {
                    Debug.LogWarning($"Repository '{repo.Name}' with ID '{repo.Id}' already exists. Not adding duplicate.");
                    return false;
                }

                _repositories.Add(repo);
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Gets a list of all repository URLs from the current repositories
        /// </summary>
        private static List<string> GetRepositoryUrls()
        {
            return _repositories
                .Where(repo => !string.IsNullOrEmpty(repo.Url))
                .Select(repo => repo.Url)
                .ToList();
        }

        /// <summary>
        /// Refreshes all repositories from their URLs to get the latest packages
        /// </summary>
        /// <returns>The number of repositories successfully refreshed</returns>
        public static int RefreshRepositoriesFromUrls()
        {
            int updatedCount = 0;
            var urls = GetRepositoryUrls();
            int totalUrls = urls.Count;
            int currentUrl = 0;

            foreach (string url in urls)
            {
                currentUrl++;
                float progress = (float)currentUrl / totalUrls;
                string currentUrlStr = url; // Capture for lambda
                EditorApplication.delayCall += () =>
                {
                    EditorUtility.DisplayProgressBar("Refreshing Repositories", 
                        $"Downloading repository {currentUrl} of {totalUrls}: {currentUrlStr}", 
                        progress);
                };

                try
                {
                    VPMRepository repository = VPMRepository.DownloadFromUrl(url).GetAwaiter().GetResult();
                    if (repository != null)
                    {
                        updatedCount++;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to refresh repository from {url}, keeping existing data: {e.Message}");
                }
            }

            EditorApplication.delayCall += () =>
            {
                EditorUtility.ClearProgressBar();
            };

            return updatedCount;
        }

        /// <summary>
        /// Removes a repository
        /// </summary>
        /// <param name="repository">The repository to remove</param>
        public static void RemoveRepository(VPMRepository repository)
        {
            if (repository != null && !string.IsNullOrEmpty(repository.LocalPath))
            {
                try
                {
                    if (File.Exists(repository.LocalPath))
                    {
                        File.Delete(repository.LocalPath);
                        
                        // Also delete the associated .meta file if it exists
                        string metaFilePath = repository.LocalPath + ".meta";
                        if (File.Exists(metaFilePath))
                        {
                            File.Delete(metaFilePath);
                        }
                    }
                    _repositories.Remove(repository);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to remove repository: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Checks if a package is installed
        /// </summary>
        /// <param name="packageName">The package name to check</param>
        /// <returns>True if the package is installed</returns>
        public static bool IsPackageInstalled(string packageName)
        {
            if (string.IsNullOrEmpty(packageName))
                return false;
                
            string packagesDir = GetPackagesDirectory();
            string packageDir = Path.Combine(packagesDir, packageName);
            
            return Directory.Exists(packageDir);
        }
        
        /// <summary>
        /// Finds the latest version of a package by name across all repositories
        /// </summary>
        /// <param name="packageName">The package name to find</param>
        /// <param name="includeUnstable">Whether to include unstable versions</param>
        /// <returns>The package version, or null if not found</returns>
        public static VPMPackageVersion FindLatestPackageVersion(string packageName, bool includeUnstable = true)
        {
            if (string.IsNullOrEmpty(packageName))
                return null;
                
            VPMPackageVersion latestVersion = null;
            
            foreach (VPMRepository repo in _repositories)
            {
                if (repo.Packages != null && repo.Packages.TryGetValue(packageName, out VPMPackage package))
                {
                    VPMPackageVersion version = package.GetLatestVersion(includeUnstable);
                    
                    if (version != null)
                    {
                        // If we haven't found a version yet, or this one is newer
                        if (latestVersion == null || 
                            (version.Version != null && latestVersion.Version != null && 
                             new Version(version.Version.Split('-')[0]) > new Version(latestVersion.Version.Split('-')[0])))
                        {
                            latestVersion = version;
                        }
                    }
                }
            }
            
            return latestVersion;
        }
        
        /// <summary>
        /// Downloads and installs a package
        /// </summary>
        /// <param name="packageVersion">The package version to install</param>
        /// <param name="onProgress">Callback for progress updates</param>
        /// <param name="onComplete">Callback when installation is complete</param>
        public static async Task<bool> InstallPackage(VPMPackageVersion packageVersion, Action<float> onProgress = null, Action<bool> onComplete = null)
        {
            if (packageVersion == null || string.IsNullOrEmpty(packageVersion.Url))
            {
                Debug.LogError("Invalid package or URL");
                onComplete?.Invoke(false);
                return false;
            }

            // Create backup before installation
            BackupPackage(packageVersion.Name);

            // Handle VRChat package special cases
            if (packageVersion.Name == "com.vrchat.avatars" || packageVersion.Name == "com.vrchat.worlds")
            {
                // Check if the other VRChat package is installed
                string otherPackage = packageVersion.Name == "com.vrchat.avatars" ? "com.vrchat.worlds" : "com.vrchat.avatars";
                
                if (IsPackageInstalled(otherPackage))
                {
                    Debug.LogError($"Cannot install {packageVersion.Name} because {otherPackage} is already installed. VRChat SDK does not support installing both Avatars and Worlds packages simultaneously.");
                    onComplete?.Invoke(false);
                    return false;
                }
                
                // Check if base package is installed, if not, install it first
                if (!IsPackageInstalled("com.vrchat.base"))
                {
                    Debug.Log("VRChat Base package is required. Installing it first...");
                    
                    // Find the latest base package version
                    VPMPackageVersion baseVersion = FindLatestPackageVersion("com.vrchat.base", true);
                    
                    if (baseVersion == null)
                    {
                        Debug.LogError("Could not find VRChat Base package. Cannot install VRChat SDK.");
                        onComplete?.Invoke(false);
                        return false;
                    }
                    
                    // Install the base package first
                    bool baseInstalled = await InstallPackage(
                        baseVersion,
                        progress => onProgress?.Invoke(progress * 0.5f), // Use first half of progress for base package
                        null
                    );
                    
                    if (!baseInstalled)
                    {
                        Debug.LogError("Failed to install VRChat Base package. Cannot continue with SDK installation.");
                        onComplete?.Invoke(false);
                        return false;
                    }
                    
                    Debug.Log("VRChat Base package installed successfully. Continuing with SDK installation...");
                }
            }

            try
            {
                string tempDir = GetTempDirectory();
                string zipPath = Path.Combine(tempDir, $"{packageVersion.Name}_{packageVersion.Version}.zip");
                string extractPath = Path.Combine(tempDir, $"{packageVersion.Name}_{packageVersion.Version}");

                // Create directories if they don't exist
                if (!Directory.Exists(tempDir))
                    Directory.CreateDirectory(tempDir);

                // Download the zip file
                using (WebClient client = new WebClient())
                {
                    client.DownloadProgressChanged += (sender, args) => onProgress?.Invoke(args.ProgressPercentage / 100f);
                    await client.DownloadFileTaskAsync(new Uri(packageVersion.Url), zipPath);
                }

                // Extract the zip file
                if (Directory.Exists(extractPath))
                    Directory.Delete(extractPath, true);

                Directory.CreateDirectory(extractPath);
                await Task.Run(() => System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractPath));

                // Move the extracted files to the Packages directory
                string packagesDir = GetPackagesDirectory();
                if (!Directory.Exists(packagesDir))
                    Directory.CreateDirectory(packagesDir);

                string packageDir = Path.Combine(packagesDir, packageVersion.Name);
                if (Directory.Exists(packageDir))
                    Directory.Delete(packageDir, true);

                // Find the package content (might be in a subdirectory)
                string sourceDir = extractPath;
                string[] packageJsonFiles = Directory.GetFiles(extractPath, "package.json", SearchOption.AllDirectories);
                if (packageJsonFiles.Length > 0)
                {
                    sourceDir = Path.GetDirectoryName(packageJsonFiles[0]);
                }

                // Move the files
                DirectoryCopy(sourceDir, packageDir, true);

                // Clean up temporary files
                try
                {
                    if (File.Exists(zipPath))
                        File.Delete(zipPath);

                    if (Directory.Exists(extractPath))
                        Directory.Delete(extractPath, true);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to clean up temporary files: {e.Message}");
                }

                // Update the manifest file
                Dictionary<string, string> dependencies = null;
                if (packageVersion.Name == "com.vrchat.avatars" || packageVersion.Name == "com.vrchat.worlds")
                {
                    // Add VRChat base as a dependency
                    dependencies = new Dictionary<string, string>
                    {
                        ["com.vrchat.base"] = "3.8.1" // This should match the version we installed
                    };
                }
                VPMManifestManager.AddOrUpdatePackage(packageVersion.Name, packageVersion.Version, dependencies);

                // Refresh AssetDatabase to detect the new package
                AssetDatabase.Refresh();

                onComplete?.Invoke(true);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to install package: {e.Message}");
                onComplete?.Invoke(false);
                return false;
            }
        }

        /// <summary>
        /// Creates a backup of a package before installation
        /// </summary>
        private static void BackupPackage(string packageName)
        {
            string packagesDir = GetPackagesDirectory();
            string packageDir = Path.Combine(packagesDir, packageName);
            string backupDir = GetBackupDirectory();
            string backupPath = Path.Combine(backupDir, $"{packageName}_{DateTime.Now:yyyyMMdd_HHmmss}");

            if (Directory.Exists(packageDir))
            {
                try
                {
                    DirectoryCopy(packageDir, backupPath, true);
                    Debug.Log($"Created backup of {packageName} at {backupPath}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to create backup of {packageName}: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Restores a package from the most recent backup
        /// </summary>
        public static bool RestorePackageFromBackup(string packageName)
        {
            string backupDir = GetBackupDirectory();
            string packagesDir = GetPackagesDirectory();
            string packageDir = Path.Combine(packagesDir, packageName);

            // Find the most recent backup
            string[] backups = Directory.GetDirectories(backupDir, $"{packageName}_*");
            if (backups.Length == 0)
            {
                Debug.LogError($"No backups found for {packageName}");
                return false;
            }

            string latestBackup = backups.OrderByDescending(d => d).First();

            try
            {
                // Remove current package if it exists
                if (Directory.Exists(packageDir))
                {
                    Directory.Delete(packageDir, true);
                }

                // Restore from backup
                DirectoryCopy(latestBackup, packageDir, true);
                Debug.Log($"Restored {packageName} from backup {latestBackup}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to restore {packageName} from backup: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Recursively copies a directory
        /// </summary>
        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException($"Source directory does not exist or could not be found: {sourceDirName}");
            }

            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, true);
            }

            if (copySubDirs)
            {
                DirectoryInfo[] dirs = dir.GetDirectories();
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }

        /// <summary>
        /// Removes an installed package
        /// </summary>
        /// <param name="packageName">The name of the package to remove</param>
        public static void RemovePackage(string packageName)
        {
            if (string.IsNullOrEmpty(packageName))
                return;

            string packagesDir = GetPackagesDirectory();
            string packageDir = Path.Combine(packagesDir, packageName);

            try
            {
                if (Directory.Exists(packageDir))
                {
                    Directory.Delete(packageDir, true);
                    
                    // Also delete the associated .meta file if it exists
                    string metaFilePath = packageDir + ".meta";
                    if (File.Exists(metaFilePath))
                    {
                        File.Delete(metaFilePath);
                    }

                    // Update the manifest file
                    VPMManifestManager.RemovePackage(packageName);

                    // Refresh AssetDatabase to detect the removed package
                    AssetDatabase.Refresh();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to remove package {packageName}: {e.Message}");
            }
        }

        /// <summary>
        /// Marks repositories for refresh on next access
        /// </summary>
        public static void MarkRepositoriesForRefresh()
        {
            _needsRepositoryRefresh = true;
        }
    }
}
