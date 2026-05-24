using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PersonAgent : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;
    private Transform target;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        
        // Find all rooms
        GameObject[] rooms = GameObject.FindGameObjectsWithTag("Finish");
        if (rooms.Length > 0)
        {
            target = rooms[Random.Range(0, rooms.Length)].transform;
            agent.SetDestination(target.position);
        }
    }

    void Update()
    {
        if (animator != null)
        {
            float speed = agent.velocity.magnitude / agent.speed;
            animator.SetFloat("Speed", speed);
        }

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            // Reached classroom, could destroy or idle
            // For now, let's just make them idle/stop
            agent.isStopped = true;
        }
    }
}
