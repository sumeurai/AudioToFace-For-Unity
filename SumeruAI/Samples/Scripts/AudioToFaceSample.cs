using System;
using System.IO;
using SumeruAI;
using SumeruAI.API;
using SumeruAI.ATF;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


public class AudioToFaceSample : MonoBehaviour
{

    [SerializeField] private SkinnedMeshRenderer[] skinnedMeshes;

    [SerializeField] private Transform rootBone;

    [SerializeField] private AudioRecord audioRecord;

    public void Bind(SkinnedMeshRenderer[] meshes, Transform root, AudioRecord record)
    {
        if (meshes != null && meshes.Length > 0)
        {
            skinnedMeshes = meshes;
        }

        if (root != null)
        {
            rootBone = root;
        }

        if (record != null)
        {
            audioRecord = record;
        }
    }


    void Start()
    {
        AudioToFaceManager.GetInstance().RegisterModel(0, Sex.Male, skinnedMeshes, rootBone, MotionType.ARKit);

        APIManager.Instance.Login();
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            SelectLocalAudioInEditor();
        }
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
            audioRecord.StopRecord((base64, _) =>
            {
                AudioToFaceManager.GetInstance().PlayFromAudio(Convert.FromBase64String(base64));
            });
        }
    }

    public void SelectLocalAudioInEditor()
    {
#if UNITY_EDITOR
        string filepath = EditorUtility.OpenFilePanel("select wav file", "", "wav");

        if (string.IsNullOrEmpty(filepath))
        {
            return;
        }

        byte[] wavBytes = File.ReadAllBytes(filepath);
        AudioToFaceManager.GetInstance().PlayFromAudio(wavBytes);
#endif
    }
}
