using SumeruAI;
using SumeruAI.API;
using System;
using System.Collections;
using System.Collections.Generic;
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
        private bool atfRequesting;


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
            if (!string.IsNullOrEmpty(audio))
            {
                AddAudioData(audio);
            }

            if (!string.IsNullOrEmpty(emote))
            {
                AddEmoteFaceData(emote, fps);
            }
        }

        public void AddAudioData(string audio)
        {
            if (string.IsNullOrEmpty(audio))
            {
                return;
            }

            AddAudioData(Convert.FromBase64String(audio));
        }

        public void AddAudioData(byte[] audioBytes)
        {
            if (audioBytes == null || audioBytes.Length == 0)
            {
                return;
            }

            ATF_Audio_Data aTfMgrData = new ATF_Audio_Data();
            aTfMgrData.audioData = audioBytes;
            aTfMgrData.time = 10.0f;
            AudioQueue.Enqueue(aTfMgrData);
        }

        public void AddEmoteFaceData(string emote, float fps)
        {
            if (string.IsNullOrEmpty(emote))
            {
                return;
            }

            AddEmoteFaceData(Convert.FromBase64String(emote), fps);
        }

        public void AddEmoteFaceData(byte[] blendshapeBytes, float fps)
        {
            if (blendshapeBytes == null || blendshapeBytes.Length == 0)
            {
                return;
            }

            ATF_Emote_Data emoteData = new ATF_Emote_Data();
            emoteData.bs = ConvertByteArrayToFloatArray(blendshapeBytes);
            emoteData.fps = fps;
            EmoteQueue.Enqueue(emoteData);
        }

        public void PlayFromAudio(byte[] wavBytes, Action onSuccess = null, Action<string> onError = null)
        {
            if (atfRequesting)
            {
                Debug.LogWarning("[ATF] Request already in progress.");
                return;
            }

            if (wavBytes == null || wavBytes.Length == 0)
            {
                onError?.Invoke("audio is empty");
                return;
            }

            atfRequesting = true;
            Interrupt();
            APIManager.Instance.RequestAudioToFace(wavBytes,
                result =>
                {
                    atfRequesting = false;
                    ApplyAtfResult(result, wavBytes);
                    onSuccess?.Invoke();
                },
                error =>
                {
                    atfRequesting = false;
                    Debug.LogError("[ATF] " + error);
                    onError?.Invoke(error);
                });
        }

        public void ApplyAtfResult(AtfProtobufResult result, byte[] fallbackAudio = null)
        {
            if (result == null)
            {
                return;
            }

            if (result.Code != 0 && result.Code != 200)
            {
                Debug.LogError($"[ATF] code={result.Code}, message={result.Message}");
                return;
            }

            bool hasResponseAudio = result.Audio != null && result.Audio.Length > 0;
            if (hasResponseAudio)
            {
                AddAudioData(result.Audio);
            }
            else if (fallbackAudio != null && fallbackAudio.Length > 0)
            {
                Debug.Log($"[ATF] response has no audio, playing local wav {fallbackAudio.Length} bytes");
                AddAudioData(fallbackAudio);
            }

            if (result.Blendshapes != null && result.Blendshapes.Length > 0)
            {
                float fps = result.Fps > 0f ? result.Fps : 30f;
                Debug.Log($"[ATF] a2f frames={result.NumFrames}, fps={fps}, bytes={result.Blendshapes.Length}");
                AddEmoteFaceData(result.Blendshapes, fps);
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

                AudioClip audioClip = NAudioPlayer.FromAudioData(mgrData.audioData);

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

