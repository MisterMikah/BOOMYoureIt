using UnityEngine;

public class BombTagger : MonoBehaviour
{
    public BombCarrier selfCarrier; // will be auto-filled

    void Awake()
    {
        if (!selfCarrier) selfCarrier = GetComponentInParent<BombCarrier>();
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true; // ensure it's a trigger
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var bm = Object.FindFirstObjectByType<BombManager>();
        if (!bm || bm.CurrentCarrier != selfCarrier) return;

        var target = other.GetComponentInParent<BombCarrier>();
        if (!target || target == selfCarrier) return;

        bm.TryTransferTo(target); // cooldown inside BombManager prevents ping-pong
    }
}
