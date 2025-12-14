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
    [SerializeField] float chaseSpeed = 5f;
    [SerializeField] float roamSpeed = 2f;

    GhostState state = GhostState.Roam;

    Transform[] waypoints;
    Transform player;
    NavMeshAgent agent;
    Vector3 lastPlayerPosition;
    int currentWaypoint = 0;
    float suspiciousTimer = 0f;
    float distToPlayer;
    bool isAttacking = false;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        player = FindFirstObjectByType<MovementController>().transform;

        waypoints = new Transform[waypointParent.childCount];
        for (int i = 0; i < waypoints.Length; i++)
            waypoints[i] = waypointParent.GetChild(i);

        GoToNextWaypoint();
    }

    private void Update()
    {
        distToPlayer = Vector3.Distance(transform.position, player.position);

        switch (state)
        {
            case GhostState.Roam:
                agent.speed = roamSpeed;
                if(agent.isStopped)
                    agent.isStopped = false;

                if (!agent.pathPending && agent.remainingDistance < 0.5f)
                    GoToNextWaypoint();
                
                if(distToPlayer <= chaseRadius)
                {
                    lastPlayerPosition = player.position;
                    state = GhostState.Chase;
                    break;
                }

                if (distToPlayer <= suspiciousRadius)
                {
                    lastPlayerPosition = player.position;
                    state = GhostState.Suspicious;
                }
                break;

            case GhostState.Suspicious:
                agent.speed = roamSpeed;
                if (agent.isStopped)
                    agent.isStopped = false;

                agent.SetDestination(lastPlayerPosition);

                suspiciousTimer += Time.deltaTime;
                if (suspiciousTimer >= suspiciousTime)
                {
                    suspiciousTimer = 0f;
                    state = GhostState.Roam;
                    GoToNextWaypoint();
                }

                if (distToPlayer <= chaseRadius && CanSeePlayer())
                {
                    state = GhostState.Chase;
                }
                break;

            case GhostState.Chase:
                agent.speed = chaseSpeed;
                agent.SetDestination(player.position);

                // Attack check
                if (distToPlayer <= attackDistance)
                {
                    state = GhostState.Attack;
                }

                // Lost sight → suspicious
                if (!CanSeePlayer())
                {
                    lastPlayerPosition = player.position;
                    state = GhostState.Suspicious;
                }
                break;

            case GhostState.Attack:
                agent.isStopped = true;
                if(!isAttacking)
                AttackPlayer();
                break;
        }
    }

    void GoToNextWaypoint()
    {
        if (waypoints.Length == 0) return;
        
        if(followRandomWaypoint)
        currentWaypoint = Random.Range(0, waypoints.Length);
        
        agent.SetDestination(waypoints[currentWaypoint].position);
        currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
    }

    bool CanSeePlayer()
    {
        float visionRadius = 10f;      // jitni door tak ghost dekh sakta
        float visionAngle = 90f;       // field of view

        // distance check
        if (Vector3.Distance(transform.position, player.position) > visionRadius)
            return false;

        // angle check
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dirToPlayer);
        if (angle > visionAngle * 0.5f)
            return false;

        // Line of sight (all check)
        Ray ray = new Ray(transform.position + Vector3.up * 1.5f, dirToPlayer);
        if (Physics.Raycast(ray, out RaycastHit hit, visionRadius))
        {
            if (hit.transform == player)
                return true;
        }

        return false;
    }


    void AttackPlayer()
    {
        isAttacking = true;
        state = GhostState.Roam;

        Debug.Log("Attacking Player!");
        StartCoroutine(AttackCooldown());
    }

    IEnumerator AttackCooldown()
    {
        yield return new WaitForSeconds(2f);
        isAttacking = false;
    }

}

public enum GhostState
{
    Roam,
    Suspicious,
    Chase,
    Attack
}
