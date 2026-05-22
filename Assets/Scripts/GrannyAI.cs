using FirstPersonMobileTools.DynamicFirstPerson;
using MobileHapticsProFreeEdition;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// ─────────────────────────────────────────────────────────────────────────────
//  GrannyAI — Horror-grade controller  (v2 — ScriptableObject-backed)
//
//  Requires in the same project:
//    • RoomNodeData.cs        (ScriptableObject per room)
//    • RoomNodeMarker.cs      (MonoBehaviour scene anchor per room)
//    • GrannyAIEditor.cs      (custom Inspector — Editor folder)
//
//  Quick-start: see the Tutorial panel on the Inspector header.
// ─────────────────────────────────────────────────────────────────────────────

public class GrannyAI : MonoBehaviour
{
    // ═════════════════════════════════════════════════════════════════════════
    #region ── Inspector ────────────────────────────────────────────────────

    [Header("── Room Graph ──────────────────────────────────────────")]
    [Tooltip("All RoomNodeData assets in the scene. Use the 'Auto-Find Room Markers' " +
             "button in the Inspector to populate this automatically.")]
    public List<RoomNodeData> allRooms = new();

    [Tooltip("The room Granny spawns in. Drag the matching RoomNodeData asset here.")]
    public RoomNodeData startRoom;

    [Header("── Patrol ───────────────────────────────────────────────")]
    [Tooltip("Ordered list of rooms Granny visits during idle patrol. " +
             "Assign 4–8 rooms for natural coverage.")]
    public List<RoomNodeData> patrolRoute = new();
    [Range(0.5f, 5f)]  public float patrolWaitMin = 1f;
    [Range(0.5f, 8f)]  public float patrolWaitMax = 3f;

    [Header("── Movement ────────────────────────────────────────────")]
    [Range(0.5f, 4f)]  public float patrolSpeed  = 2.0f;
    [Range(1f,   4f)]  public float curiousSpeed = 3.0f;
    [Range(1f,   5f)]  public float huntSpeed    = 3.8f;
    [Range(1f,   8f)]  public float chaseSpeed   = 5.5f;
    [Range(0f,  10f)]  public float startDelay   = 4f;

    [Header("── Belief Thresholds ───────────────────────────────────")]
    [Tooltip("Belief ≥ this → Curious state.")]
    [Range(0.05f, 0.5f)]  public float beliefCuriousThreshold = 0.30f;
    [Tooltip("Belief ≥ this → Hunt state.")]
    [Range(0.3f,  0.95f)] public float beliefHuntThreshold    = 0.65f;
    [Tooltip("Cross-floor belief gate: Granny won't use stairs unless a cross-floor room " +
             "reaches this belief.")]
    [Range(0.5f, 1f)]     public float crossFloorBeliefGate   = 0.75f;
    [Tooltip("Belief decay per second (no cues arriving).")]
    [Range(0.005f, 0.2f)] public float beliefDecayRate        = 0.04f;
    [Tooltip("Grudge heat decay per second (10× slower than belief).")]
    [Range(0.001f, 0.05f)]public float grudgeDecayRate        = 0.005f;

    [Header("── Sound Perception ────────────────────────────────────")]
    [Tooltip("Max room-hops sound travels from source.")]
    [Range(1, 5)]          public int   soundPropagationHops  = 2;
    [Range(0.3f, 1f)]      public float soundBeliefLoud       = 0.85f;
    [Range(0.1f, 0.8f)]    public float soundBeliefQuiet      = 0.45f;
    [Tooltip("Belief multiplier applied per hop (< 1 = attenuates with distance).")]
    [Range(0.1f, 0.9f)]    public float soundHopAttenuation   = 0.45f;
    [Tooltip("Extra attenuation when a door is closed on a connection.")]
    [Range(0.05f, 0.9f)]   public float closedDoorAttenuation = 0.30f;

    [Header("── Vision ───────────────────────────────────────────────")]
    [Range(20f, 120f)]     public float visionAngle           = 60f;
    [Range(2f,  20f)]      public float visionRange           = 10f;
    [Tooltip("Physics layers that block Granny's line-of-sight (walls, closed doors).")]
    public LayerMask visionBlockMask;

    [Header("── Proximity Detection ─────────────────────────────────")]
    [Tooltip("If the player enters this circle radius (X and Z axes only), Granny starts chasing instantly.")]
    [Range(0.5f, 10f)]     public float proximityDetectionRadius = 3f;
    [Tooltip("Maximum allowed Y height difference to prevent triggers across floors.")]
    [Range(0.5f, 5f)]      public float proximityMaxYDifference  = 1.5f;

    [Header("── Attack ───────────────────────────────────────────────")]
    [Range(0.5f, 4f)]      public float attackDistance        = 1.8f;
    [Range(0.5f, 4f)]      public float attackAnimDuration    = 1.5f;

    [Header("── Horror: Near Miss ───────────────────────────────────")]
    [Tooltip("Near-miss only fires when belief is in this range. " +
             "Below it = not scary enough. Above it = Granny goes to Chase instead.")]
    [Range(0.3f, 0.7f)]    public float nearMissBeliefMin     = 0.50f;
    [Range(0.5f, 0.95f)]   public float nearMissBeliefMax     = 0.74f;
    [Tooltip("Player must be within this world-distance for the near-miss to fire.")]
    [Range(0.5f, 5f)]      public float nearMissTriggerDist   = 2.5f;
    [Tooltip("Seconds before another near-miss can fire.")]
    [Range(10f, 120f)]     public float nearMissCooldown      = 30f;

    [Header("── Horror: Tension ─────────────────────────────────────")]
    [Tooltip("Fear score rise rate per second while hunting/chasing.")]
    [Range(0.01f, 0.2f)]   public float fearRiseRate          = 0.06f;
    [Tooltip("Fear score fall rate per second while patrolling.")]
    [Range(0.005f, 0.1f)]  public float fearFallRate          = 0.02f;
    [Tooltip("Speed bonus added to Granny at maximum fear (on top of chase speed).")]
    [Range(0f, 2f)]        public float maxFearSpeedBonus     = 0.8f;

    [Header("── Horror: False Scares ────────────────────────────────")]
    [Tooltip("Seconds between false-scare beats.")]
    [Range(20f, 180f)]     public float falseScareCooldown    = 45f;
    [Tooltip("Probability of a false scare firing on each patrol stop.")]
    [Range(0f, 0.6f)]      public float falseScareProbability = 0.25f;

    [Header("── Animations ──────────────────────────────────────────")]
    public Animator anim;
    public string animIdleName    = "Granny_Idle";
    public string animWalkName    = "Granny_Walk";
    public string animAttackName  = "Granny_Attack";
    public string animReactName   = "Granny_React";
    public string animCrazyName   = "Granny_Crazy";

    [Header("── Ambient Voices ───────────────────────────────────────")]
    public AudioSource grannyAudioSource;
    public AudioClip footstepClip;
    public List<AudioClip> ambientHorrorVoices = new List<AudioClip>();
    [Tooltip("Random voice interval when player is FAR from Granny (seconds).")]
    [Range(1f, 20f)] public float ambientVoiceIntervalFarMin  = 5f;
    [Range(1f, 30f)] public float ambientVoiceIntervalFarMax  = 10f;
    [Tooltip("Random voice interval when player is NEAR Granny (seconds).")]
    [Range(1f, 10f)] public float ambientVoiceIntervalNearMin = 2f;
    [Range(1f, 15f)] public float ambientVoiceIntervalNearMax = 5f;
    [Tooltip("Distance threshold (metres) that switches near vs far voice intervals.")]
    [Range(2f, 20f)] public float ambientVoiceNearDistance    = 8f;
    [Tooltip("Audio clip played the moment Granny sees the player.")]
    public AudioClip onSeePlayerAudio;

    [Header("── Mental Breakdown Settings ────────────────────────────")]
    public AudioClip grannyMentalBreakdown;
    public GameObject mentalBreakDownAlert;

    #endregion

    // ═════════════════════════════════════════════════════════════════════════
    #region ── Public Events ─────────────────────────────────────────────────

    /// <summary>Fired when Granny's attack animation lands on the player.</summary>
    public static event Action OnAttackPlayer;

    /// <summary>Fired every frame with the current 0–1 fear score.
    /// Subscribe from a post-processing manager to drive vignette / aberration.</summary>
    public static event Action<float> OnFearScoreChanged;

    /// <summary>Fired on every state transition.</summary>
    public static event Action<GrannyState> OnStateChanged;

    #endregion

    // ═════════════════════════════════════════════════════════════════════════
    #region ── Private Runtime Fields ───────────────────────────────────────

    // State
    private GrannyState _state = GrannyState.Idle;
    private GrannyState State
    {
        get => _state;
        set
        {
            if (_state == value) return;
            var prev = _state;
            _state   = value;
            OnStateChanged?.Invoke(_state);
            HandleStateTransition(prev, _state);

            if (mentalBreakDownAlert != null)
            {
                mentalBreakDownAlert.SetActive(_state == GrannyState.Crazy);
            }
        }
    }

    // Components
    private NavMeshAgent agent;
    private Transform    player;

    // Room tracking
    private RoomNodeData currentTargetRoom;
    private RoomNodeData grannyCurrentRoom;
    private RoomNodeData playerLastKnownRoom;

    // Patrol
    private int  patrolIndex;
    private bool isWaitingAtWaypoint;

    // Flags / timers
    private bool  isAttacking;
    private bool  isWaitingAtStart;
    private float nearMissCooldownTimer;
    private float falseScareCooldownTimer;
    private float crazyCooldownTimer;
    private float fearScore;

    // Anchor
    private Vector3    startPos;
    private Quaternion startRot;

    // Music — fear-score driven
    private bool _suspensePlaying = false;

    private RoomNodeMarker[] _cachedMarkers;

    // ── Editor-accessible runtime read-outs ──────────────────────────────────
    // (GrannyAIEditor reads these to show live status in Inspector)
    [NonSerialized] public GrannyState EditorCurrentState;
    [NonSerialized] public float       EditorFearScore;
    [NonSerialized] public string      EditorTargetRoom = "—";
    [NonSerialized] public string      EditorGrannyRoom = "—";

    #endregion

    // ═════════════════════════════════════════════════════════════════════════
    #region ── Unity Lifecycle ───────────────────────────────────────────────

    private void OnDestroy()
    {
        // Clear static events when Granny is destroyed (scene transition)
        OnAttackPlayer = null;
        OnFearScoreChanged = null;
        OnStateChanged = null;
    }

    // Tracks whether the game has ended (fail or win) so audio is silenced
    private bool _isGameEnded = false;

    private void OnEnable()
    {
        GameManager.OnGameStarted     += ResetGranny;
        CanvasManager.OnGameRetry     += ResetGranny;
        GameManager.OnSurvivalStarted += EnterSurvivalMode;
        GameManager.OnGameEnd         += OnGameEnded;
    }

    private void OnDisable()
    {
        GameManager.OnGameStarted     -= ResetGranny;
        CanvasManager.OnGameRetry     -= ResetGranny;
        GameManager.OnSurvivalStarted -= EnterSurvivalMode;
        GameManager.OnGameEnd         -= OnGameEnded;

        if (mentalBreakDownAlert != null)
            mentalBreakDownAlert.SetActive(false);
    }

    /// <summary>Called on both win and fail. Immediately silences all Granny audio.</summary>
    private void OnGameEnded()
    {
        _isGameEnded = true;

        if (grannyAudioSource != null)
        {
            grannyAudioSource.Stop();
            grannyAudioSource.volume = 0f;
        }
    }

    private void Start()
    {
        agent  = GetComponent<NavMeshAgent>();
        player = FindFirstObjectByType<MovementController>(FindObjectsInactive.Include).transform;

        agent.updateRotation = true;
        startPos = transform.position;
        startRot = transform.rotation;

        _cachedMarkers = FindObjectsByType<RoomNodeMarker>(FindObjectsSortMode.None);

        if (mentalBreakDownAlert != null)
        {
            mentalBreakDownAlert.SetActive(false);
        }

        InitBeliefMap();
        ResetGranny();
    }

    private void Update()
    {
        if (grannyAudioSource != null)
        {
            // Active = game running, not paused, not ended (fail or win)
            bool isGameActive = !_isGameEnded &&
                                GameManager.Instance != null &&
                                GameManager.Instance.isGameStarted &&
                                Time.timeScale > 0;

            grannyAudioSource.volume = (SoundManager.SoundVol > 0 && isGameActive) ? 1f : 0f;

            if (!isGameActive)
            {
                if (grannyAudioSource.isPlaying)
                    grannyAudioSource.Pause();
            }
            else
            {
                if (!grannyAudioSource.isPlaying)
                    grannyAudioSource.UnPause();
            }
        }

        if (GameManager.Instance == null || !GameManager.Instance.isGameStarted || isAttacking || isWaitingAtStart || Time.timeScale == 0) return;

        UpdateGrannyCurrentRoom();
        DecayBelief();
        UpdateFearScore();
        TickTimers();
        RunStateMachine();
        ApplyFearSpeedBonus();
        HandleMusicTransitions();
        PushEditorReadouts();
    }

    // LookAt IK removed — was not working correctly.

    #endregion

    // ═════════════════════════════════════════════════════════════════════════
    #region ── Initialisation ────────────────────────────────────────────────

    private void InitBeliefMap()
    {
        foreach (var room in allRooms)
            room?.RuntimeReset();
    }

    #endregion

    // ═════════════════════════════════════════════════════════════════════════
    #region ── Room Graph Helpers ────────────────────────────────────────────

    /// <summary>Returns the RoomNodeData whose scene marker is closest to worldPos.</summary>
    private RoomNodeData GetRoomForPosition(Vector3 worldPos)
    {
        if (_cachedMarkers == null || _cachedMarkers.Length == 0)
            _cachedMarkers = FindObjectsByType<RoomNodeMarker>(FindObjectsSortMode.None);

        RoomNodeData best  = null;
        float        bestD = float.MaxValue;

        foreach (var m in _cachedMarkers)
        {
            if (m.data == null) continue;
            float d = Vector3.Distance(worldPos, m.transform.position);
            if (d < bestD) { bestD = d; best = m.data; }
        }
        return best;
    }

    /// <summary>BFS from source room up to maxHops, returning each reachable room
    /// and the cumulative sound-propagation factor (1.0 at source → attenuates).</summary>
    private Dictionary<RoomNodeData, float> BfsRooms(RoomNodeData source, int maxHops)
    {
        var visited = new Dictionary<RoomNodeData, float> { [source] = 1f };
        var queue   = new Queue<(RoomNodeData room, int hops, float factor)>();
        queue.Enqueue((source, 0, 1f));

        while (queue.Count > 0)
        {
            var (room, hops, factor) = queue.Dequeue();
            if (hops >= maxHops || room.connections == null) continue;

            foreach (var conn in room.connections)
            {
                if (conn?.target == null || visited.ContainsKey(conn.target)) continue;
                float att = factor * soundHopAttenuation;
                if (!conn.isDoorOpen) att *= closedDoorAttenuation;
                visited[conn.target] = att;
                queue.Enqueue((conn.target, hops + 1, att));
            }
        }
        return visited;
    }

    /// <summary>Returns the highest-belief room on Granny's current floor,
    /// with a cross-floor fallback gated by crossFloorBeliefGate.</summary>
    private RoomNodeData GetHighestBeliefRoom()
    {
        int          grannyFloor = grannyCurrentRoom?.floorIndex ?? 0;
        RoomNodeData bestSame    = null;
        RoomNodeData bestOther   = null;
        float        scoreSame   = 0f;
        float        scoreOther  = 0f;

        foreach (var room in allRooms)
        {
            if (room == null || room.beliefScore <= 0.01f) continue;
            if (room.floorIndex == grannyFloor)
            {
                if (room.beliefScore > scoreSame)  { scoreSame  = room.beliefScore; bestSame  = room; }
            }
            else
            {
                if (room.beliefScore > scoreOther) { scoreOther = room.beliefScore; bestOther = room; }
            }
        }

        if (bestSame  != null) return bestSame;
        if (bestOther != null && scoreOther >= crossFloorBeliefGate) return bestOther;
        return null;
    }

    private float GetMaxBeliefScore()
    {
        float max = 0f;
        foreach (var room in allRooms) if (room != null && room.beliefScore > max) max = room.beliefScore;
        return max;
    }

    private void UpdateGrannyCurrentRoom()
        => grannyCurrentRoom = GetRoomForPosition(transform.position);

    private Transform GetMarkerTransform(RoomNodeData data)
    {
        if (_cachedMarkers == null)
            _cachedMarkers = FindObjectsByType<RoomNodeMarker>(FindObjectsSortMode.None);

        foreach (var m in _cachedMarkers)
            if (m.data == data) return m.transform;
        return null;
    }

    #endregion

    // ═════════════════════════════════════════════════════════════════════════
    #region ── Perception ────────────────────────────────────────────────────

    /// <summary>Call this from any object the player disturbs.
    /// loud = true  → trap, dropped object, door slam
    /// loud = false → footstep, crouch-walk</summary>
    public void HearSound(Vector3 worldOrigin, bool loud)
    {
        RoomNodeData source = GetRoomForPosition(worldOrigin);
        if (source == null) return;

        float baseStrength = loud ? soundBeliefLoud : soundBeliefQuiet;
        var   affected     = BfsRooms(source, soundPropagationHops);

        foreach (var kvp in affected)
        {
            float injected           = baseStrength * kvp.Value;
            kvp.Key.beliefScore      = Mathf.Max(kvp.Key.beliefScore, injected);
            kvp.Key.grudgeHeat       = Mathf.Min(1f, kvp.Key.grudgeHeat + injected * 0.3f);
            kvp.Key.lastHeardTime    = Time.time;
        }

        playerLastKnownRoom = source;
        EvaluateStateFromBelief();
    }

    private bool HasVisualOnPlayer()
    {
        if (grannyCurrentRoom == null) return false;

        RoomNodeData playerRoom = GetRoomForPosition(player.position);
        bool adjacentOrSame = playerRoom == grannyCurrentRoom;

        if (!adjacentOrSame && grannyCurrentRoom.connections != null)
        {
            foreach (var conn in grannyCurrentRoom.connections)
                if (conn.target == playerRoom && conn.isDoorOpen) { adjacentOrSame = true; break; }
        }
        if (!adjacentOrSame) return false;

        Vector3 toPlayer = (player.position - transform.position).normalized;
        if (Vector3.Angle(transform.forward, toPlayer) > visionAngle * 0.5f) return false;
        if (Vector3.Distance(transform.position, player.position) > visionRange)  return false;

        return !Physics.Raycast(transform.position + Vector3.up * 1.4f,
                                toPlayer, visionRange, visionBlockMask);
    }

    // Tracks whether Granny already played the see-player audio this detection event
    private bool _seePlayerAudioPlayed = false;

    private void CheckVision()
    {
        bool detected = false;

        // Proximity detection (X and Z axes only, within Y difference limit)
        if (player != null)
        {
            Vector3 grannyPos = transform.position;
            Vector3 playerPos = player.position;
            float distXZ = Vector2.Distance(new Vector2(grannyPos.x, grannyPos.z), new Vector2(playerPos.x, playerPos.z));
            float distY = Mathf.Abs(grannyPos.y - playerPos.y);

            if (distXZ <= proximityDetectionRadius && distY <= proximityMaxYDifference)
                detected = true;
        }

        // Vision detection (angle, range, line of sight)
        if (!detected && HasVisualOnPlayer())
            detected = true;

        if (detected)
        {
            // Play "see player" audio once per chase event
            if (!_seePlayerAudioPlayed && onSeePlayerAudio != null && grannyAudioSource != null)
            {
                grannyAudioSource.PlayOneShot(onSeePlayerAudio);
                _seePlayerAudioPlayed = true;
            }

            RoomNodeData pRoom = GetRoomForPosition(player.position);
            if (pRoom != null)
            {
                pRoom.beliefScore   = 1f;
                pRoom.grudgeHeat    = Mathf.Min(1f, pRoom.grudgeHeat + 0.5f);
                pRoom.lastHeardTime = Time.time;
                playerLastKnownRoom = pRoom;
            }

            if (State != GrannyState.Chase && State != GrannyState.Attack && State != GrannyState.Survival && State != GrannyState.Crazy)
                State = GrannyState.Chase;
        }
        else
        {
            // Reset so the audio plays again next time she spots the player
            _seePlayerAudioPlayed = false;
        }
    }

    #endregion

    // ═════════════════════════════════════════════════════════════════════════
    #region ── Belief System ─────────────────────────────────────────────────

    private void DecayBelief()
    {
        foreach (var room in allRooms)
        {
            if (room == null) continue;
            room.beliefScore = Mathf.MoveTowards(room.beliefScore, 0f, beliefDecayRate * Time.deltaTime);
            room.grudgeHeat  = Mathf.MoveTowards(room.grudgeHeat,  0f, grudgeDecayRate  * Time.deltaTime);
        }
    }

    private void EvaluateStateFromBelief()
    {
        if (State == GrannyState.Chase  || State == GrannyState.Attack ||
            State == GrannyState.Survival || State == GrannyState.NearMiss || State == GrannyState.Crazy) return;

        float max = GetMaxBeliefScore();
        if      (max >= beliefHuntThreshold)    State = GrannyState.Hunt;
        else if (max >= beliefCuriousThreshold) State = GrannyState.Curious;
    }

    #endregion

    // ═════════════════════════════════════════════════════════════════════════
    #region ── State Machine ─────────────────────────────────────────────────

    private void RunStateMachine()
    {
        CheckVision();

        switch (State)
        {
            case GrannyState.Patrol:   TickPatrol();   break;
            case GrannyState.Curious:  TickCurious();  break;
            case GrannyState.Hunt:     TickHunt();     break;
            case GrannyState.Chase:    TickChase();    break;
            case GrannyState.Survival: TickSurvival(); break;
        }
    }

    private void HandleStateTransition(GrannyState from, GrannyState to)
    {
        switch (to)
        {
            case GrannyState.Idle:
                anim?.Play(animIdleName); break;
            default:
                anim?.Play(animWalkName); break;
        }
    }

    // ── Patrol ────────────────────────────────────────────────────────────────
    private void TickPatrol()
    {
        agent.speed = patrolSpeed;
        if (!isWaitingAtWaypoint && patrolRoute.Count > 0 &&
            !agent.pathPending && agent.remainingDistance < 0.6f)
            StartCoroutine(PatrolWaitThenAdvance());
    }

    private IEnumerator PatrolWaitThenAdvance()
    {
        isWaitingAtWaypoint = true;

        if (falseScareCooldownTimer <= 0f && UnityEngine.Random.value < falseScareProbability)
            yield return StartCoroutine(FalseScareBeat());
        else
            yield return new WaitForSeconds(UnityEngine.Random.Range(patrolWaitMin, patrolWaitMax));

        RoomNodeData next = PickPatrolDestination();
        NavigateToRoom(next);
        isWaitingAtWaypoint = false;
    }

    private RoomNodeData PickPatrolDestination()
    {
        if (UnityEngine.Random.value < 0.30f)
        {
            RoomNodeData hottest  = null;
            float        hotScore = 0f;
            foreach (var r in allRooms)
                if (r != null && r.grudgeHeat > hotScore) { hotScore = r.grudgeHeat; hottest = r; }
            if (hottest != null && hotScore > 0.1f) return hottest;
        }
        patrolIndex = (patrolIndex + 1) % patrolRoute.Count;
        return patrolRoute[patrolIndex];
    }

    // ── Curious ───────────────────────────────────────────────────────────────
    private void TickCurious()
    {
        agent.speed = curiousSpeed;

        RoomNodeData target = GetHighestBeliefRoom();
        if (target == null) { State = GrannyState.Patrol; return; }

        NavigateToRoom(target);

        float max = GetMaxBeliefScore();
        if      (max >= beliefHuntThreshold)   State = GrannyState.Hunt;
        else if (max < beliefCuriousThreshold) State = GrannyState.Patrol;
    }

    // ── Hunt ──────────────────────────────────────────────────────────────────
    private void TickHunt()
    {
        agent.speed = huntSpeed;

        RoomNodeData target = GetHighestBeliefRoom();
        if (target == null) { State = GrannyState.Curious; return; }

        NavigateToRoom(target);

        if (grannyCurrentRoom == target) TryTriggerNearMiss(target);

        float max = GetMaxBeliefScore();
        if (max < beliefCuriousThreshold) State = GrannyState.Curious;
    }

    // ── Chase ─────────────────────────────────────────────────────────────────
    private void TickChase()
    {
        agent.speed = chaseSpeed;
        agent.SetDestination(PredictPlayerPosition());

        if (Vector3.Distance(transform.position, player.position) <= attackDistance)
            StartCoroutine(AttackSequence());

        if (!HasVisualOnPlayer() && GetMaxBeliefScore() < beliefHuntThreshold)
            State = GrannyState.Hunt;
    }

    // ── Survival ──────────────────────────────────────────────────────────────
    private void TickSurvival()
    {
        agent.speed = chaseSpeed;
        agent.SetDestination(player.position);

        if (Vector3.Distance(transform.position, player.position) <= attackDistance)
            StartCoroutine(AttackSequence());
    }

    #endregion

    // ═════════════════════════════════════════════════════════════════════════
    #region ── Navigation Helpers ────────────────────────────────────────────

    private void NavigateToRoom(RoomNodeData room)
    {
        if (room == null) return;
        if (room == currentTargetRoom && agent.hasPath) return;

        currentTargetRoom = room;
        Transform marker  = GetMarkerTransform(room);
        if (marker != null)
            agent.SetDestination(marker.position);
    }

    private Vector3 PredictPlayerPosition()
    {
        RoomNodeData playerRoom = GetRoomForPosition(player.position);
        if (playerRoom?.connections != null)
        {
            foreach (var conn in playerRoom.connections)
            {
                if (conn?.target == null || !conn.target.isExitRoom || !conn.isDoorOpen) continue;
                Transform exitMarker = GetMarkerTransform(conn.target);
                if (exitMarker == null) continue;
                float exitDist = Vector3.Distance(transform.position, exitMarker.position);
                if (exitDist < agent.remainingDistance * 1.2f)
                    return exitMarker.position;
            }
        }
        return player.position;
    }

    #endregion

    // ═════════════════════════════════════════════════════════════════════════
    #region ── Horror Systems ────────────────────────────────────────────────

    private void TryTriggerNearMiss(RoomNodeData room)
    {
        if (nearMissCooldownTimer > 0f)                                 return;
        if (!room.isHidingSpotRoom)                                     return;
        if (room.beliefScore < nearMissBeliefMin ||
            room.beliefScore > nearMissBeliefMax)                       return;
        if (GetRoomForPosition(player.position) != room)                return;
        if (HasVisualOnPlayer())                                        return;
        if (Vector3.Distance(transform.position, player.position) >
            nearMissTriggerDist)                                        return;

        StartCoroutine(NearMissBeat());
    }

    private IEnumerator NearMissBeat()
    {
        State = GrannyState.NearMiss;
        nearMissCooldownTimer = nearMissCooldown;
        agent.isStopped = true;

        // Approximate turn — slightly off so she doesn't look exactly at the player
        Vector3 approxDir = (player.position - transform.position
                             + UnityEngine.Random.insideUnitSphere * 0.8f).normalized;
        approxDir.y = 0f;

        Quaternion startRot = transform.rotation;
        Quaternion lookRot  = Quaternion.LookRotation(approxDir);

        float elapsed = 0f;
        while (elapsed < 2.0f)
        {
            transform.rotation = Quaternion.Slerp(startRot, lookRot, elapsed / 2.0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        anim?.Play(animReactName);
        yield return new WaitForSeconds(1.5f);

        // One step forward
        agent.isStopped = false;
        agent.SetDestination(transform.position + transform.forward * 0.8f);
        yield return new WaitForSeconds(0.8f);

        // Turn away — the payoff
        agent.isStopped = true;
        Quaternion awayRot = Quaternion.LookRotation(-transform.forward);
        elapsed = 0f;
        while (elapsed < 1.2f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, awayRot, elapsed / 1.2f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        agent.isStopped = false;
        if (currentTargetRoom != null)
            currentTargetRoom.beliefScore = Mathf.Max(0f, currentTargetRoom.beliefScore - 0.4f);

        State = GrannyState.Hunt;
    }

    private IEnumerator FalseScareBeat()
    {
        falseScareCooldownTimer = falseScareCooldown;
        agent.isStopped = true;
        anim?.Play(animReactName);
        yield return new WaitForSeconds(1.8f);

        if (grannyCurrentRoom?.connections != null && grannyCurrentRoom.connections.Count > 0)
        {
            var conn = grannyCurrentRoom.connections[
                UnityEngine.Random.Range(0, grannyCurrentRoom.connections.Count)];
            Transform t = GetMarkerTransform(conn.target);
            if (t != null)
            {
                agent.isStopped = false;
                agent.SetDestination(t.position);
                yield return new WaitForSeconds(2.5f);
            }
        }

        agent.isStopped = false;
        yield return new WaitForSeconds(UnityEngine.Random.Range(patrolWaitMin, patrolWaitMax));
    }

    private void UpdateFearScore()
    {
        bool escalating = State == GrannyState.Hunt    || State == GrannyState.Chase ||
                          State == GrannyState.Survival || State == GrannyState.NearMiss;

        fearScore = escalating
            ? Mathf.MoveTowards(fearScore, 1f, fearRiseRate * Time.deltaTime)
            : Mathf.MoveTowards(fearScore, 0f, fearFallRate * Time.deltaTime);

        OnFearScoreChanged?.Invoke(fearScore);
    }

    private void ApplyFearSpeedBonus()
    {
        if (State == GrannyState.Chase || State == GrannyState.Hunt || State == GrannyState.Survival)
            agent.speed += maxFearSpeedBonus * fearScore;
    }

    private void TickTimers()
    {
        if (nearMissCooldownTimer   > 0f) nearMissCooldownTimer   -= Time.deltaTime;
        if (falseScareCooldownTimer > 0f) falseScareCooldownTimer -= Time.deltaTime;
        
        if (crazyCooldownTimer > 0f && State != GrannyState.Crazy && State != GrannyState.Attack && State != GrannyState.Survival)
        {
            crazyCooldownTimer -= Time.deltaTime;
            if (crazyCooldownTimer <= 0f)
            {
                StartCoroutine(CrazyRoutine());
            }
        }
    }

    #endregion

    // ═════════════════════════════════════════════════════════════════════════
    #region ── Music ─────────────────────────────────────────────────────────

    private void HandleMusicTransitions()
    {
        // Granny is "running" (actively chasing the player)
        bool isRunning = State == GrannyState.Chase ||
                         State == GrannyState.Hunt  ||
                         State == GrannyState.Survival;

        if (isRunning && !_suspensePlaying)
        {
            if (State == GrannyState.Survival)
            {
                Debug.Log("[GrannyAI] Granny running (Survival) — PlayGameGrannyMusic()");
                SoundManager.Instance.PlayGameGrannyMusic();
            }
            else
            {
                Debug.Log($"[GrannyAI] Granny running ({State}) — PlaySuspenseMusic()");
                SoundManager.Instance.PlaySuspenseMusic();
            }
            _suspensePlaying = true;
        }
        else if (!isRunning && _suspensePlaying)
        {
            if (GameManager.Instance != null && !GameManager.Instance.isInSurvivalMode)
            {
                Debug.Log($"[GrannyAI] Granny stopped running ({State}) — PlayNothing()");
                SoundManager.Instance.PlayNothing();
            }
            _suspensePlaying = false;
        }
    }

    #endregion

    // ═════════════════════════════════════════════════════════════════════════
    #region ── Attack / Reset ────────────────────────────────────────────────

    private IEnumerator AttackSequence()
    {
        if (isAttacking) yield break;
        isAttacking     = true;
        agent.isStopped = true;
        GameHaptics.Instance.FailureHaptic();

        transform.position = player.position + player.forward * 0.8f;
        transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));

        anim?.Play(animAttackName);
        OnAttackPlayer?.Invoke();

        yield return new WaitForSeconds(attackAnimDuration);

        isAttacking     = false;
        agent.isStopped = false;

        if (State != GrannyState.Survival) State = GrannyState.Patrol;
    }

    public void ResetGranny()
    {
        StopAllCoroutines();
        _state = GrannyState.Idle;
        if (mentalBreakDownAlert != null)
        {
            mentalBreakDownAlert.SetActive(false);
        }
        StartCoroutine(StartDelayRoutine());
    }

    private IEnumerator StartDelayRoutine()
    {
        isWaitingAtStart = true;
        agent.enabled    = false;

        transform.position = startPos;
        transform.rotation = startRot;
        anim?.Play(animIdleName);

        InitBeliefMap();
        fearScore = patrolIndex = 0;
        nearMissCooldownTimer = falseScareCooldownTimer = 0f;
        crazyCooldownTimer = UnityEngine.Random.Range(30f, 60f);

        yield return new WaitForSeconds(startDelay);

        agent.enabled    = true;
        isWaitingAtStart = false;
        State            = GrannyState.Patrol;

        if (patrolRoute.Count > 0) NavigateToRoom(patrolRoute[0]);

        _isGameEnded = false;  // clear on reset so a retry works
        StartCoroutine(AmbientVoicesRoutine());
    }

    private IEnumerator AmbientVoicesRoutine()
    {
        while (true)
        {
            // Choose interval based on how close the player is
            float dist = player != null
                ? Vector3.Distance(transform.position, player.position)
                : float.MaxValue;

            float waitTime = dist <= ambientVoiceNearDistance
                ? UnityEngine.Random.Range(ambientVoiceIntervalNearMin, ambientVoiceIntervalNearMax)
                : UnityEngine.Random.Range(ambientVoiceIntervalFarMin,  ambientVoiceIntervalFarMax);

            yield return new WaitForSeconds(waitTime);

            // Don't play ambient voices during pause, fail, or win
            bool canPlay = !_isGameEnded &&
                           GameManager.Instance != null &&
                           GameManager.Instance.isGameStarted &&
                           Time.timeScale > 0;

            if (canPlay && ambientHorrorVoices != null && ambientHorrorVoices.Count > 0 && grannyAudioSource != null)
            {
                AudioClip clip = ambientHorrorVoices[UnityEngine.Random.Range(0, ambientHorrorVoices.Count)];
                grannyAudioSource.PlayOneShot(clip);
            }
        }
    }

    private IEnumerator CrazyRoutine()
    {
        var prevState = State;
        State = GrannyState.Crazy;
        agent.isStopped = true;
        anim?.Play(animCrazyName);

        if (grannyAudioSource != null && grannyMentalBreakdown != null)
        {
            grannyAudioSource.PlayOneShot(grannyMentalBreakdown);
        }

        yield return new WaitForSeconds(10f);

        State = (prevState == GrannyState.Attack || prevState == GrannyState.NearMiss) ? GrannyState.Patrol : prevState;
        if (State != GrannyState.Survival) agent.isStopped = false;
        crazyCooldownTimer = UnityEngine.Random.Range(30f, 60f);
    }

    private void EnterSurvivalMode() => State = GrannyState.Survival;

    #endregion

    // ═════════════════════════════════════════════════════════════════════════
    #region ── Editor Utilities (called from GrannyAIEditor) ─────────────────

    /// <summary>Editor button: scans the scene for all RoomNodeMarker components
    /// and populates allRooms with their data assets.</summary>
    public void Editor_AutoFindRooms()
    {
        allRooms.Clear();
        var markers = FindObjectsByType<RoomNodeMarker>(FindObjectsSortMode.None);
        foreach (var m in markers)
            if (m.data != null && !allRooms.Contains(m.data))
                allRooms.Add(m.data);
        Debug.Log($"[GrannyAI] Auto-found {allRooms.Count} rooms.");
    }

    /// <summary>Editor button: creates an empty GameObject with a RoomNodeMarker
    /// at the scene-view camera position.</summary>
    public static GameObject Editor_CreateRoomMarker(string roomName, Vector3 position)
    {
        var go = new GameObject($"Room_{roomName}");
        go.transform.position = position;
        go.AddComponent<RoomNodeMarker>();
        Debug.Log($"[GrannyAI] Created room marker '{roomName}' at {position}.");
        return go;
    }

    /// <summary>Editor button: validates the room graph — checks for null
    /// connections, orphaned nodes, and missing markers.</summary>
    public List<string> Editor_ValidateRoomGraph()
    {
        var issues = new List<string>();
        var markers = FindObjectsByType<RoomNodeMarker>(FindObjectsSortMode.None);
        var markerDataSet = new HashSet<RoomNodeData>();
        foreach (var m in markers) if (m.data != null) markerDataSet.Add(m.data);

        foreach (var room in allRooms)
        {
            if (room == null) { issues.Add("Null entry in allRooms list."); continue; }
            if (!markerDataSet.Contains(room))
                issues.Add($"Room '{room.roomId}' has no scene marker — place a RoomNodeMarker with this asset.");
            if (room.connections == null || room.connections.Count == 0)
                issues.Add($"Room '{room.roomId}' has no connections — is it isolated?");
            else
            {
                foreach (var conn in room.connections)
                {
                    if (conn.target == null)
                        issues.Add($"Room '{room.roomId}' has a connection with a null target.");
                }
            }
        }

        if (patrolRoute.Count < 2)
            issues.Add("Patrol route has fewer than 2 rooms — Granny won't patrol meaningfully.");
        if (startRoom == null)
            issues.Add("startRoom is not assigned.");

        return issues;
    }

    private void PushEditorReadouts()
    {
        EditorCurrentState = State;
        EditorFearScore    = fearScore;
        EditorTargetRoom   = currentTargetRoom?.roomId ?? "—";
        EditorGrannyRoom   = grannyCurrentRoom?.roomId ?? "—";
    }

    #endregion

    // ═════════════════════════════════════════════════════════════════════════
    #region ── Gizmos ────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
        // Proximity detection circle (Yellow)
        UnityEditor.Handles.color = new Color(1f, 0.92f, 0.016f, 0.08f);
        UnityEditor.Handles.DrawSolidDisc(transform.position, Vector3.up, proximityDetectionRadius);
        UnityEditor.Handles.color = new Color(1f, 0.92f, 0.016f, 0.6f);
        UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.up, proximityDetectionRadius);

        // Vision cone — filled arc
        UnityEditor.Handles.color = new Color(0f, 1f, 1f, 0.12f);
        UnityEditor.Handles.DrawSolidArc(transform.position + Vector3.up * 1.4f,
                                          Vector3.up, Quaternion.Euler(0, -visionAngle * 0.5f, 0)
                                          * transform.forward, visionAngle, visionRange);
        UnityEditor.Handles.color = new Color(0f, 1f, 1f, 0.8f);
        UnityEditor.Handles.DrawWireArc(transform.position + Vector3.up * 1.4f,
                                         Vector3.up, Quaternion.Euler(0, -visionAngle * 0.5f, 0)
                                         * transform.forward, visionAngle, visionRange, 2f);

        // Attack sphere
        UnityEditor.Handles.color = new Color(1f, 0.1f, 0.1f, 0.15f);
        UnityEditor.Handles.DrawSolidDisc(transform.position, Vector3.up, attackDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);

        // Near-miss range
        UnityEditor.Handles.color = new Color(1f, 0.5f, 0f, 0.1f);
        UnityEditor.Handles.DrawSolidDisc(transform.position, Vector3.up, nearMissTriggerDist);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, nearMissTriggerDist);

        // Label
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2.8f,
            $"State: {_state}\nFear: {fearScore:P0}",
            new GUIStyle { fontSize = 10, fontStyle = FontStyle.Bold,
                           normal = { textColor = Color.white } });
#endif
    }

    #endregion
}

// ─────────────────────────────────────────────────────────────────────────────
public enum GrannyState
{
    Idle, Patrol, Curious, Hunt, Chase, NearMiss, Attack, Survival, Crazy
}
