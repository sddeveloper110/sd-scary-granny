using UnityEngine;
using UnityEngine.AI;

public class GhostAi : MonoBehaviour
{
    NavMeshAgent agent;

    [SerializeField] Transform waypointParent; 
    [SerializeField] float suspiciousTime = 3f; // Time to stay suspicious
    [SerializeField] float attackDistance = 2f; // Distance to attack
    [SerializeField] float chaseSpeed = 5f;
    [SerializeField] float roamSpeed = 2f;

    GhostState state;
    Transform[] waypoints;
    Transform player;
    Vector3 lastPlayerPosition;
    int currentWaypoint = 0;
    float suspiciousTimer = 0f;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        player = FindFirstObjectByType<PlayerController>().transform;

        waypoints = new Transform[waypointParent.childCount];
        for (int i = 0; i < waypoints.Length; i++)
        {
            waypoints[i] = waypointParent.GetChild(i);
        }

        GoToNextWaypoint();
    }


    private void Update()
    {
        switch (state)
        {
            case GhostState.Roam:
                agent.speed = roamSpeed;
                if (!agent.pathPending && agent.remainingDistance < 0.5f)
                {
                    GoToNextWaypoint();
                }

                if (CanSeePlayer())
                {
                    state = GhostState.Chase;
                }
                break;

            case GhostState.Suspicious:
                agent.speed = roamSpeed;
                if (!agent.pathPending && agent.remainingDistance < 0.5f)
                {
                    suspiciousTimer += Time.deltaTime;
                    if (suspiciousTimer >= suspiciousTime)
                    {
                        suspiciousTimer = 0f;
                        state = GhostState.Roam;
                        GoToNextWaypoint();
                    }
                }

                agent.SetDestination(lastPlayerPosition);
                break;

            case GhostState.Chase:
                agent.speed = chaseSpeed;
                agent.SetDestination(player.position);

                float distance = Vector3.Distance(transform.position, player.position);
                if (distance <= attackDistance)
                {
                    state = GhostState.Attack;
                }

                if (!CanSeePlayer())
                {
                    lastPlayerPosition = player.position;
                    state = GhostState.Suspicious;
                }
                break;

            case GhostState.Attack:
                agent.isStopped = true;
                AttackPlayer();
                break;
        }
    }

    void GoToNextWaypoint()
    {
        if (waypoints.Length == 0) return;

        agent.SetDestination(waypoints[currentWaypoint].position);
        currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
    }

    bool CanSeePlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        Ray ray = new Ray(transform.position + Vector3.up, direction);
        if (Physics.Raycast(ray, out RaycastHit hit, 10f))
        {
            if (hit.transform == player)
                return true;
        }
        return false;
    }

    void AttackPlayer()
    {
        Debug.Log("Attacking Player!");
    }
}

public enum GhostState
{
    Roam,
    Suspicious,
    Chase,
    Attack
}
