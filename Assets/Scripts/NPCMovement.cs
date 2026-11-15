using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum NPCState { Patrol, Flee, FindFood }
public enum NPCRole { Server, Stealer }

[RequireComponent(typeof(NavMeshAgent))]
public class NPCMovement : MonoBehaviour
{
    [SerializeField] private NPCRole role = NPCRole.Server;
    public NPCState currentState;
    public Transform player;
    [Min(0.1f)] public float fleeDistance = 5f;
    [Min(0.1f)] public float stealingDistance = 1.5f;
    [SerializeField] private List<Vector3> patrolPoints = new();

    private NavMeshAgent agent;
    private GameObject targetFood;
    private GameObject foodGrabbed;
    [SerializeField] private GameObject[] spawnableFoods = {};
    [SerializeField] private float foodOffset = 0.5f;
    [SerializeField] private float handsHeight = 0.8f;
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
        if (role == NPCRole.Stealer) currentState = NPCState.FindFood;
        else currentState = NPCState.Patrol;
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
            if (currentState == NPCState.Flee) GameSounds.Play("Flee", transform.position);
        }

        switch (currentState)
        {
            case NPCState.Patrol: Patrol(); break;
            case NPCState.Flee: Flee(); break;
            case NPCState.FindFood: FindFood(); break;
        }
    }

    void LateUpdate()
    {
        if (foodGrabbed != null)
        {
            foodGrabbed.transform.position = transform.position + transform.forward * foodOffset + Vector3.up * handsHeight;
        }
    }

    void OnDestroy()
    {
        if (foodGrabbed != null) Destroy(foodGrabbed);
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

        // Once safe, finish the stolen food and go back to the normal job.
        if (foodGrabbed != null)
        {
            Destroy(foodGrabbed);
            foodGrabbed = null;
        }

        if (role == NPCRole.Server)
        {
            currentState = NPCState.Patrol;
            return;
        }

        bool hasFood = targetFood != null && targetFood.activeInHierarchy && !targetFood.GetComponent<EdibleItem>().IsConsumed;
        if (!hasFood) hasFood = FindClosestFood() != null;
        currentState = hasFood ? NPCState.FindFood : NPCState.Patrol;
    }

    void Patrol()
    {
        if (patrolPoints.Count == 0) return;

        // Destination reached
        if (agent.hasPath && !agent.pathPending && agent.pathStatus == NavMeshPathStatus.PathComplete &&
            agent.remainingDistance <= Mathf.Max(0.5f, agent.stoppingDistance))
        {
            // Spawn Food
            if (role == NPCRole.Server) { SpawnFood(); }

            // Go to next point
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
        if (away.magnitude >= fleeDistance)
        {
            agent.ResetPath();
            return;
        }
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
        if (targetFood == null || !targetFood.activeInHierarchy || targetFood.GetComponent<EdibleItem>().IsConsumed)
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
            GameSounds.Play("Steal", transform.position);
            foodGrabbed = targetFood;
            // Claim the food so another NPC or the player cannot take it too.
            foodGrabbed.GetComponent<EdibleItem>().MarkConsumed();
            foodGrabbed.transform.SetParent(null, true);
            foreach (Collider foodCollider in foodGrabbed.GetComponentsInChildren<Collider>())
                foodCollider.enabled = false;
            targetFood = null;
            agent.ResetPath();
            currentState = NPCState.Flee;
        }
    }

    GameObject FindClosestFood()
    {
        GameObject closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (GameObject food in GameObject.FindGameObjectsWithTag("NPCTarget"))
        {
            EdibleItem edible = food.GetComponent<EdibleItem>();
            if (edible == null || edible.IsConsumed) continue;
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

    void SpawnFood()
    {
        if (spawnableFoods.Length == 0) return;
        int itemID = Random.Range(0, spawnableFoods.Length);
        GameObject food = Instantiate(spawnableFoods[itemID], patrolPoints[patrolIndex], Quaternion.identity);
        food.tag = "NPCTarget";
        if (food.GetComponent<Collider>() == null) food.AddComponent<BoxCollider>();
        if (food.GetComponent<EdibleItem>() == null) food.AddComponent<EdibleItem>();
    }
}
