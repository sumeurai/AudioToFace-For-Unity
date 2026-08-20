using System;
using System.Collections;
using SumeruAI.Core;

namespace SumeruAI.API
{

    [Serializable]
    public class LoginReqData
    {
        public string accessKey;

        public string secretKey;
    }

    [Serializable]
    public class LoginRepData : BaseResponse
    {
        public LoginRepBodyData data;
    }

    [Serializable]
    public class LoginRepBodyData
    {
        public string accessToken;

        public int expiresIn;
    }



    [Serializable]
    public class ATFReqData
    {
        public string traceId;

        /// <summary>
        /// audio base64
        /// </summary>
        public string data;
    }

    [Serializable]
    public class ATFRepData : BaseResponse
    {
        public ATFRepBodyData data;
    }


    [Serializable]
    public class ATFRepBodyData
    {
        /// <summary>
        /// id
        /// </summary>
        public Int64 id;

        /// <summary>
        /// blendshape base64
        /// </summary>
        public string emoteKey;

        /// <summary>
        /// audio base64
        /// </summary>
        public string audioKey;

        /// <summary>
        /// fps
        /// </summary>
        public float fps;
    }


}
