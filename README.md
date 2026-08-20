# SumeruAI AudioToFace for Unity

Drive any ARKit-ready 3D character with speech-synchronized facial animation.

Send a WAV clip (microphone or file) to the [SumeruAI](https://www.sumeruai.us/) Audio-to-Face API. The plugin returns blendshape coefficients and plays them back in sync with audio.

Get an **Access Key** and **Secret Key** from the [Developers](https://www.sumeruai.us/) page before running the sample.

> Do not commit real keys. Keep `APISettingsConfig.asset` empty in the public repo and fill credentials locally.

---

## Features

- Offline Audio-to-Face: one WAV in, blendshape frames + optional audio out
- ARKit blendshape playback (FLAME / bone-driven modes are also in the runtime)
- Microphone capture at 16 kHz PCM WAV (`AudioRecord`)
- Editor WAV picker in the sample
- Project Settings UI: **Edit → Project Settings → SumeruAI → API Settings**
- Protobuf response by default, with JSON fallback

---

## Requirements

| Item | Version / note |
| --- | --- |
| Unity | 2020.3 LTS or newer |
| Render Pipeline | **HDRP** for the bundled Xandra sample. Runtime code only needs `SkinnedMeshRenderer` |
| Network | HTTPS access to `https://api.sumeruai.us/` |
| Character | ARKit blendshape names on the face mesh (see [Character setup](#character-setup)) |
| Audio | PCM WAV. The sample recorder uses **16 kHz**, 16-bit |

---

## Folder layout

```
Assets/SumeruAI/
├── Runtime/          # API, AudioToFace manager, protobuf parser
├── Editor/           # Project Settings window
├── Plugins/          # NAudio (WAV → AudioClip)
├── Resources/        # APISettingsConfig.asset (put keys here)
└── Samples/
    ├── Scenes/ATF.unity
    ├── Scripts/      # AudioToFaceSample, AudioRecord
    └── Models/Xandra # HDRP sample character
```

---

## Install

1. Copy `Assets/SumeruAI` into your Unity project (keep `.meta` files).
2. If you are not using HDRP, you can omit `Samples/Models/Xandra` and still use the runtime on your own character.
3. Open **SumeruAI → API Settings** (or **Edit → Project Settings → SumeruAI → API Settings**).
4. Enter Access Key and Secret Key, then **Save Settings**.  
   The asset must live under a `Resources` folder so it loads at runtime.

Default endpoints (already set on the sample config):

| Setting | Value |
| --- | --- |
| Base URL | `https://api.sumeruai.us/` |
| Login | `v1/access/auth` |
| Audio-to-Face | `v1/audio-to-face/offline-mesh` |

---

## Run the sample

1. Open `Assets/SumeruAI/Samples/Scenes/ATF.unity`.
2. Press Play. `AudioToFaceSample` logs in and registers the character as ARKit.
3. Use the UI:
   - **Start Record** / **Stop Record** — capture from the default microphone, then send.
   - **Select Local Audio** — pick a `.wav` in the Editor (or press **J**).
4. After the API returns, the plugin plays audio and drives the face.

---

## Integrate into your scene

Minimal flow: register a character, log in, send WAV bytes.

```csharp
using SumeruAI;
using SumeruAI.API;
using SumeruAI.ATF;
using UnityEngine;

public class MyAtfSetup : MonoBehaviour
{
    [SerializeField] SkinnedMeshRenderer[] faceMeshes;
    [SerializeField] Transform rootBone;

    void Start()
    {
        AudioToFaceManager.GetInstance().RegisterModel(
            id: 0,
            sex: Sex.Female,
            skinnedMeshRenderers: faceMeshes,
            RootBone: rootBone,
            motionType: MotionType.ARKit);

        APIManager.Instance.Login();
    }

    public void PlayWav(byte[] wavBytes)
    {
        AudioToFaceManager.GetInstance().PlayFromAudio(
            wavBytes,
            onSuccess: () => Debug.Log("ATF started"),
            onError: err => Debug.LogError(err));
    }
}
```

Microphone → WAV → play:

```csharp
audioRecord.StopRecord((base64, wavBytes) =>
{
    AudioToFaceManager.GetInstance().PlayFromAudio(wavBytes);
});
```

### Multiple characters

Call `RegisterModel` with a unique `id` per character. Playback goes to every registered model. Use `UnRegisterModel(id)` when a character is destroyed.

### Events

```csharp
var atf = AudioToFaceManager.GetInstance();
atf.StartSpeechEvent += () => { /* audio started */ };
atf.StopSpeechEvent  += () => { /* audio finished */ };
atf.StopMotionEvent  += () => { /* blendshapes finished */ };
atf.Interrupt(); // cancel queues and fade current expression
```

---

## Character setup

`MotionType.ARKit` matches blendshape names on each `SkinnedMeshRenderer` against ARKit names (case-insensitive substring), for example:

`EyeBlinkLeft`, `JawOpen`, `MouthSmileLeft`, `BrowInnerUp`, `CheekPuff`, …

Head / eye bones are used when present (`head`, `jaw`, and eye transforms). Name blendshapes close to ARKit; MetaHuman-style `Mesh.xxx` names are also mapped.

`MotionType.FLAME` and `MotionType.Bones` are available in the runtime if your mesh uses those conventions.

---

## API overview

Callers normally use `PlayFromAudio`. The HTTP layer is:

1. **Login** `POST v1/access/auth`  
   Body: `{ "accessKey", "secretKey" }` → `accessToken`
2. **Audio-to-Face** `POST v1/audio-to-face/offline-mesh`  
   Header: `Authorization: <token>`, `Accept: application/x-protobuf`  
   Body: `{ "traceId", "data": "<wav-base64>" }`

Response (protobuf preferred; JSON still accepted):

| Field | Meaning |
| --- | --- |
| `fps` | Blendshape frame rate (sample uses 30 if missing) |
| `audio` / `audioKey` | Optional WAV. If empty, the local clip is played |
| `blendshapes` / `emoteKey` | Little-endian float32, **61 values per frame** (52 ARKit + head/eye extras) |

Only one ATF request runs at a time. A new `PlayFromAudio` while a request is in flight is ignored.

---

## Platform notes

- Networking and callbacks run on the Unity main thread.
- `AudioRecord` uses `Microphone` (available on Editor, Windows, and most players with mic permission).
- Local file picker uses `UnityEditor` and is Editor-only.
- NAudio is bundled for WAV decoding. See [ThirdPartyNotices.md](ThirdPartyNotices.md).

---

## License

Use of the SumeruAI API is subject to your account terms on [sumeruai.us](https://www.sumeruai.us/).  
Third-party libraries and sample assets: [ThirdPartyNotices.md](ThirdPartyNotices.md).

Issues and contributions are welcome.
