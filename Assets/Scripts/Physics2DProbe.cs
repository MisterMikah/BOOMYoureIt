using UnityEngine;

public class Physics2DProbeFull : MonoBehaviour
{
    public string floorLayerName = "Floor";

    void Start()
    {
        var rb = GetComponent<Rigidbody2D>();
        Debug.Log($"[Probe] RB2D bodyType={rb.bodyType}, simulated={rb.simulated}, gravityScale={rb.gravityScale}, collisionDetection={rb.collisionDetectionMode}");

        // List ALL colliders in this hierarchy (root + children)
        var allCols = GetComponentsInChildren<Collider2D>(true);
        Debug.Log($"[Probe] Player has {allCols.Length} Collider2D(s):");
        foreach (var c in allCols)
            Debug.Log($"   - {c.name}  layer={LayerMask.LayerToName(c.gameObject.layer)}  isTrigger={c.isTrigger}  enabled={c.enabled}");

        // The collider attached to THIS object (same as RB2D)
        var rootCol = GetComponent<Collider2D>();
        Debug.Log($"[Probe] ROOT collider -> {(rootCol ? rootCol.name : "NONE")}  isTrigger={(rootCol ? rootCol.isTrigger : false)}  layer={(rootCol ? LayerMask.LayerToName(rootCol.gameObject.layer) : "n/a")}");

        // Is the matrix ignoring Player × Floor?
        int playerLayer = gameObject.layer;
        int floorLayer = LayerMask.NameToLayer(floorLayerName);
        Debug.Log($"[Probe] IgnoreLayerCollision(Player, {floorLayerName})? = {Physics2D.GetIgnoreLayerCollision(playerLayer, floorLayer)}");

        // Quick downward ray to see what we stand on
        var hit = Physics2D.Raycast(transform.position, Vector2.down, 5f, 1 << floorLayer);
        Debug.Log(hit.collider
            ? $"[Probe] Raycast down hit '{hit.collider.name}' on layer '{LayerMask.LayerToName(hit.collider.gameObject.layer)}' at distance {hit.distance}"
            : "[Probe] Raycast down found NO Floor within 5 units");

        // Small overlap at feet for floor
        var overlaps = Physics2D.OverlapCircleAll(transform.position + Vector3.down * 0.5f, 0.6f, 1 << floorLayer);
        Debug.Log($"[Probe] Overlap feet vs Floor: {overlaps.Length} collider(s)");
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        Debug.Log($"[Probe] OnCollisionEnter2D with '{c.collider.name}' layer '{LayerMask.LayerToName(c.collider.gameObject.layer)}'  isTrigger={c.collider.isTrigger}");
    }
}
