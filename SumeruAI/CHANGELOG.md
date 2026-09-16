# Changelog

All notable changes to this package are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.0.6] - 2026-09-16

### Added

- Unity Package Manager install (`com.sumeruai.atf`) via Git URL `?path=/SumeruAI`. API keys are saved under the project's `Assets` so they survive package updates.
- Built-in and URP Xandra sample scenes (`ATF_BuiltIn.unity`, `ATF_URP.unity`), in addition to the existing HDRP `ATF.unity`.
- Pipeline-specific materials (`Materials/HDRP`, `Materials/URP`, `Materials/BuiltIn`) and CoreRP character shaders so the sample runs without the HDRP package.
- Editor import gate that hides HDRP Shader Graphs when HDRP is not installed.
- `SumeruAI → Samples` menu and a Package Manager sample that copy scenes into `Assets`, so Git installs can open them (package folders are read-only).

### Changed

- Sample textures live under `Textures/` instead of sitting next to materials.
- README covers Git UPM, GitHub Releases `.unitypackage`, and copy-into-Assets.
- Character setup docs describe ARKit name matching; they no longer imply MetaHuman `Mesh.xxx` naming.

### Fixed

- Runtime no longer calls `UnityEditor` APIs outside `#if UNITY_EDITOR`, so player builds compile.

## [0.0.5] - 2026-08-26

### Fixed

- Sample record / stop buttons call `AudioToFaceSample`.
- Reset Xandra prefab pose.

## [0.0.4] - 2026-08-20

### Added

- Protobuf Audio-to-Face (`offline-mesh`), with JSON fallback.
- `PlayFromAudio`: one WAV in, blendshapes + audio out.
- Local WAV playback when the server returns no audio.
- WAV / MP3 decode via NAudio.
- Xandra HDRP + ARKit sample scene `ATF.unity`.

### Changed

- Request body is now `{ "traceId", "data": " " }`.
- Sample scene renamed from `AudioToFace.unity` to `ATF.unity` (HDRP).

## [0.0.3] - 2026-03-31

### Changed

- Login API updates.
- README updates.
- Removed default Access Key / Secret Key from the shipped config.

## [0.0.2] - 2026-03-07

### Changed

- Replaced the FBX model used in the sample scene.

## [0.0.1] - 2026-03-06

Initial release: Audio-to-Face runtime, sample scene, and Project Settings API keys.
