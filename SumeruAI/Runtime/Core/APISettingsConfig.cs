using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;


namespace SumeruAI.API
{
    [CreateAssetMenu(fileName = "APISettingsConfig", menuName = "SumeruAI/API Settings Config")]

    public class APISettingsConfig : ScriptableObject
    {

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
                    instance = Resources.Load<APISettingsConfig>("APISettingsConfig");

                    if (instance == null)
                    {
                        instance = CreateInstance<APISettingsConfig>();
                    }
                }

                return instance;
            }
        }



        public bool IsValid()
        {
            return !string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey);
        }

        protected string CombineUrl(string url, string path)
        {
            if (string.IsNullOrEmpty(url)) return path;
            if (string.IsNullOrEmpty(path)) return url;

            return $"{url.TrimEnd('/')}/{path.TrimStart('/')}";
        }

#if UNITY_EDITOR

        public void SetAccessKeyAndSecretKeyFromEditor(string aKey, string sKey)
        {
            accessKey = aKey;
            secretKey = sKey;
            EditorUtility.SetDirty(this);
        }

#endif

    }
}
