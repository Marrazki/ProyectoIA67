using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DarkZone : MonoBehaviour
{
    [Tooltip("Cost multiplier to apply to grid nodes inside this zone (>=1). Higher = harder to traverse.")]
    public float costMultiplier = 2f;

    Collider _collider;

    void OnEnable()
    {
        _collider = GetComponent<Collider>();
        Apply();
    }

    void OnDisable()
    {
        Reset();
    }

    void OnValidate()
    {
        _collider = GetComponent<Collider>();
        if (_collider != null && Application.isPlaying)
            Apply();
    }

    void Apply()
    {
        if (GridManager.instance == null || _collider == null) return;
        GridManager.instance.ApplyCostMultiplierToArea(_collider.bounds, Mathf.Max(1f, costMultiplier));
    }

    void Reset()
    {
        if (GridManager.instance == null || _collider == null) return;
        GridManager.instance.ResetCostMultiplierInArea(_collider.bounds);
    }
}
