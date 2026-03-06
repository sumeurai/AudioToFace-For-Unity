using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;

namespace SumeruAI.Core
{

    [Serializable]
    public class BaseResponse
    {
        public int code;
        public string message;
    }

    public class BaseHttpRequest
    {

        public async Task PostAsync<TRequest, TResponse>(
            string url,
            string accessToken,
            TRequest requestData,
            Action<TResponse> onSuccess,
            Action<string> onFail = null,
            bool writedata = false)
            where TResponse : BaseResponse
        {
            string jsonstr = JsonUtility.ToJson(requestData);

#if UNITY_EDITOR
            Debug.Log("URL: " + url);
            Debug.Log("JSON: " + jsonstr);

            // string filepath = Path.Combine(Application.dataPath.Replace("Assets", ""),"request.json");
            // File.WriteAllText(filepath,jsonstr);
#endif

            using (var uwr = new UnityWebRequest(url))
            {
                uwr.method = UnityWebRequest.kHttpVerbPOST;
                uwr.SetRequestHeader("Content-Type", "application/json");
                if (!string.IsNullOrEmpty(accessToken))
                {
                    uwr.SetRequestHeader("Authorization", accessToken);
                }

                byte[] bytes = Encoding.UTF8.GetBytes(jsonstr);
                uwr.uploadHandler = new UploadHandlerRaw(bytes);
                uwr.downloadHandler = new DownloadHandlerBuffer();

                var asyncOperation = uwr.SendWebRequest();

                while (!asyncOperation.isDone)
                {
                    await Task.Yield();
                }

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    onFail?.Invoke(uwr.error);
                    return;
                }

#if UNITY_EDITOR
                Debug.Log("Response: " + uwr.downloadHandler.text);
                // if (writedata)
                // {
                //     string filepath = Path.Combine(Application.dataPath.Replace("Assets", ""),"respone.json");
                //     File.WriteAllText(filepath, uwr.downloadHandler.text);
                // }
#endif

                TResponse response = JsonUtility.FromJson<TResponse>(uwr.downloadHandler.text);

                if (response != null)
                {
                    if (response.code == 200)
                    {
                        onSuccess?.Invoke(response);
                    }
                    else
                    {
                        onFail?.Invoke(response.message);
                    }
                }
                else
                {
                    onFail?.Invoke("Response parse failed");
                }
            }
        }


    }
}
