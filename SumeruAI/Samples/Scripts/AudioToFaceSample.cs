using System;
using System.Collections;
using System.Collections.Generic;
using SumeruAI;
using UnityEngine;
using SumeruAI.API;
using SumeruAI.ATF;
using System.IO;



#if UNITY_EDITOR
using UnityEditor;
#endif


public class AudioToFaceSample : MonoBehaviour
{

    [SerializeField] private SkinnedMeshRenderer[] skinnedMeshes;

    [SerializeField] private Transform rootBone;

    [SerializeField] private AudioRecord audioRecord;



    void Start()
    {
        AudioToFaceManager.GetInstance().RegisterModel(0, Sex.Male, skinnedMeshes, rootBone, MotionType.ARKit);

        Login();
    }

    private void OnDestroy()
    {

    }


    private void Login()
    {
        APIManager.Instance.Login();
    }


    void Update()
    {

    }

    public void StartRecord()
    {
        if (audioRecord)
        {
            audioRecord.StartRecord();
        }
    }

    public void StopRecord()
    {
        if (audioRecord)
        {
            ATFReqData reqData = new ATFReqData();
            reqData.status = "start";
            reqData.traceId = Guid.NewGuid().ToString("N");


            audioRecord.StopRecord((base64, data) =>
            {
                reqData.dialogueBase64 = base64;

                APIManager.Instance.Request<ATFReqData, ATFRepData>(APISettingsConfig.Instance.ATFMeshUrl,
                    reqData,
                    (repdata) =>
                    {
                        AudioToFaceManager.GetInstance().AddAudioFaceData(repdata.data.emoteKey,
                            repdata.data.audioKey, repdata.data.fps);
                    });
            });
        }
    }

    public void SelectLocalAudioInEditor()
    {
#if UNITY_EDITOR
        string filepath = EditorUtility.OpenFilePanel("select wav file", "", "wav");

        byte[] wavBytes = File.ReadAllBytes(filepath);

        string base64 = Convert.ToBase64String(wavBytes);

        ATFReqData reqData = new ATFReqData();
        reqData.status = "start";
        reqData.traceId = Guid.NewGuid().ToString("N");
        reqData.dialogueBase64 = base64;

        APIManager.Instance.Request<ATFReqData, ATFRepData>(APISettingsConfig.Instance.ATFMeshUrl, reqData,
            (repData) =>
            {
                AudioToFaceManager.GetInstance().AddAudioFaceData(repData.data.emoteKey, repData.data.audioKey, repData.data.fps);
            });

#endif
    }
}

