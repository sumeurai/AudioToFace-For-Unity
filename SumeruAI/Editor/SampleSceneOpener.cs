using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SumeruAI.Editor
{
    static class SampleSceneOpener
    {
        const string DestFolder = "Assets/SumeruAI Samples";

        [MenuItem("SumeruAI/Samples/Open HDRP Scene (ATF)", false, 10)]
        static void OpenHdrp()
        {
            Open("ATF.unity");
        }

        [MenuItem("SumeruAI/Samples/Open URP Scene (ATF_URP)", false, 11)]
        static void OpenUrp()
        {
            Open("ATF_URP.unity");
        }

        [MenuItem("SumeruAI/Samples/Open Built-in Scene (ATF_BuiltIn)", false, 12)]
        static void OpenBuiltIn()
        {
            Open("ATF_BuiltIn.unity");
        }

        static void Open(string sceneFile)
        {
            string srcAsset = SumeruAIEditorPaths.RootAssetPath.TrimEnd('/') + "/Samples/Scenes/" + sceneFile;
            string srcDisk = ToDiskPath(srcAsset);
            if (string.IsNullOrEmpty(srcDisk) || !File.Exists(srcDisk))
            {
                EditorUtility.DisplayDialog(
                    "SumeruAI Sample",
                    "Could not find " + srcAsset + ".\nReinstall the package or copy SumeruAI into Assets.",
                    "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (!SumeruAIEditorPaths.IsPackagePath(srcAsset))
            {
                EditorSceneManager.OpenScene(srcAsset);
                return;
            }

            if (!AssetDatabase.IsValidFolder(DestFolder))
            {
                AssetDatabase.CreateFolder("Assets", "SumeruAI Samples");
            }

            string destAsset = DestFolder + "/" + sceneFile;
            string destDisk = ToDiskPath(destAsset);
            if (!File.Exists(destDisk))
            {
                File.Copy(srcDisk, destDisk, false);
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog(
                    "SumeruAI Sample",
                    "Git packages are read-only, so the scene was copied to:\n" + destAsset + "\n\nOpen that copy. Do not double-click the scene inside Packages.",
                    "OK");
            }

            EditorSceneManager.OpenScene(destAsset);
        }

        static string ToDiskPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return null;
            }

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar)));
        }
    }
}
