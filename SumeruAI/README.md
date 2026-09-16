# SumeruAI AudioToFace for Unity

Drive any ARKit-ready 3D character with speech-synchronized facial animation.

Send a WAV clip (microphone or file) to the [SumeruAI](https://www.sumeruai.us/) Audio-to-Face API. The plugin returns blendshape coefficients and plays them back in sync with audio.

Get an **Access Key** and **Secret Key** from the [Developers](https://www.sumeruai.us/api/developers/keys) page before running the sample.

> Do not commit real keys. Keep `APISettingsConfig.asset` empty in the public repo and fill credentials locally.

---


https://github.com/user-attachments/assets/e621056c-3cb6-45af-ada3-ccb010bbf698


## Features

- Offline Audio-to-Face: audio in, blendshape frames + optional audio out
- ARKit blendshape playback (FLAME / bone-driven modes are also in the runtime)
- Microphone capture at 16 kHz PCM WAV (`AudioRecord`)
- Editor WAV picker in the sample
- Project Settings UI: **Edit → Project Settings → SumeruAI → API Settings**
- Install from Unity Package Manager (Git URL), a `.unitypackage` on [Releases](https://github.com/sumeurai/AudioToFace-For-Unity/releases), or copy into `Assets`
- Protobuf response by default, with JSON fallback

---

## Requirements

| Item | Version / note |
| --- | --- |
| Unity | 2020.3 LTS or newer |
| Render Pipeline | Runtime works on **Built-in, URP, and HDRP**. The Xandra look is pipeline-specific (see [Run the sample](#run-the-sample)) |
| Network | HTTPS access to `https://api.sumeruai.us/` |
| Character | ARKit blendshape names on the face mesh (see [Character setup](#character-setup)) |
| Audio | Send **WAV** to the API. The sample recorder is **16 kHz**, 16-bit PCM. Playback also accepts MP3 via NAudio |

---

## Folder layout

```
SumeruAI/
├── package.json
├── CHANGELOG.md
├── Runtime/                 # Pipeline-agnostic API + blendshape playback
├── Editor/
├── Plugins/                 # NAudio (WAV/MP3 → AudioClip)
├── Resources/
└── Samples/
    ├── Scripts/             # AudioToFaceSample, AudioRecord, CoreRP setup
    ├── Shaders/CoreRP/      # Hair (Built-in/URP); Lit fallbacks
    ├── Scenes/
    │   ├── ATF.unity          # HDRP
    │   ├── ATF_URP.unity      # URP
    │   └── ATF_BuiltIn.unity  # Built-in
    └── Models/Xandra/
        ├── Materials/
        │   ├── HDRP/        # M_Face, M_Skin, M_Hair, …
        │   ├── URP/         # same slot names
        │   └── BuiltIn/     # same slot names
        ├── Textures/        # Face, Hair, Cloth, Eye, Skin, Lashes
        └── Shaders/         # HDRP Shader Graphs (hidden without HDRP)
```

---

## Install

### Package Manager (recommended)

In Unity: **Window → Package Manager → + → Install package from git URL**, then paste:

```
https://github.com/sumeurai/AudioToFace-For-Unity.git?path=/SumeruAI
```

Or add this to your project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.sumeruai.atf": "https://github.com/sumeurai/AudioToFace-For-Unity.git?path=/SumeruAI"
  }
}
```

`?path=/SumeruAI` is required because `package.json` lives in the `SumeruAI` folder, not the repository root. Package id is `com.sumeruai.atf`.

### Unity package (GitHub Releases)

Each [release](https://github.com/sumeurai/AudioToFace-For-Unity/releases) includes a `.unitypackage` exported from the `SumeruAI` folder.

1. Download the `.unitypackage` from the latest release.
2. In Unity: **Assets → Import Package → Custom Package…** and select the file (or double-click it).
3. Import into `Assets/SumeruAI` (keep the `.meta` files checked).

This is the same layout as copying the folder into `Assets`. It is **not** a Package Manager install. HDRP: `Assets/SumeruAI/Samples/Scenes/ATF.unity`. URP: `ATF_URP.unity`. Built-in: `ATF_BuiltIn.unity`.

### Copy into Assets

1. Copy the `SumeruAI` folder into your Unity project's `Assets` directory (keep `.meta` files).
2. If you are not using HDRP, you can omit `Samples/Models/Xandra/Shaders` (HDRP graphs) and open `ATF_URP` or `ATF_BuiltIn` instead of `ATF`.

### After install

1. Open **SumeruAI → API Settings** (or **Edit → Project Settings → SumeruAI → API Settings**).
2. Enter Access Key and Secret Key, then **Save Settings**.  
   Keys are written to `Assets/SumeruAI/Resources/APISettingsConfig.asset` so they stay in your project when the Git package updates. That asset must live under a `Resources` folder so it loads at runtime.

Default endpoints (already set on the sample config):

| Setting | Value |
| --- | --- |
| Base URL | `https://api.sumeruai.us/` |
| Login | `v1/access/auth` |
| Audio-to-Face | `v1/audio-to-face/offline-mesh` |

---

## Run the sample

Pick the scene that matches your render pipeline:

| Pipeline | Scene | Look |
| --- | --- | --- |
| **HDRP** | `Samples/Scenes/ATF.unity` | Original Xandra HDRP skin / hair |
| **URP** | `Samples/Scenes/ATF_URP.unity` | `Materials/URP` + CoreRP hair |
| **Built-in** | `Samples/Scenes/ATF_BuiltIn.unity` | `Materials/BuiltIn` + CoreRP hair |

Package Manager paths are under `Packages/com.sumeruai.atf/…`. Copied or `.unitypackage` installs use `Assets/SumeruAI/…`.

`ATF_URP` and `ATF_BuiltIn` remap Xandra when the scene opens (`XandraCoreRpSetup`): URP uses `Materials/URP`, Built-in uses `Materials/BuiltIn`. Hair and lashes use `SumeruAI/CoreRP/CharacterHair`. Save the scene once after that so the overrides stick. Face animation is the same ARKit blendshape path.

HDRP still looks different: skin SSS and the HDRP Hair BSDF have no Built-in/URP equivalent in Unity 2020.3. The CoreRP sample matches lighting (key / fill / rim) and PBR textures as closely as those pipelines allow.

If you are not using HDRP, you can delete `Samples/Models/Xandra/Shaders` (HDRP Shader Graphs) to avoid import warnings. Keep `Shaders/CoreRP` and `Materials/URP` plus `Materials/BuiltIn`. The plugin also hides that HDRP `Shaders` folder automatically when the HDRP package is not installed (moved to `Shaders~`, which Unity ignores). The first import may still print Shader Graph errors once; they clear after scripts compile and the folder is hidden.

1. Open the scene for your pipeline.
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

`MotionType.ARKit` maps blendshapes on each registered `SkinnedMeshRenderer` onto the 52 ARKit names. Which rule runs depends on the blendshape name:

1. **Substring (usual case)** — the blendshape name contains an ARKit name, ignoring case.  
   `JawOpen`, `jawOpen`, and `head_lod0_mesh_eyeBlinkLeft` all match `JawOpen` / `EyeBlinkLeft`.
2. **Indexed `Mesh` names** — if the name **contains `Mesh`** (this check is case-sensitive) and looks like `Something.Mesh{n}`, `{n}` is the index into the ARKit list (`Mesh0` = `EyeBlinkLeft`, `Mesh17` = `JawOpen`, …).  
   This is a numeric alias used by some exported meshes. It is **not** Unreal MetaHuman `CTRL_expressions.*` naming. MetaHuman (or any mesh) that already embeds ARKit names in the blendshape string uses rule 1.

Examples of ARKit names: `EyeBlinkLeft`, `JawOpen`, `MouthSmileLeft`, `BrowInnerUp`, `CheekPuff`, …

Each API frame is **61 floats**: 52 ARKit weights, then `HeadYaw` / `HeadPitch` / `HeadRoll` and left/right eye yaw/pitch/roll. The 52 shapes drive the mesh. The extra 9 channels only move bones if those transforms were assigned on the face data.

`MotionType.FLAME` looks for `PExpression_{300+i}` / `NExpression_{300+i}` on the mesh. `MotionType.Bones` is for bone-driven faces that already have pose data in the runtime.

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
- NAudio is bundled for WAV/MP3 decoding on playback. See [ThirdPartyNotices.md](ThirdPartyNotices.md).
- `ATF_URP` / `ATF_BuiltIn` use `SumeruAI/CoreRP` character shaders (no HDRP or URP package includes). Hair is `CharacterHair`; skin/cloth/eyes use `CharacterLit` with optional skin wrap. URP reads the main directional light (`_MainLightColor`); Fill / Rim are applied as extra lights by `XandraCoreRpSetup`.
- The plugin is MIT-licensed (`com.sumeruai.atf`). Use of the SumeruAI API is separate (account terms on [sumeruai.us](https://www.sumeruai.us/)).

---

## License

This Unity plugin is [MIT](https://github.com/sumeurai/AudioToFace-For-Unity/blob/main/LICENSE).  
Use of the SumeruAI API is subject to your account terms on [sumeruai.us](https://www.sumeruai.us/).  
Third-party libraries and sample assets: [ThirdPartyNotices.md](ThirdPartyNotices.md).  
Version history: [CHANGELOG.md](CHANGELOG.md).

Issues and contributions are welcome.
