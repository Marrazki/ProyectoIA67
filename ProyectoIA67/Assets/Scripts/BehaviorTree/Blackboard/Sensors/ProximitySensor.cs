using UnityEngine;

public class ProximitySensor : SensorBase
{
    readonly Transform _origin;
    readonly Transform _player;
    readonly float _baseRange;
    readonly float _darkRangeMultiplier;

    public ProximitySensor(Transform origin, Transform player, float baseRange, float darkRangeMultiplier, Blackboard blackboard)
        : base(blackboard)
    {
        _origin = origin;
        _player = player;
        _baseRange = baseRange;
        _darkRangeMultiplier = Mathf.Clamp01(darkRangeMultiplier);
    }

    public override void Sense()
    {
        if (_player == null)
        {
            _blackboard.Set<bool>(BB.CanSeePlayer, false);
            return;
        }

        float effectiveRange = _baseRange;
        if (GridManager.instance != null)
        {
            Node playerNode = GridManager.instance.NodeFromWorldPoint(_player.position);
            if (playerNode != null && playerNode.costMultiplier > 1f)
                effectiveRange *= _darkRangeMultiplier; // reduce detection range in darkness
        }

        if (Vector3.Distance(_origin.position, _player.position) < effectiveRange)
        {
            _blackboard.Set<bool>(BB.CanSeePlayer, true);
            _blackboard.Set<Vector3>(BB.LastKnownPosition, _player.position);
            _blackboard.Set<bool>(BB.HasClue, true);

            // Alert other guards globally
            AlertSystem.BroadcastAlert(_player.position);
        }
        else
        {
            _blackboard.Set<bool>(BB.CanSeePlayer, false);
        }
    }
}
