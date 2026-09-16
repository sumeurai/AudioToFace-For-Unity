using System.IO;
using UnityEditor;
using UnityEngine;

namespace SumeruAI.Editor
{
    internal static class SumeruAIEditorPaths
    {
        public const string ProjectRoot = "Assets/SumeruAI";
        public const string ProjectResources = "Assets/SumeruAI/Resources";
        public const string ProjectConfigAsset = "Assets/SumeruAI/Resources/APISettingsConfig.asset";

        public static string RootAssetPath
        {
            get
            {
                var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(SumeruAIEditorPaths).Assembly);
                if (info != null && !string.IsNullOrEmpty(info.assetPath))
                {
                    return info.assetPath.TrimEnd('/');
                }

                return ProjectRoot;
            }
        }

        public static string LogoAssetPath
        {
            get { return RootAssetPath + "/Editor/Texture/logo.png"; }
        }

        public static string PackageVersion
        {
            get
            {
                var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(SumeruAIEditorPaths).Assembly);
                if (info != null && !string.IsNullOrEmpty(info.version))
                {
                    return info.version;
                }

                string jsonPath = Path.Combine(RootAssetPath, "package.json");
                if (!File.Exists(jsonPath))
                {
                    return "";
                }

                string json = File.ReadAllText(jsonPath);
                const string key = "\"version\"";
                int keyIndex = json.IndexOf(key);
                if (keyIndex < 0)
                {
                    return "";
                }

                int colon = json.IndexOf(':', keyIndex);
                int firstQuote = json.IndexOf('"', colon + 1);
                int secondQuote = json.IndexOf('"', firstQuote + 1);
                if (firstQuote < 0 || secondQuote < 0)
                {
                    return "";
                }

                return json.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
            }
        }

        public static bool IsPackagePath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            return assetPath.Replace('\\', '/').StartsWith("Packages/");
        }

        public static bool IsWritableProjectConfig(API.APISettingsConfig config)
        {
            if (config == null || !EditorUtility.IsPersistent(config))
            {
                return false;
            }

            return !IsPackagePath(AssetDatabase.GetAssetPath(config));
        }

        public static void EnsureProjectResourcesFolder()
        {
            if (!AssetDatabase.IsValidFolder(ProjectRoot))
            {
                AssetDatabase.CreateFolder("Assets", "SumeruAI");
            }

            if (!AssetDatabase.IsValidFolder(ProjectResources))
            {
                AssetDatabase.CreateFolder("Assets/SumeruAI", "Resources");
            }
        }
    }
}
