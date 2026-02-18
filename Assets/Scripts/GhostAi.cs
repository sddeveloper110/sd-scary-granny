using System;
using System.Collections;
using FirstPersonMobileTools.DynamicFirstPerson;
using UnityEngine;
using UnityEngine.AI;

public class GhostAi : MonoBehaviour
{
    [SerializeField] Transform waypointParent;
    [SerializeField] bool followRandomWaypoint;

    [Header("Radius Settings")]
    [SerializeField] float suspiciousRadius = 10f;
    [SerializeField] float chaseRadius = 6f;

    [Header("State Settings")]
    [SerializeField] float suspiciousTime = 3f;
    [SerializeField] float attackDistance = 2f;
    [SerializeField] float rotationSpeed = 10f; // How fast she turns

    [Header("Animation Settings")]
    [SerializeField] Animator anim;
    [SerializeField] string walkAnimName = "Granny_Walk";
    [SerializeField] string attackAnimName = "Granny_Attack";
    [SerializeField] float attackAnimDuration = 1.5f;

    [Header("Head Look Settings")]
    [SerializeField] Transform headBone; // Drag Granny's Head bone here
    [SerializeField] float lookAtWeight = 0f; // Smoothes the head turn
    [SerializeField] float lookAtSpeed = 5f;

    [Header("Events")]
    public static Action OnAttackEnemy;

    private Vector3 startPosition;
    private Quaternion startRotation;

    [Header("Startup Settings")]
    [SerializeField] float startDelay = 4f;

    GhostState state = GhostState.Roam;
    Transform[] waypoints;
    Transform player;
    NavMeshAgent agent;
    Vector3 lastPlayerPosition;
    int currentWaypoint = 0;
    float suspiciousTimer = 0f;
    bool isAttacking = false;

    private void OnEnable()
    {
        CanvasManager.OnGameStart += ResetGhost;
        CanvasManager.OnGameRetry += ResetGhost;
    }
    private void OnDisable()
    {
        CanvasManager.OnGameStart -= ResetGhost;
        CanvasManager.OnGameRetry -= ResetGhost;
    }
    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;

        // Save starting transform for reset function
        startPosition = transform.position;
        startRotation = transform.rotation;

        player = FindFirstObjectByType<MovementController>(FindObjectsInactive.Include).transform;

        waypoints = new Transform[waypointParent.childCount];
        for (int i = 0; i < waypoints.Length; i++)
               waypoints[i] = waypointParent.GetChild(i);

        // Start the delayed activation
        StartCoroutine(StartWithDelay());
    }

    IEnumerator StartWithDelay()
    {
        //// Shuru mein agent aur logic band rahegi
        isAttacking = true;
      //  if (anim != null) anim.Play("Idle"); // Agar idle animation hai toh, warna walk hi rehne dein

        yield return new WaitForSeconds(startDelay);

        isAttacking = false;
        if (anim != null) anim.Play(walkAnimName);
        GoToNextWaypoint();
    }

    // --- RESET FUNCTION ---
    public void ResetGhost()
    {
        StopAllCoroutines(); // Purani saari movement/attack cancel

        isAttacking = false;
        agent.enabled = false; // Teleport ke liye disable zaroori hai

        transform.position = startPosition;
        transform.rotation = startRotation;

        agent.enabled = true;
        state = GhostState.Roam;

        // Reset hone ke baad bhi thoda wait karke dobara shuru karegi
        StartCoroutine(StartWithDelay());
    }

    private void Update()
    {
        if (!GameManager.Instance.isGameStarted)
        {
            return;
        }
        if (isAttacking ) return;

        HandleManualRotation();
        float distToPlayer = Vector3.Distance(transform.position, player.position);

        switch (state)
        {
            case GhostState.Roam:
                if (!agent.pathPending && agent.remainingDistance < 0.5f)
                    GoToNextWaypoint();

                if (distToPlayer <= chaseRadius && CanSeePlayer()) state = GhostState.Chase;
                else if (distToPlayer <= suspiciousRadius) state = GhostState.Suspicious;
                break;

            case GhostState.Suspicious:
                agent.SetDestination(lastPlayerPosition);
                suspiciousTimer += Time.deltaTime;

                if (suspiciousTimer >= suspiciousTime)
                {
                    suspiciousTimer = 0f;
                    state = GhostState.Roam;
                }
                if (distToPlayer <= chaseRadius && CanSeePlayer()) state = GhostState.Chase;
                break;

            case GhostState.Chase:
                agent.SetDestination(player.position);
                if (distToPlayer <= attackDistance) StartCoroutine(AttackSequence());
                if (!CanSeePlayer() && distToPlayer > chaseRadius) state = GhostState.Suspicious;
                break;
        }
    }

    // --- MANUAL ROTATION FOR ROOT MOTION ---
    void HandleManualRotation()
    {
        Vector3 targetDirection = Vector3.zero;

        if (state == GhostState.Chase)
        {
            // When chasing, look directly at the player
            targetDirection = (player.position - transform.position).normalized;
        }
        else
        {
            // When roaming/suspicious, look where the NavMesh path is actually going
            // This prevents her from walking "backwards" or "sideways"
            if (agent.steeringTarget != Vector3.zero)
            {
                targetDirection = (agent.steeringTarget - transform.position).normalized;
            }
        }

        if (targetDirection != Vector3.zero)
        {
            targetDirection.y = 0; // Keep her vertical
            Quaternion targetRot = Quaternion.LookRotation(targetDirection);

            // Use a higher multiplier for rotationSpeed if it feels slow
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }

    // --- HEAD LOOK LOGIC ---
    private void OnAnimatorIK(int layerIndex)
    {
        if (anim == null) return;

        // Only look at player when Chasing or Suspicious
        bool shouldLook = (state == GhostState.Chase || state == GhostState.Suspicious) && !isAttacking;

        lookAtWeight = Mathf.Lerp(lookAtWeight, shouldLook ? 1f : 0f, Time.deltaTime * lookAtSpeed);

        if (lookAtWeight > 0.01f)
        {
            anim.SetLookAtWeight(lookAtWeight, 0.2f, 0.8f, 0.9f, 0.5f);
            anim.SetLookAtPosition(player.position + Vector3.up * 1.5f); // Aim for player's head area
        }
    }

    [Header("Jumpscare Settings")]
    [SerializeField] float forwardOffset = 1.2f; // Distance in front of player's face
    [SerializeField] float heightOffset = 0f;    // Adjust if she appears too high or low

    IEnumerator AttackSequence()
    {
        isAttacking = true;
        agent.enabled = false; // Disable NavMeshAgent to allow manual position snapping

        // 1. Calculate the position directly in front of the player
        // player.forward is the direction the player is looking
        Vector3 jumpscarePos = player.position + (player.forward * forwardOffset);
        jumpscarePos.y = player.position.y + heightOffset;

        // 2. Snap Granny to that position
        transform.position = jumpscarePos;

        // 3. Make Granny look exactly at the player's face
        Vector3 lookAtPlayer = player.position;
        lookAtPlayer.y = transform.position.y; // Keep her vertical
        transform.LookAt(lookAtPlayer);

        // 4. Play Animation and Event
        if (anim != null) anim.Play(attackAnimName);
        OnAttackEnemy?.Invoke();

        Debug.Log("Jumpscare Snapped!");

        yield return new WaitForSeconds(attackAnimDuration);

        // 5. Cleanup
        isAttacking = false;
        agent.enabled = true; // Re-enable for roaming
        agent.isStopped = false;

        if (anim != null) anim.Play(walkAnimName);

        state = GhostState.Roam;
        GoToNextWaypoint();
    }
    void GoToNextWaypoint()
    {
        if (waypoints.Length == 0) return;
        if (followRandomWaypoint) currentWaypoint = UnityEngine.Random.Range(0, waypoints.Length);
        agent.SetDestination(waypoints[currentWaypoint].position);
        currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
    }

    bool CanSeePlayer()
    {
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        Ray ray = new Ray(transform.position + Vector3.up * 1.5f, dirToPlayer);
        if (Physics.Raycast(ray, out RaycastHit hit, 15f))
        {
            if (hit.transform == player) return true;
        }
        return false;
    }
}

public enum GhostState
{
    Roam,
    Suspicious,
    Chase,
    Attack
}
