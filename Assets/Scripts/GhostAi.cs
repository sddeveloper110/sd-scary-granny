using System;
using System.Collections;
using FirstPersonMobileTools.DynamicFirstPerson;
using UnityEngine;
using UnityEngine.AI;

public class GhostAi : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] Transform waypointParent;
    [SerializeField] float roamSpeed = 3.5f;
    [SerializeField] float chaseSpeed = 5.5f;
    [SerializeField] float startDelay = 4f; // Timer delay

    [Header("Detection Settings")]
    [SerializeField] float suspiciousRadius = 12f;
    [SerializeField] float chaseRadius = 7f;
    [SerializeField] float suspiciousTime = 4f;
    [SerializeField] float attackDistance = 2f;

    [Header("Animations & IK")]
    [SerializeField] Animator anim;
    [SerializeField] string walkAnimName = "Granny_Walk";
    [SerializeField] string idleAnimName = "Granny_Idle"; // Added for the delay period
    [SerializeField] string attackAnimName = "Granny_Attack";
    [SerializeField] float attackAnimDuration = 1.5f;
    [Range(0, 1)][SerializeField] float lookAtWeight = 1.0f;

    public static Action OnAttackEnemy;

    // --- State Property with Music Logic ---
    private GhostState _state = GhostState.Roam;
    private GhostState State
    {
        get => _state;
        set
        {
            if (_state == value) return; // Exit if state hasn't actually changed
            _state = value;
        }
    }

    private NavMeshAgent agent;
    private Transform player;
    private Transform[] waypoints;

    private float susTimer;
    private bool isAttacking;
    private bool shouldPlaySuspense = false;
    private bool isWaitingAtStart; // New delay bool
    private int currentWaypoint;
    private Vector3 startPos;
    private Quaternion startRot;

    private void OnEnable()
    {
        GameManager.OnGameStarted += ResetGhost;
        CanvasManager.OnGameRetry += ResetGhost;
        GameManager.OnSurvivalStarted += () => State = GhostState.Survival;
    }

    private void OnDisable()
    {
        GameManager.OnGameStarted -= ResetGhost;
        CanvasManager.OnGameRetry -= ResetGhost;
        GameManager.OnSurvivalStarted -= () => State = GhostState.Survival;
    }

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        player = FindFirstObjectByType<MovementController>(FindObjectsInactive.Include).transform;

        waypoints = new Transform[waypointParent.childCount];
        for (int i = 0; i < waypoints.Length; i++) waypoints[i] = waypointParent.GetChild(i);

        startPos = transform.position;
        startRot = transform.rotation;

        agent.updateRotation = true;
        ResetGhost();
    }

    private void Update()
    {
        if (!GameManager.Instance.isGameStarted || isAttacking || isWaitingAtStart) return;

        HandleLogic();
        MusicHandler();
    }

    private void HandleLogic()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (State)
        {
            case GhostState.Roam:
                agent.speed = roamSpeed;
                if (!agent.pathPending && agent.remainingDistance < 0.5f)
                    GoToNextWaypoint();

                if (distanceToPlayer <= chaseRadius) State = GhostState.Chase;
                else if (distanceToPlayer <= suspiciousRadius) State = GhostState.Suspicious;
                break;

            case GhostState.Suspicious:
                agent.speed = roamSpeed;
                agent.SetDestination(player.position);

                susTimer += Time.deltaTime;
                if (distanceToPlayer <= chaseRadius)
                {
                    State = GhostState.Chase;
                    susTimer = 0;
                }
                else if (susTimer >= suspiciousTime || distanceToPlayer > suspiciousRadius)
                {
                    susTimer = 0;
                    State = GhostState.Roam;
                }
                break;

            case GhostState.Chase:
                agent.speed = chaseSpeed;
                agent.SetDestination(player.position);
                shouldPlaySuspense = true;
                if (distanceToPlayer <= attackDistance) StartCoroutine(AttackSequence());
                if (distanceToPlayer > chaseRadius)
                {
                    shouldPlaySuspense = false;
                    State = GhostState.Suspicious;
                }
                break;

            case GhostState.Survival:
                agent.speed = chaseSpeed;
                shouldPlaySuspense = true;
                agent.SetDestination(player.position);
                if (distanceToPlayer <= attackDistance) StartCoroutine(AttackSequence());
                break;
        }
    }

    // --- Fixed Music Logic (Called only once per state change) ---

    private bool lastSuspenseState; // Previous frame ka record rakhne ke liye

    private void MusicHandler()
    {
        // Agar current state purani state se mukhtalif hai (bool has flipped)
        if (shouldPlaySuspense != lastSuspenseState)
        {
            if (shouldPlaySuspense)
            {
                // Pehli dafa true hua
                SoundManager.Instance.PlayGameGrannyMusic();
            }
            else
            {
                // Pehli dafa false hua
                SoundManager.Instance.PlayGameDefaultMusic();
            }

            // Update last state taake agli frame mein yeh 'if' trigger na ho
            lastSuspenseState = shouldPlaySuspense;
        }
    }

    private void GoToNextWaypoint()
    {
        if (waypoints.Length == 0) return;
        currentWaypoint = UnityEngine.Random.Range(0, waypoints.Length);
        agent.SetDestination(waypoints[currentWaypoint].position);
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (anim == null) return;
        bool shouldLook = (State == GhostState.Chase || State == GhostState.Survival) && !isAttacking && !isWaitingAtStart;

        if (shouldLook)
        {
            anim.SetLookAtWeight(lookAtWeight, 0.1f, 0.9f, 1.0f, 0.5f);
            anim.SetLookAtPosition(player.position + Vector3.up * 1.3f);
        }
        else anim.SetLookAtWeight(0);
    }

    IEnumerator AttackSequence()
    {
        isAttacking = true;
        agent.isStopped = true;

        transform.position = player.position + (player.forward * 1.2f);
        transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));

        if (anim != null) anim.Play(attackAnimName);
        OnAttackEnemy?.Invoke();

        yield return new WaitForSeconds(attackAnimDuration);

        isAttacking = false;
        agent.isStopped = false;
        if (State != GhostState.Survival) State = GhostState.Roam;
    }

    public void ResetGhost()
    {
        StopAllCoroutines();
        StartCoroutine(StartDelayRoutine());
    }

    IEnumerator StartDelayRoutine()
    {
        isWaitingAtStart = true;
        agent.enabled = false;

        transform.position = startPos;
        transform.rotation = startRot;

        if (anim != null) anim.Play(idleAnimName);

        yield return new WaitForSeconds(startDelay);

        agent.enabled = true;
        isWaitingAtStart = false;
        State = GhostState.Roam;
        if (anim != null) anim.Play(walkAnimName);
        GoToNextWaypoint();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, suspiciousRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRadius);
    }
}
public enum GhostState
{
    Roam, Survival,Chase,Attack,Suspicious
}