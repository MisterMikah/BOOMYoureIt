using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BombTugOfWarUI : MonoBehaviour
{
    [Header("Refs")]
    public BombManager bombManager;
    public Image leftFill;                 // Filled/Horizontal/Origin=Left
    public Image rightFill;                // Filled/Horizontal/Origin=Right
    public RectTransform handle;           // bomb icon at the seam
    public TextMeshProUGUI timerText;      // optional bomb timer (playing only)

    [Header("Behavior")]
    public float driftPerSecond = 0.15f;   // carrier loses → drift toward carrier side
    public float explosionShove = 0.2f;    // extra shove on explode (toward carrier side)
    public float handleLerp = 12f;

    // Convention: 0 = full LEFT side empty (right full), 1 = left full
    // We show left fill = value, right fill = 1 - value.
    [Range(0f, 1f)] public float value = 0.5f;

    RectTransform barRect;
    bool isPlaying = false;

    void Awake()
    {
        barRect = GetComponent<RectTransform>();
        if (!bombManager) bombManager = FindFirstObjectByType<BombManager>();
    }

    void OnEnable()
    {
        BombManager.OnBombExplode += OnBombExplode;
        BombManager.OnCarrierSet += OnCarrierSet;
        BombManager.OnPhaseChanged += OnPhaseChanged;

        var bm = bombManager ?? FindFirstObjectByType<BombManager>();
        if (bm != null) OnPhaseChanged(bm.CurrentPhaseName); // init
        ApplyFills();
        UpdateHandleAtSeam();
    }

    void OnDisable()
    {
        BombManager.OnBombExplode -= OnBombExplode;
        BombManager.OnCarrierSet -= OnCarrierSet;
        BombManager.OnPhaseChanged -= OnPhaseChanged;
    }

    void Update()
    {
        // move the handle/fills if needed for display, but DO NOT drift unless playing
        if (!isPlaying || bombManager == null || bombManager.CurrentCarrier == null)
        {
            ApplyFills();
            UpdateHandleAtSeam();
            return;
        }

        // Drift only in Playing:
        bool carrierIsLeft = (bombManager.CurrentCarrier == bombManager.players[0]);
        float dir = carrierIsLeft ? -1f : +1f;   // per your spec: left holds → bar moves left
        value = Mathf.Clamp01(value + dir * driftPerSecond * Time.deltaTime);

        ApplyFills();
        UpdateHandleAtSeam();
    }

    void OnPhaseChanged(string phase)
    {
        if (phase == "Countdown")
        {
            isPlaying = false;
            value = 0.5f;            // reset immediately when respawn → countdown
            ApplyFills();
            UpdateHandleAtSeam();
        }
        else if (phase == "Playing")
        {
            isPlaying = true;
        }
        else // PostExplosion
        {
            isPlaying = false;       // keep last value visible, but no drift
        }
    }



    void OnBombExplode(BombCarrier carrier)
    {
        if (!bombManager || bombManager.players == null || bombManager.players.Length < 2) return;

        // Shove TOWARD the carrier's side (extra loss for holder)
        bool carrierIsLeft = (carrier == bombManager.players[0]);
        float shoveDir = carrierIsLeft ? -1f : +1f;            // left -> shove left, right -> shove right
        value = Mathf.Clamp01(value + shoveDir * explosionShove);

        ApplyFills();
        UpdateHandleAtSeam();
    }

    void OnCarrierSet(BombCarrier newCarrier)
    {
        if (!isPlaying) return;       // no nudges outside play
        bool carrierIsLeft = (newCarrier == bombManager.players[0]);
        float nudge = 0.02f * (carrierIsLeft ? -1f : +1f);     // tiny shift toward carrier side
        value = Mathf.Clamp01(value + nudge);
        ApplyFills();
        UpdateHandleAtSeam();
    }

    void ApplyFills()
    {
        // Left shows left advantage directly; right is the complement
        if (leftFill) leftFill.fillAmount = value;        // 0..1 from left
        if (rightFill) rightFill.fillAmount = 1f - value;   // 1..0 from right
    }

    void UpdateHandleAtSeam()
    {
        if (!handle || !barRect) return;
        float width = barRect.rect.width, half = width * 0.5f;
        float targetX = Mathf.Lerp(-half, +half, value);
        var pos = handle.anchoredPosition;
        pos.x = Mathf.Lerp(pos.x, targetX, Mathf.Clamp01(handleLerp * Time.deltaTime));
        pos.x = Mathf.Round(pos.x);
        handle.anchoredPosition = pos;
    }
}
