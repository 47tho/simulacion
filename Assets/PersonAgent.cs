using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PersonAgent : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;
    
    private RoomData currentRoom;
    private Transform spawnPoint;
    private float stayTime;
    private float timer;
    private bool isStaying;
    private bool isReturning;
    
    private float minStay;
    private float maxStay;
    private float returnChance;

    public void Initialize(RoomData room, Transform spawn, float min, float max, float chance)
    {
        currentRoom = room;
        spawnPoint = spawn;
        minStay = min;
        maxStay = max;
        returnChance = chance;
        
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        
        if (currentRoom != null && currentRoom.transform != null)
        {
            currentRoom.currentOccupancy++;
            
            // Wait a frame to ensure agent is on NavMesh before setting destination
            StartCoroutine(SetInitialDestination());
        }
        else
        {
            Debug.LogWarning("NPC Initialized without a valid room target.");
        }
    }

    System.Collections.IEnumerator SetInitialDestination()
    {
        yield return new WaitForSeconds(0.1f);
        
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        
        // Ensure agent is enabled
        agent.enabled = true;
        
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }

        if (currentRoom != null && currentRoom.transform != null)
        {
            bool success = agent.SetDestination(currentRoom.transform.position);
            agent.isStopped = false;
            Debug.Log($"NPC {gameObject.name} heading to {currentRoom.name}. Success: {success}");
        }
        else
        {
            Debug.LogError($"NPC {gameObject.name} initialized without target room.");
        }
    }

    void Update()
    {
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        float speed = agent.velocity.magnitude / agent.speed;
        if (animator != null)
        {
            animator.SetFloat("Speed", speed);
            animator.SetBool("IsGrounded", true);
        }

        if (!agent.pathPending && agent.remainingDistance < 0.8f)
        {
            if (!isStaying && !isReturning)
            {
                StartStaying();
            }
            else if (isStaying)
            {
                timer += Time.deltaTime;
                if (timer >= stayTime)
                {
                    OnStayFinished();
                }
            }
            else if (isReturning)
            {
                // Reached spawn point, we can recycle or destroy
                Destroy(gameObject);
            }
        }
    }

    void StartStaying()
    {
        isStaying = true;
        timer = 0;
        stayTime = Random.Range(minStay, maxStay);
        agent.isStopped = true;
    }

    void OnStayFinished()
    {
        isStaying = false;
        if (currentRoom != null) currentRoom.currentOccupancy--;
        
        if (Random.value < returnChance)
        {
            isReturning = true;
            agent.isStopped = false;
            agent.SetDestination(spawnPoint.position);
        }
        else
        {
            // Maybe find another room? For now just stay idle or destroy
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (isStaying && currentRoom != null)
        {
            currentRoom.currentOccupancy--;
        }
    }
}
