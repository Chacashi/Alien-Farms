---
tags: [modulo, modulo/networking]
---
# Módulo Networking

Carpeta `Assets/Scripts/Networking/` · 132 tipos. Las cajas punteadas son tipos de otros módulos.

Leyenda: `--|>` hereda · `..|>` implementa · `-->` tiene un campo/propiedad de ese tipo. Se muestran campos `[SerializeField]` y la API pública.

## Diagrama de clases
```mermaid
classDiagram
  direction LR
  class AnimalNetState {
    <<struct>>
    +AnimalState State
    +PlayerId Holder
    +PlayerId ZoneOwner
    +DropReason LastDrop
    +NetworkSerialize(BufferSerializer~T~ serializer) void
    +Equals(AnimalNetState other) bool
    +Equals(object obj) bool
    +GetHashCode() int
  }
  class ApprovalDecision {
    <<struct>>
    +ApprovalOutcome Outcome
    +PlayerId Seat
    +string Reason
    +bool Approved
    +AssignSeat(PlayerId seat)$ ApprovalDecision
    +Reconnect(PlayerId seat)$ ApprovalDecision
    +Reject(string reason)$ ApprovalDecision
  }
  class ApprovalOutcome {
    <<enumeration>>
    Rejected
    AssignSeat
    Reconnect
  }
  class ApprovalPolicy {
    <<struct>>
    +int ProtocolVersion
    +int PlayerSeats
    +float ReconnectGraceSeconds
    +From(NetworkSettings settings)$ ApprovalPolicy
  }
  class ApprovalRules {
    <<static>>
    +CheckMembership(bool isHostClient, bool onlineSession, bool hasUgsPlayerId)$ SessionMembership
    +NeedsIdentity(bool payloadValid, ConnectionPayload payload, ApprovalPolicy policy)$ bool
    +Evaluate(bool payloadValid, ConnectionPayload payload, ApprovalPolicy policy)$ ApprovalDecision
    +IsSeatFree(PlayerSlot seat, double now, float graceSeconds)$ bool
    +IsReservationExpired(PlayerSlot seat, double now, float graceSeconds)$ bool
  }
  class BombNetState {
    <<struct>>
    +BombState State
    +PlayerId Holder
    +PlayerId Thrower
    +NetworkSerialize(BufferSerializer~T~ serializer) void
    +Equals(BombNetState other) bool
    +Equals(object obj) bool
    +GetHashCode() int
  }
  class CarriableOwner {
    <<struct>>
    +CarriableOwner Server$
    +CarriableOwner Unresolved$
    +CarriableOwnerKind Kind
    +PlayerId Seat
    +ForSeat(PlayerId seat)$ CarriableOwner
    +Equals(CarriableOwner other) bool
    +Equals(object obj) bool
    +GetHashCode() int
    +bool operator$
    +ToString() string
  }
  class CarriableOwnerKind {
    <<enumeration>>
    Server
    Player
    Unresolved
  }
  class CarriableOwnerRules {
    <<static>>
    +Resolve(ulong ownerClientId, ulong serverClientId, PlayerId claimant, PlayerId ownerClientSeat)$ CarriableOwner
    +IsLabelShown(CarriableOwner owner, PlayerId claimant, bool isPureClient)$ bool
    +PreviousOwnerSeat(PlayerId lookedUp, PlayerId cached, PlayerId claimant)$ PlayerId
    +TagIndex(CarriableOwner owner)$ int
    +Tag(CarriableOwner owner)$ string
    +SeatTag(PlayerId seat)$ string
    +DescribeClient(ulong clientId, ulong serverClientId, PlayerId seat)$ string
  }
  class CarriableOwnerTint {
    <<MonoBehaviour>>
    -NetworkCarriable _carriable
    -Renderer[] _indicators
    -bool _hideWhenServerOwned
    -Color _neutralColor
  }
  class CarriableOwnershipLabel {
    <<MonoBehaviour>>
    -NetworkCarriable _carriable
    -TMP_Text _text
    -Transform _billboard
    -string _yesMark
    -string _noMark
  }
  class CharacterPickRules {
    <<static>>
    +Pick(int seatIndex, int otherCharacter, int rosterCount)$ int
  }
  class CloudSaveErrorRules {
    <<static>>
    +ToKey(CloudSaveExceptionReason reason)$ string
    +IsRetryable(CloudSaveExceptionReason reason)$ bool
  }
  class CloudSavePlayerStatsStore {
    +LoadOwnAsync() Task~StatsStoreResult~
    +LoadPublicAsync(string playerId) Task~StatsStoreResult~
    +SaveOwnAsync(PlayerStatsRecord record, string writeLock) Task~StatsStoreResult~
  }
  class CloudSaveStatsService {
    <<MonoBehaviour>>
    -NetworkSettings _settings
    -OnlineSessionChannel _session
    -PlayerProgressChannel _progress
    -NetworkRuntimeAnchor _anchor
  }
  class ConnectionApprovalHandler {
    <<MonoBehaviour>>
    -NetworkSettings _settings
    -float _membershipWaitSeconds
    -NetworkManager _manager
    -RelaySessionConnection _sessionConnection
    +bool IsServerStopping
    +ServerSlotDirectory Slots
    +bool GraceEnabled
    +double Now
    +event Action~PlayerId~ SeatReserved
    +event Action~PlayerId, PlayerConnectionStatus~ SeatReleased
    +SetGraceEnabled(bool enabled) void
    +NotifyLeaving(ulong clientId) void
    +ReleaseLeftMember(string ugsPlayerId) void
    ...3 más
  }
  class ConnectionPayload {
    <<struct>>
    +int ProtocolVersion
    +FixedString32Bytes DisplayName
    +FixedString64Bytes UgsPlayerId
    +Create(int protocolVersion, string displayName, string ugsPlayerId)$ ConnectionPayload
    +bool HasUgsPlayerId
    +WithSanitizedName() ConnectionPayload
    +NetworkSerialize(BufferSerializer~T~ serializer) void
    +ToBytes() byte[]
    +TryParse(byte data, ConnectionPayload payload)$ bool
  }
  class DebugViewSwitch {
    +event Action Changed
    +bool HasOverride
    +IsVisible(bool defaultVisible) bool
    +SetVisible(bool visible) void
    +Toggle(bool defaultVisible) void
    +ClearOverride() void
    +Reset() void
  }
  class DevConnectionPanel {
    <<MonoBehaviour>>
    -NetworkRuntimeAnchor _anchor
    -MatchSession _session
    -NetworkSettings _settings
    -string _displayName
    -TMP_Text _statusLabel
    -TMP_InputField _addressField
    -OnlineSessionChannel _sessionChannel
    -TMP_InputField _codeField
    -OnlineLobbyChannel _lobbyChannel
    +SignIn() void
    +CreateSession() void
    +JoinSession() void
    +LeaveSession() void
    ...4 más
  }
  class DirectConnection {
    <<MonoBehaviour>>
    -UnityTransport _transport
    -string _address
    -int _port
    -string _listenAddress
    +string Address
    +int Port
    +SetAddress(string address) void
    +StartHostAsync(NetworkManager manager) Task~bool~
    +StartClientAsync(NetworkManager manager) Task~bool~
  }
  class DisconnectReasons {
    <<static>>
    +IsKnown(string reason)$ bool
    +Normalize(string reason)$ string
    +Resolve(string reason, bool hostAnnouncedLeaving)$ string
  }
  class DisplayNameResolver {
    <<static>>
    +Resolve(string typedName, string ugsName, string fallbackName, string instanceTag)$ string
  }
  class HostShutdownSequence {
    +bool IsRunning
    +ShouldDefer(bool isServer, int notifiedClients, float noticeSeconds)$ bool
    +Begin(double now, float noticeSeconds) void
    +Reset() void
    +Step(double now, bool isListening, bool shutdownInProgress, bool hasRemoteClients) HostShutdownStep
  }
  class HostShutdownStep {
    <<enumeration>>
    Wait
    DisconnectClients
    Shutdown
    Complete
  }
  class IConnectionStrategy {
    <<interface>>
    +StartHostAsync(NetworkManager manager) Task~bool~
    +StartClientAsync(NetworkManager manager) Task~bool~
  }
  class IPendingStatsStore {
    <<interface>>
    +Load(string ownerId) PendingStats
    +Save(string ownerId, PendingStats pending) void
    +Clear(string ownerId) void
  }
  class IPlayerStatsStore {
    <<interface>>
    +LoadOwnAsync() Task~StatsStoreResult~
    +LoadPublicAsync(string playerId) Task~StatsStoreResult~
    +SaveOwnAsync(PlayerStatsRecord record, string writeLock) Task~StatsStoreResult~
  }
  class InputCommandBatch {
    <<struct>>
    +int Count
    +Push(PlayerInputCommand command) void
    +Get(int index) PlayerInputCommand
    +Clear() void
    +NetworkSerialize(BufferSerializer~T~ serializer) void
  }
  class InputCommandBuffer {
    +int TargetDepth
    +int PendingCount
    +uint LastConsumedSeq
    +Enqueue(PlayerInputCommand command) bool
    +Consume(bool fresh) PlayerInputCommand
    +Clear() void
  }
  class InterpolatedPose {
    <<struct>>
    +Vector3 Position
    +Vector3 Velocity
    +float FacingYaw
    +float StunRemaining
    +PlayerSnapshotFlags Flags
    +bool IsClamped
    +Vector3 FacingDirection
  }
  class LeaveNoticeMessenger {
    <<MonoBehaviour>>
    -NetworkManager _manager
    -ConnectionApprovalHandler _approval
    +bool HostAnnouncedLeaving
    +event Action HostLeavingAnnounced
    +SendLeavingNotice() bool
    +SendHostLeavingNotice() int
  }
  class LobbyRequestResult {
    <<enumeration>>
    Accepted
    Released
    Unchanged
    RejectedNotSeated
    RejectedOutOfRange
    RejectedTaken
    RejectedClosed
    RejectedLocked
  }
  class LobbyRules {
    <<static>>
    +EvaluateRequest(LobbySlotEntry entries, int count, ulong senderClientId)$ LobbyRequestResult
    +ApplyRequest(LobbySlotEntry entries, int count, ulong senderClientId)$ LobbyRequestResult
    +IsTaken(LobbySlotEntry entries, int count, byte index, PlayerId except)$ bool
    +CanStart(LobbySlotEntry entries, int count, int requiredSeats)$ bool
    +Reconcile(LobbySlotEntry current, int count, ReadOnlySpan~PlayerSlot~ seats, LobbySlotEntry output)$ int
    +ToErrorKey(LobbyRequestResult result)$ string
    +IsRejected(LobbyRequestResult result)$ bool
  }
  class LobbySlotEntry {
    <<struct>>
    +PlayerId Id
    +ulong ClientId
    +byte CharacterIndex
    +FixedString32Bytes DisplayName
    +bool HasCharacter
    +WithCharacter(byte characterIndex) LobbySlotEntry
    +NetworkSerialize(BufferSerializer~T~ serializer) void
    +Equals(LobbySlotEntry other) bool
    +Equals(object obj) bool
    +GetHashCode() int
  }
  class LobbyStateSpawner {
    <<MonoBehaviour>>
    -NetworkRuntimeAnchor _anchor
    -OnlineSessionChannel _session
    -OnlineLobbyChannel _lobbyChannel
    -NetworkObject _lobbyPrefab
  }
  class LocalPlayerRigLookup {
    <<MonoBehaviour>>
    -NetworkRuntimeAnchor _anchor
    +TryGetRig(PlayerId id, PlayerRig rig) bool
  }
  class MatchClock {
    <<static>>
    +CountdownSecondsLeft(double countdownEndTime, double now)$ int
    +DisplaySeconds(MatchPhase phase, double roundEndTime, double now, float fullDuration)$ int
    +DisplaySeconds(MatchPhase phase, double roundEndTime, double now, float fullDuration, float frozenRemaining)$ int
    +TicksCountdown(MatchPhase phase, float frozenRemaining)$ bool
  }
  class MatchStatsTally {
    +bool SawPlayingLive
    +bool IsCompleted
    +bool IsRoundInProgress
    +OnPhaseChanged(MatchPhase previous, MatchPhase current) void
    +TryComplete(PlayerId local, MatchResult result, int animals, int tackles, PlayerStatsSummary delta) bool
    +TryCompleteEarly(bool localWon, int animals, int tackles, PlayerStatsSummary delta) bool
  }
  class MembershipMatch {
    <<enumeration>>
    Unknown
    Same
    Other
  }
  class MembershipRules {
    <<static>>
    +Classify(string sessionCode, string joinCode)$ MembershipMatch
  }
  class Mode {
    <<enumeration>>
    Idle
    ServerRemote
    OwnerClient
  }
  class Mode {
    <<enumeration>>
    Idle
    OwnerSender
    ServerReceiver
  }
  class NameTagRules {
    <<static>>
    +IsVisible(bool isSpawned, bool showNameTags, bool isLocalOwner, PlayerId seat)$ bool
    +ResolveName(string displayName, PlayerId seat)$ string
    +ResolveColor(CharacterDefinition character, Color fallback)$ Color
    +ClientDetail(bool showClientId, ulong clientId)$ string
  }
  class NetworkAnimal {
    -Animal _animal
    +Animal Animal
    +AnimalNetState ReplicatedState
    +IGrabbable Grabbable
    +PlayerId Claimant
    #Awake() void
    +ServerPrepareForSpawn() void
    +OnNetworkSpawn() void
    +OnNetworkDespawn() void
    #RestoreAuthorityPhysics() void
    #OnValidate() void
  }
  class NetworkAnimalPoolHandler {
    +Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation) NetworkObject
    +Destroy(NetworkObject networkObject) void
  }
  class NetworkBomb {
    -Bomb _bomb
    +Bomb Bomb
    +BombNetState ReplicatedState
    +IGrabbable Grabbable
    +PlayerId Claimant
    #Awake() void
    +ServerPrepareForSpawn() void
    +OnNetworkSpawn() void
    +OnNetworkDespawn() void
    #RestoreAuthorityPhysics() void
    #OnValidate() void
  }
  class NetworkBombPoolHandler {
    +Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation) NetworkObject
    +Destroy(NetworkObject networkObject) void
  }
  class NetworkCarriable {
    <<abstract>>
    -NetworkSettings _settings
    -NetworkTransform _networkTransform
    +event Action~NetworkCarriable, ulong, ulong~ OwnershipChanged
    +event Action~NetworkCarriable~ ViewChanged
    +IGrabbable Grabbable*
    +PlayerId Claimant*
    +NetworkRuntimeAnchor Anchor
    +NetworkSettings Settings
    +bool IsInPlay
    +int SpawnCount
    +ResolveOwner() CarriableOwner
    +Bind(NetworkRuntimeAnchor anchor) void
    +TryRequestThrow(float yaw, float pitch) bool
    ...13 más
  }
  class NetworkCarry {
    -NetworkSettings _settings
    -PlayerGrabber _grabber
    -PlayerAim _aim
    -ThirdPersonCamera _camera
    -NetworkPlayerInput _input
    -PlayerInputHandler _localInput
    +bool IsThrowPending
    +OnNetworkSpawn() void
    +OnNetworkDespawn() void
    +ServerHandleThrowRequest(IGrabbable target, float yaw, float pitch, ulong senderClientId) void
  }
  class NetworkCommandSource {
    +event Action GrabStarted
    +event Action GrabCanceled
    +Vector2 Move
    +Vector2 Look
    +bool IsLookPointerDelta
    +bool IsSprintHeld
    +bool IsGrabHeld
    +bool IsAimHeld
    +bool IsPaused
    +Apply(PlayerInputCommand command) void
    +Reset() void
  }
  class NetworkLobbyState {
    -NetworkRuntimeAnchor _anchor
    -NetworkSettings _settings
    -CharacterRoster _roster
    -OnlineLobbyChannel _lobbyChannel
    +OnNetworkSpawn() void
    +OnNetworkDespawn() void
    +OnDestroy() void
  }
  class NetworkMatchExit {
    <<MonoBehaviour>>
    -NetworkRuntimeAnchor _anchor
    -MatchManager _match
    +bool BothReady
  }
  class NetworkMatchState {
    -MatchManager _match
    -ObstacleField _obstacles
    +MatchPhase Phase
    +double RoundEndTime
    +ScoreBoard Scores
    +PlayerId ForfeitWinner
    +bool IsClockFrozen
    +float FrozenRemaining
    +OnNetworkSpawn() void
    +OnNetworkDespawn() void
    +ServerFreezeClock() void
    +ServerResumeClock() void
  }
  class NetworkNameTag {
    <<MonoBehaviour>>
    -NetworkPlayer _player
    -NameTagView _view
    -NetworkRuntimeAnchor _anchor
    -NetworkSettings _settings
    -Color _fallbackColor
  }
  class NetworkPenLedger {
    +GetCount(PlayerId playerId, int animalTypeIndex) int
    +OnNetworkSpawn() void
    +OnNetworkDespawn() void
  }
  class NetworkPlayer {
    -CharacterRoster _characters
    -NetworkRuntimeAnchor _anchor
    -PlayerRig _rig
    -PlayerIdentity _identity
    -PlayerAppearance _appearance
    -PlayerMovement _movement
    -PlayerGrabber _grabber
    -PlayerStunReceiver _stunReceiver
    -NetworkPlayerMotion _motion
    -NetworkCarry _carry
    -PlayerInput _playerInput
    -Camera _camera
    -AudioListener _audioListener
    ...17 más
  }
  class NetworkPlayerInput {
    -NetworkSettings _settings
    -PlayerInputHandler _localInput
    -ThirdPersonCamera _camera
    -PlayerMovement _movement
    -PlayerGrabber _grabber
    -PlayerAim _aim
    +IPlayerCommandSource ServerCommands
    +bool IsRemotePaused
    +uint LastAppliedSeq
    +OnNetworkSpawn() void
    +OnNetworkDespawn() void
  }
  class NetworkPlayerMotion {
    -NetworkSettings _settings
    -PlayerMovement _movement
    -Rigidbody _rb
    -NetworkPlayerInput _input
    +int BufferedSnapshots
    +OnNetworkSpawn() void
    +MarkTeleport() void
  }
  class NetworkPlayerRoster {
    <<MonoBehaviour>>
    -NetworkRuntimeAnchor _anchor
    -CharacterRoster _characters
    -NetworkObject _playerPrefab
    -Transform[] _spawnPoints
    +event Action~PlayerId, bool~ PlayerSpawned
    +int JoinedCount
    +TryGetRig(PlayerId id, PlayerRig rig) bool
    +RespawnAll() void
    +SetControlsEnabled(bool enabled) void
  }
  class NetworkPoolLog {
    +Spawned(NetworkCarriable carriable, int use) void
    +Despawned(NetworkCarriable carriable) void
  }
  class NetworkPoolStats {
    <<struct>>
    +int Active
    +int Inactive
    +int Created
    +Equals(NetworkPoolStats other) bool
    +Equals(object obj) bool
    +GetHashCode() int
    +bool operator$
  }
  class NetworkRootLifetime {
    <<MonoBehaviour>>
    -NetworkRuntimeAnchor _anchor
    -NetworkSettings _settings
    -NetworkManager _manager
    -ConnectionApprovalHandler _approval
    -NetworkSceneLoader _sceneLoader
    -MonoBehaviour _connection
    -LeaveNoticeMessenger _leaveNotice
    -OnlineSessionService _sessionService
    +bool IsShutdownPending
    +event Action HostLeaveStarted
    +NetworkManager Manager
    +ConnectionApprovalHandler Approval
    +NetworkSceneLoader SceneLoader
    ...8 más
  }
  class NetworkRuntimeAnchor {
    <<ScriptableObject>>
    +NetworkRootLifetime Root
    +bool HasRoot
    +NetworkManager Manager
    +ConnectionApprovalHandler Approval
    +ServerSlotDirectory Slots
    +NetworkSceneLoader SceneLoader
    +IConnectionStrategy Connection
    +NetworkPlayer LocalPlayer
    +Camera ViewCamera
    +PlayerStatsLedger PlayerStats
    +bool IsServer
    +bool IsPureClient
    +Publish(NetworkRootLifetime root) void
    ...12 más
  }
  class NetworkSceneLoader {
    <<MonoBehaviour>>
    -NetworkManager _manager
    +bool IsLoading
    +event Action~string~ LoadCompleted
    +LoadGameplay() bool
  }
  class NetworkSeatPresence {
    +SeatPresenceBoard Board
    +ServerWrite(SeatPresenceBoard board, double serverNow) void
    +OnNetworkSpawn() void
    +OnNetworkDespawn() void
  }
  class NetworkSpawnRegistry {
    <<MonoBehaviour>>
    -NetworkRuntimeAnchor _anchor
    -AnimalSpawnSettings _animalSettings
    -BombData _bombData
    -AnimalSpawner _animalSpawner
    -BombSpawner _bombSpawner
    +NetworkPoolStats AnimalStats
    +NetworkPoolStats BombStats
  }
  class NetworkStatsFormatter {
    <<static>>
    +Write(char buffer, NetworkStatsSample sample)$ int
  }
  class NetworkStatsSample {
    <<struct>>
    +NetworkPoolStats Animals
    +NetworkPoolStats Bombs
    +bool HasTransport
    +int RxPacketsPerSecond
    +int RxQueueMeanTenths
    +int RxQueueMax
    +int QueueCapacity
    +int LongestFrameMs
    +Equals(NetworkStatsSample other) bool
    +Equals(object obj) bool
    +GetHashCode() int
    +bool operator$
  }
  class NgoSessionNetworkHandler {
    +string DisconnectReason
    +bool Started
    +bool Retired
    +StartAsync(NetworkConfiguration configuration) Task
    +StartAsync(NetworkConfiguration configuration, CancellationToken cancellationToken) Task
    +Retire() void
    +StopAsync() Task
    +Shutdown() bool
  }
  class ObstacleLayoutState {
    <<struct>>
    +int Count
    +FromPlacements(ObstaclePlacement placements, int count)$ ObstacleLayoutState
    +CopyTo(ObstaclePlacement destination) int
    +Pack(ObstaclePlacement placement)$ ushort
    +Unpack(ushort packed)$ ObstaclePlacement
    +NetworkSerialize(BufferSerializer~T~ serializer) void
    +Equals(ObstacleLayoutState other) bool
    +Equals(object obj) bool
    +GetHashCode() int
  }
  class OnlineMatchBootstrap {
    <<MonoBehaviour>>
    -NetworkRuntimeAnchor _anchor
    -MatchSession _session
    -MatchManager _match
    -MatchExit _exit
    -AnimalSpawner _animalSpawner
    -BombSpawner _bombSpawner
    -ObstacleField _obstacles
    -OnlineMatchStatsRecorder _stats
    +SimulationRole AppliedRole
  }
  class OnlineMatchExitHandler {
    +RequestRematch() void
    +ReturnToTitle() bool
  }
  class OnlineMatchStatsRecorder {
    <<MonoBehaviour>>
    -NetworkRuntimeAnchor _anchor
    -AnimalSpawnSettings _spawnSettings
    -NetworkSettings _settings
    +RecordLeaveLoss() void
  }
  class OnlineModeRules {
    <<static>>
    +ModeFor(OnlineSessionStatus status)$ GameMode
  }
  class OnlineSessionErrorMapper {
    <<static>>
    +FromSessionError(SessionError error)$ string
    +FromRequestErrorCode(int errorCode, string fallbackKey)$ string
    +FromRelayReason(RelayExceptionReason reason)$ string
    +FromJoinException(Exception exception)$ string
    +IsSessionFull(Exception exception)$ bool
    +FromDisconnectReason(string reason)$ string
    +FromException(Exception exception, string fallbackKey)$ string
  }
  class OnlineSessionService {
    <<MonoBehaviour>>
    -NetworkSettings _settings
    -OnlineSessionChannel _channel
    -MatchSession _session
    -string _displayName
    -bool _signInOnStart
    -NetworkRootLifetime _root
    -NetworkManager _manager
    -RelaySessionConnection _connection
    +UgsBootstrapper Bootstrapper
    +ISession CurrentSession
    +ClearRejoinOffer() void
    +TryRemovePlayerAsync(string ugsPlayerId) Task
  }
  class OwnershipLabelCache {
    +Get(CarriableOwner owner, bool isOwner, bool hasAuthority) string
  }
  class OwnershipLabelToggle {
    <<static>>
    +bool HasOverride$
    +IsVisible(bool defaultVisible)$ bool
    +SetVisible(bool visible)$ void
    +Toggle(bool defaultVisible)$ void
    +ClearOverride()$ void
  }
  class PenEntry {
    <<struct>>
    +int AnimalTypeIndex
    +int Count
    +NetworkSerialize(BufferSerializer~T~ serializer) void
    +Equals(PenEntry other) bool
    +Equals(object obj) bool
    +GetHashCode() int
  }
  class PendingSegmentJson {
    -string batch
    -int matches
    -int wins
    -int draws
    -int losses
    -int animals
    -int tackles
    +string Batch
    +int Matches
    +int Wins
    +int Draws
    +int Losses
    +int Animals
    +int Tackles
  }
  class PendingStats {
    <<struct>>
    +PlayerStatsSummary HeadDelta
    +string HeadBatchId
    +bool HeadSent
    +PlayerStatsSummary TailDelta
    +string TailBatchId
    +bool HasHead
    +bool HasTail
    +bool IsEmpty
    +PlayerStatsSummary Delta
    +string BatchId
    +Enqueue(PlayerStatsSummary delta, string deltaId) PendingStats
    +MarkHeadSent() PendingStats
    +DropHead() PendingStats
    ...4 más
  }
  class PendingStatsCodec {
    <<static>>
    +Write(PendingStats pending)$ string
    +Read(string json, PendingStats pending)$ StatsReadResult
  }
  class PendingStatsJson {
    -int v
    -string batch
    -int matches
    -int wins
    -int draws
    -int losses
    -int animals
    -int tackles
    -bool sent
    -PendingSegmentJson tail
    +int Version
    +string Batch
    +int Matches
    ...7 más
  }
  class Phase {
    <<enumeration>>
    Idle
    Announcing
    Disconnecting
    Stopping
  }
  class PlayerInputCommand {
    <<struct>>
    +uint Seq
    +sbyte MoveX
    +sbyte MoveY
    +ushort YawQuantized
    +short PitchCentidegrees
    +byte Buttons
    +Create(uint seq, Vector2 move, float yaw, float pitch, bool sprint, bool grab, bool aim, bool paused)$ PlayerInputCommand
    +Vector2 Move
    +float Yaw
    +float Pitch
    +bool IsSprintHeld
    +bool IsGrabHeld
    +bool IsAimHeld
    ...2 más
  }
  class PlayerPrefsPendingStatsStore {
    +KeyFor(string ownerId)$ string
    +Load(string ownerId) PendingStats
    +Save(string ownerId, PendingStats pending) void
    +Clear(string ownerId) void
  }
  class PlayerSlot {
    <<struct>>
    +PlayerId Id
    +ulong ClientId
    +FixedString32Bytes DisplayName
    +byte CharacterIndex
    +bool Connected
    +FixedString64Bytes UgsPlayerId
    +double DisconnectedAt
    +bool HasSavedPose
    +Vector3 SavedPosition
    +float SavedFacingYaw
    +bool IsOccupied
    +bool HasCharacter
    +bool IsAwaitingReconnect
  }
  class PlayerSnapshot {
    <<struct>>
    +uint Tick
    +double ServerTime
    +Vector3 Position
    +Vector3 Velocity
    +float FacingYaw
    +float StunRemaining
    +uint AckSeq
    +byte TeleportEpoch
    +PlayerSnapshotFlags Flags
    +NetworkSerialize(BufferSerializer~T~ serializer) void
  }
  class PlayerSnapshotFlags {
    <<enumeration>>
    None
    Sprinting
    Aiming
  }
  class PlayerStatsCodec {
    <<static>>
    +Write(PlayerStatsRecord record, IDictionary~string, object~ target)$ void
    +Read(IReadOnlyDictionary~string, object~ source, PlayerStatsRecord record)$ StatsReadResult
    +TryReadCount(object value, int count)$ bool
  }
  class PlayerStatsConflictOutcome {
    <<enumeration>>
    Ignored
    AlreadyApplied
    Rebased
    ReadOnly
  }
  class PlayerStatsLedger {
    +event Action Changed
    +string OwnerId
    +int Epoch
    +PlayerStatsLoadState LoadState
    +PlayerStatsSaveState SaveState
    +string LastErrorKey
    +int FailedAttempts
    +PlayerStatsSummary Confirmed
    +string WriteLock
    +PendingStats Pending
    +bool HasPending
    +bool IsSaving
    +PlayerStatsSummary Displayed
    ...14 más
  }
  class PlayerStatsLoadState {
    <<enumeration>>
    NotLoaded
    Loading
    Loaded
    Failed
    ReadOnly
  }
  class PlayerStatsRecord {
    <<struct>>
    +PlayerStatsRecord Empty$
    +int Version
    +PlayerStatsSummary Stats
    +string LastBatchId
    +Equals(PlayerStatsRecord other) bool
    +Equals(object obj) bool
    +GetHashCode() int
  }
  class PlayerStatsRules {
    <<static>>
    +ForMatch(PlayerId local, MatchResult result, int animals, int tackles)$ PlayerStatsSummary
    +Win(int animals, int tackles)$ PlayerStatsSummary
    +Draw(int animals, int tackles)$ PlayerStatsSummary
    +Loss(int animals, int tackles)$ PlayerStatsSummary
    +Add(PlayerStatsSummary a, PlayerStatsSummary b)$ PlayerStatsSummary
    +Subtract(PlayerStatsSummary a, PlayerStatsSummary b)$ PlayerStatsSummary
    +Sanitize(PlayerStatsSummary stats)$ PlayerStatsSummary
    +IsZero(PlayerStatsSummary stats)$ bool
  }
  class PlayerStatsSaveState {
    <<enumeration>>
    Idle
    Saving
    Saved
    Failed
  }
  class PlayerStatsSync {
    +bool IsRetryScheduled
    +int Attempts
    +Start() void
    +Dispose() void
  }
  class PoolGrowthTracker {
    +int Reference
    +Observe(int created) int
  }
  class PoolLifecycleRules {
    <<static>>
    +DescribeUse(int use)$ string
    +Growth(int previousCreated, int currentCreated)$ int
  }
  class PoolStatsToggle {
    <<static>>
    +bool HasOverride$
    +IsVisible(bool defaultVisible)$ bool
    +SetVisible(bool visible)$ void
    +Toggle(bool defaultVisible)$ void
    +ClearOverride()$ void
  }
  class PoolStatsView {
    <<MonoBehaviour>>
    -NetworkSettings _settings
    -NetworkRuntimeAnchor _anchor
    -float _refreshInterval
    -NetworkSpawnRegistry _registry
    -TMP_Text _label
    -GameObject _content
  }
  class PooledParenting {
    <<static>>
    +BeginRelease(NetworkObject networkObject, bool previousSync)$ void
    +EndRelease(NetworkObject networkObject, bool previousSync)$ void
  }
  class PresenceChange {
    <<struct>>
    +PlayerId Id
    +PlayerConnectionStatus Status
    +float GraceLeft
  }
  class ReconnectGraceCoordinator {
    <<MonoBehaviour>>
    -NetworkRuntimeAnchor _anchor
    -NetworkSettings _settings
    -MatchManager _match
    -NetworkMatchState _matchState
    -NetworkSeatPresence _presence
    -NetworkPlayerRoster _roster
  }
  class ReconnectGraceRules {
    <<static>>
    +GraceEndServerTime(double serverNow, double realtimeNow, double disconnectedAt, float grace)$ double
    +AnyAwaiting(ReadOnlySpan~PlayerSlot~ seats)$ bool
    +BuildSeat(PlayerSlot seat, PlayerConnectionStatus lastRelease, double serverNow, double realtimeNow, float grace)$ SeatPresence
    +KeepReservation(SeatPresence previous, SeatPresence rebuilt)$ SeatPresence
    +BuildBoard()$ SeatPresenceBoard
  }
  class RejoinOfferRules {
    <<static>>
    +ShouldOffer(bool isHost, string errorKey, string sessionId, string code)$ bool
    +ExpiresAt(double now, float graceSeconds)$ double
    +IsExpired(double expiresAt, double now)$ bool
    +KeepsOfferAfterFailure(string errorKey)$ bool
    +ShouldAutoRejoin(bool hasOffer, OnlineSessionStatus status, bool networkReachable, double now, double nextAttemptAt)$ bool
    +FallsBackToCodeJoin(string reconnectErrorKey)$ bool
  }
  class RelaySessionConnection {
    <<MonoBehaviour>>
    -NetworkSettings _settings
    -UnityTransport _transport
    -string _region
    -float _connectTimeoutSeconds
    -float _alreadyInSessionRetrySeconds
    -float _hostLeftWaitSeconds
    -float _reconnectConnectWaitSeconds
    +ISession Session
    +string LastErrorKey
    +bool HasSession
    +bool IsOnline
    +IsSessionMember(string ugsPlayerId) bool
    +RefreshSessionAsync() Task
    ...9 más
  }
  class RelaySessionStart {
    +ISession Session
    +NgoSessionNetworkHandler Handler
    +string ErrorKey
    +string LeftSessionId
    +bool IsSuccess
    +WithLeftSession(string sessionId) RelaySessionStart
    +Succeeded(ISession session, NgoSessionNetworkHandler handler)$ RelaySessionStart
    +Failed(string errorKey)$ RelaySessionStart
  }
  class RivalRules {
    <<static>>
    +TryFindRival(ReadOnlySpan~OnlineParticipant~ participants, string ugsPlayerId)$ bool
  }
  class ScoreBoard {
    <<struct>>
    +int Player1
    +int Player2
    +Get(PlayerId id) int
    +With(PlayerId id, int score) ScoreBoard
    +ToResult() MatchResult
    +NetworkSerialize(BufferSerializer~T~ serializer) void
    +Equals(ScoreBoard other) bool
    +Equals(object obj) bool
    +GetHashCode() int
  }
  class SeatDropOutcome {
    <<enumeration>>
    Released
    Left
    Reserved
  }
  class SeatGraceRules {
    <<static>>
    +OnDisconnect(bool noticedLeaving, bool graceEnabled)$ SeatDropOutcome
    +OnDisconnect(bool noticedLeaving, bool graceEnabled, bool serverStopping)$ SeatDropOutcome
    +ShouldRemoveRejectedMember(bool roomFull, bool serverStopping, bool hasUgsId, bool ugsIdInUse)$ bool
    +GraceLeft(PlayerSlot seat, double now, float graceSeconds)$ float
    +CollectExpired(ReadOnlySpan~PlayerSlot~ seats, double now, float graceSeconds, Span~PlayerId~ output)$ int
  }
  class SeatPresence {
    <<struct>>
    +SeatPresence Empty$
    +bool IsSeated
    +ulong ClientId
    +PlayerConnectionStatus Status
    +double GraceEndTime
    +Connected(ulong clientId)$ SeatPresence
    +Reconnecting(ulong clientId, double graceEndTime)$ SeatPresence
    +Released(PlayerConnectionStatus status)$ SeatPresence
    +NetworkSerialize(BufferSerializer~T~ serializer) void
    +Equals(SeatPresence other) bool
    +Equals(object obj) bool
    +GetHashCode() int
  }
  class SeatPresenceBoard {
    <<struct>>
    +SeatPresence Player1
    +SeatPresence Player2
    +Get(PlayerId id) SeatPresence
    +With(PlayerId id, SeatPresence presence) SeatPresenceBoard
    +NetworkSerialize(BufferSerializer~T~ serializer) void
    +Equals(SeatPresenceBoard other) bool
    +Equals(object obj) bool
    +GetHashCode() int
  }
  class SeatPresenceRules {
    <<static>>
    +Diff(SeatPresenceBoard previous, SeatPresenceBoard current, double now, Span~PresenceChange~ output)$ int
    +Current(SeatPresenceBoard board, double now, Span~PresenceChange~ output)$ int
    +GraceLeft(SeatPresence seat, double now)$ float
    +Publishes(SeatPresence previous, SeatPresence current)$ bool
  }
  class ServerSlotDirectory {
    +ReadOnlySpan~PlayerSlot~ Seats
    +int ConnectedCount
    +int CountAwaiting
    +TryApply(ApprovalDecision decision, ulong clientId, ConnectionPayload payload) bool
    +TryAssign(PlayerId id, ulong clientId, ConnectionPayload payload) bool
    +TryReconnect(PlayerId id, ulong clientId, ConnectionPayload payload) bool
    +TrySetCharacter(PlayerId id, byte characterIndex) bool
    +TrySavePose(PlayerId id, Vector3 position, float facingYaw) bool
    +ClearSavedPose(PlayerId id) bool
    +TryFindAwaiting(FixedString64Bytes ugsPlayerId, PlayerId id) bool
    +IsUgsIdInUse(FixedString64Bytes ugsPlayerId) bool
    +TryGetByClient(ulong clientId, PlayerSlot slot) bool
    +TryGet(PlayerId id, PlayerSlot slot) bool
    ...5 más
  }
  class ServerStopLatch {
    +bool IsRequested
    +Request(bool isServer) bool
    +OnServerStopped() void
    +Reset() void
    +ShouldRejectJoin(ulong clientId, bool serverStopping)$ bool
  }
  class SessionExit {
    <<enumeration>>
    Leave
    AwaitHostEnd
    Forget
  }
  class SessionMembership {
    <<enumeration>>
    NotRequired
    Member
    Unconfirmed
    Rejected
  }
  class SessionSdkWorkarounds {
    <<static>>
    +IsRegistered(ISession session)$ bool
    +GetRegistered(string sessionType)$ ISession
    +WaitUntilUnregisteredAsync(ISession session, float timeoutSeconds)$ Task~bool~
    +ReleaseOrphanAsync(ISession session, float timeoutSeconds)$ Task~bool~
  }
  class SimulatedFailureStatsStore {
    +LoadOwnAsync() Task~StatsStoreResult~
    +LoadPublicAsync(string playerId) Task~StatsStoreResult~
    +SaveOwnAsync(PlayerStatsRecord record, string writeLock) Task~StatsStoreResult~
  }
  class SnapshotAddResult {
    <<enumeration>>
    Rejected
    Added
    Reset
  }
  class SnapshotBuffer {
    +int Count
    +int Capacity
    +TryGetNewest(PlayerSnapshot snapshot) bool
    +Add(PlayerSnapshot snapshot) SnapshotAddResult
    +Clear() void
  }
  class SnapshotInterpolator {
    <<static>>
    +TrySample(SnapshotBuffer buffer, double renderTime, InterpolatedPose pose)$ bool
  }
  class StatsReadResult {
    <<enumeration>>
    Ok
    Missing
    NewerSchema
    Corrupt
  }
  class StatsStoreOutcome {
    <<enumeration>>
    Ok
    Conflict
    RetryableError
    Error
  }
  class StatsStoreResult {
    <<struct>>
    +StatsStoreOutcome Outcome
    +StatsReadResult Read
    +PlayerStatsRecord Record
    +string WriteLock
    +string ErrorKey
    +float RetryAfter
    +bool IsOk
    +Loaded(StatsReadResult read, PlayerStatsRecord record, string writeLock)$ StatsStoreResult
    +Saved(string writeLock)$ StatsStoreResult
    +Conflict()$ StatsStoreResult
    +Retryable(string errorKey, float retryAfter)$ StatsStoreResult
    +Failed(string errorKey)$ StatsStoreResult
  }
  class Status {
    <<enumeration>>
    NoRoot
    Offline
    Starting
    Hosting
    Connecting
    Connected
  }
  class ThrowViewRules {
    <<static>>
    +IsWithinTolerance(float yaw, float pitch, float knownYaw, float knownPitch, float toleranceDegrees)$ bool
  }
  class TransportStatsTracker {
    +bool IsRunning
    +float LongestFrame
    +int RxPerSecond
    +Tick(bool running, float frameSeconds) bool
    +NotifyPaused() void
    +RequestRebase() void
    +SampleRx(ulong totalRxPackets, float now) int
  }
  class UgsBootstrapper {
    +string Profile
    +string LastErrorKey
    +bool IsSignedIn
    +IAuthenticationService Authentication
    +string PlayerId
    +string PlayerName
    +ResolveProcessProfile()$ string
    +EnsureSignedInAsync() Task~bool~
  }
  class UgsProfileResolver {
    <<static>>
    +Resolve(IReadOnlyList~string~ commandLineArgs, bool isMainEditor)$ string
    +Sanitize(string raw)$ string
    +FindArgumentValue(IReadOnlyList~string~ args, string argument)$ string
  }
  class Animal:::external
  <<abstract>> Animal
  class AnimalData:::external
  <<ScriptableObject>> AnimalData
  class AnimalSpawnSettings:::external
  <<ScriptableObject>> AnimalSpawnSettings
  class AnimalSpawner:::external
  <<MonoBehaviour>> AnimalSpawner
  class Bomb:::external
  <<MonoBehaviour>> Bomb
  class BombData:::external
  <<ScriptableObject>> BombData
  class BombSpawner:::external
  <<MonoBehaviour>> BombSpawner
  class CharacterDefinition:::external
  <<ScriptableObject>> CharacterDefinition
  class CharacterRoster:::external
  <<ScriptableObject>> CharacterRoster
  class IAnimalNetworkHook:::external
  <<interface>> IAnimalNetworkHook
  class IBombNetworkHook:::external
  <<interface>> IBombNetworkHook
  class IGrabbable:::external
  <<interface>> IGrabbable
  class IMatchExitHandler:::external
  <<interface>> IMatchExitHandler
  class IPlayerCommandSource:::external
  <<interface>> IPlayerCommandSource
  class IPlayerRigLookup:::external
  <<interface>> IPlayerRigLookup
  class IPlayerRoster:::external
  <<interface>> IPlayerRoster
  class MatchExit:::external
  <<MonoBehaviour>> MatchExit
  class MatchManager:::external
  <<MonoBehaviour>> MatchManager
  class MatchSession:::external
  <<ScriptableObject>> MatchSession
  class MatchSettings:::external
  <<ScriptableObject>> MatchSettings
  class NameTagView:::external
  <<MonoBehaviour>> NameTagView
  class NetworkSettings:::external
  <<ScriptableObject>> NetworkSettings
  class ObstacleField:::external
  <<MonoBehaviour>> ObstacleField
  class ObstaclePlacement:::external
  <<struct>> ObstaclePlacement
  class OnlineLobbyChannel:::external
  <<ScriptableObject>> OnlineLobbyChannel
  class OnlineLobbySeat:::external
  <<struct>> OnlineLobbySeat
  class OnlineParticipant:::external
  <<struct>> OnlineParticipant
  class OnlineSessionChannel:::external
  <<ScriptableObject>> OnlineSessionChannel
  class PlayerAim:::external
  <<MonoBehaviour>> PlayerAim
  class PlayerAppearance:::external
  <<MonoBehaviour>> PlayerAppearance
  class PlayerGrabber:::external
  <<MonoBehaviour>> PlayerGrabber
  class PlayerIdentity:::external
  <<MonoBehaviour>> PlayerIdentity
  class PlayerInputHandler:::external
  <<MonoBehaviour>> PlayerInputHandler
  class PlayerMovement:::external
  <<MonoBehaviour>> PlayerMovement
  class PlayerProgressChannel:::external
  <<ScriptableObject>> PlayerProgressChannel
  class PlayerRig:::external
  <<MonoBehaviour>> PlayerRig
  class PlayerStatsSummary:::external
  <<struct>> PlayerStatsSummary
  class PlayerStunReceiver:::external
  <<MonoBehaviour>> PlayerStunReceiver
  class ThirdPersonCamera:::external
  <<MonoBehaviour>> ThirdPersonCamera
  CarriableOwnerTint --> NetworkCarriable
  CarriableOwnershipLabel --> NetworkCarriable
  CarriableOwnershipLabel --> OwnershipLabelCache
  CloudSavePlayerStatsStore ..|> IPlayerStatsStore
  CloudSaveStatsService --> NetworkRuntimeAnchor
  CloudSaveStatsService --> NetworkSettings
  CloudSaveStatsService --> OnlineSessionChannel
  CloudSaveStatsService --> PlayerProgressChannel
  CloudSaveStatsService --> PlayerStatsSync
  ConnectionApprovalHandler --> NetworkSettings
  ConnectionApprovalHandler --> RelaySessionConnection
  ConnectionApprovalHandler --> ServerSlotDirectory
  ConnectionApprovalHandler --> ServerStopLatch
  DevConnectionPanel --> MatchSession
  DevConnectionPanel --> NetworkRuntimeAnchor
  DevConnectionPanel --> NetworkSettings
  DevConnectionPanel --> OnlineLobbyChannel
  DevConnectionPanel --> OnlineSessionChannel
  DirectConnection ..|> IConnectionStrategy
  IPendingStatsStore --> PendingStats
  IPlayerStatsStore --> StatsStoreResult
  InputCommandBatch --> PlayerInputCommand
  InputCommandBuffer --> PlayerInputCommand
  LeaveNoticeMessenger --> ConnectionApprovalHandler
  LobbyStateSpawner --> NetworkRuntimeAnchor
  LobbyStateSpawner --> OnlineLobbyChannel
  LobbyStateSpawner --> OnlineSessionChannel
  LocalPlayerRigLookup --> NetworkRuntimeAnchor
  LocalPlayerRigLookup ..|> IPlayerRigLookup
  NetworkAnimal --> Animal
  NetworkAnimal --> AnimalNetState
  NetworkAnimal --> IGrabbable
  NetworkAnimal --|> NetworkCarriable
  NetworkAnimalPoolHandler --> AnimalData
  NetworkAnimalPoolHandler --> AnimalSpawner
  NetworkAnimalPoolHandler --> NetworkPoolLog
  NetworkAnimalPoolHandler --> NetworkRuntimeAnchor
  NetworkBomb --> Bomb
  NetworkBomb --> BombNetState
  NetworkBomb --> IGrabbable
  NetworkBomb --|> NetworkCarriable
  NetworkBombPoolHandler --> BombSpawner
  NetworkBombPoolHandler --> NetworkPoolLog
  NetworkBombPoolHandler --> NetworkRuntimeAnchor
  NetworkCarriable --> IGrabbable
  NetworkCarriable --> NetworkRuntimeAnchor
  NetworkCarriable --> NetworkSettings
  NetworkCarry --> NetworkPlayerInput
  NetworkCarry --> NetworkSettings
  NetworkCarry --> PlayerAim
  NetworkCarry --> PlayerGrabber
  NetworkCarry --> PlayerInputHandler
  NetworkCarry --> ThirdPersonCamera
  NetworkCommandSource ..|> IPlayerCommandSource
  NetworkLobbyState --> CharacterRoster
  NetworkLobbyState --> LobbySlotEntry
  NetworkLobbyState --> NetworkRuntimeAnchor
  NetworkLobbyState --> NetworkSettings
  NetworkLobbyState --> OnlineLobbyChannel
  NetworkLobbyState --> OnlineLobbySeat
  NetworkLobbyState --> OnlineSessionChannel
  NetworkLobbyState --> PlayerSlot
  NetworkMatchExit --> MatchManager
  NetworkMatchExit --> NetworkRuntimeAnchor
  NetworkMatchState --> MatchManager
  NetworkMatchState --> MatchSettings
  NetworkMatchState --> ObstacleField
  NetworkMatchState --> ObstaclePlacement
  NetworkMatchState --> ScoreBoard
  NetworkNameTag --> NameTagView
  NetworkNameTag --> NetworkPlayer
  NetworkNameTag --> NetworkRuntimeAnchor
  NetworkNameTag --> NetworkSettings
  NetworkPenLedger --> PenEntry
  NetworkPlayer --> CharacterDefinition
  NetworkPlayer --> CharacterRoster
  NetworkPlayer --> NetworkCarry
  NetworkPlayer --> NetworkPlayerMotion
  NetworkPlayer --> NetworkRuntimeAnchor
  NetworkPlayer --> PlayerAppearance
  NetworkPlayer --> PlayerGrabber
  NetworkPlayer --> PlayerIdentity
  NetworkPlayer --> PlayerMovement
  NetworkPlayer --> PlayerRig
  NetworkPlayer --> PlayerStunReceiver
  NetworkPlayerInput --> IPlayerCommandSource
  NetworkPlayerInput --> InputCommandBatch
  NetworkPlayerInput --> InputCommandBuffer
  NetworkPlayerInput --> NetworkCommandSource
  NetworkPlayerInput --> NetworkSettings
  NetworkPlayerInput --> PlayerAim
  NetworkPlayerInput --> PlayerGrabber
  NetworkPlayerInput --> PlayerInputHandler
  NetworkPlayerInput --> PlayerMovement
  NetworkPlayerInput --> ThirdPersonCamera
  NetworkPlayerMotion --> NetworkPlayerInput
  NetworkPlayerMotion --> NetworkSettings
  NetworkPlayerMotion --> PlayerMovement
  NetworkPlayerMotion --> SnapshotBuffer
  NetworkPlayerRoster --> CharacterRoster
  NetworkPlayerRoster --> NetworkPlayer
  NetworkPlayerRoster --> NetworkRuntimeAnchor
  NetworkPlayerRoster --> NetworkSceneLoader
  NetworkPlayerRoster --> PlayerRig
  NetworkPlayerRoster ..|> IPlayerRoster
  NetworkPoolLog --> NetworkSpawnRegistry
  NetworkPoolLog --> PoolGrowthTracker
  NetworkRootLifetime --> ConnectionApprovalHandler
  NetworkRootLifetime --> HostShutdownSequence
  NetworkRootLifetime --> IConnectionStrategy
  NetworkRootLifetime --> LeaveNoticeMessenger
  NetworkRootLifetime --> NetworkRuntimeAnchor
  NetworkRootLifetime --> NetworkSceneLoader
  NetworkRootLifetime --> NetworkSettings
  NetworkRootLifetime --> OnlineSessionService
  NetworkRuntimeAnchor --> ConnectionApprovalHandler
  NetworkRuntimeAnchor --> IConnectionStrategy
  NetworkRuntimeAnchor --> NetworkPlayer
  NetworkRuntimeAnchor --> NetworkRootLifetime
  NetworkRuntimeAnchor --> NetworkSceneLoader
  NetworkRuntimeAnchor --> PlayerStatsLedger
  NetworkRuntimeAnchor --> ServerSlotDirectory
  NetworkSeatPresence --> PresenceChange
  NetworkSeatPresence --> SeatPresenceBoard
  NetworkSpawnRegistry --> AnimalSpawnSettings
  NetworkSpawnRegistry --> AnimalSpawner
  NetworkSpawnRegistry --> BombData
  NetworkSpawnRegistry --> BombSpawner
  NetworkSpawnRegistry --> NetworkPoolLog
  NetworkSpawnRegistry --> NetworkPoolStats
  NetworkSpawnRegistry --> NetworkRuntimeAnchor
  NetworkSpawnRegistry ..|> IAnimalNetworkHook
  NetworkSpawnRegistry ..|> IBombNetworkHook
  NetworkStatsSample --> NetworkPoolStats
  OnlineMatchBootstrap --> AnimalSpawner
  OnlineMatchBootstrap --> BombSpawner
  OnlineMatchBootstrap --> MatchExit
  OnlineMatchBootstrap --> MatchManager
  OnlineMatchBootstrap --> MatchSession
  OnlineMatchBootstrap --> NetworkRuntimeAnchor
  OnlineMatchBootstrap --> ObstacleField
  OnlineMatchBootstrap --> OnlineMatchStatsRecorder
  OnlineMatchExitHandler --> MatchSession
  OnlineMatchExitHandler --> NetworkRuntimeAnchor
  OnlineMatchExitHandler --> OnlineMatchStatsRecorder
  OnlineMatchExitHandler ..|> IMatchExitHandler
  OnlineMatchStatsRecorder --> AnimalSpawnSettings
  OnlineMatchStatsRecorder --> LeaveNoticeMessenger
  OnlineMatchStatsRecorder --> MatchStatsTally
  OnlineMatchStatsRecorder --> NetworkRuntimeAnchor
  OnlineMatchStatsRecorder --> NetworkSettings
  OnlineSessionService --> MatchSession
  OnlineSessionService --> NetworkRootLifetime
  OnlineSessionService --> NetworkSettings
  OnlineSessionService --> OnlineParticipant
  OnlineSessionService --> OnlineSessionChannel
  OnlineSessionService --> RelaySessionConnection
  OnlineSessionService --> UgsBootstrapper
  OwnershipLabelToggle --> DebugViewSwitch
  PendingStats --> PlayerStatsSummary
  PendingStatsJson --> PendingSegmentJson
  PlayerPrefsPendingStatsStore ..|> IPendingStatsStore
  PlayerStatsLedger --> IPendingStatsStore
  PlayerStatsLedger --> PendingStats
  PlayerStatsLedger --> PlayerStatsSummary
  PlayerStatsRecord --> PlayerStatsSummary
  PlayerStatsSync --> IPlayerStatsStore
  PlayerStatsSync --> NetworkSettings
  PlayerStatsSync --> OnlineParticipant
  PlayerStatsSync --> OnlineSessionChannel
  PlayerStatsSync --> PlayerProgressChannel
  PlayerStatsSync --> PlayerStatsLedger
  PlayerStatsSync --> PlayerStatsSummary
  PoolStatsToggle --> DebugViewSwitch
  PoolStatsView --> NetworkRuntimeAnchor
  PoolStatsView --> NetworkSettings
  PoolStatsView --> NetworkSpawnRegistry
  PoolStatsView --> NetworkStatsSample
  PoolStatsView --> TransportStatsTracker
  ReconnectGraceCoordinator --> ConnectionApprovalHandler
  ReconnectGraceCoordinator --> MatchManager
  ReconnectGraceCoordinator --> NetworkMatchState
  ReconnectGraceCoordinator --> NetworkPlayerRoster
  ReconnectGraceCoordinator --> NetworkRootLifetime
  ReconnectGraceCoordinator --> NetworkRuntimeAnchor
  ReconnectGraceCoordinator --> NetworkSeatPresence
  ReconnectGraceCoordinator --> NetworkSettings
  ReconnectGraceCoordinator --> SeatPresenceBoard
  RelaySessionConnection --> NetworkSettings
  RelaySessionConnection --> RelaySessionStart
  RelaySessionConnection ..|> IConnectionStrategy
  RelaySessionStart --> NgoSessionNetworkHandler
  SeatPresenceBoard --> SeatPresence
  ServerSlotDirectory --> PlayerSlot
  SimulatedFailureStatsStore ..|> IPlayerStatsStore
  SnapshotBuffer --> PlayerSnapshot
  StatsStoreResult --> PlayerStatsRecord
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Tipos

| Tipo | Clase | Descripción |
|---|---|---|
| [[AnimalNetState]] | struct | Replicated gameplay state of an animal: its `AnimalState`, who carries it, whose pen holds it and why it last left a player's hands (clients… |
| [[ApprovalDecision]] | struct | Outcome of `ApprovalRules.Evaluate`: how the client is admitted (and to which seat), or rejected with a reason key sent to the client. |
| [[ApprovalOutcome]] | enumeration | What an `ApprovalDecision` lets the connecting client do. |
| [[ApprovalPolicy]] | struct | Session limits the `ApprovalRules` apply. Immutable; built from `NetworkSettings`. |
| [[ApprovalRules]] | static | Pure connection approval rules, in order: same protocol, valid payload, one connection per UGS player (a dropped player gets its own seat ba… |
| [[BombNetState]] | struct | Replicated gameplay state of a bomb: its `BombState`, who carries it and who threw it. 3 bytes. |
| [[CarriableOwner]] | struct | Resolved owner of a carriable object: the server, a seat, or a client not resolved yet. |
| [[CarriableOwnerKind]] | enumeration | Who a carriable object belongs to, as shown by its ownership tint and debug label. |
| [[CarriableOwnerRules]] | static | Pure mapping from NGO ownership to what the tint / debug label / log show. On a host the server's client id is also P1's: what the host carr… |
| [[CarriableOwnerTint]] | MonoBehaviour | Course topic 2: the object takes the color of whoever owns it. Colors indicator renderers (a ring / halo child, never the object's own art) … |
| [[CarriableOwnershipLabel]] | MonoBehaviour | Course topic 2: world-space debug label over a carriable object, "Owner: P2 · IsOwner ✓ · HasAuthority ✗" (owner as resolved by `NetworkCarr… |
| [[CharacterPickRules]] | static | Pure rule of the default character of a seat that reached the gameplay scene without a pick (e.g. a new client taking a seat freed mid-match… |
| [[CloudSaveErrorRules]] | static | Cloud Save failure reason → localization key (`PlayerProgressErrorKeys`) and whether to retry. |
| [[CloudSavePlayerStatsStore]] | class | The only class that talks to Cloud Save (3.4.x): the stats record lives in Player Data under `PlayerStatsCodec.Key` with the Public access c… |
| [[CloudSaveStatsService]] | MonoBehaviour | Cloud stats service on the persistent NetworkRoot (task 2.8). Composition root of `PlayerStatsSync`: the anchor's `NetworkRuntimeAnchor.Play… |
| [[ConnectionApprovalHandler]] | MonoBehaviour | Server connection approval: parses the `ConnectionPayload` (name sanitized), verifies in an online session that the declared UGS player id i… |
| [[ConnectionPayload]] | struct | What a client sends in `NetworkConfig.ConnectionData` when it connects. Written by the client, read by the server's approval (`ApprovalRules… |
| [[DebugViewSwitch]] | class | Machine-local visibility switch of one debug view: without an override the view follows its default (a NetworkSettings flag); the debug over… |
| [[DevConnectionPanel]] | MonoBehaviour | Development connection panel (logic only; the Canvas lives in the scene). Host / Client / Shutdown are wired to buttons' OnClick. Host and C… |
| [[DirectConnection]] | MonoBehaviour | Direct IP connection (Multiplayer Play Mode, LAN or the relay fallback): sets the UnityTransport address and port, then starts NGO synchrono… |
| [[DisconnectReasons]] | static | The only disconnect / rejection reasons the game sends, as stable keys the UI maps to text. NGO fills DisconnectReason with raw transport te… |
| [[DisplayNameResolver]] | static | Name sent in the connection payload and the session: the one typed on this device, else the generated UGS name, else the serialized fallback… |
| [[HostShutdownSequence]] | class | Graceful shutdown of a host that still has remote clients (pure logic, driven every frame by `NetworkRootLifetime`). The host already sent t… |
| [[HostShutdownStep]] | enumeration | What `HostShutdownSequence.Step` asks the root to do on this frame. |
| [[IConnectionStrategy]] | interface | How a session is started: direct IP (`DirectConnection`) or, later, Sessions + Relay. Asynchronous so a relay allocation fits the same contr… |
| [[IPendingStatsStore]] | interface | Local storage of the unconfirmed stats, one entry per account (survives closing the game). Implementations ignore (and delete) invalid or ne… |
| [[IPlayerStatsStore]] | interface | Strategy for the persisted stats of the signed-in player (key `PlayerStatsCodec.Key`). Implementations never throw: every failure comes back… |
| [[InputCommandBatch]] | struct | The last `MaxCommands` commands of a player, newest first, sent in every unreliable input packet so a lost packet is recovered by the next o… |
| [[InputCommandBuffer]] | class | Server-side jitter buffer of one player's commands, consumed one per physics step. Waits until `TargetDepth` commands are queued before play… |
| [[InterpolatedPose]] | struct | Pose sampled by `SnapshotInterpolator` at a render time. |
| [[LeaveNoticeMessenger]] | MonoBehaviour | Voluntary-leave notice over an NGO named message: a pure client calls `SendLeavingNotice` right before it shuts down, so the server frees it… |
| [[LobbyRequestResult]] | enumeration | Outcome of a character request in the online room (see `LobbyRules.EvaluateRequest`). |
| [[LobbyRules]] | static | Pure rules of the online room, applied by the server on a small mirror of the replicated seats (never on the NetworkList itself). Requests a… |
| [[LobbySlotEntry]] | struct | One seat of the online room as replicated by `NetworkLobbyState`. Element of a NetworkList, so it is unmanaged and equatable. `CharacterInde… |
| [[LobbyStateSpawner]] | MonoBehaviour | MainMenu-only: spawns the networked room (`NetworkLobbyState`) once the host is in a Sessions + Relay session. Lives in the scene and not on… |
| [[LocalPlayerRigLookup]] | MonoBehaviour | Online `IPlayerRigLookup` for the shared menus (pause, results): only the avatar this machine owns is found, so a menu is routed to the loca… |
| [[MatchClock]] | static | What a client shows from the replicated server times (0 messages per second for the timer): the whole seconds left of the countdown and of t… |
| [[MatchStatsTally]] | class | Decides which round this instance records (plain C#): only one whose Playing phase it saw live, once. A reconnection that enters straight in… |
| [[MembershipMatch]] | enumeration | How a joined session (read through a probe) relates to the join code asked for (`MembershipRules.Classify`). |
| [[MembershipRules]] | static | Pure rules of reclaiming a lobby membership this player still holds (a join by code refused as 'already in a session'): only the session who… |
| [[Mode]] | enumeration |  |
| [[Mode]] | enumeration |  |
| [[NameTagRules]] | static | Pure rules of `NetworkNameTag`: when an avatar's tag shows, which name and color it uses and the debug "client N" line (course topic 9: the … |
| [[NetworkAnimal]] | class | Network adapter of a pooled `Animal`. Pose: NetworkTransform + NetworkRigidbody (server authority). Server: mirrors `Animal.StateChanged` (a… |
| [[NetworkAnimalPoolHandler]] | class | NGO instance handler for one animal type, on top of the spawner's existing pool. Clients: a spawn takes a pooled Replica (`AnimalTypePool.Ge… |
| [[NetworkBomb]] | class | Network adapter of a pooled `Bomb`. Pose: NetworkTransform + NetworkRigidbody (server authority). Server: mirrors `Bomb.StateChanged` into `… |
| [[NetworkBombPoolHandler]] | class | NGO instance handler for the bomb prefab, on top of the `BombSpawner` pool. Clients: a spawn takes a pooled Replica (`BombSpawner.GetForRepl… |
| [[NetworkCarriable]] | abstract | Shared network side of something a player carries (animals, bombs). Server: while carried the object is owned by the holder's client and par… |
| [[NetworkCarry]] | class | Release and throw of what a remote player carries. Server, remote avatar: the grabber runs with `ReleaseMode.External`. Grab released withou… |
| [[NetworkCommandSource]] | class | `IPlayerCommandSource` fed with network commands (server side of a remote player). Holds the last applied command and raises `GrabStarted` /… |
| [[NetworkLobbyState]] | class | Networked online room (prefab LobbyState, spawned in MainMenu by `LobbyStateSpawner` on the host). The server owns the seats and picks (`Lob… |
| [[NetworkMatchExit]] | MonoBehaviour | Online rematch (server side). Each player's "Rematch" button sets its own ReadyForRematch (an owner-written NetworkVariable on its NetworkPl… |
| [[NetworkMatchState]] | class | Replicated match state, on the in-scene MatchNetwork object (inert in local mode: never spawned). Server: follows the MatchManager through i… |
| [[NetworkNameTag]] | MonoBehaviour | Drives the `NameTagView` of a Player_Net avatar (online only; Player.prefab has no tag): Name: the replicated display name, else the seat ta… |
| [[NetworkPenLedger]] | class | Replicated "inventory" of each pen: one `NetworkList{T}` of `PenEntry` per player, on the in-scene MatchNetwork object (inert in local mode)… |
| [[NetworkPlayer]] | class | Network identity of a Player_Net avatar. The roster hands PlayerId, character and name over before spawning and the server writes them in On… |
| [[NetworkPlayerInput]] | class | Input over the network, one command per physics step. Owner (pure client): samples its `PlayerInputHandler` and camera view and sends the la… |
| [[NetworkPlayerMotion]] | class | Player position sync with custom snapshots (no NetworkTransform: later prediction and lag compensation need to know which server time is dra… |
| [[NetworkPlayerRoster]] | MonoBehaviour | Online `IPlayerRoster` (server only): spawns a Player_Net as the player object of every seated client at its seat's spawn point, keeps the r… |
| [[NetworkPoolLog]] | class | Optional lifecycle log of the network pools (course topics 6 and 7), on server and clients: every network spawn / despawn of a pooled animal… |
| [[NetworkPoolStats]] | struct | Pool counters for the debug overlay: instances in play, waiting in the pool, and ever created. |
| [[NetworkRootLifetime]] | MonoBehaviour | Lifetime of the persistent NetworkRoot: the first instance survives scene loads and publishes its services on the `NetworkRuntimeAnchor`; an… |
| [[NetworkRuntimeAnchor]] | ScriptableObject | Runtime anchor: the persistent NetworkRoot publishes its services here so scene objects (roster, bootstrap, dev panel) reach them through a … |
| [[NetworkSceneLoader]] | MonoBehaviour | Server-side scene loading through NGO scene management: the host loads the gameplay scene (Single) and every client, including late joiners,… |
| [[NetworkSeatPresence]] | class | Replicated `SeatPresenceBoard` on the in-scene MatchNetwork object, written by the server (the `ReconnectGraceCoordinator`). Single emitter … |
| [[NetworkSpawnRegistry]] | MonoBehaviour | Network pooling of animals and bombs (online gameplay scene, under OnlineModeRoot). Every instance: registers an `INetworkPrefabInstanceHand… |
| [[NetworkStatsFormatter]] | static | Writes a `NetworkStatsSample` into a reusable char buffer (no allocation; shown with TMP_Text.SetCharArray). Pure, so the layout is unit tes… |
| [[NetworkStatsSample]] | struct | One reading of the online debug stats view: pool counters of this machine and, when a transport is running, its receive side (packets per se… |
| [[NgoSessionNetworkHandler]] | class | Sessions network handler for this project's NetworkManager: replaces the SDK's default NGO integration (which uses NetworkManager.Singleton … |
| [[ObstacleLayoutState]] | struct | The obstacle layout of the Animal Zone, one packed slot per `ObstaclePlacement` (slot order): bits 10-15 = variant + 1 (0 = empty slot), bit… |
| [[OnlineMatchBootstrap]] | MonoBehaviour | Online setup of the shared match objects. Pure client: the match is the server's, so MatchManager is disabled and the spawners and obstacle … |
| [[OnlineMatchExitHandler]] | class | Online match exit. Back to title: records a loss first when leaving during live Playing (persisted before the shutdown, see `OnlineMatchStat… |
| [[OnlineMatchStatsRecorder]] | MonoBehaviour | Records this machine's online rounds into the stats ledger (task 2.8). Lives under OnlineModeRoot (online only; local matches never count). … |
| [[OnlineModeRules]] | static | Which `GameMode` the `AlienFarms.Data.MatchSession` must hold for an online session status. Online from the moment a create / join / rejoin … |
| [[OnlineSessionErrorMapper]] | static | Maps Unity Gaming Services failures and NGO disconnect reasons to `OnlineSessionErrorKeys`. Pure. Note: the Sessions SDK reports some lobby … |
| [[OnlineSessionService]] | MonoBehaviour | Online session service on the persistent NetworkRoot: answers the `OnlineSessionChannel` requests (sign in, create, join by code, leave) thr… |
| [[OwnershipLabelCache]] | class | Every text the ownership debug label can show ("Owner: P2 · IsOwner ✓ · HasAuthority ✗"), built once on first use and then returned by refer… |
| [[OwnershipLabelToggle]] | static | Machine-local switch of the ownership debug labels (not replicated, no PlayerId: it is a debug view of this screen). Without an override the… |
| [[PenEntry]] | struct | One line of a player's pen ledger: how many animals of a type (index in the spawn settings' entries) the pen holds. Element of a NetworkList… |
| [[PendingSegmentJson]] | class | JsonUtility shape of the tail segment of the local pending stats (`PendingStatsCodec`, v2). Field names are the JSON names, so they delibera… |
| [[PendingStats]] | struct | Stats recorded on this machine and not yet confirmed by the cloud (persisted locally through `IPendingStatsStore`), in two segments so a del… |
| [[PendingStatsCodec]] | static | JSON of the local pending stats (v2): { "v":2, "batch":"…", "matches", "wins", "draws", "losses", "animals", "tackles", "sent":bool, "tail":… |
| [[PendingStatsJson]] | class | JsonUtility shape of the local pending stats (`PendingStatsCodec`): the head at the top level (what a v1 entry holds), plus sent and the tai… |
| [[Phase]] | enumeration |  |
| [[PlayerInputCommand]] | struct | One physics step of a player's input, quantized for the wire (11 bytes): sequence, move (sbyte per axis), camera view (yaw as ushort over 36… |
| [[PlayerPrefsPendingStatsStore]] | class | `IPendingStatsStore` on PlayerPrefs, key stats.pending.{ugsId} (one per account, so MPPM clones sharing PlayerPrefs never collide). Every wr… |
| [[PlayerSlot]] | struct | Server-side record of one player seat (plan: PlayerId, UgsPlayerId, ClientId, Name, Character, Connected, DisconnectedAt, SavedPose). A seat… |
| [[PlayerSnapshot]] | struct | Authoritative state of one player after a server physics step, sent unreliably every step. `ServerTime` is on the NGO server clock, so clien… |
| [[PlayerSnapshotFlags]] | enumeration | Bit flags of a `PlayerSnapshot` (teleports travel as `PlayerSnapshot.TeleportEpoch`). |
| [[PlayerStatsCodec]] | static | Schema of the cloud stats record (key `Key`, a JSON object the Cloud Save SDK serializes): { "v": 1, "matches", "wins", "draws", "losses", "… |
| [[PlayerStatsConflictOutcome]] | enumeration | What `PlayerStatsLedger.ResolveConflict` concluded from the reloaded record. |
| [[PlayerStatsLedger]] | class | Synchronization model of the local player's cloud stats (plain C#, no I/O, no collections). Holds the owner, the confirmed record and its wr… |
| [[PlayerStatsLoadState]] | enumeration | Load state of the `PlayerStatsLedger`. |
| [[PlayerStatsRecord]] | struct | The persisted stats of one player (cloud key `PlayerStatsCodec.Key`): schema version, absolute totals and the id of the last batch applied (… |
| [[PlayerStatsRules]] | static | Pure accumulation rules of the lifetime stats. Saturating, never negative, no allocation. |
| [[PlayerStatsSaveState]] | enumeration | Save state of the `PlayerStatsLedger` (mirrors PlayerProgressSaveStatus). |
| [[PlayerStatsSync]] | class | Orchestration behind `CloudSaveStatsService` (plain C#, so it is tested in EditMode with a fake store): Follows the signed-in UGS id of `Onl… |
| [[PoolGrowthTracker]] | class | Pure: how many instances a pool created beyond everything seen so far. Starts at the prewarm size, so instances created at load time (even w… |
| [[PoolLifecycleRules]] | static | Pure rules of the pool lifecycle log (unit tested). |
| [[PoolStatsToggle]] | static | Machine-local switch of the pool / transport stats view (`PoolStatsView`). Without an override it follows NetworkSettings.ShowPoolStats; the… |
| [[PoolStatsView]] | MonoBehaviour | Online debug view (logic only; the label lives in the scene): this machine's pool counters for animals and bombs (active / in pool / created… |
| [[PooledParenting]] | static | Lets a pool put a despawned `NetworkObject` back under its (non-network) container. NGO reverts and logs an error for any parent change of a… |
| [[PresenceChange]] | struct | One `GameEvents.OnPlayerConnectionChanged` to raise, as found by `SeatPresenceRules`. |
| [[ReconnectGraceCoordinator]] | MonoBehaviour | Server-only composition of the reconnection grace in the gameplay scene (under OnlineModeRoot). While it lives the approval handler keeps a … |
| [[ReconnectGraceRules]] | static | Pure rules of the `ReconnectGraceCoordinator`: the seat table (realtime clock) turned into the replicated `SeatPresenceBoard` (server clock)… |
| [[RejoinOfferRules]] | static | Pure rules of the client's reconnection offer: when a drop arms it, until when it lasts and whether a failed rejoin keeps it for another try… |
| [[RelaySessionConnection]] | MonoBehaviour | Connection through Unity Sessions + Relay: the host creates a private session with a Relay network (join code shown to the other player) and… |
| [[RelaySessionStart]] | class | Outcome of one create / join through `RelaySessionConnection`: the session and the network handler of that start only. Late cleanup (a start… |
| [[RivalRules]] | static | Whose public stats the online room shows as the rival's. No allocation. |
| [[ScoreBoard]] | struct | Both players' published scores. The match result is derived from it (no result message). |
| [[SeatDropOutcome]] | enumeration | What the server does with the seat of a client that disconnected (see `SeatGraceRules.OnDisconnect`). |
| [[SeatGraceRules]] | static | Pure rules of the reconnection grace on the server: what a disconnection does to a seat, which reservations expired and how much grace is le… |
| [[SeatPresence]] | struct | Replicated presence of one player seat: who holds it and its `PlayerConnectionStatus`. A seat held for a dropped player is `PlayerConnection… |
| [[SeatPresenceBoard]] | struct | Presence of both player seats, replicated by `NetworkSeatPresence`. Written whole by the server (rebuilt from the seat table), so it stays a… |
| [[SeatPresenceRules]] | static | Pure rules of `NetworkSeatPresence`: which `PresenceChange`s a board transition publishes. No allocation. |
| [[ServerSlotDirectory]] | class | Server-side seat table: which client holds Player1 / Player2. Plain C#, owned by the approval handler and shared with the roster and the roo… |
| [[ServerStopLatch]] | class | "The host is leaving" state of the server's connection handling (plain C#, owned by `ConnectionApprovalHandler`). Requested when the root st… |
| [[SessionExit]] | enumeration |  |
| [[SessionMembership]] | enumeration | Whether the UGS player id a client declared in its `ConnectionPayload` belongs to the host's online session (`ApprovalRules.CheckMembership`… |
| [[SessionSdkWorkarounds]] | static | Workarounds for com.unity.services.multiplayer 2.4.1. ISession.LeaveAsync() first unsubscribes from the lobby's deleted / kicked events and … |
| [[SimulatedFailureStatsStore]] | class | Decorator that makes every call fail as offline (retryable) without touching the network while shouldFail is true (read on every call, so it… |
| [[SnapshotAddResult]] | enumeration | What `SnapshotBuffer.Add` did with a snapshot. |
| [[SnapshotBuffer]] | class | Fixed ring of received `PlayerSnapshot`s, oldest first and strictly increasing by tick. Out-of-order or duplicate snapshots are dropped; whe… |
| [[SnapshotInterpolator]] | static | Samples a `SnapshotBuffer` at a render time (server clock): linear between the two snapshots around it, clamped to the oldest / newest outsi… |
| [[StatsReadResult]] | enumeration | Outcome of decoding persisted stats (cloud record or local pending). |
| [[StatsStoreOutcome]] | enumeration | How an `IPlayerStatsStore` call ended. |
| [[StatsStoreResult]] | struct | Outcome of an `IPlayerStatsStore` call, free of SDK types. A load carries the decoded `Record` (`Read`) and its `WriteLock`; a save carries … |
| [[Status]] | enumeration |  |
| [[ThrowViewRules]] | static | Server check of a remote throw: the view the owner reports must be close to the last view the server knows for that player (from its input c… |
| [[TransportStatsTracker]] | class | Pure per-session state of the transport lines of `PoolStatsView`: detects a new session (the driver started), tracks the longest frame of th… |
| [[UgsBootstrapper]] | class | Initializes Unity Gaming Services (environment from NetworkSettings, one Authentication profile per Multiplayer Play Mode instance, see `Ugs… |
| [[UgsProfileResolver]] | static | Picks the Unity Authentication profile of this process so every Multiplayer Play Mode instance (and every build started on the same PC) sign… |

## Depende de
[[Modulo Animals|Animals]] (11), [[Modulo Core|Core]] (144), [[Modulo Data|Data]] (55), [[Modulo Input|Input]] (4), [[Modulo Items|Items]] (9), [[Modulo Match|Match]] (8), [[Modulo Player|Player]] (29), [[Modulo UI|UI]] (1), [[Modulo Zones|Zones]] (4)