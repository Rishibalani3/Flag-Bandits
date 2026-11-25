using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Fusion;
using FlagBandits.Network;

namespace FlagBandits.Gameplay
{
    public enum FlagState
    {
        AtBase = 0,
        Carried = 1,
        Dropped = 2
    }

    /// <summary>
    /// Master-Client authoritative Capture The Flag state machine (AtBase -> Carried -> Dropped).
    /// Syncs state, carrier PlayerRef, ground anchor, and drop countdown across all clients.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkFlag : NetworkBehaviour
    {
        private static readonly List<NetworkFlag> activeFlags = new List<NetworkFlag>();
        public static IReadOnlyList<NetworkFlag> ActiveFlags => activeFlags;

        [Header("Configuration")]
        [SerializeField] private Team flagTeam = Team.TeamA;
        [SerializeField] private CTFSettings settings;

        [Header("Visuals")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Renderer[] teamColoredRenderers;

        // ---- Networked State (Master Client authoritative) ----
        [Networked] public FlagState State { get; set; }
        [Networked] public Team OwningTeam { get; set; }
        [Networked] public PlayerRef CarrierPlayer { get; set; }
        [Networked] public Vector3 WorldAnchorPosition { get; set; }
        [Networked] public TickTimer ReturnTimer { get; set; }

        public event Action<NetworkFlag, FlagState, FlagState> OnFlagStateChanged;

        public Team Team => (Object != null && Object.IsValid && OwningTeam != Team.None) ? OwningTeam : flagTeam;
        public CTFSettings Settings => settings;

        private Vector3 basePosition;
        private Quaternion baseRotation = Quaternion.identity;
        private bool baseResolved;
        private Vector3 lastCarrierPosition;
        private TickTimer pickupCooldownTimer;
        private FlagState cachedState;
        private readonly Dictionary<PlayerRef, bool> lastObservedDead = new Dictionary<PlayerRef, bool>();
        private readonly Dictionary<PlayerRef, TickTimer> respawnPickupGrace = new Dictionary<PlayerRef, TickTimer>();

        private float PickupRadius => settings != null ? settings.flagPickupRadius : 1.8f;
        private float ReturnRadius => settings != null ? settings.flagReturnRadius : 1.8f;
        private float MaxHeightDiff => settings != null ? settings.maxInteractionHeightDiff : 2.5f;
        private float DroppedReturnDelay => settings != null ? settings.flagDroppedReturnDelay : 10f;
        private float DropCooldown => settings != null ? settings.flagPickupCooldownAfterDrop : 0.5f;
        private float PostRespawnDelay => settings != null ? settings.postRespawnPickupDelay : 0.5f;
        private bool TouchOwnToReturn => settings == null || settings.touchOwnDroppedFlagToReturn;
        private Vector3 CarryOffset => settings != null ? settings.flagCarryOffset : new Vector3(0f, 1.35f, -0.45f);
        private float CarryTilt => settings != null ? settings.carryTiltAngle : -20f;

        public static NetworkFlag FindFlag(Team team)
        {
            for (int i = activeFlags.Count - 1; i >= 0; i--)
            {
                var flag = activeFlags[i];
                if (flag == null)
                {
                    activeFlags.RemoveAt(i);
                    continue;
                }
                if (flag.Object != null && flag.Object.IsValid && flag.Team == team) return flag;
            }
            return null;
        }

        public static bool IsPlayerCarryingAnyFlag(PlayerRef player)
        {
            return TryGetCarriedFlag(player, out _);
        }

        public static bool TryGetCarriedFlag(PlayerRef player, out NetworkFlag carriedFlag)
        {
            carriedFlag = null;
            if (player == PlayerRef.None) return false;

            for (int i = activeFlags.Count - 1; i >= 0; i--)
            {
                var flag = activeFlags[i];
                if (flag == null)
                {
                    activeFlags.RemoveAt(i);
                    continue;
                }

                if (flag.Object != null && flag.Object.IsValid
                    && flag.State == FlagState.Carried
                    && flag.CarrierPlayer == player)
                {
                    carriedFlag = flag;
                    return true;
                }
            }
            return false;
        }

        public override void Spawned()
        {
            if (!activeFlags.Contains(this))
            {
                activeFlags.Add(this);
            }

            if (settings == null)
            {
                settings = Resources.Load<CTFSettings>("CTFSettings");
            }

            ResolveBaseAnchor();

            if (HasStateAuthority && OwningTeam == Team.None)
            {
                OwningTeam = flagTeam;
                State = FlagState.AtBase;
                CarrierPlayer = PlayerRef.None;
                WorldAnchorPosition = basePosition;
                ReturnTimer = TickTimer.None;
            }

            cachedState = State;
            ApplyTeamColors();
            UpdateVisualTransform();
            gameObject.name = $"Flag_{Team}";
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            activeFlags.Remove(this);
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;

            ResolveBaseAnchor();
            UpdatePlayerRespawnTracking();

            switch (State)
            {
                case FlagState.AtBase:
                    TickAtBase();
                    break;
                case FlagState.Carried:
                    TickCarried();
                    break;
                case FlagState.Dropped:
                    TickDropped();
                    break;
            }
        }

        public override void Render()
        {
            if (State != cachedState)
            {
                var prev = cachedState;
                cachedState = State;
                OnFlagStateChanged?.Invoke(this, prev, State);
            }

            UpdateVisualTransform();
        }

        /// <summary>
        /// Remaining seconds before a dropped flag auto-returns to base (0 if not dropped).
        /// </summary>
        public float GetRemainingReturnTime()
        {
            if (Object == null || !Object.IsValid || State != FlagState.Dropped || Runner == null || !Runner.IsRunning)
            {
                return 0f;
            }
            return ReturnTimer.RemainingTime(Runner) ?? 0f;
        }

        /// <summary>
        /// Master-Client-only reset back onto the home pedestal (used by timeout, touch-return, and captures).
        /// </summary>
        public void ReturnToBase()
        {
            if (!HasStateAuthority) return;

            ResolveBaseAnchor();
            State = FlagState.AtBase;
            CarrierPlayer = PlayerRef.None;
            ReturnTimer = TickTimer.None;
            WorldAnchorPosition = basePosition;
            pickupCooldownTimer = TickTimer.CreateFromSeconds(Runner, DropCooldown);
            transform.SetPositionAndRotation(basePosition, baseRotation);

            Debug.Log($"[NetworkFlag] {Team} flag returned to base.");
        }

        private void TickAtBase()
        {
            transform.SetPositionAndRotation(basePosition, baseRotation);

            if (!pickupCooldownTimer.ExpiredOrNotRunning(Runner)) return;

            if (TryFindNearestPlayer(WorldAnchorPosition, PickupRadius, wantTeammate: false, out var enemy))
            {
                PickUpBy(enemy);
            }
        }

        private void TickCarried()
        {
            bool carrierStillInRoom = CarrierPlayer != PlayerRef.None && Runner.ActivePlayers.Contains(CarrierPlayer);
            bool foundCarrier = NetworkPlayer.TryGetPlayer(CarrierPlayer, out var carrier);

            if (!carrierStillInRoom || !foundCarrier || carrier == null || carrier.Object == null || !carrier.Object.IsValid)
            {
                DropAt(lastCarrierPosition);
                return;
            }

            if (carrier.IsDead)
            {
                DropAt(lastCarrierPosition);
                return;
            }

            lastCarrierPosition = carrier.transform.position;
            transform.position = carrier.transform.TransformPoint(CarryOffset);
        }

        private void TickDropped()
        {
            transform.position = WorldAnchorPosition;

            if (ReturnTimer.Expired(Runner))
            {
                Debug.Log($"[NetworkFlag] {Team} flag drop timer expired — returning to base.");
                ReturnToBase();
                return;
            }

            if (!pickupCooldownTimer.ExpiredOrNotRunning(Runner)) return;

            if (TouchOwnToReturn && TryFindNearestPlayer(WorldAnchorPosition, ReturnRadius, wantTeammate: true, out var teammate))
            {
                Debug.Log($"[NetworkFlag] {Team} flag returned by {teammate.Object.StateAuthority}.");
                ReturnToBase();
                return;
            }

            if (TryFindNearestPlayer(WorldAnchorPosition, PickupRadius, wantTeammate: false, out var enemy))
            {
                PickUpBy(enemy);
            }
        }

        private void PickUpBy(NetworkPlayer enemy)
        {
            State = FlagState.Carried;
            CarrierPlayer = enemy.Object.StateAuthority;
            ReturnTimer = TickTimer.None;
            lastCarrierPosition = enemy.transform.position;

            Debug.Log($"[NetworkFlag] {Team} flag picked up by {CarrierPlayer}!");
        }

        private void DropAt(Vector3 rawPosition)
        {
            if (TryResolveGroundDropPosition(rawPosition, out Vector3 dropPos))
            {
                State = FlagState.Dropped;
                CarrierPlayer = PlayerRef.None;
                WorldAnchorPosition = dropPos;
                ReturnTimer = TickTimer.CreateFromSeconds(Runner, DroppedReturnDelay);
                pickupCooldownTimer = TickTimer.CreateFromSeconds(Runner, DropCooldown);
                transform.position = dropPos;

                Debug.Log($"[NetworkFlag] {Team} flag dropped at {dropPos}! Returning in {DroppedReturnDelay:F0}s.");
            }
            else
            {
                // Carrier died out-of-bounds (e.g. void fall) — immediately return flag to base.
                Debug.Log($"[NetworkFlag] {Team} flag fell out of bounds — returning to base.");
                ReturnToBase();
            }
        }

        private bool TryResolveGroundDropPosition(Vector3 originPos, out Vector3 groundPos)
        {
            Vector3 probeStart = originPos + Vector3.up * 1.0f;
            var hits = Physics.RaycastAll(probeStart, Vector3.down, 25f, ~0, QueryTriggerInteraction.Ignore);
            float closestDist = float.MaxValue;
            Vector3 bestPoint = originPos;
            bool foundHit = false;

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider == null) continue;
                if (hits[i].collider.GetComponentInParent<NetworkPlayer>() != null) continue;
                if (hits[i].distance < closestDist)
                {
                    closestDist = hits[i].distance;
                    bestPoint = hits[i].point;
                    foundHit = true;
                }
            }

            if (foundHit)
            {
                groundPos = bestPoint;
                return true;
            }

            if (originPos.y >= -5f)
            {
                groundPos = new Vector3(originPos.x, Mathf.Max(0f, originPos.y), originPos.z);
                return true;
            }

            groundPos = basePosition;
            return false;
        }

        private void UpdatePlayerRespawnTracking()
        {
            var players = NetworkPlayer.ActivePlayers;
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                if (player == null || player.Object == null || !player.Object.IsValid) continue;

                PlayerRef playerRef = player.Object.StateAuthority;
                bool isDeadNow = player.IsDead;

                if (lastObservedDead.TryGetValue(playerRef, out bool wasDead) && wasDead && !isDeadNow)
                {
                    respawnPickupGrace[playerRef] = TickTimer.CreateFromSeconds(Runner, PostRespawnDelay);
                }

                lastObservedDead[playerRef] = isDeadNow;
            }
        }

        private bool IsPlayerBlockedFromFlagPickup(PlayerRef playerRef)
        {
            return respawnPickupGrace.TryGetValue(playerRef, out var timer) && !timer.ExpiredOrNotRunning(Runner);
        }

        private bool TryFindNearestPlayer(Vector3 anchorPos, float radius, bool wantTeammate, out NetworkPlayer nearest)
        {
            nearest = null;
            float bestDistSq = radius * radius;
            var players = NetworkPlayer.ActivePlayers;

            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                if (player == null || player.Object == null || !player.Object.IsValid) continue;
                if (player.PlayerTeam == Team.None || player.IsDead) continue;
                if (!wantTeammate && IsPlayerBlockedFromFlagPickup(player.Object.StateAuthority)) continue;

                bool isTeammate = player.PlayerTeam == Team;
                if (isTeammate != wantTeammate) continue;

                Vector3 diff = player.transform.position - anchorPos;
                if (Mathf.Abs(diff.y) > MaxHeightDiff) continue;

                float horizontalDistSq = diff.x * diff.x + diff.z * diff.z;
                if (horizontalDistSq <= bestDistSq)
                {
                    bestDistSq = horizontalDistSq;
                    nearest = player;
                }
            }

            return nearest != null;
        }

        private void ResolveBaseAnchor()
        {
            if (baseResolved) return;

            var zones = FindObjectsByType<TeamBaseZone>();
            Team targetTeam = Team;
            for (int i = 0; i < zones.Length; i++)
            {
                if (zones[i].Team == targetTeam)
                {
                    basePosition = zones[i].FlagPedestal.position;
                    baseRotation = zones[i].FlagPedestal.rotation;
                    baseResolved = true;
                    return;
                }
            }

            basePosition = transform.position;
            baseRotation = transform.rotation;
            baseResolved = true;
        }

        private void UpdateVisualTransform()
        {
            switch (State)
            {
                case FlagState.AtBase:
                    transform.SetPositionAndRotation(WorldAnchorPosition, baseRotation);
                    if (visualRoot != null)
                    {
                        visualRoot.localRotation = Quaternion.identity;
                    }
                    break;

                case FlagState.Carried:
                    if (NetworkPlayer.TryGetPlayer(CarrierPlayer, out var carrier) && carrier != null)
                    {
                        transform.position = carrier.transform.TransformPoint(CarryOffset);
                        transform.rotation = carrier.transform.rotation;
                    }
                    else
                    {
                        transform.position = WorldAnchorPosition;
                    }
                    if (visualRoot != null)
                    {
                        visualRoot.localRotation = Quaternion.Euler(CarryTilt, 0f, 0f);
                    }
                    break;

                case FlagState.Dropped:
                    float bobAmp = settings != null ? settings.droppedBobAmplitude : 0.12f;
                    float bobSpeed = settings != null ? settings.droppedBobSpeed : 3f;
                    float rotSpeed = settings != null ? settings.droppedRotateSpeed : 90f;
                    float bobOffset = (Mathf.Sin(Time.time * bobSpeed) + 1f) * 0.5f * bobAmp;

                    transform.position = WorldAnchorPosition + Vector3.up * bobOffset;
                    transform.rotation = Quaternion.Euler(0f, (Time.time * rotSpeed) % 360f, 0f);
                    if (visualRoot != null)
                    {
                        visualRoot.localRotation = Quaternion.identity;
                    }
                    break;
            }
        }

        private void ApplyTeamColors()
        {
            if (teamColoredRenderers == null || teamColoredRenderers.Length == 0)
            {
                var banner = transform.Find("VisualRoot/Banner");
                if (banner != null)
                {
                    var r = banner.GetComponent<Renderer>();
                    if (r != null) teamColoredRenderers = new[] { r };
                }
            }

            if (teamColoredRenderers == null) return;

            Color teamCol = NetworkPlayer.GetTeamColor(Team);
            for (int i = 0; i < teamColoredRenderers.Length; i++)
            {
                var rend = teamColoredRenderers[i];
                if (rend == null || rend.material == null) continue;

                if (rend.material.HasProperty("_BaseColor"))
                    rend.material.SetColor("_BaseColor", teamCol);
                else if (rend.material.HasProperty("_Color"))
                    rend.material.color = teamCol;
            }
        }
    }
}
