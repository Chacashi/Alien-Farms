---
tags: [modulo, modulo/audio]
---
# Módulo Audio

Carpeta `Assets/Scripts/Audio/` · 19 tipos. Las cajas punteadas son tipos de otros módulos.

Leyenda: `--|>` hereda · `..|>` implementa · `-->` tiene un campo/propiedad de ese tipo. Se muestran campos `[SerializeField]` y la API pública.

## Diagrama de clases
```mermaid
classDiagram
  direction LR
  class AnimalSfxPlayer {
    <<MonoBehaviour>>
    -AudioSource[] _voices
  }
  class AudioMixerParameters {
    <<static>>
    +GetVolumeParameter(VolumeChannel channel)$ string
  }
  class AudioVariation {
    <<static>>
    +PickRandom(T items, IRandomSource rng)$ T
    +PickPitch(Vector2 range, IRandomSource rng)$ float
    +RampPitch(float basePitch, float elapsed, float rampTime, float multiplier)$ float
    +SelectVoice(bool busy, int next)$ int
  }
  class AudioVolumeService {
    <<MonoBehaviour>>
    -AudioMixer _mixer
    -AudioVolumeSettings _settings
    +bool IsMuted
    +GetLevel(VolumeChannel channel) float
    +SetLevel(VolumeChannel channel, float level) void
    +SetMuted(bool muted) void
    +Save() void
  }
  class BombSfxPlayer {
    <<MonoBehaviour>>
    -BombData _data
    -AudioSource[] _oneShotVoices
    -AudioSource[] _fuseVoices
  }
  class CharacterSelectSfxPlayer {
    <<MonoBehaviour>>
    -UiSfxLibrary _library
    -AudioSource[] _voices
  }
  class IVolumeStore {
    <<interface>>
    +GetFloat(string key, float defaultValue) float
    +SetFloat(string key, float value) void
    +GetBool(string key, bool defaultValue) bool
    +SetBool(string key, bool value) void
    +Save() void
  }
  class MatchConnectionCueTracker {
    +SetPhase(MatchPhase phase) void
    +Reset() void
    +Apply(PlayerId id, PlayerConnectionStatus status, Span~UiSfxCue~ output, bool isLocalSeat) int
  }
  class MatchStatusSfxPlayer {
    <<MonoBehaviour>>
    -UiSfxLibrary _library
    -AudioSource[] _voices
  }
  class MusicPlayer {
    <<MonoBehaviour>>
    -AudioClip _track
    -bool _playDuringPause
  }
  class OnlineLobbyCueRules {
    <<static>>
    +Diff(OnlineLobbySnapshot prev, OnlineLobbySnapshot curr)$ int
  }
  class OnlineLobbySnapshot {
    <<struct>>
    +OnlineLobbyPhase Phase
    +int SeatCount
    +OnlineLobbySeat Seat0
    +OnlineLobbySeat Seat1
    +bool HasLocalSeat
    +bool IsInRoom
    +Capture(OnlineLobbyChannel channel)$ OnlineLobbySnapshot
    +TryGetSeat(PlayerId id, OnlineLobbySeat seat) bool
  }
  class OnlineSessionCueRules {
    <<static>>
    +OnStatusChanged(OnlineSessionStatus prev, OnlineSessionStatus curr)$ UiSfxCue
    +IsArrivalNotice(OnlineSessionStatus status, string errorKey)$ bool
  }
  class OnlineSessionSfxPlayer {
    <<MonoBehaviour>>
    -UiSfxLibrary _library
    -OnlineSessionChannel _session
    -OnlineLobbyChannel _lobby
    -bool _playRemotePicks
    -AudioSource[] _voices
  }
  class PlayerPrefsVolumeStore {
    +GetFloat(string key, float defaultValue) float
    +SetFloat(string key, float value) void
    +GetBool(string key, bool defaultValue) bool
    +SetBool(string key, bool value) void
    +Save() void
  }
  class SfxVoicePool {
    +Play(SoundCue cue, IRandomSource rng, bool ignoreListenerPause) bool
  }
  class VolumeChannel {
    <<enumeration>>
    Master
    Music
    Sfx
  }
  class VolumeMath {
    <<static>>
    +LinearToDecibels(float linear)$ float
    +DecibelsToLinear(float decibels)$ float
  }
  class VolumeState {
    +bool IsMuted
    +GetLevel(VolumeChannel channel) float
    +SetLevel(VolumeChannel channel, float level) bool
    +SetMuted(bool muted) bool
    +GetAppliedDecibels(VolumeChannel channel) float
    +Save() void
    +GetKey(VolumeChannel channel)$ string
  }
  class AudioVolumeSettings:::external
  <<ScriptableObject>> AudioVolumeSettings
  class Bomb:::external
  <<MonoBehaviour>> Bomb
  class BombData:::external
  <<ScriptableObject>> BombData
  class IRandomSource:::external
  <<interface>> IRandomSource
  class OnlineLobbyChannel:::external
  <<ScriptableObject>> OnlineLobbyChannel
  class OnlineLobbySeat:::external
  <<struct>> OnlineLobbySeat
  class OnlineSessionChannel:::external
  <<ScriptableObject>> OnlineSessionChannel
  class UiSfxLibrary:::external
  <<ScriptableObject>> UiSfxLibrary
  AnimalSfxPlayer --> IRandomSource
  AudioVolumeService --> AudioVolumeSettings
  AudioVolumeService --> VolumeState
  BombSfxPlayer --> Bomb
  BombSfxPlayer --> BombData
  BombSfxPlayer --> IRandomSource
  CharacterSelectSfxPlayer --> IRandomSource
  CharacterSelectSfxPlayer --> SfxVoicePool
  CharacterSelectSfxPlayer --> UiSfxLibrary
  MatchStatusSfxPlayer --> IRandomSource
  MatchStatusSfxPlayer --> MatchConnectionCueTracker
  MatchStatusSfxPlayer --> SfxVoicePool
  MatchStatusSfxPlayer --> UiSfxLibrary
  OnlineLobbySnapshot --> OnlineLobbySeat
  OnlineSessionSfxPlayer --> IRandomSource
  OnlineSessionSfxPlayer --> OnlineLobbyChannel
  OnlineSessionSfxPlayer --> OnlineLobbySnapshot
  OnlineSessionSfxPlayer --> OnlineSessionChannel
  OnlineSessionSfxPlayer --> SfxVoicePool
  OnlineSessionSfxPlayer --> UiSfxLibrary
  PlayerPrefsVolumeStore ..|> IVolumeStore
  VolumeState --> IVolumeStore
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Tipos

| Tipo | Clase | Descripción |
|---|---|---|
| [[AnimalSfxPlayer]] | MonoBehaviour | Plays animal voices: the grab voice when any player picks an animal up (stealing included, since the thief grabs it again after the tackle k… |
| [[AudioMixerParameters]] | static | Names of the exposed AudioMixer parameters (volume of each group, in dB). The only place these strings live: they must match the exposed par… |
| [[AudioVariation]] | static | Pure helpers that add variety to repeated sounds (random clip and pitch, voice selection). |
| [[AudioVolumeService]] | MonoBehaviour | Per-scene volume service: loads the saved levels (PlayerPrefs) and drives the exposed volume parameters of the AudioMixer. Not a singleton: … |
| [[BombSfxPlayer]] | MonoBehaviour | Plays the stun bomb sounds: spawn pop, grab, a looped fuse per armed bomb (its pitch rises while it flies and it is cut when that bomb deton… |
| [[CharacterSelectSfxPlayer]] | MonoBehaviour |  |
| [[IVolumeStore]] | interface | Key/value persistence for volume preferences (PlayerPrefs at runtime, a fake in tests). |
| [[MatchConnectionCueTracker]] | class |  |
| [[MatchStatusSfxPlayer]] | MonoBehaviour |  |
| [[MusicPlayer]] | MonoBehaviour | Loops a scene's music track on a 2D AudioSource (route its output to the Music mixer group). Optionally keeps playing while the game is paus… |
| [[OnlineLobbyCueRules]] | static |  |
| [[OnlineLobbySnapshot]] | struct |  |
| [[OnlineSessionCueRules]] | static |  |
| [[OnlineSessionSfxPlayer]] | MonoBehaviour |  |
| [[PlayerPrefsVolumeStore]] | class | `IVolumeStore` backed by `PlayerPrefs` (bools stored as 0/1 ints). |
| [[SfxVoicePool]] | class |  |
| [[VolumeChannel]] | enumeration | Volume channels exposed by the AlienFarmsMixer. Values index per-channel arrays. |
| [[VolumeMath]] | static | Conversions between linear slider volume (0..1) and AudioMixer attenuation (dB). |
| [[VolumeState]] | class | Plain C# volume model: linear level per `VolumeChannel` plus a master mute, loaded from and written through an `IVolumeStore`. Missing keys … |

## Depende de
[[Modulo Animals|Animals]] (1), [[Modulo Core|Core]] (23), [[Modulo Data|Data]] (19), [[Modulo Items|Items]] (1), [[Modulo Match|Match]] (1), [[Modulo Networking|Networking]] (3), [[Modulo Utilities|Utilities]] (12)