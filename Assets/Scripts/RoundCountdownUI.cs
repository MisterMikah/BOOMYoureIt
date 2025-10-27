using UnityEngine;
using TMPro;

public class RoundCountdownUI : MonoBehaviour
{
    [Header("Refs")]
    public TextMeshProUGUI label;   // assign your center TMP text

    BombManager bm;
    bool visible;

    void Awake()
    {
        if (!label) label = GetComponent<TextMeshProUGUI>();
        if (label)
        {
            label.raycastTarget = false;
            label.enabled = true;     // keep component enabled
            label.text = "";          // start clean
            var c = label.color;
            c.a = 1f;                 // make sure alpha isn't 0
            label.color = c;
        }
    }

    void OnEnable()
    {
        bm = FindFirstObjectByType<BombManager>();

        BombManager.OnPhaseChanged += HandlePhase;
        BombManager.OnCountdownTick += HandleTick;

        // Initialize immediately in case events already fired this frame
        if (bm != null)
        {
            HandlePhase(bm.CurrentPhaseName);
            if (bm.CurrentPhaseName == "Countdown")
            {
                HandleTick(bm.preRoundCountdown);   // seed “3” (or whatever) instantly
            }
        }
    }

    void OnDisable()
    {
        BombManager.OnPhaseChanged -= HandlePhase;
        BombManager.OnCountdownTick -= HandleTick;
    }

    void HandlePhase(string phase)
    {
        bool shouldShow = (phase == "Countdown");
        SetVisible(shouldShow);

        if (shouldShow && bm != null)
        {
            // seed again (defensive)
            HandleTick(bm.preRoundCountdown);
        }
    }

    void HandleTick(float secondsLeft)
    {
        if (!visible || label == null) return;

        int s = Mathf.CeilToInt(secondsLeft);
        label.text = (s > 0) ? s.ToString() : "GO!";
        // Debug line for sanity; comment out if noisy
        // Debug.Log($"[CountdownUI] tick={s}");
    }

    void SetVisible(bool show)
    {
        visible = show;
        if (!label) return;

        // Prefer hiding by alpha to avoid disabling the component
        var cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();
        cg.alpha = show ? 1f : 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;
    }
}
