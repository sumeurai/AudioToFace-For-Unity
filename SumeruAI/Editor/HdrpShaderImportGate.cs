using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SumeruAI.Editor
{
    [InitializeOnLoad]
    static class HdrpShaderImportGate
    {
        static HdrpShaderImportGate()
        {
            EditorApplication.delayCall += Sync;
        }

        static bool HasHdrp()
        {
            RenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline;
            if (pipeline != null)
            {
                string typeName = pipeline.GetType().FullName;
                if (!string.IsNullOrEmpty(typeName) && typeName.Contains("HDRenderPipeline"))
                {
                    return true;
                }
            }

            // GetAllRegisteredPackages is Unity 2021.1+; detect the HDRP assembly instead.
            if (System.Type.GetType(
                    "UnityEngine.Rendering.HighDefinition.HDRenderPipelineAsset, Unity.RenderPipelines.HighDefinition.Runtime") !=
                null)
            {
                return true;
            }

            return Directory.Exists("Packages/com.unity.render-pipelines.high-definition");
        }

        static void Sync()
        {
            string root = SumeruAIEditorPaths.RootAssetPath.TrimEnd('/', '\\');
            string shadersRel = root + "/Samples/Models/Xandra/Shaders";
            string hiddenRel = shadersRel + "~";
            string shadersAbs = Path.GetFullPath(shadersRel);
            string hiddenAbs = Path.GetFullPath(hiddenRel);

            try
            {
                if (HasHdrp())
                {
                    if (!Directory.Exists(shadersAbs) && Directory.Exists(hiddenAbs))
                    {
                        Directory.Move(hiddenAbs, shadersAbs);
                        string hiddenMeta = hiddenAbs + ".meta";
                        string shadersMeta = shadersAbs + ".meta";
                        if (File.Exists(hiddenMeta) && !File.Exists(shadersMeta))
                        {
                            File.Move(hiddenMeta, shadersMeta);
                        }

                        AssetDatabase.Refresh();
                    }

                    return;
                }

                if (!Directory.Exists(shadersAbs))
                {
                    return;
                }

                if (Directory.Exists(hiddenAbs))
                {
                    Directory.Delete(shadersAbs, true);
                }
                else
                {
                    Directory.Move(shadersAbs, hiddenAbs);
                }

                string meta = shadersAbs + ".meta";
                if (File.Exists(meta))
                {
                    File.Delete(meta);
                }

                Debug.Log("[SumeruAI] Hidden HDRP Shader Graphs (no HDRP package). Use ATF_URP or ATF_BuiltIn.");
                AssetDatabase.Refresh();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[SumeruAI] Could not hide HDRP Shader Graphs: " + ex.Message);
            }
        }
    }
}
