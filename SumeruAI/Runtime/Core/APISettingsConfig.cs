using UnityEngine;

namespace SumeruAI.API
{
    [CreateAssetMenu(fileName = "APISettingsConfig", menuName = "SumeruAI/API Settings Config")]
    public class APISettingsConfig : ScriptableObject
    {
        const string DefaultBaseUrl = "https://api.sumeruai.us/";
        const string DefaultLogin = "v1/access/auth";
        const string DefaultAtfMesh = "v1/audio-to-face/offline-mesh";

        [SerializeField] private string accessKey;
        [SerializeField] private string secretKey;

        [SerializeField] private string baseUrl;
        [SerializeField] private string login;
        [SerializeField] private string atfMesh;

        public string AccessKey
        {
            get { return accessKey; }
        }

        public string SecretKey
        {
            get { return secretKey; }
        }

        public string BaseUrl
        {
            get { return baseUrl; }
        }

        public string LoginUrl
        {
            get { return CombineUrl(baseUrl, login); }
        }

        public string ATFMeshUrl
        {
            get { return CombineUrl(baseUrl, atfMesh); }
        }

        private static APISettingsConfig instance;

        public static APISettingsConfig Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = SelectConfig(Resources.LoadAll<APISettingsConfig>(""));

                    if (instance == null)
                    {
                        instance = CreateInstance<APISettingsConfig>();
                        instance.ApplyBuiltInDefaults();
                    }
                }

                return instance;
            }
        }

        public static void ReloadInstance()
        {
            instance = null;
        }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey);
        }

        public void ApplyBuiltInDefaults()
        {
            if (string.IsNullOrEmpty(baseUrl))
            {
                baseUrl = DefaultBaseUrl;
            }

            if (string.IsNullOrEmpty(login))
            {
                login = DefaultLogin;
            }

            if (string.IsNullOrEmpty(atfMesh))
            {
                atfMesh = DefaultAtfMesh;
            }
        }

        protected string CombineUrl(string url, string path)
        {
            if (string.IsNullOrEmpty(url)) return path;
            if (string.IsNullOrEmpty(path)) return url;

            return $"{url.TrimEnd('/')}/{path.TrimStart('/')}";
        }

        static APISettingsConfig SelectConfig(APISettingsConfig[] configs)
        {
            if (configs == null || configs.Length == 0)
            {
                return null;
            }

            APISettingsConfig fallback = null;

            for (int i = 0; i < configs.Length; i++)
            {
                APISettingsConfig config = configs[i];
                if (config == null)
                {
                    continue;
                }

#if UNITY_EDITOR
                string assetPath = UnityEditor.AssetDatabase.GetAssetPath(config);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    string normalized = assetPath.Replace('\\', '/');
                    if (normalized.StartsWith("Assets/"))
                    {
                        return config;
                    }
                }
#endif
                if (config.IsValid())
                {
                    return config;
                }

                if (fallback == null)
                {
                    fallback = config;
                }
            }

            return fallback;
        }

#if UNITY_EDITOR
        public void SetAccessKeyAndSecretKeyFromEditor(string aKey, string sKey)
        {
            accessKey = aKey;
            secretKey = sKey;
            ApplyBuiltInDefaults();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
