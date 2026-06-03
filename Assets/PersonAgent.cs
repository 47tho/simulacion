using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PersonAgent : MonoBehaviour
{
    private enum AgentState { WalkingToRoom, Staying, Returning, Idle }
    private AgentState currentState = AgentState.Idle;

    private NavMeshAgent agent;
    private Animator animator;
    
    private RoomData currentRoom;
    private Transform spawnPoint;
    private float stayTime;
    private float timer;
    
    private float minStay;
    private float maxStay;
    private float returnChance;
    
    private float stateStartTime;
    private Vector3 lastPosition;
    private float stuckTimer;

    public void Initialize(RoomData room, Transform spawn, float min, float max, float chance)
    {
        currentRoom = room;
        spawnPoint = spawn;
        minStay = min;
        maxStay = max;
        returnChance = chance;
        
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        
        // 1. Reset agent
        agent.enabled = false;
        
        // 2. Snap to NavMesh
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 15.0f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
        }
        
        // 3. Enable and Configure
        agent.enabled = true;
        if (agent.isOnNavMesh)
        {
            agent.Warp(transform.position);
        }
        
        // Reduce radius to allow more fluid movement in crowds
        agent.radius = 0.4f;
        agent.avoidancePriority = Random.Range(1, 99);
        agent.isStopped = false;

        if (currentRoom != null && currentRoom.transform != null)
        {
            currentRoom.currentOccupancy++;
            SetMoveTarget(currentRoom.transform.position, AgentState.WalkingToRoom);
        }
        else
        {
            currentState = AgentState.Idle;
        }
        
        lastPosition = transform.position;
        stuckTimer = 0;
    }

    private void SetMoveTarget(Vector3 targetPos, AgentState newState)
    {
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 15.0f, NavMesh.AllAreas))
        {
            if (agent.SetDestination(hit.position))
            {
                currentState = newState;
                stateStartTime = Time.time;
                agent.isStopped = false;
            }
            else
            {
                // If it fails, we still try to be in walking state, CheckStuck will handle it
                currentState = newState;
                stateStartTime = Time.time;
            }
        }
    }

    void Update()
    {
        // CRITICAL: Check agent validity before anything else
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        UpdateAnimation();

        switch (currentState)
        {
            case AgentState.WalkingToRoom:
                if (CheckStuck()) return; // Stop update if destroyed
                
                // Extra guard for remainingDistance
                if (agent.isOnNavMesh && !agent.pathPending && agent.hasPath)
                {
                    if (agent.remainingDistance <= agent.stoppingDistance + 0.4f)
                    {
                        StartStaying();
                    }
                }
                break;

            case AgentState.Staying:
                timer += Time.deltaTime;
                if (timer >= stayTime)
                {
                    OnStayFinished();
                }
                break;

            case AgentState.Returning:
                if (CheckStuck()) return; // Stop update if destroyed
                
                if (agent.isOnNavMesh && !agent.pathPending && agent.hasPath)
                {
                    if (agent.remainingDistance <= agent.stoppingDistance + 0.4f)
                    {
                        Destroy(gameObject);
                    }
                }
                break;
        }
    }

    private bool CheckStuck()
    {
        if (!agent.isOnNavMesh) return false;

        // If path becomes invalid, try to re-calculate
        if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
                if (currentState == AgentState.WalkingToRoom && currentRoom != null)
                    agent.SetDestination(currentRoom.transform.position);
                else if (currentState == AgentState.Returning && spawnPoint != null)
                    agent.SetDestination(spawnPoint.position);
            }
        }

        // If we haven't moved much in the last 2 seconds
        if (Vector3.Distance(transform.position, lastPosition) < 0.05f)
        {
            stuckTimer += Time.deltaTime;
            
            // Periodically retry pathfinding if stuck
            if (stuckTimer > 2.0f && stuckTimer % 2.0f < Time.deltaTime)
            {
                 if (currentState == AgentState.WalkingToRoom && currentRoom != null)
                    agent.SetDestination(currentRoom.transform.position);
                 else if (currentState == AgentState.Returning && spawnPoint != null)
                    agent.SetDestination(spawnPoint.position);
            }

            // If stuck for 10 seconds, remove from simulation to clear the way
            if (stuckTimer > 10.0f)
            {
                Debug.LogWarning($"NPC {name} permanently stuck at {transform.position} trying to reach {(currentState == AgentState.WalkingToRoom ? currentRoom.name : "Spawn")}. Removing.");
                Destroy(gameObject);
                return true;
            }
        }
        else
        {
            stuckTimer = 0;
        }
        lastPosition = transform.position;
        return false;
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        float speed = agent.velocity.magnitude / agent.speed;
        
        // Show walking animation if path is pending or we are stuck
        if ((currentState == AgentState.WalkingToRoom || currentState == AgentState.Returning) && (agent.pathPending || (speed < 0.1f && stuckTimer > 0)))
        {
            speed = 0.5f; 
        }

        animator.SetFloat("Speed", speed);
        animator.SetBool("IsGrounded", true);
    }

    void StartStaying()
    {
        currentState = AgentState.Staying;
        timer = 0;
        stayTime = Random.Range(minStay, maxStay);
        agent.isStopped = true;
        if (animator != null) animator.SetFloat("Speed", 0f);
    }

    void OnStayFinished()
    {
        if (currentRoom != null) currentRoom.currentOccupancy--;
        
        if (Random.value < returnChance && spawnPoint != null)
        {
            SetMoveTarget(spawnPoint.position, AgentState.Returning);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ForceReturn()
    {
        if (currentState == AgentState.Returning || currentState == AgentState.Idle) return;
        
        if (currentState == AgentState.Staying && currentRoom != null)
        {
            currentRoom.currentOccupancy--;
        }
        
        if (spawnPoint != null)
        {
            SetMoveTarget(spawnPoint.position, AgentState.Returning);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}