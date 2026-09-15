using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FedericoTools.SpriteToolBox.Editor
{
    internal sealed class SpriteToolboxDocumentPersistenceService
    {
        private const string LastDocumentPreferenceKey = "FedericoTools.SpriteToolbox.LastDocument";
        private const string RecentFilesPreferenceKeyPrefix = "FedericoTools.SpriteToolbox.RecentFiles.";
        private readonly List<string> recentFilePaths;

        public SpriteToolboxDocumentPersistenceService(List<string> recentFilePaths)
        {
            this.recentFilePaths = recentFilePaths ?? throw new ArgumentNullException(nameof(recentFilePaths));
            LoadRecentFiles();
        }

        public IReadOnlyList<string> RecentFilePaths => recentFilePaths;

        public SpriteDocumentAsset RestoreLastDocument()
        {
            string assetPath = EditorPrefs.GetString(LastDocumentPreferenceKey, string.Empty);
            if (string.IsNullOrEmpty(assetPath)) return null;
            SpriteDocumentAsset asset = AssetDatabase.LoadAssetAtPath<SpriteDocumentAsset>(assetPath);
            if (asset == null) EditorPrefs.DeleteKey(LastDocumentPreferenceKey);
            return asset;
        }

        public void Save(SpriteDocumentAsset asset)
        {
            if (asset == null) return;
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Remember(asset);
        }

        public SpriteDocumentAsset SaveAs(SpriteDocument document)
        {
            string assetPath = EditorUtility.SaveFilePanelInProject("Save Sprite Toolbox project", "NewSpriteDocument", "asset", "Choose a Unity project folder.");
            SpriteDocumentAsset asset = CreateProjectAsset(document, assetPath);
            if (asset == null) return null;
            AddRecent(assetPath);
            Remember(asset);
            EditorGUIUtility.PingObject(asset);
            return asset;
        }

        public SpriteDocumentAsset ExportCopy(SpriteDocument document)
        {
            string assetPath = EditorUtility.SaveFilePanelInProject("Export Sprite Toolbox project", "NewSpriteDocument", "asset", "Choose a Unity project folder.");
            SpriteDocumentAsset asset = CreateProjectAsset(document, assetPath);
            if (asset == null) return null;
            AddRecent(assetPath);
            EditorGUIUtility.PingObject(asset);
            return asset;
        }

        public SpriteDocumentAsset LoadAsset(string path, out string assetPath)
        {
            assetPath = ToAssetPath(path);
            SpriteDocumentAsset asset = AssetDatabase.LoadAssetAtPath<SpriteDocumentAsset>(assetPath);
            if (asset != null)
            {
                AddRecent(assetPath);
                Remember(asset);
            }
            return asset;
        }

        public void AddRecent(string path)
        {
            recentFilePaths.Remove(path);
            recentFilePaths.Insert(0, path);
            if (recentFilePaths.Count > 20) recentFilePaths.RemoveAt(recentFilePaths.Count - 1);
            SaveRecentFiles();
        }

        public void RemoveRecent(string path)
        {
            recentFilePaths.Remove(path);
            SaveRecentFiles();
        }

        public void ClearRecent()
        {
            recentFilePaths.Clear();
            SaveRecentFiles();
        }

        public void ClearLastDocument()
        {
            EditorPrefs.DeleteKey(LastDocumentPreferenceKey);
        }

        public static string ToAssetPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            return path.StartsWith("Assets/") ? path : FileUtil.GetProjectRelativePath(path);
        }

        private static SpriteDocumentAsset CreateProjectAsset(SpriteDocument document, string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) || document == null) return null;
            SpriteDocumentAsset asset = ScriptableObject.CreateInstance<SpriteDocumentAsset>();
            asset.ReplaceDocument(document.Clone());
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static void Remember(SpriteDocumentAsset asset)
        {
            string assetPath = AssetDatabase.GetAssetPath(asset);
            if (!string.IsNullOrEmpty(assetPath)) EditorPrefs.SetString(LastDocumentPreferenceKey, assetPath);
        }

        private void LoadRecentFiles()
        {
            string json = EditorPrefs.GetString(GetRecentFilesPreferenceKey(), string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            RecentFilePathsData data = JsonUtility.FromJson<RecentFilePathsData>(json);
            if (data == null || data.Paths == null)
            {
                return;
            }

            recentFilePaths.Clear();
            foreach (string path in data.Paths)
            {
                if (!string.IsNullOrEmpty(path) && !recentFilePaths.Contains(path))
                {
                    recentFilePaths.Add(path);
                }
            }
        }

        private void SaveRecentFiles()
        {
            RecentFilePathsData data = new RecentFilePathsData { Paths = recentFilePaths.ToArray() };
            EditorPrefs.SetString(GetRecentFilesPreferenceKey(), JsonUtility.ToJson(data));
        }

        private static string GetRecentFilesPreferenceKey()
        {
            return RecentFilesPreferenceKeyPrefix + Hash128.Compute(Application.dataPath).ToString();
        }

        [Serializable]
        private sealed class RecentFilePathsData
        {
            public string[] Paths;
        }
    }
}
