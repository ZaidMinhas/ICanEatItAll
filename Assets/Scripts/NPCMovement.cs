using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;


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
    private Vector3 targetPosition;
    private UnityEngine.AI.NavMeshAgent agent;

    public NPCState currentState;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();

        if (currentState == NPCState.Patrol)
        {
            // Direct children only
            foreach (Transform child in transform)
            {
                patrolPoints.Add(child.position);
                // Debug.Log(child.gameObject.name);
            }
            // Debug.Log(patrolPoints);

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
        // Destination reached
        if(Vector3.Distance(transform.position, targetPosition) < 0.5)
        {
            Debug.Log("At Destination");
            
            switch (currentState)
            {
                case NPCState.Patrol:
                    // Go to next point
                    MoveToNextPoint();
                    break;
            }
            
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
