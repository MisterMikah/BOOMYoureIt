using UnityEngine;
using System;

public class BombManager : MonoBehaviour
{
    [Header("Timing & Scoring")]
    public float roundBombTime = 8f;          // time until explosion
    public float pointsDrainPerSecond = 1f;   // “lose points while holding” rate
    public float RemainingTime => Mathf.Max(timer, 0f);
    public float Remaining01 => roundBombTime > 0 ? Mathf.Clamp01(timer / roundBombTime) : 0f; // 1 → full time, 0 → boom

    [Header("Players")]
    public BombCarrier[] players;             // assign all players in scene

    [Header("Transfer Rules")]
    public float transferCooldown = 0.3f;     // global cooldown after a pass

    public BombCarrier CurrentCarrier { get; private set; }

    public static event Action<float> OnBombTick;
    public static event Action<BombCarrier> OnCarrierSet;
    public static event Action<BombCarrier> OnBombExplode;

    float timer;
    float lastTransferTime = -999f;
    
    void Start()
    {
        timer = roundBombTime;
        // choose starter however you want; placeholder:
        SetCarrier(players[1]);
    }

    void Update()
    {
        if (!CurrentCarrier) return;

        timer -= Time.deltaTime;
        OnBombTick?.Invoke(Mathf.Max(timer, 0f));

        // drain “score” while holding (hook into your scoreboard here)
        ScoreWhileHolding(CurrentCarrier, pointsDrainPerSecond * Time.deltaTime);

        if (timer <= 0f)
        {
            OnBombExplode?.Invoke(CurrentCarrier);
            ApplyExplosionScore(CurrentCarrier);  // big penalty / opponent bonus
            EndRound();                           // round wrap-up (life loss, reset)
        }
    }

    // Called by tagger when two players touch
    public bool TryTransferTo(BombCarrier next)
    {
        if (!next || next == CurrentCarrier) return false;
        if (Time.time - lastTransferTime < transferCooldown) return false;

        SetCarrier(next);
        lastTransferTime = Time.time;

        // Choose whether to reset the timer here or keep it ticking:
        // timer = roundBombTime; // <- uncomment for per-pass mini-rounds
        return true;
    }

    void SetCarrier(BombCarrier who)
    {
        foreach (var p in players) p.SetCarrier(false);
        CurrentCarrier = who;
        CurrentCarrier.SetCarrier(true);
        OnCarrierSet?.Invoke(CurrentCarrier);
    }

    void EndRound()
    {
        // TODO: your life-loss + next-round init
        timer = roundBombTime;
        // Example: leader starts next round, or rotate starter, etc.
        SetCarrier(players[0]);
    }

    // ----- hook these into your real scoring/UI -----
    void ScoreWhileHolding(BombCarrier carrier, float delta)
    {
        // ScoreSystem.Instance.AddHoldingPenalty(carrier, delta);
    }

    void ApplyExplosionScore(BombCarrier carrier)
    {
        // ScoreSystem.Instance.ApplyExplosionPenalty(carrier);
    }
}
