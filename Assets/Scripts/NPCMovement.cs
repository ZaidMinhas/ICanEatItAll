using System.Collections.Generic;
using UnityEngine;


public enum NPCState
{
    Patrol,
    Flee,
    FindFood
}

public class NPCMovement : MonoBehaviour
{
    [SerializeField]
    private List<Vector3> patrolPoints = new();
    private Vector3 currentPoint;
    private int currentPointID = 0;
    private UnityEngine.AI.NavMeshAgent agent;

    public NPCState currentState;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();

        if (agent == null || !agent.isOnNavMesh)
        {
            Debug.LogWarning("NPC needs a NavMeshAgent placed on a baked NavMesh.", this);
            enabled = false;
            return;
        }

        if (currentState == NPCState.Patrol)
        {
            // Direct children only
            foreach (Transform child in transform)
            {
                patrolPoints.Add(child.position);
                // Debug.Log(child.gameObject.name);
            }
            // Debug.Log(patrolPoints);

            if (patrolPoints.Count == 0)
            {
                Debug.LogWarning("NPC needs at least one patrol point.", this);
                enabled = false;
                return;
            }

            currentPoint = patrolPoints[0];
            // Debug.Log(currentPoint);
            // Debug.Log(agent);
            // Debug.Log(agent.destination);
            agent.destination = currentPoint;
        }
        else
        {
            FindClosestTarget();
        }
    }

    void FixedUpdate()
    {
        // Wait until the agent has calculated its route to the actual patrol point.
        if (currentState != NPCState.Patrol || !agent.isOnNavMesh || agent.pathPending)
        {
            return;
        }

        // Remaining distance follows the path and ignores the NPC's height offset.
        if (agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathComplete &&
            agent.remainingDistance <= Mathf.Max(0.5f, agent.stoppingDistance))
        {
            MoveToNextPoint();
        }
    }

    void FindClosestTarget()
    {   
        // Search for closest target
        float closestDistance = 1000000.0f;
        GameObject closestTarget = null;

        GameObject[] targets = GameObject.FindGameObjectsWithTag("NPCTarget");

        foreach (GameObject potentialTarget in targets)
        {
            print("Target: " + potentialTarget.name);
            float distance = Vector3.Distance(transform.position, potentialTarget.transform.position);
            print("Distance: " + distance);

            if (distance < closestDistance)
            {
                closestTarget = potentialTarget;
                closestDistance = distance;
            }
        }   

        if (closestTarget)
        {
            print("Target: " + closestTarget.name);
            agent.destination = closestTarget.transform.position;
        }
    }

    void MoveToNextPoint()
    {
        currentPointID += 1;
        if (currentPointID > patrolPoints.Count - 1)
        {
            currentPointID = 0;
        }
        currentPoint = patrolPoints[currentPointID];
        agent.destination = currentPoint;
    }
}
