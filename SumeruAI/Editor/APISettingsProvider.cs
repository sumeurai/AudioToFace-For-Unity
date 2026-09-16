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

        private bool hasWritableConfigAsset;

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
            hasWritableConfigAsset = SumeruAIEditorPaths.IsWritableProjectConfig(config);

            if (config != null)
            {
                accessKey = config.AccessKey;
                secretKey = config.SecretKey;
            }
        }

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);

            GUILayout.Space(15);

            DisplayLogo();

            GUILayout.Space(20);

            var titleStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                margin = new RectOffset(5, 5, 5, 5)
            };
            EditorGUILayout.LabelField("API Configuration", titleStyle);

            GUILayout.Space(10);

            if (!hasWritableConfigAsset)
            {
                EditorGUILayout.HelpBox(
                    "Save writes credentials to Assets/SumeruAI/Resources so they stay in your project when the plugin is installed from Package Manager.",
                    MessageType.Info);
                GUILayout.Space(10);
            }

            EditorGUILayout.Space(5);
            accessKey = EditorGUILayout.TextField("AccessKey:", accessKey);
            GUILayout.Space(5);
            secretKey = EditorGUILayout.TextField("SecretKey:", secretKey);
            GUILayout.Space(15);

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

                if (hasWritableConfigAsset)
                {
                    APISettingsConfig.Instance.SetAccessKeyAndSecretKeyFromEditor(accessKey, secretKey);
                    AssetDatabase.SaveAssets();
                }
                else
                {
                    SaveProjectSettings();
                }
            }
        }

        private void DisplayLogo()
        {
            Texture2D logo = AssetDatabase.LoadAssetAtPath<Texture2D>(SumeruAIEditorPaths.LogoAssetPath);
            if (logo == null)
            {
                return;
            }

            float logoWidth = 64f;
            float logoHeight = 64f * (logo.height / (float)logo.width);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(logo, GUILayout.Width(logoWidth), GUILayout.Height(logoHeight));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void SaveProjectSettings()
        {
            SumeruAIEditorPaths.EnsureProjectResourcesFolder();

            APISettingsConfig source = APISettingsConfig.Instance;
            APISettingsConfig asset = Object.Instantiate(source);
            asset.SetAccessKeyAndSecretKeyFromEditor(accessKey, secretKey);

            string filename = SumeruAIEditorPaths.ProjectConfigAsset;
            APISettingsConfig existing = AssetDatabase.LoadAssetAtPath<APISettingsConfig>(filename);
            if (existing != null)
            {
                existing.SetAccessKeyAndSecretKeyFromEditor(accessKey, secretKey);
                Object.DestroyImmediate(asset);
            }
            else
            {
                AssetDatabase.CreateAsset(asset, filename);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            APISettingsConfig.ReloadInstance();
            hasWritableConfigAsset = true;
        }
    }
}
