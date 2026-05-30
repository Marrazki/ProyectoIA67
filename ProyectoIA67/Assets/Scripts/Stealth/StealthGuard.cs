using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class StealthGuard : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;

    [Header("Detección")]
    public float detectionRange = 6f;
    [Tooltip("Multiplicador de rango cuando el jugador está en una zona oscura.")]
    [Range(0.1f, 1f)]
    public float darkZoneDetectionMultiplier = 0.6f;

    [Header("Patrulla")]
    public Transform[] waypoints;
    public float patrolSpeed = 2f;
    public float waypointReachDistance = 0.4f;

    [Header("Persecución")]
    public float chaseSpeed = 4f;

    [Header("Investigación")]
    public float investigateSpeed = 3f;
    public float investigateReachDistance = 0.5f;

    [Header("Ataque")]
    public float attackRange = 1.2f;

    [Header("A* navigation")]
    public float pathSpeed = 3f;
    public float pathNodeReachDistance = 0.2f;

    [Header("Alerta global")]
    public bool broadcastAlert = true;

    Blackboard _blackboard;
    SensorBase _sensor;
    BTNode _tree;

    int _waypointIndex;
    Vector3 _currentTarget;
    List<Node> _currentPath;
    int _pathIndex;
    Vector3 _lastRequestedTarget;

    void Start()
    {
        _blackboard = new Blackboard();
        _sensor = new ProximitySensor(transform, player, detectionRange, darkZoneDetectionMultiplier, _blackboard);
        BuildTree();
    }

    void OnEnable()
    {
        AlertSystem.OnAlert += OnGlobalAlert;
    }

    void OnDisable()
    {
        AlertSystem.OnAlert -= OnGlobalAlert;
    }

    void Update()
    {
        if (player == null) return;

        _sensor.Sense();
        _tree?.Tick();
        UpdatePathIfNeeded();
        FollowPath();
    }

    void OnGlobalAlert(Vector3 alertPosition)
    {
        if (_blackboard == null) return;
        _blackboard.Set<Vector3>(BB.LastKnownPosition, alertPosition);
        _blackboard.Set<bool>(BB.HasClue, true);
    }

    void BuildTree()
    {
        _tree = new Selector(
            new Sequence(
                new Condition(LowHealth, "VidaBaja?"),
                new BTAction(Flee, "Huir")
            ) { Name = "Huir si vida baja" },
            new Sequence(
                new Condition(EstaCerca, "EstaCerca?"),
                new BTAction(Attack, "Atacar")
            ) { Name = "Atacar si cerca" },
            new Sequence(
                new Condition(CanSeePlayer, "VeoJugador?"),
                new BTAction(Chase, "Perseguir")
            ) { Name = "Perseguir si veo" },
            new Sequence(
                new Condition(HasClue, "TengoPista?"),
                new BTAction(Investigate, "Investigar")
            ) { Name = "Investigar pista" },
            new Sequence(
                new BTAction(Patrol, "Patrullar")
            ) { Name = "Patrullar" }
        ) { Name = "Guard Root" };
    }

    bool LowHealth() => false;

    bool CanSeePlayer() => _blackboard.Get<bool>(BB.CanSeePlayer);

    bool HasClue() => _blackboard.Get<bool>(BB.HasClue);

    bool EstaCerca()
    {
        if (!_blackboard.Get<bool>(BB.CanSeePlayer)) return false;
        if (!_blackboard.Has(BB.LastKnownPosition)) return false;
        return Vector3.Distance(transform.position, _blackboard.Get<Vector3>(BB.LastKnownPosition)) < attackRange;
    }

    NodeStatus Flee()
    {
        GetComponent<Renderer>().material.color = Color.magenta;
        Vector3 dir = (transform.position - player.position).normalized;
        transform.position += dir * (chaseSpeed * Time.deltaTime);
        transform.LookAt(transform.position + dir);
        return NodeStatus.Running;
    }

    NodeStatus Chase()
    {
        GetComponent<Renderer>().material.color = Color.yellow;
        Vector3 targetPos = _blackboard.Get<Vector3>(BB.LastKnownPosition);
        SetPathTarget(targetPos);
        return NodeStatus.Running;
    }

    NodeStatus Investigate()
    {
        GetComponent<Renderer>().material.color = Color.red;
        Vector3 targetPos = _blackboard.Get<Vector3>(BB.LastKnownPosition);
        SetPathTarget(targetPos);

        if (Vector3.Distance(transform.position, targetPos) < investigateReachDistance)
        {
            _blackboard.Remove(BB.HasClue);
            return NodeStatus.Success;
        }

        return NodeStatus.Running;
    }

    NodeStatus Patrol()
    {
        GetComponent<Renderer>().material.color = Color.cyan;
        if (waypoints == null || waypoints.Length == 0)
            return NodeStatus.Running;

        Vector3 targetPos = waypoints[_waypointIndex].position;
        SetPathTarget(targetPos);

        if (Vector3.Distance(transform.position, targetPos) < waypointReachDistance)
            _waypointIndex = (_waypointIndex + 1) % waypoints.Length;

        return NodeStatus.Running;
    }

    NodeStatus Attack()
    {
        GetComponent<Renderer>().material.color = Color.black;
        transform.LookAt(player);
        return NodeStatus.Running;
    }

    void SetPathTarget(Vector3 target)
    {
        if (_currentTarget == target && _currentPath != null && _pathIndex < _currentPath.Count)
            return;

        _currentTarget = target;
        RequestPath(target);
    }

    void RequestPath(Vector3 destination)
    {
        if (AStarPathfinder.instance == null || GridManager.instance == null)
            return;

        _lastRequestedTarget = destination;
        _currentPath = AStarPathfinder.instance.FindPath(transform.position, destination);
        _pathIndex = 0;
    }

    void UpdatePathIfNeeded()
    {
        if (_currentPath == null || _pathIndex >= _currentPath.Count)
            return;

        if (Vector3.Distance(_currentTarget, _lastRequestedTarget) > 0.5f)
            RequestPath(_currentTarget);
    }

    void FollowPath()
    {
        if (_currentPath == null || _currentPath.Count == 0 || _pathIndex >= _currentPath.Count)
            return;

        Vector3 nodePos = _currentPath[_pathIndex].worldPosition;
        Vector3 target3D = new Vector3(nodePos.x, transform.position.y, nodePos.z);
        transform.position = Vector3.MoveTowards(transform.position, target3D, pathSpeed * Time.deltaTime);

        Vector3 dir = (target3D - transform.position).normalized;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);

        if (Vector3.Distance(transform.position, target3D) < pathNodeReachDistance)
            _pathIndex++;
    }

    void OnDrawGizmos()
    {
        if (_currentPath == null) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < _currentPath.Count; i++)
        {
            Gizmos.DrawSphere(_currentPath[i].worldPosition, 0.1f);
            if (i > 0)
                Gizmos.DrawLine(_currentPath[i - 1].worldPosition, _currentPath[i].worldPosition);
        }
    }
}
