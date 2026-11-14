using UnityEngine;
using UnityEngine.UIElements;

public class NPCMovement : MonoBehaviour
{
    private Transform targetPosition;
    private UnityEngine.AI.NavMeshAgent agent;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        FindClosestTarget();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void FindClosestTarget()
    {
        
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        
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
}
