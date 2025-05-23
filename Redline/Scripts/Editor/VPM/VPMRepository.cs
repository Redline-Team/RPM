using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Redline.Editor.VPM
{
    /// <summary>
    /// Represents a VPM Repository with its packages
    /// </summary>
    [Serializable]
    public class VPMRepository
    {
        public string Name;
        public string Author;
        public string Url;
        public string Id;
        public Dictionary<string, VPMPackage> Packages = new Dictionary<string, VPMPackage>();
        
        /// <summary>
        /// The local file path where this repository is stored
        /// </summary>
        [NonSerialized]
        public string LocalPath;

        /// <summary>
        /// Downloads a VPM repository from a URL and saves it to the local VPM directory
        /// </summary>
        /// <param name="url">The URL to download the repository from</param>
        /// <returns>The downloaded and parsed repository, or null if failed</returns>
        public static async Task<VPMRepository> DownloadFromUrl(string url)
        {
            Debug.Log($"Downloading repository from URL: {url}");
            try
            {
                // Create the directory if it doesn't exist
                string vpmDirectory = VPMManager.GetVPMConfigDirectory();
                Debug.Log($"VPM Directory from Manager: {vpmDirectory}");
                
                if (!Directory.Exists(vpmDirectory))
                {
                    Debug.Log($"Creating VPM directory: {vpmDirectory}");
                    Directory.CreateDirectory(vpmDirectory);
                }
                else
                {
                    Debug.Log($"VPM directory already exists: {vpmDirectory}");
                }

                // Download the repository JSON
                Debug.Log("Downloading repository JSON...");
                using (WebClient client = new WebClient())
                {
                    string json = await Task.Run(() => client.DownloadString(url));
                    Debug.Log($"Downloaded JSON (first 100 chars): {json.Substring(0, Math.Min(100, json.Length))}...");
                    
                    // Parse the JSON
                    VPMRepository repository = ParseRepositoryJson(json);
                    
                    if (repository != null)
                    {
                        Debug.Log($"Repository parsed successfully. Name: {repository.Name}, Author: {repository.Author}");
                        
                        // Extract repository name from the URL if the name is missing
                        if (string.IsNullOrEmpty(repository.Name))
                        {
                            Debug.Log("Repository name is missing, extracting from URL...");
                            try
                            {
                                // Try to extract a meaningful name from the URL
                                Uri uri = new Uri(url);
                                string host = uri.Host;
                                Debug.Log($"URL host: {host}");
                                
                                // Use the domain name as part of the repository name
                                string[] hostParts = host.Split('.');
                                if (hostParts.Length >= 2)
                                {
                                    repository.Name = hostParts[hostParts.Length - 2];
                                    // Capitalize first letter
                                    repository.Name = char.ToUpper(repository.Name[0]) + repository.Name.Substring(1) + " Repo";
                                    Debug.Log($"Generated repository name from host: {repository.Name}");
                                }
                                else
                                {
                                    repository.Name = host + " Repo";
                                    Debug.Log($"Generated repository name from host: {repository.Name}");
                                }
                            }
                            catch (Exception ex)
                            {
                                // Fallback if URI parsing fails
                                Debug.LogError($"Failed to extract name from URL: {ex.Message}");
                                repository.Name = "Repository_" + DateTime.Now.Ticks;
                                Debug.Log($"Using fallback repository name: {repository.Name}");
                            }
                        }
                        
                        // Generate a filename based on the repository name
                        string repoName = repository.Name;
                        Debug.Log($"Using repository name for file: {repoName}");
                        
                        // Ensure the repository name is valid for a filename
                        // Keep spaces but replace invalid filename characters
                        foreach (char invalidChar in Path.GetInvalidFileNameChars())
                        {
                            if (invalidChar != ' ') // Keep spaces
                            {
                                repoName = repoName.Replace(invalidChar, '_');
                            }
                        }
                        
                        string filePath = Path.Combine(vpmDirectory, repoName + ".json");
                        Debug.Log($"Sanitized file name: {repoName}.json");
                        Debug.Log($"Full file path for saving: {filePath}");
                        
                        // Save the formatted JSON to disk
                        string formattedJson = JsonConvert.SerializeObject(repository, Formatting.Indented);
                        Debug.Log($"Saving JSON to file: {filePath}");
                        
                        try {
                            File.WriteAllText(filePath, formattedJson);
                            Debug.Log($"Successfully saved repository to: {filePath}");
                            
                            // Verify the file was created
                            if (File.Exists(filePath)) {
                                Debug.Log($"Verified file exists at: {filePath}");
                            } else {
                                Debug.LogError($"File was not created at: {filePath}");
                            }
                        } catch (Exception ex) {
                            Debug.LogError($"Failed to write file: {ex.Message}");
                        }
                        
                        repository.LocalPath = filePath;
                        return repository;
                    }
                    else
                    {
                        Debug.LogError("Failed to parse repository JSON");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to download repository from {url}: {e.Message}");
                Debug.LogException(e);
            }
            
            return null;
        }

        /// <summary>
        /// Parses a JSON string into a VPMRepository object
        /// </summary>
        /// <param name="json">The JSON string to parse</param>
        /// <returns>The parsed repository, or null if parsing failed</returns>
        public static VPMRepository ParseRepositoryJson(string json)
        {
            try
            {
                Debug.Log($"Parsing JSON: {json.Substring(0, Math.Min(100, json.Length))}...");
                JObject repoJson = JObject.Parse(json);
                
                // Helper function to get property with case insensitivity
                string GetPropertyCaseInsensitive(JObject obj, string propertyName)
                {
                    // Try exact match first
                    if (obj[propertyName] != null)
                        return obj[propertyName].ToString();
                    
                    // Try case-insensitive match
                    foreach (var prop in obj.Properties())
                    {
                        if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                            return prop.Value.ToString();
                    }
                    
                    return null;
                }
                
                VPMRepository repository = new VPMRepository
                {
                    Name = GetPropertyCaseInsensitive(repoJson, "name"),
                    Author = GetPropertyCaseInsensitive(repoJson, "author"),
                    Url = GetPropertyCaseInsensitive(repoJson, "url"),
                    Id = GetPropertyCaseInsensitive(repoJson, "id"),
                    Packages = new Dictionary<string, VPMPackage>()
                };
                
                Debug.Log($"Parsed repository: Name={repository.Name}, Author={repository.Author}, Url={repository.Url}, Id={repository.Id}");

                // Parse packages - look for both "packages" and "Packages"
                JObject packages = repoJson["packages"] as JObject ?? repoJson["Packages"] as JObject;
                if (packages != null)
                {
                    Debug.Log($"Found packages section with {packages.Count} packages");
                    foreach (var packagePair in packages)
                    {
                        string packageId = packagePair.Key;
                        JObject packageObj = packagePair.Value as JObject;
                        
                        if (packageObj != null)
                        {
                            VPMPackage package = new VPMPackage
                            {
                                Id = packageId,
                                Versions = new Dictionary<string, VPMPackageVersion>()
                            };

                            // Parse versions - look for both "versions" and "Versions"
                            JObject versions = packageObj["versions"] as JObject ?? packageObj["Versions"] as JObject;
                            if (versions != null)
                            {
                                Debug.Log($"Found versions section with {versions.Count} versions for package {packageId}");
                                foreach (var versionPair in versions)
                                {
                                    string versionString = versionPair.Key;
                                    JObject versionObj = versionPair.Value as JObject;
                                    
                                    if (versionObj != null)
                                    {
                                        VPMPackageVersion version = new VPMPackageVersion
                                        {
                                            Name = GetPropertyCaseInsensitive(versionObj, "name"),
                                            DisplayName = GetPropertyCaseInsensitive(versionObj, "displayName"),
                                            Version = GetPropertyCaseInsensitive(versionObj, "version"),
                                            Unity = GetPropertyCaseInsensitive(versionObj, "unity"),
                                            Description = GetPropertyCaseInsensitive(versionObj, "description"),
                                            ChangelogUrl = GetPropertyCaseInsensitive(versionObj, "changelogUrl"),
                                            Url = GetPropertyCaseInsensitive(versionObj, "url"),
                                            ZipSHA256 = GetPropertyCaseInsensitive(versionObj, "zipSHA256")
                                        };
                                        
                                        // Parse author if available - look for both "author" and "Author"
                                        JObject authorObj = versionObj["author"] as JObject ?? versionObj["Author"] as JObject;
                                        if (authorObj != null)
                                        {
                                            version.AuthorName = GetPropertyCaseInsensitive(authorObj, "name");
                                            version.AuthorUrl = GetPropertyCaseInsensitive(authorObj, "url");
                                        }
                                        
                                        package.Versions[versionString] = version;
                                    }
                                }
                            }
                            
                            repository.Packages[packageId] = package;
                        }
                    }
                }
                
                return repository;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse repository JSON: {e.Message}");
                Debug.LogException(e);
                return null;
            }
        }

        /// <summary>
        /// Loads a repository from a local file
        /// </summary>
        /// <param name="filePath">The path to the repository JSON file</param>
        /// <returns>The loaded repository, or null if loading failed</returns>
        public static VPMRepository LoadFromFile(string filePath)
        {
            Debug.Log($"Loading repository from file: {filePath}");
            try
            {
                if (File.Exists(filePath))
                {
                    Debug.Log($"File exists: {filePath}");
                    string json = File.ReadAllText(filePath);
                    Debug.Log($"Read {json.Length} characters from file");
                    
                    VPMRepository repository = ParseRepositoryJson(json);
                    if (repository != null)
                    {
                        Debug.Log($"Successfully parsed repository: {repository.Name}");
                        repository.LocalPath = filePath;
                        return repository;
                    }
                    else
                    {
                        Debug.LogError($"Failed to parse repository from file: {filePath}");
                    }
                }
                else
                {
                    Debug.LogError($"File does not exist: {filePath}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load repository from {filePath}: {e.Message}");
                Debug.LogException(e);
            }
            
            return null;
        }
    }
}
