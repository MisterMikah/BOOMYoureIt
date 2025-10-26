using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BombTugOfWarUI : MonoBehaviour
{
    [Header("Refs")]
    public BombManager bombManager;        // drag BombManager in scene
    public Image leftFill;                 // P1 color (Filled/Horizontal/Left)
    public Image rightFill;                // P2 color (Filled/Horizontal/Right)
    public RectTransform handle;           // bomb icon on the seam
    public TextMeshProUGUI timerText;      // optional countdown

    [Header("Behavior")]
    [Tooltip("How fast the meter drifts (0..1) per second toward the non-bomb side.")]
    public float driftPerSecond = 0.15f;

    [Tooltip("Extra shove toward the non-bomb side on explosion.")]
    public float explosionShove = 0.15f;

    [Tooltip("Smooths the handle movement.")]
    public float handleLerp = 12f;

    // 0 = fully left (P1 advantage), 1 = fully right (P2 advantage)
    [Range(0f, 1f)] public float value = 0.5f;

    RectTransform barRect;

    void Awake()
    {
        barRect = GetComponent<RectTransform>();
        if (!bombManager) bombManager = FindFirstObjectByType<BombManager>();
    }

    void OnEnable()
    {
        BombManager.OnBombExplode += OnBombExplode;
        BombManager.OnCarrierSet += OnCarrierSet;
    }

    void OnDisable()
    {
        BombManager.OnBombExplode -= OnBombExplode;
        BombManager.OnCarrierSet -= OnCarrierSet;
    }

    void Update()
    {
        if (!bombManager || bombManager.CurrentCarrier == null) return;

        // Drift: carrier loses, so value moves toward the OTHER side
        // Assume players[0] = left, players[1] = right
        bool carrierIsLeft = (bombManager.CurrentCarrier == bombManager.players[0]);
        float dir = carrierIsLeft ? +1f : -1f; // +1 drives value right, -1 drives value left
        value = Mathf.Clamp01(value + dir * driftPerSecond * Time.deltaTime);

        // Fills using the SAME convention as the handle
        if (leftFill) leftFill.fillAmount = value;        // 0..1 from left
        if (rightFill) rightFill.fillAmount = 1f - value;   // 1..0 from right

        // Optional timer
        if (timerText)
            timerText.text = Mathf.CeilToInt(bombManager.RemainingTime).ToString();

        // Place the handle exactly on the seam
        UpdateHandleAtSeam();
    }

    void OnBombExplode(BombCarrier carrier)
    {
        if (!bombManager || bombManager.players == null || bombManager.players.Length < 2) return;

        bool carrierIsLeft = (carrier == bombManager.players[0]);
        float shoveDir = carrierIsLeft ? +1f : -1f; // shove toward non-bomb side
        value = Mathf.Clamp01(value + shoveDir * explosionShove);
    }

    void OnCarrierSet(BombCarrier newCarrier)
    {
        // Tiny visual nudge when a tag happens
        bool carrierIsLeft = (newCarrier == bombManager.players[0]);
        float nudge = 0.02f * (carrierIsLeft ? +1f : -1f);
        value = Mathf.Clamp01(value + nudge);
    }

    void UpdateHandleAtSeam()
    {
        if (!handle || !barRect) return;

        float width = barRect.rect.width;
        float half = width * 0.5f;

        // Seam position from -half (left) to +half (right)
        float targetX = Mathf.Lerp(-half, +half, value);

        // Optional padding if the icon clips the bar edges
        const float padding = 0f;
        targetX = Mathf.Clamp(targetX, -half + padding, +half - padding);

        // Smooth + pixel snap
        var pos = handle.anchoredPosition;
        pos.x = Mathf.Lerp(pos.x, targetX, Mathf.Clamp01(handleLerp * Time.deltaTime));
        pos.x = Mathf.Round(pos.x);
        handle.anchoredPosition = pos;
    }
}
