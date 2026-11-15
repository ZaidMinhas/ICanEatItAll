using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum NPCState { Patrol, Flee, FindFood }

[RequireComponent(typeof(NavMeshAgent))]
public class NPCMovement : MonoBehaviour
{
    public NPCState currentState;
    public Transform player;
    [Min(0.1f)] public float fleeDistance = 5f;
    [Min(0.1f)] public float stealingDistance = 1.5f;
    [SerializeField] private List<Vector3> patrolPoints = new();

    private NavMeshAgent agent;
    private GameObject targetFood;
    private int patrolIndex;
    private NPCState previousState;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) player = playerObject.transform;
        }
        // Child objects mark the patrol positions when the game starts.
        foreach (Transform point in transform) patrolPoints.Add(point.position);
    }

    void Update()
    {
        if (!agent.isOnNavMesh) return;
        ChooseState();

        // Forget the old destination when choosing a different state.
        if (currentState != previousState)
        {
            agent.ResetPath();
            targetFood = null;
            previousState = currentState;
        }

        switch (currentState)
        {
            case NPCState.Patrol: Patrol(); break;
            case NPCState.Flee: Flee(); break;
            case NPCState.FindFood: FindFood(); break;
        }
    }

    void ChooseState()
    {
        if (player != null)
        {
            Vector3 away = transform.position - player.position;
            away.y = 0;
            // Start fleeing nearby, but keep running until there is a bigger gap.
            float dangerDistance = fleeDistance / 2f;
            if (currentState == NPCState.Flee) dangerDistance = fleeDistance;
            if (away.magnitude < dangerDistance)
            {
                currentState = NPCState.Flee;
                return;
            }
        }

        bool hasFood = targetFood != null && targetFood.activeInHierarchy;
        if (!hasFood) hasFood = FindClosestFood() != null;
        currentState = hasFood ? NPCState.FindFood : NPCState.Patrol;
    }

    void Patrol()
    {
        if (patrolPoints.Count == 0) return;

        if (agent.hasPath && !agent.pathPending && agent.pathStatus == NavMeshPathStatus.PathComplete &&
            agent.remainingDistance <= Mathf.Max(0.5f, agent.stoppingDistance))
        {
            patrolIndex++;
            if (patrolIndex == patrolPoints.Count) patrolIndex = 0;
        }
        agent.SetDestination(patrolPoints[patrolIndex]);
    }

    void Flee()
    {
        if (player == null) { agent.ResetPath(); return; }

        Vector3 away = transform.position - player.position;
        away.y = 0;
        if (away.magnitude >= fleeDistance) { agent.ResetPath(); return; }
        if (away == Vector3.zero) away = transform.forward;

        // Choose walkable ground away from the player.
        Vector3 destination = transform.position + away.normalized * fleeDistance;
        if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 2f, agent.areaMask))
            agent.SetDestination(hit.position);
        else
            agent.ResetPath();
    }

    void FindFood()
    {
        if (targetFood == null || !targetFood.activeInHierarchy)
        {
            targetFood = FindClosestFood();
            agent.ResetPath();
            if (targetFood != null) agent.SetDestination(targetFood.transform.position);
        }
        if (targetFood == null || agent.pathPending) return;

        // Give up if the route becomes blocked.
        if (agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            targetFood = null;
            agent.ResetPath();
            return;
        }

        // Check the path too, so the NPC cannot steal through a wall.
        if (agent.remainingDistance <= stealingDistance &&
            Vector3.Distance(transform.position, targetFood.transform.position) <= stealingDistance)
        {
            targetFood.SetActive(false);
            Destroy(targetFood);
            targetFood = null;
            agent.ResetPath();
        }
    }

    GameObject FindClosestFood()
    {
        GameObject closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (GameObject food in GameObject.FindGameObjectsWithTag("NPCTarget"))
        {
            if (food.GetComponent<EdibleItem>() == null) continue;
            float distance = Vector3.Distance(transform.position, food.transform.position);
            if (distance >= closestDistance) continue;

            // Ignore food the NPC cannot reach.
            NavMeshPath path = new NavMeshPath();
            if (!agent.CalculatePath(food.transform.position, path) ||
                path.status != NavMeshPathStatus.PathComplete) continue;

            closest = food;
            closestDistance = distance;
        }
        return closest;
    }
}
