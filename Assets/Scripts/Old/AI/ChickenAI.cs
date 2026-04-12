using UnityEngine;
using UnityEngine.AI;

public class ChickenAI : MonoBehaviour
{
    public enum State
    {
        Idle,
        Wander,
        Flee
    }

    private State currentState;

    private float vert;
    private float animState;

    public Animator animator;
    private NavMeshAgent agent;
    private Transform currentTarget;

    [Header("Movement")]
    public float wanderRadius = 10f;
    public float moveSpeed = 2f;
    public float runSpeed = 6f;

    [Header("Idle")]
    public float minIdleTime = 1f;
    public float maxIdleTime = 3f;
    private float idleTimer;

    [Header("Detection")]
    public float detectDistance = 6f;
    public float loseDistance = 8f;

    private Vector3 wanderTarget;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        agent.angularSpeed = 120f;
        agent.acceleration = 8f;
        agent.stoppingDistance = 0.2f;

        ChangeState(State.Idle);
    }

    void Update()
    {
        currentTarget = FindClosestPlayer();
        float distance = currentTarget != null
            ? Vector3.Distance(transform.position, currentTarget.position)
            : float.PositiveInfinity;

        // GLOBAL TRANSITIONS
        if (distance < detectDistance)
        {
            ChangeState(State.Flee);
        }
        else if (currentState == State.Flee && distance > loseDistance)
        {
            ChangeState(State.Idle);
        }

        // STATE LOGIC
        switch (currentState)
        {
            case State.Idle:
                UpdateIdle();
                break;

            case State.Wander:
                UpdateWander();
                break;

            case State.Flee:
                UpdateFlee();
                break;
        }

        UpdateAnimation();
    }

    // ===================== STATES =====================

    void UpdateIdle()
    {
        agent.isStopped = true;

        idleTimer -= Time.deltaTime;
        if (idleTimer <= 0f)
        {
            ChangeState(State.Wander);
        }
    }

    void UpdateWander()
    {
        agent.isStopped = false;
        agent.speed = moveSpeed;

        if (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance)
        {
            // pick new random point
            Vector3 randomDir = Random.insideUnitSphere * wanderRadius + transform.position;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDir, out hit, wanderRadius, 1))
            {
                wanderTarget = hit.position;
                agent.SetDestination(wanderTarget);
            }

            // go idle after reaching
            ChangeState(State.Idle);
        }
    }

    void UpdateFlee()
    {
        if (currentTarget == null)
        {
            ChangeState(State.Idle);
            return;
        }

        agent.isStopped = false;
        agent.speed = runSpeed;

        Vector3 direction = (transform.position - currentTarget.position).normalized;
        Vector3 fleeTarget = transform.position + direction * wanderRadius;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(fleeTarget, out hit, wanderRadius, 1))
        {
            agent.SetDestination(hit.position);
        }
    }

    // ===================== STATE SWITCH =====================

    void ChangeState(State newState)
    {
        if (currentState == newState) return;

        currentState = newState;

        switch (newState)
        {
            case State.Idle:
                idleTimer = Random.Range(minIdleTime, maxIdleTime);
                break;

            case State.Wander:
                break;

            case State.Flee:
                break;
        }
    }

    // ===================== ANIMATION =====================

    void UpdateAnimation()
    {
        vert = agent.velocity.magnitude;

        if (currentState == State.Flee)
            animState = 1;
        else if (currentState == State.Wander)
            animState = 0.5f;
        else
            animState = 0;

        animator.SetFloat("Vert", vert);
        animator.SetFloat("State", animState);
    }

    Transform FindClosestPlayer()
    {
        Transform closest = null;
        float closestDistanceSqr = float.PositiveInfinity;

        foreach (PlayerController player in PlayerController.ActivePlayers)
        {
            if (player == null) continue;

            float distanceSqr = (player.transform.position - transform.position).sqrMagnitude;
            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closest = player.transform;
            }
        }

        return closest;
    }
}
