# AudioToFace-For-Unity

Audio-to-3D Face SDK for Unity enables developers to generate real-time 3D facial animations from speech audio using our AI-powered API. Simply feed in an audio clip or audio stream, and the SDK returns a sequence of facial blendshape coefficients that can be applied to any 3D character model — bringing your avatars to life with natural, speech-synchronized expressions.

> ⚠️ This repository is tailored as a sample. Replace API endpoints, keys and assets with your own when integrating into a real application.

---

https://github.com/user-attachments/assets/491d7d98-b6d3-46f0-9da7-aa770689b905

## 🎯 Features

- Microphone recording with automatic WAV conversion (`AudioRecord`)
- Login and HTTP request handling via `APIManager` and `APISettingsConfig`
- Streaming of audio and emote frames into your scene using `AudioToFaceManager`
- Sample MonoBehaviour (`AudioToFaceSample`) with UI buttons for recording, sending audio, and selecting local WAV files


## 🔧 Setup Instructions

1. **Open the project** in Unity (2020.3+ recommended).
2. **Configure the APISettingsConfig asset** (a default configuration is provided for demonstration; you can either modify it directly or create your own):
   - Get your `Access Key` and `Secret Key` from the `Developers` page at `https://www.sumeruai.us/`.
   - **Option A**: Modify the default configuration at `Assets/SumeruAI/Resources` – fill in the `Access Key`, `Secret Key`, and the base URL plus relative paths for `login` and `atfMesh`.
   - **Option B**: Create a new configuration – in the Project window, `Right-click → Create → SumeruAI → API Settings Config`, then fill in the required API credentials and paths from the `Developers` page.
   - The asset must reside under a `Resources` folder so it can be loaded at runtime.

3. **Scene preparation**:
   - Open the sample scene: `Assets/SumeruAI/Samples/Scenes/AudioToFace.unity`
   - The scene already includes a GameObject with `AudioToFaceSample` attached.
   
4. **Run the sample**:
   - Press Play in the Editor.
   - Use the UI buttons: "Start Record" to begin recording, "Stop Record" to stop and send the audio; "Select Local Audio" (Editor only) to pick a WAV file and send it.
   - After the server responds, the manager will enqueue audio and blendshape data. The face should animate and the audio play back.

5. **Build targets**: The code currently supports Editor and Windows (`Application.platform` check for writing files). Add platform-specific paths if needed.


## 📡 API Protocol

Data classes defined in `RequestUtil.cs` are serialized as JSON for communication:

```csharp
[Serializable]
public class ATFReqData
{
    public string status;              // "start" or "stop" etc.
    public string dialogueBase64;      // base64-encoded WAV bytes
    public string lastDialogueBase64;  // optional
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
    public long id;
    public string emoteKey;            // base64-encoded float array for blendshapes
    public string audioKey;            // base64-encoded WAV bytes
    public float fps;
}
```

The sample sends requests via `APIManager.Request<TReq,TRep>`, which automatically attaches the access token obtained from `Login()`.


## 🧩 Extending the Sample

- **Multiple characters**: call `AudioToFaceManager.GetInstance().RegisterModel` with distinct IDs.
- **Custom recorders**: replace `AudioRecord` with your own microphone or file logic – just call `AudioToFaceManager.AddAudioFaceData` with the returned base64 strings.
- **UI hooks**: subscribe to `AudioToFaceManager` events (`StartSpeechEvent`, `StopSpeechEvent`, `StopMotionEvent`, etc.) for in-game notifications.


## ✅ Notes & Tips

- All networking is asynchronous; callbacks are executed on the Unity main thread.
- The manager converts the server's byte arrays into float arrays and then into `EmoteData` objects for playback.
- The sample includes minimal error logging; expand it when integrating into production.
- Editor-only code uses `UnityEditor` APIs guarded by `#if UNITY_EDITOR`.


---

Feel free to fork and adapt this sample for your own AudioToFace workflows. Contributions and issues are welcome!
