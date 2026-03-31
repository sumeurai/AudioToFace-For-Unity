using SumeruAI.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


namespace SumeruAI.API
{
    public class APIManager : MonoBehaviour
    {
        private static APIManager instance;

        public static APIManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject o = new GameObject("APIManager");
                    instance = o.AddComponent<APIManager>();
                }
                return instance;
            }
        }

        [SerializeField] private string accessToken;

        public string AccessToken
        {
            get
            {
                return accessToken;
            }
            private set
            {
                accessToken = value;
            }
        }


        private BaseHttpRequest httpRequest;

        public BaseHttpRequest HttpRequest
        {
            get
            {
                if (httpRequest == null)
                {
                    httpRequest = new BaseHttpRequest();
                }
                return httpRequest;
            }
        }


        private void Awake()
        {
            DontDestroyOnLoad(this);
        }


        public async Task<TRep> RequestAsync<TReq, TRep>(
            string url,
            TReq requestData,
            Action<TRep> onSuccess = null,
            Action<string> onError = null)
            where TRep : BaseResponse
        {
            try
            {
                string token = url == APISettingsConfig.Instance.LoginUrl ? "" : AccessToken;

                TRep response = null;
                await HttpRequest.PostAsync<TReq, TRep>(
                    url,
                    token,
                    requestData,
                    (data) =>
                    {
                        response = data;
                        onSuccess?.Invoke(data);
                    },
                    (error) =>
                    {
                        Debug.LogError($"[API Error] {url}: {error}");
                        onError?.Invoke(error);
                    }
                );

                return response;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[API Exception] {url}: {ex.Message}");
                onError?.Invoke(ex.Message);
                return null;
            }
        }

        public void Request<TReq, TRep>(
            string url,
            TReq requestData,
            Action<TRep> onSuccess = null,
            Action<string> onError = null)
            where TRep : BaseResponse
        {
            _ = RequestAsync<TReq, TRep>(url, requestData, onSuccess, onError);
        }



        public void Login()
        {
            if (!APISettingsConfig.Instance.IsValid())
            {
                Debug.LogError("[API Error] accesskey or secretkey is empty!");
                return;
            }

            LoginReqData reqData = new LoginReqData();
            reqData.accessKey = APISettingsConfig.Instance.AccessKey;
            // reqData.secretKey = ComputeStringMD5(APISettingsConfig.Instance.SecretKey);
            reqData.secretKey = APISettingsConfig.Instance.SecretKey;

            Request<LoginReqData, LoginRepData>(APISettingsConfig.Instance.LoginUrl,reqData, (repData) =>
            {
                AccessToken = repData.data.accessToken;
            });
        }


        public string ComputeStringMD5(string s)
        {
            // Create MD5 hash creator
            MD5 hashCreator = MD5.Create();

            // Convert the input string to a byte array
            byte[] data = hashCreator.ComputeHash(Encoding.UTF8.GetBytes(s));

            // Create a StringBuilder to collect the bytes and create a string
            StringBuilder stringBuilder = new StringBuilder();

            // Loop through each byte of the hashed data and format each one as a hexadecimal string
            for (int i = 0; i < data.Length; i++)
            {
                stringBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string
            return stringBuilder.ToString();
        }



    }
}
