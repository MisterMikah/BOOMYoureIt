using UnityEngine;

[DisallowMultipleComponent]
public class ForceSolidRootCollider2D : MonoBehaviour
{
    Collider2D col;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        if (!col) col = gameObject.AddComponent<BoxCollider2D>();
        col.isTrigger = false;
        Debug.Log($"[ForceSolid] Awake -> {col.name} isTrigger={col.isTrigger}");
    }

    void Start()
    {
        if (col) { col.isTrigger = false; }
        Debug.Log($"[ForceSolid] Start -> {col.name} isTrigger={col.isTrigger}");
    }

    void LateUpdate()
    {
        if (!col) return;
        if (col.isTrigger)
        {
            col.isTrigger = false;
            Debug.LogWarning("[ForceSolid] Someone set root collider to Trigger at runtime; forcing it back to solid.");
        }
    }

#if UNITY_EDITOR
    // Also enforce in Edit Mode when values change in Inspector
    void OnValidate()
    {
        var c = GetComponent<Collider2D>();
        if (c && c.isTrigger) c.isTrigger = false;
    }
#endif
}
