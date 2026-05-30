using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class EnemyAI : MonoBehaviour
{
    public enum EnemyState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Flee
    }
    
    private EnemyState _currentState = EnemyState.Idle;
    public TextMeshPro exclamation;
    private float _exclamationTime = 2f;
    private float _exclamationTimer = 0f;

    [Header("Iddle Settings")]
    public float idleTime = 3f;
    private float _idleTimer = 0f;

    [Header("Patrol Settings")]
    public Transform[] waypoints;
    private int _currentWaypointIndex = 0;
    public float moveSpeed = 2f;
    public float waypointReachDistance = 0.5f;

    [Header("Chase Settings")]
    public Transform player;
    public float detectionRange = 5f;
    public float chaseSpeed = 4f;

    [Header("Attack Settings")]
    public float attackRange = 1.5f;
    public float attackSpeed = 0f;

    [Header("Flee Settings")]
    public float fleeSpeed = 9f;

    [Header("Health")]
    public float health = 100f;
    public float maxHealth = 100f;
    public float minimumHealth = 0f;

    void Start()
    {
        exclamation.enabled = false;
        _currentState = EnemyState.Idle;
    }

    void Update()
    {
        switch (_currentState)
        {
            case EnemyState.Idle:
                IdleState();
                GetComponent<Renderer>().material.color = Color.green;
                break;
            case EnemyState.Patrol:
                PatrolState();
                GetComponent<Renderer>().material.color = Color.cyan;
                break;
            case EnemyState.Chase:
                ChaseState();
                GetComponent<Renderer>().material.color = Color.yellow;
                break;
            case EnemyState.Attack:
                AttackState();
                GetComponent<Renderer>().material.color = Color.red;
                break;
            case EnemyState.Flee:
                FleeState();
                GetComponent<Renderer>().material.color = Color.magenta;
                break;
        }

        if (health < 30f)
        {
            _currentState = EnemyState.Flee;
        }

        if (health < minimumHealth)
        {
            health = minimumHealth;
        }

        Hurt();
        Regenerate();
    }

    void IdleState()
    {
        _idleTimer += Time.deltaTime;
        if (_idleTimer >= idleTime)
        {
            _currentState = EnemyState.Patrol;
            _idleTimer = 0f;
            Debug.Log("Cambio a Patrol");
        }

        Debug.Log("Estado Idle - Esperando...");
    }

    void PatrolState() 
    {
        if (waypoints.Length == 0)
        {
            _currentState = EnemyState.Idle;
            return;
        }

        Transform targetWaypoint = waypoints[_currentWaypointIndex];
        Vector3 direction = (targetWaypoint.position - transform.position).normalized;
        transform.position += direction * (moveSpeed * Time.deltaTime);
        transform.LookAt(targetWaypoint);

        float distance = Vector3.Distance(transform.position, targetWaypoint.position);
        if (distance < waypointReachDistance)
        {
            _currentWaypointIndex = (_currentWaypointIndex + 1) % waypoints.Length;
        }

        if (CanSeePlayer())
        {
            _currentState = EnemyState.Chase;
            Debug.Log("JUGADOR DETECTADO - Cambio a chase");
        }
    }

    void ChaseState()
    {
        Debug.Log("Estado CHASE - persiguiendo");
        _exclamationTimer += Time.deltaTime;
        if (_exclamationTimer <= _exclamationTime)
        {
            exclamation.enabled = true;
        }
        else
        {
            exclamation.enabled = false;
        }

        Vector3 direction = (player.position - transform.position).normalized;
        transform.position += direction * (chaseSpeed * Time.deltaTime);
        transform.LookAt(player);

        if (!CanSeePlayer())
        {
            _currentState = EnemyState.Patrol;
            _exclamationTimer = 0f;
            exclamation.enabled = false;
        }

        if (CanAttackPlayer())
        { 
            _currentState = EnemyState.Attack;
            _exclamationTimer = 0f;
            exclamation.enabled = false;
        }
    }

    void AttackState()
    {
        Debug.Log("Atacando");
        if (!CanAttackPlayer())
        {
            _currentState = EnemyState.Chase;
        }
    }

    void FleeState()
    {
        if (health > 3f)
        {
            _currentState = EnemyState.Patrol;
        }

        Vector3 fleeDirection = (transform.position - player.position).normalized;
        transform.position += fleeDirection * (chaseSpeed * Time.deltaTime);
    }

    bool CanSeePlayer()
    {
        if (player == null) return false;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        return distanceToPlayer < detectionRange;
    }

    bool CanAttackPlayer()
    {
        if (player == null) return false;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        return distanceToPlayer < attackRange;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        if (player != null && CanSeePlayer())
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.DrawLine(transform.position, player.position);
        }
    }

    void Regenerate()
    {
        if (health < 100)
        {
            health += 10f * Time.deltaTime;
        }
    }

    void Hurt()
    {
        if (Keyboard.current.qKey.isPressed)
        {
            health -= 1f;
        }
    }
}
