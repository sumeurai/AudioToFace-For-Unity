using SumeruAI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SumeruAI.ATF
{
    public static class LiveLinkTrackingData
    {
        // The proper names of each ARKit blendshape
        public static readonly string[] BSNames =
        {
            "EyeBlinkLeft",
            "EyeLookDownLeft",
            "EyeLookInLeft",
            "EyeLookOutLeft",
            "EyeLookUpLeft",
            "EyeSquintLeft",
            "EyeWideLeft",
            "EyeBlinkRight",
            "EyeLookDownRight",
            "EyeLookInRight",
            "EyeLookOutRight",
            "EyeLookUpRight",
            "EyeSquintRight",
            "EyeWideRight",
            "JawForward",
            "JawLeft",
            "JawRight",
            "JawOpen",
            "MouthClose",
            "MouthFunnel",
            "MouthPucker",
            "MouthLeft",
            "MouthRight",
            "MouthSmileLeft",
            "MouthSmileRight",
            "MouthFrownLeft",
            "MouthFrownRight",
            "MouthDimpleLeft",
            "MouthDimpleRight",
            "MouthStretchLeft",
            "MouthStretchRight",
            "MouthRollLower",
            "MouthRollUpper",
            "MouthShrugLower",
            "MouthShrugUpper",
            "MouthPressLeft",
            "MouthPressRight",
            "MouthLowerDownLeft",
            "MouthLowerDownRight",
            "MouthUpperUpLeft",
            "MouthUpperUpRight",
            "BrowDownLeft",
            "BrowDownRight",
            "BrowInnerUp",
            "BrowOuterUpLeft",
            "BrowOuterUpRight",
            "CheekPuff",
            "CheekSquintLeft",
            "CheekSquintRight",
            "NoseSneerLeft",
            "NoseSneerRight",
            "TongueOut",
            "HeadYaw",
            "HeadPitch",
            "HeadRoll",
            "EyeYawLeft", // LeftEyeYaw
            "EyePitchLeft", // LeftEyePitch
            "EyeRollLeft", // LeftEyeRoll
            "EyeYawRight", // RightEyeYaw
            "EyePitchRight", // RightEyePitch
            "EyeRollRight"
        }; // RightEyeRoll

        // 
        public static readonly HashSet<string> DisabledBSNames = new HashSet<string>
        {
            // "EyeLookDownLeft",
            // "EyeLookInLeft",
            // "EyeLookOutLeft",
            // "EyeLookUpLeft",
            //
            // "EyeLookDownRight",
            // "EyeLookInRight",
            // "EyeLookOutRight",
            // "EyeLookUpRight",
        };

        public static bool IsNameEnabled(string name)
        {
            return string.IsNullOrEmpty(name) || !DisabledBSNames.Contains(name);
        }

        public static bool TryGetName(int index, out string name)
        {
            if (index >= 0 && index < BSNames.Length)
            {
                name = BSNames[index];
                return true;
            }

            name = null;
            return false;
        }
    }

    [Serializable]
    public class LiveLinkData
    {
        public string Name;
        public float Value;
    }


    [Serializable]
    public class BoneData
    {
        public string Name;
        public Vector3 Pos;
        public Vector3 Rot;
    }


    public class FaceData
    {
        public SkinnedMeshRenderer[] SkinnedMeshRenderers;
        public Transform Head;
        public Transform LeftEye;
        public Transform RightEye;
        public Transform Jaw;
        public Sex Sex;
        public MotionType motionType;
        public bool EyeControl { set; get; }
        public Dictionary<string, List<BoneData>> BS51Datas = new Dictionary<string, List<BoneData>>();
        public Dictionary<string, BoneData> NeutralData = new Dictionary<string, BoneData>();
        public List<Transform> ValidBones = new List<Transform>();
        private Dictionary<string, BoneData> CurFrameBoneData = new Dictionary<string, BoneData>();
        private Quaternion initialRotationJaw;
        private Quaternion initialRotationHead;
        private List<ATFPartData> ATFPartDatas = new List<ATFPartData>();


        public void Init()
        {
            if (Jaw != null)
                initialRotationJaw = Jaw.localRotation;

            if (Head != null)
                initialRotationHead = Head.localRotation;


            if (motionType == MotionType.ARKit)
            {
                foreach (var mesh in SkinnedMeshRenderers)
                {
                    ATFPartData aTFPartData = new ATFPartData();
                    aTFPartData.Init(mesh);
                    ATFPartDatas.Add(aTFPartData);
                }
            }

            EyeControl = false;
        }

        public void SetFrameData(float[] data)
        {
            CurFrameBoneData.Clear();
            int count = Mathf.Min(data.Length, LiveLinkTrackingData.BSNames.Length);
            for (int i = 0; i < count; i++)
            {
                if (!LiveLinkTrackingData.TryGetName(i, out var rawName) || !LiveLinkTrackingData.IsNameEnabled(rawName))
                    continue;

                string bsName = rawName.ToLower();

                //if (!bsName.Contains("eye"))
                //    continue;

                if (BS51Datas.ContainsKey(bsName))
                {
                    float scale = 1;
                    var boneDatas = BS51Datas[bsName];
                    for (int j = 0; j < boneDatas.Count; j++)
                    {
                        BoneData tempData = new BoneData();

                        var boneData = boneDatas[j];
                        tempData.Name = boneData.Name;


                        tempData.Pos = boneData.Pos * data[i] * scale;
                        tempData.Rot = boneData.Rot * data[i] * scale;
                        if (CurFrameBoneData.ContainsKey(boneData.Name))
                        {
                            CurFrameBoneData[boneData.Name].Pos += tempData.Pos;
                            CurFrameBoneData[boneData.Name].Rot += tempData.Rot;
                        }
                        else
                        {
                            CurFrameBoneData.Add(tempData.Name, tempData);
                        }
                    }
                }
            }

            foreach (var bone in ValidBones)
            {
                if (CurFrameBoneData.ContainsKey(bone.name))
                {
                    bone.localPosition = CurFrameBoneData[bone.name].Pos + NeutralData[bone.name].Pos;
                    bone.localEulerAngles = CurFrameBoneData[bone.name].Rot + NeutralData[bone.name].Rot;
                }
            }

        }

        public void SetFLAMEBS(float[] data)
        {
            foreach (var skin in SkinnedMeshRenderers)
            {

                for (int i = 0; i < 50; i++)
                {
                    string name = $"Expression_{i + 300}";
                    var val = data[i];
                    float mult = 10.0F;
                    val = val * mult;

                    string Pname = $"P{name}";
                    string Nname = $"N{name}";
                    float Pval = 0;
                    float Nval = 0;
                    if (val > 0)
                    {
                        Pval = val;
                    }
                    else
                    {
                        Nval = -val;
                    }

                    int bid = -1;
                    bid = skin.sharedMesh.GetBlendShapeIndex(Pname);
                    if (bid < 0)
                    {
                        // Debug.Log($"Not found:{Pname}");
                    }
                    else
                    {
                        skin.SetBlendShapeWeight(bid, Pval);
                    }

                    bid = skin.sharedMesh.GetBlendShapeIndex(Nname);
                    if (bid < 0)
                    {
                        //Debug.Log($"Not found:{Nname}");
                    }
                    else
                    {
                        skin.SetBlendShapeWeight(bid, Nval);
                    }
                }
            }

            Matrix4x4 T = Matrix4x4.identity;
            float w = 1.0F;
            Vector3 axisAngle = new Vector3(data[56] * w, data[57] * w, data[58] * w);
            Matrix4x4 transformedRotation = TransformRotationMatrix(axisAngle, T);
            Jaw.localRotation = initialRotationJaw * transformedRotation.rotation;
        }

        public void SetARKitBs(float[] data)
        {
            int count = Mathf.Min(data.Length, LiveLinkTrackingData.BSNames.Length);
            for (int i = 0; i < count; i++)
            {
                if (!LiveLinkTrackingData.TryGetName(i, out var name) || !LiveLinkTrackingData.IsNameEnabled(name))
                    continue;

                var value = data[i];
                value = Mathf.Clamp(value, 0, 1);
                float temp = value / 100 * 100;
                temp = Mathf.Min(temp * 100, 100);

                foreach (var part in ATFPartDatas)
                {
                    part.SetBlendShapeValue(name, temp);
                }
            }

            var headDefaultRot = initialRotationHead.eulerAngles;

            if (Head != null && data.Length > 54)
            {
                float yaw = LiveLinkTrackingData.IsNameEnabled("HeadYaw") ? data[52] : 0f;
                float pitch = LiveLinkTrackingData.IsNameEnabled("HeadPitch") ? data[53] : 0f;
                float roll = LiveLinkTrackingData.IsNameEnabled("HeadRoll") ? data[54] : 0f;
                Head.localEulerAngles = new Vector3(headDefaultRot.x + pitch * -50,
                    headDefaultRot.y + yaw * 50, headDefaultRot.z + roll * 50);
            }

            if (!EyeControl && LeftEye != null && RightEye != null && data.Length > 60)
            {
                LeftEye.localEulerAngles = new Vector3(
                    GetAngle(LiveLinkTrackingData.IsNameEnabled("EyePitchLeft") ? data[56] : 0f),
                    GetAngle(LiveLinkTrackingData.IsNameEnabled("EyeYawLeft") ? data[55] : 0f),
                    GetAngle(LiveLinkTrackingData.IsNameEnabled("EyeRollLeft") ? data[57] : 0f));
                RightEye.localEulerAngles = new Vector3(
                    GetAngle(LiveLinkTrackingData.IsNameEnabled("EyePitchRight") ? data[59] : 0f),
                    GetAngle(LiveLinkTrackingData.IsNameEnabled("EyeYawRight") ? data[58] : 0f),
                    GetAngle(LiveLinkTrackingData.IsNameEnabled("EyeRollRight") ? data[60] : 0f));
            }
        }

        private Matrix4x4 TransformRotationMatrix(Vector3 axisAngle, Matrix4x4 T)
        {
            Matrix4x4 rotationMatrix = AxisAngleToRotationMatrix(axisAngle);
            return T.inverse * rotationMatrix * T;
        }

        private float GetAngle(float value)
        {
            return value / 3.14f * 180f;
        }

        private Matrix4x4 AxisAngleToRotationMatrix(Vector3 axisAngle)
        {
            Vector3 axis = axisAngle.normalized;
            float angle = axisAngle.magnitude;

            float angleInDegrees = angle * Mathf.Rad2Deg;

            Quaternion rotation = Quaternion.AngleAxis(angleInDegrees, axis);

            Matrix4x4 rotationMatrix4x4 = Matrix4x4.Rotate(rotation);

            return rotationMatrix4x4;
        }


    }



    public class ATFMgrData
    {
        private EATFDataType curType;
        private float curInterval;
        private float curTime;
        private bool isPlay;
        private float cachTime;
        private int curFrame;
        private int firstDimension;
        private int secondDimension;
        private float totalTime;
        private Queue<float[]> BSData = new Queue<float[]>();
        private Queue<float[]> BSDataBlender = new Queue<float[]>();
        private int chatBox = -1;
        private float Weight;
        private int Dir;
        private float[] TempBs;
        private FaceData faceData;
        private float femaleEyeOffset = 0.012f;
        private float maleEyeOffset = 0.0142f;
        private Vector3 eyePoint;
        public bool IsBusy { get; set; }
        public UnityAction<ATFMgrData> SpeechOverEvent;
        public UnityAction<ATFMgrData> MotionOverEvent;

        public float[] audio;
        public float time;

        public void Init(FaceData faceData)
        {
            this.faceData = faceData;

            faceData.Init();

            curType = EATFDataType.None;
            Weight = 1;
            Dir = 1;

            if (faceData.Sex == Sex.Male)
            {
                eyePoint = new Vector3(0, 0, maleEyeOffset);
            }
            else
            {
                eyePoint = new Vector3(0, 0, femaleEyeOffset);
            }

        }

        public void SetBlendShapeValue(float[] data)
        {
            if (faceData == null)
                return;

            if (data.Length == 0)
                return;

            switch (faceData.motionType)
            {
                case MotionType.Bones:

                    faceData.SetFrameData(data);

                    break;
                case MotionType.ARKit:

                    faceData.SetARKitBs(data);

                    break;
                case MotionType.FLAME:

                    faceData.SetFLAMEBS(data);

                    break;
            }
        }


        public void StartEmote(EmoteData EmoteData)
        {
            curInterval = EmoteData.Interval;

            if (EmoteData.aTFDataType == EATFDataType.Motion)
            {
                if (BSData.Count == 0 && BSDataBlender.Count == 0)
                {
                    TempBs = EmoteData.EmoteValues[0];
                    SetBSData(EmoteData);
                }
                else if (BSData.Count > 0 && BSDataBlender.Count == 0)
                {
                    Dir = 1;
                    SetBSDataBlender(EmoteData);
                }
                else if (BSData.Count == 0 && BSDataBlender.Count >= 0)
                {
                    Dir = -1;
                    SetBSData(EmoteData);
                }

            }
            else if (EmoteData.aTFDataType == EATFDataType.Chat)
            {
                if (chatBox == -1)
                {
                    if (BSData.Count == 0 && BSDataBlender.Count == 0)
                    {

                        TempBs = EmoteData.EmoteValues[0];
                        SetBSData(EmoteData);
                        chatBox = 0;
                    }
                    else if (BSData.Count == 0 && BSDataBlender.Count != 0)
                    {
                        SetBSData(EmoteData);
                        chatBox = 0;
                        Dir = -1;
                    }
                    else if (BSData.Count != 0 && BSDataBlender.Count == 0)
                    {
                        SetBSDataBlender(EmoteData);
                        chatBox = 1;
                        Dir = 1;
                    }
                }
                else if (chatBox == 0)
                {
                    SetBSData(EmoteData);
                }
                else if (chatBox == 1)
                {
                    SetBSDataBlender(EmoteData);
                }
            }

            curType = EmoteData.aTFDataType;

            isPlay = true;
        }

        private void SetBSData(EmoteData EmoteData)
        {
            for (int i = 0; i < EmoteData.EmoteValues.Count; i++)
            {
                BSData.Enqueue(EmoteData.EmoteValues[i]);
            }
        }

        private void SetBSDataBlender(EmoteData EmoteData)
        {
            for (int i = 0; i < EmoteData.EmoteValues.Count; i++)
            {
                BSDataBlender.Enqueue(EmoteData.EmoteValues[i]);
            }
        }

        private float[] GetData(float deltaTime)
        {
            if (BSDataBlender.Count == 0 && BSData.Count > 0)
            {
                IsBusy = false;
                return BSData.Dequeue();
            }
            else if (BSDataBlender.Count > 0 && BSData.Count > 0)
            {
                IsBusy = true;
                Weight -= 5 * deltaTime;
                Weight = Mathf.Clamp(Weight, 0, 1);
                float[] bd = BSData.Dequeue();
                float[] bdd = BSDataBlender.Dequeue();

                if (bd.Length != bdd.Length)
                {
                    throw new ArgumentException("Arrays must not be null and must have the same length.");
                }

                float[] result = new float[bd.Length];
                for (int i = 0; i < bd.Length; i++)
                {
                    if (Dir == 1)
                    {
                        result[i] = bd[i] * Weight + (1 - Weight) * bdd[i];
                    }
                    else
                    {
                        result[i] = bd[i] * (1 - Weight) + Weight * bdd[i];
                    }
                }

                if (Weight == 0)
                {
                    if (Dir == 1)
                        BSData.Clear();
                    else
                        BSDataBlender.Clear();

                    Dir = 1;
                    Weight = 1;
                }
                else if (BSData.Count == 0 || BSDataBlender.Count == 0)
                {
                    Dir = 1;
                    Weight = 1;
                }

                return result;
            }
            else if (BSDataBlender.Count > 0 && BSData.Count == 0)
            {

                IsBusy = false;
                return BSDataBlender.Dequeue();

            }

            return null;
        }

        public void Interrupt()
        {
            BSData.Clear();
            BSDataBlender.Clear();

            int decaySteps = 15;

            for (int i = 1; i <= decaySteps; ++i)
            {

                int count = 0;

                switch (faceData.motionType)
                {
                    case MotionType.ARKit:
                        count = 61;
                        break;

                    case MotionType.FLAME:
                        count = 65;
                        break;
                }

                if (TempBs != null)
                {
                    float[] Data = new float[count];

                    for (int j = 0; j < count; j++)
                    {
                        Data[j] = TempBs[j] * (1.0f - (float)i / decaySteps);
                    }

                    BSData.Enqueue(Data);
                }
            }
        }

        public void PlayEmote(float deltaTime)
        {
            totalTime += deltaTime;
            if (BSData.Count > 0 || BSDataBlender.Count > 0)
            {
                SetBlendShapeValue(TempBs);

                cachTime += deltaTime;

                while (cachTime >= curInterval)
                {
                    cachTime -= curInterval;
                    TempBs = GetData(deltaTime);
                }
            }
            else if (BSData.Count == 0 || BSDataBlender.Count == 0)
            {
                isPlay = false;
                curFrame = 0;
                cachTime = 0;
                totalTime = 0;
                Weight = 1;
                chatBox = -1;
                Dir = 1;

                if (curType == EATFDataType.Chat)
                {
                    SpeechOverEvent(this);
                }
                else if (curType == EATFDataType.Motion)
                {
                    MotionOverEvent(this);
                }

                curType = EATFDataType.None;
                IsBusy = false;
            }
        }

        public void Update(float deltaTime)
        {
            if (!isPlay)
                return;

            PlayEmote(deltaTime);
        }

        private Quaternion CaculateRotation(Vector3 Point, Transform Eye)
        {
            Vector3 v1 = Point - Eye.position;
            Vector3 v2 = Camera.main.transform.position - Eye.position;

            Vector3 cross = Vector3.Cross(v1, v2);
            float angle = Vector3.Angle(v1, v2);
            Quaternion q = Quaternion.AngleAxis(angle, cross);

            return q * Quaternion.Euler(0, 0, 0);
        }

    }

    public class ATFPartData
    {
        public SkinnedMeshRenderer MeshRenderer;
        public Dictionary<string, string> BSMappingDic = new Dictionary<string, string>();
        public Dictionary<string, int> BSIndexDic = new Dictionary<string, int>();

        public void Init(SkinnedMeshRenderer skinnedMeshRenderer)
        {
            MeshRenderer = skinnedMeshRenderer;
            int BSCount = MeshRenderer.sharedMesh.blendShapeCount;

            for (int i = 0; i < BSCount; i++)
            {
                var tempName = MeshRenderer.sharedMesh.GetBlendShapeName(i);
                BSIndexDic.Add(tempName, i);

                string mappingName = "";

                if (tempName.Contains("Mesh"))
                {
                    mappingName = GetMappingTestName(tempName);
                }
                else
                {
                    mappingName = GetMappingName(tempName);
                }

                if (mappingName != null)
                    BSMappingDic.Add(mappingName, tempName);

            }
        }

        public void SetBlendShapeValue(string name, float value)
        {
            if (!BSMappingDic.ContainsKey(name))
                return;

            if (MeshRenderer == null)
                return;

            MeshRenderer.SetBlendShapeWeight(BSIndexDic[BSMappingDic[name]], value);
        }

        private string GetMappingTestName(string name)
        {
            var tempName = name.Split('.')[1];
            tempName = tempName.Split('h')[1];
            return LiveLinkTrackingData.BSNames[int.Parse(tempName)];
        }

        private string GetMappingName(string name)
        {
            var Count = LiveLinkTrackingData.BSNames.Length;

            for (int i = 0; i < Count; i++)
            {
                var tempLiveName = LiveLinkTrackingData.BSNames[i].ToLower();
                if (name.ToLower().Contains(tempLiveName))
                {
                    return LiveLinkTrackingData.BSNames[i];
                }
            }

            return null;
        }
    }

    public struct EmoteData
    {
        public float Interval;
        public List<float[]> EmoteValues;
        public EATFDataType aTFDataType;

        public EmoteData(float Interval, List<float[]> EmoteValues, EATFDataType aTFDataType)
        {
            this.Interval = Interval;
            this.EmoteValues = EmoteValues;
            this.aTFDataType = aTFDataType;
        }
    }

    public enum MotionType
    {
        Bones,
        ARKit,
        FLAME
    }

    [Serializable]
    public class ATFStreamResponseData
    {
        public string asrResult;

        public string audio;

        public string bs;

        public string content;

        public float audioTimeLen;

        public float fps;

        public string status;

    };

    public enum EATFDataType
    {
        None,
        Motion,
        Chat,
    }

}