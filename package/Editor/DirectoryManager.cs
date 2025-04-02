using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Eu4ng.Utilities.Editor
{
    public static class DirectoryManager
    {
        const string ASSETS = "Assets";

        public static void CreateAsset(Object asset, string folderPath, string fileName, string extension = "asset")
        {
            var directory = CreateDirectory(folderPath);
            AssetDatabase.CreateAsset(asset, Path.Combine(directory, fileName + "." + extension));
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(asset));
        }

        public static void ClearDirectory(string folderPath)
        {
            string directory = CombineFolders(ParseFolderPath(folderPath));

            if (!Directory.Exists(directory)) return;
            var filePaths = Directory.GetFiles(directory);
            foreach (var filePath in filePaths)
            {
                Directory.Delete(filePath);
            }
        }

        public static string CreateDirectory(string folderPath)
        {
            string directory = string.Empty;
            var folders = ParseFolderPath(folderPath);
            foreach (string folder in folders)
            {
                directory = Path.Combine(directory, folder);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    LogUtilities.Log("Directory(" + directory + ") is created.");
                }
            }

            AssetDatabase.ImportAsset(directory);

            return directory;
        }

        static List<string> ParseFolderPath(string folderPath)
        {
            var folders = new List<string>
            {
                ASSETS
            };

            var parsedFolderPath = new List<string>();
            foreach (var folder in folderPath.Split('/'))
            {
                parsedFolderPath.AddRange(folder.Split(Path.PathSeparator));
            }
            parsedFolderPath.Remove(ASSETS);

            folders.AddRange(parsedFolderPath);

            return folders;
        }

        static string CombineFolders(List<string> folders)
        {
            var directory = string.Empty;
            foreach (var folder in folders)
            {
                directory = Path.Combine(directory, folder);
            }

            return directory;
        }
    }
}
