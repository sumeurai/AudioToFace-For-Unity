using SumeruAI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

namespace SumeruAI.ATF
{
    
    public struct ATF_Audio_Data
    {
        public float[] audio;
        public float time;
        public byte[] audioData;
    }

    public struct ATF_Emote_Data
    {
        public float[] bs;
        public float fps;
    }


    public class AudioToFaceManager : MonoBehaviour
    {
        private static AudioToFaceManager instance;

        public static AudioToFaceManager GetInstance()
        {
            if (instance == null)
            {
                GameObject go = new GameObject("AudioToFaceManager");
                instance = go.AddComponent<AudioToFaceManager>();
                // DontDestroyOnLoad(go);
            }

            return instance;
        }


        public UnityAction StartSpeechEvent;

        public UnityAction StopSpeechEvent;

        public UnityAction StopMotionEvent;

        private AudioSource audioSource;

        public AudioSource AudioSource
        {
            get { return audioSource; }
        }

        private Coroutine audioCoroutine;

        private List<AudioClip> audioClips = new List<AudioClip>();
        private Queue<ATF_Audio_Data> AudioQueue = new Queue<ATF_Audio_Data>();
        private Queue<ATF_Emote_Data> EmoteQueue = new Queue<ATF_Emote_Data>();
        public Dictionary<int, ATFMgrData> ATFDataDic = new Dictionary<int, ATFMgrData>();
        private Dictionary<string, Transform> FaceBones = new Dictionary<string, Transform>();


        private void Awake()
        {
            audioSource = this.gameObject.AddComponent<AudioSource>();

        }

        private void Update()
        {
            foreach (var dic in ATFDataDic)
            {
                dic.Value.Update(Time.deltaTime);
            }

            HandleStreamATFData();
        }

        public void AddAudioFaceData(string emote, string audio, float fps)
        {
            if (!string.IsNullOrEmpty(emote) && !string.IsNullOrEmpty(audio))
            {
                byte[] tempAudioBuffer = Convert.FromBase64String(audio);

#if UNITY_EDITOR
                // write data to disk
                // string filepath = Path.Combine(Application.dataPath.Replace("Assets", ""), "audio.wav");
                // File.WriteAllBytes(filepath, tempAudioBuffer);
#endif

                ATF_Audio_Data aTfMgrData = new ATF_Audio_Data();

                aTfMgrData.audioData = tempAudioBuffer;
                aTfMgrData.time = 10.0f;

                AudioQueue.Enqueue(aTfMgrData);

                ATF_Emote_Data emoteData = new ATF_Emote_Data();

                byte[] tempEmoteBuffer = Convert.FromBase64String(emote);

                emoteData.bs = ConvertByteArrayToFloatArray(tempEmoteBuffer);

                emoteData.fps = fps;

                EmoteQueue.Enqueue(emoteData);
            }

        }

        public void RegisterModel(int id, Sex sex, SkinnedMeshRenderer[] skinnedMeshRenderers, Transform RootBone,
            MotionType motionType)
        {
            ATFMgrData aTfMgrData = new ATFMgrData();
            FaceBones.Clear();
            CollectBones(RootBone);
            FaceData faceData = new FaceData();
            faceData.Sex = sex;
            if (FaceBones.ContainsKey("head"))
            {
                faceData.Head = FaceBones["head"];
            }

            if (FaceBones.ContainsKey("jaw"))
            {
                faceData.Jaw = FaceBones["jaw"];
            }

            faceData.SkinnedMeshRenderers = skinnedMeshRenderers;
            faceData.motionType = motionType;

            aTfMgrData.Init(faceData);

            if (!ATFDataDic.ContainsKey(id))
            {
                ATFDataDic.Add(id, aTfMgrData);
            }

            aTfMgrData.SpeechOverEvent += OnSpeechOver;
            aTfMgrData.MotionOverEvent += OnMotionOver;
        }

        public void UnRegisterModel(int id)
        {
            if (ATFDataDic.ContainsKey(id))
            {
                ATFDataDic.Remove(id);
            }
        }

        public void Interrupt()
        {
            EmoteQueue.Clear();

            AudioQueue.Clear();

            if (audioCoroutine != null)
            {
                StopCoroutine(audioCoroutine);
                audioCoroutine = null;
            }

            audioSource.Stop();
            audioClips.Clear();

            foreach (var face in ATFDataDic)
            {
                face.Value.Interrupt();
            }
        }

        private void OnSpeechOver(ATFMgrData aTfMgrData)
        {
            if (StopSpeechEvent != null)
                StopSpeechEvent();
        }

        private void OnMotionOver(ATFMgrData aTfMgrData)
        {
            if (StopMotionEvent != null)
                StopMotionEvent();
        }


        private void HandleStreamATFData()
        {
            while (AudioQueue.Count > 0)
            {
                ATF_Audio_Data mgrData = AudioQueue.Dequeue();

                AudioClip audioClip = NAudioPlayer.FromWavData(mgrData.audioData);

                audioClips.Add(audioClip);

                if (audioClips.Count == 1)
                {
                    if (StartSpeechEvent != null)
                        StartSpeechEvent();

                    audioCoroutine = StartCoroutine(PlayAudioSequence());
                }
            }

            while (EmoteQueue.Count > 0)
            {
                ATF_Emote_Data data = EmoteQueue.Dequeue();

                List<float[]> tempEmoteData = new List<float[]>();

                for (int i = 0; i < data.bs.Length; i += 61)
                {
                    float[] eachFrameData = new float[61];
                    for (int j = 0; j < 61; j++)
                    {
                        eachFrameData[j] = data.bs[i + j];
                    }

                    tempEmoteData.Add(eachFrameData);
                }

                EmoteData emoteData = new EmoteData(1.0f / (float)data.fps, tempEmoteData, EATFDataType.Chat);

                PlayEmoteData(emoteData);
            }

        }


        private IEnumerator PlayAudioSequence()
        {
            for (int i = 0; i < audioClips.Count; i++)
            {
                audioSource.clip = audioClips[i];
                audioSource.Play();

                yield return new WaitForSeconds(audioClips[i].length);
            }

            audioClips.Clear();

            if (StopSpeechEvent != null)
                StopSpeechEvent();
        }


        private float[] ConvertByteArrayToFloatArray(byte[] byteArray)
        {
            if (byteArray.Length % 4 != 0)
                throw new ArgumentException("Byte array length must be divisible by 4.");

            float[] floatArray = new float[byteArray.Length / 4];

            for (int i = 0; i < byteArray.Length; i += 4)
            {
                // If your system uses little endian, you don't need to reverse the byte order
                // if (BitConverter.IsLittleEndian)
                //  Array.Reverse(byteArray, i, 4);

                floatArray[i / 4] = BitConverter.ToSingle(byteArray, i);
                //  Debug.Log(floatArray[i / 4]);
            }

            return floatArray;
        }


        private void CollectBones(Transform curBone)
        {
            for (int i = 0; i < curBone.childCount; i++)
            {
                CollectBones(curBone.GetChild(i));
            }
        }

        public void PlayEmoteData(EmoteData emoteData)
        {
            foreach (var face in ATFDataDic)
            {
                face.Value.StartEmote(emoteData);
            }
        }

    }

}

