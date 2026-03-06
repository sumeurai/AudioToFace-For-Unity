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
        /// <summary>
        /// start middle end
        /// </summary>
        public string status;

        /// <summary>
        /// audio base64
        /// </summary>
        public string dialogueBase64;

        /// <summary>
        /// pre audio base64
        /// </summary>
        public string lastDialogueBase64;

        /// <summary>
        /// 
        /// </summary>
        public string traceId;

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
