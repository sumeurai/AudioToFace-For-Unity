using System.Collections;
using System.Collections.Generic;
using System.IO;
using SumeruAI.API;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SumeruAI.Editor
{
    public class APISettingsProvider : SettingsProvider
    {

        private const string SETTINGS_PATH = "Project/SumeruAI/APISettingsProvider";

        private string accessKey;
        private string secretKey;

        private bool hasConfigAsset;

        public APISettingsProvider(string path, SettingsScope scopes) : base(path, scopes)
        {
        }


        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            var provider = new APISettingsProvider(SETTINGS_PATH, SettingsScope.Project)
            {
                label = "API Settings",
                keywords = new[] { "settings" }
            };

            return provider;
        }


        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);

            APISettingsConfig config = APISettingsConfig.Instance;
            hasConfigAsset = config != null && EditorUtility.IsPersistent(config);

            if (hasConfigAsset)
            {
                accessKey = config.AccessKey;
                secretKey = config.SecretKey;
            }
        }

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);

            GUILayout.Space(15);

            // Display Logo
            DisplayLogo();

            GUILayout.Space(20);

            // Settings Title
            var titleStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                margin = new RectOffset(5, 5, 5, 5)
            };
            EditorGUILayout.LabelField("API Configuration", titleStyle);

            GUILayout.Space(10);

            // Warning Box
            if (!hasConfigAsset)
            {
                EditorGUILayout.HelpBox("No API settings asset was found in a Resources folder. Please create one and configure your API credentials.", MessageType.Warning);
                GUILayout.Space(10);
            }

            // Input Fields with better styling
            EditorGUILayout.Space(5);
            accessKey = EditorGUILayout.TextField("AccessKey:", accessKey);
            GUILayout.Space(5);
            secretKey = EditorGUILayout.TextField("SecretKey:", secretKey);
            GUILayout.Space(15);

            // Save Button with better styling
            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset(10, 10, 8, 8),
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };

            if (GUILayout.Button("Save Settings", buttonStyle, GUILayout.Height(35)))
            {
                if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
                {
                    EditorUtility.DisplayDialog("Warning", "Please fill accessKey and secretKey then save it", "OK");
                    return;
                }

                if (!hasConfigAsset)
                {
                    string folder = EditorUtility.OpenFolderPanel("Save Settings", "Assets", "");
                    string relativePath = FileUtil.GetProjectRelativePath(folder);
                    SaveSettings(relativePath);
                }
                else
                {
                    APISettingsConfig.Instance.SetAccessKeyAndSecretKeyFromEditor(accessKey, secretKey);
                }
            }

        }

        private void DisplayLogo()
        {
            try
            {
                // Load logo from Editor/Texture folder using AssetDatabase
                string logoPath = "Assets/SumeruAI/Editor/Texture/logo.png";
                Texture2D logo = AssetDatabase.LoadAssetAtPath<Texture2D>(logoPath);

                if (logo != null)
                {
                    float logoWidth = 64f;
                    float logoHeight = 64f * (logo.height / (float)logo.width);

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(logo, GUILayout.Width(logoWidth), GUILayout.Height(logoHeight));
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    Debug.LogWarning("No file in Assets/SumeruAI/Editor/Texture/logo.png");
                }
            }
            catch
            {
                // Silently ignore if logo not found
            }
        }


        private void SaveSettings(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {

                if (path.Contains("Resources"))
                {
                    string filename = Path.Combine(path, "APISettingsConfig.asset");

                    APISettingsConfig config = APISettingsConfig.Instance;
                    config.SetAccessKeyAndSecretKeyFromEditor(accessKey, secretKey);
                    AssetDatabase.CreateAsset(config, filename);
                    AssetDatabase.SaveAssets();
                    hasConfigAsset = true;
                }
                else
                {
                    EditorUtility.DisplayDialog("Warning", "Please select the Resources folder", "OK");
                }

            }

        }


    }

}