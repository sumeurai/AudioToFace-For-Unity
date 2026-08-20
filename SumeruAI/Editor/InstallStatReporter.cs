using System;
using System.Text;
using SumeruAI.API;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace SumeruAI.Editor
{
    [Serializable]
    public class EventStatsIncrementReq
    {
        public string bizType;
        public string eventType;
        public string eventKey;
        public string product;
        public string packageName;
        public string version;
        public string fromPage;
        public string repo;
        public string anonId;
        public string sdkVersion;
        public string unityVersion;
        public string machineId;
        public int userId;
        public string keyId;
        public string plan;
        public string apiName;
        public int latencyMs;
        public int callCount7d;
        public bool allBlank;
    }

    [Serializable]
    public class EventStatsIncrementRep
    {
        public EventStatsIncrementRepData data;
        public int code;
        public string msg;
    }

    [Serializable]
    public class EventStatsIncrementRepData
    {
        public string bizType;
        public string bizTypeName;
        public string eventType;
        public string eventTypeName;
        public string eventKey;
        public string statDate;
        public int eventCount;
        public int userCount;
    }

    [InitializeOnLoad]
    static class InstallStatReporter
    {
        const string EventPath = "v1/event-stats/increment";
        const string PrefKeyPrefix = "SumeruAI_InstallStat_";
        const string AnonIdPrefKey = "SumeruAI_AnonId";
        const string DebugLogPrefKey = "SumeruAI_InstallStat_DebugLog";

        const string BizType = "12";
        const string EventType = "2";
        const string EventKey = "sdk/unity/install_success";
        const string Product = "atf";
        const string PackageName = "atf";

        static bool s_Started;

        static bool DebugLogEnabled
        {
            get { return EditorPrefs.GetBool(DebugLogPrefKey, false); }
            set { EditorPrefs.SetBool(DebugLogPrefKey, value); }
        }

        static InstallStatReporter()
        {
            if (Application.isBatchMode)
            {
                return;
            }

            EditorApplication.delayCall += TryReport;
        }

        //[MenuItem("SumeruAI/Retry Install Stat", false, 0)]
        static void RetryReport()
        {
            string prefKey = PrefKeyPrefix + Application.dataPath.GetHashCode();
            EditorPrefs.DeleteKey(prefKey);
            s_Started = false;
            TryReport();
        }

        //[MenuItem("SumeruAI/Install Stat Debug Log", false, 1)]
        static void ToggleDebugLog()
        {
            DebugLogEnabled = !DebugLogEnabled;
            Debug.Log($"[SumeruAI] Install stat debug log: {(DebugLogEnabled ? "ON" : "OFF")}");
        }

        //[MenuItem("SumeruAI/Install Stat Debug Log", true)]
        static bool ToggleDebugLogValidate()
        {
            Menu.SetChecked("SumeruAI/Install Stat Debug Log", DebugLogEnabled);
            return true;
        }

        static async void TryReport()
        {
            if (s_Started)
            {
                return;
            }

            string prefKey = PrefKeyPrefix + Application.dataPath.GetHashCode();
            if (EditorPrefs.GetBool(prefKey, false))
            {
                Log($"[SumeruAI] Install stat skipped: already reported ({prefKey})");
                return;
            }

            string baseUrl = APISettingsConfig.Instance != null ? APISettingsConfig.Instance.BaseUrl : null;
            if (string.IsNullOrEmpty(baseUrl))
            {
                return;
            }

            s_Started = true;

            EventStatsIncrementReq req = BuildRequest();
            string url = CombineUrl(baseUrl, EventPath);
            string json = JsonUtility.ToJson(req);

            Log($"[SumeruAI] Install stat URL: {url}");
            Log($"[SumeruAI] Install stat body: {json}");

            using (UnityWebRequest uwr = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                byte[] body = Encoding.UTF8.GetBytes(json);
                uwr.uploadHandler = new UploadHandlerRaw(body);
                uwr.downloadHandler = new DownloadHandlerBuffer();
                uwr.SetRequestHeader("Content-Type", "application/json");

                UnityWebRequestAsyncOperation op = uwr.SendWebRequest();
                while (!op.isDone)
                {
                    await System.Threading.Tasks.Task.Yield();
                }

                string respText = uwr.downloadHandler != null ? uwr.downloadHandler.text : "";
                Log($"[SumeruAI] Install stat response: HTTP {(int)uwr.responseCode} result={uwr.result} error={uwr.error}\n{respText}");

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    s_Started = false;
                    LogWarning($"[SumeruAI] Install stat failed: {uwr.error}");
                    return;
                }

                EventStatsIncrementRep response = JsonUtility.FromJson<EventStatsIncrementRep>(uwr.downloadHandler.text);
                if (response == null || response.code != 200)
                {
                    s_Started = false;
                    string msg = response != null ? response.msg : "Response parse failed";
                    LogWarning($"[SumeruAI] Install stat failed: {msg}");
                    return;
                }

                EditorPrefs.SetBool(prefKey, true);
            }
        }

        static void Log(string message)
        {
            if (DebugLogEnabled)
            {
                Debug.Log(message);
            }
        }

        static void LogWarning(string message)
        {
            if (DebugLogEnabled)
            {
                Debug.LogWarning(message);
            }
        }

        static EventStatsIncrementReq BuildRequest()
        {
            string unityVersion = Application.unityVersion;
            string machineId = SystemInfo.deviceUniqueIdentifier;
            string anonId = GetOrCreateAnonId();
            string keyId = APISettingsConfig.Instance != null ? APISettingsConfig.Instance.AccessKey : null;

            var req = new EventStatsIncrementReq
            {
                bizType = BizType,
                eventType = EventType,
                eventKey = EventKey,
                product = Product,
                packageName = PackageName,
                version = "",
                fromPage = "",
                repo = "",
                anonId = anonId,
                sdkVersion = "",
                unityVersion = unityVersion,
                machineId = machineId,
                userId = 0,
                keyId = keyId ?? "",
                plan = "",
                apiName = "",
                latencyMs = 0,
                callCount7d = 0,
                allBlank = true
            };

            req.allBlank = IsOptionalBlank(req);
            return req;
        }

        static bool IsOptionalBlank(EventStatsIncrementReq req)
        {
            return string.IsNullOrEmpty(req.product)
                   && string.IsNullOrEmpty(req.packageName)
                   && string.IsNullOrEmpty(req.version)
                   && string.IsNullOrEmpty(req.fromPage)
                   && string.IsNullOrEmpty(req.repo)
                   && string.IsNullOrEmpty(req.anonId)
                   && string.IsNullOrEmpty(req.sdkVersion)
                   && string.IsNullOrEmpty(req.unityVersion)
                   && string.IsNullOrEmpty(req.machineId)
                   && req.userId == 0
                   && string.IsNullOrEmpty(req.keyId)
                   && string.IsNullOrEmpty(req.plan)
                   && string.IsNullOrEmpty(req.apiName)
                   && req.latencyMs == 0
                   && req.callCount7d == 0;
        }

        static string GetOrCreateAnonId()
        {
            string anonId = EditorPrefs.GetString(AnonIdPrefKey, "");
            if (!string.IsNullOrEmpty(anonId))
            {
                return anonId;
            }

            anonId = Guid.NewGuid().ToString("N");
            EditorPrefs.SetString(AnonIdPrefKey, anonId);
            return anonId;
        }

        static string CombineUrl(string url, string path)
        {
            if (string.IsNullOrEmpty(url))
            {
                return path;
            }

            if (string.IsNullOrEmpty(path))
            {
                return url;
            }

            return $"{url.TrimEnd('/')}/{path.TrimStart('/')}";
        }
    }
}
