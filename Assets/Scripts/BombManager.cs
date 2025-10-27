using UnityEngine;
using System;

public class BombManager : MonoBehaviour
{
    [Header("Timing & Scoring")]
    public float roundBombTime = 8f;            // time until explosion (Playing)
    public float pointsDrainPerSecond = 1f;     // drain while holding (Playing)
    public float postExplosionDelay = 3f;       // free move after boom
    public float preRoundCountdown = 3f;        // 3..2..1..GO
    public bool freezePlayersDuringCountdown = true;

    [Header("Players")]
    public BombCarrier[] players;               // [0]=left, [1]=right
    public int startingLives = 5;
    public Transform[] spawnPoints;

    [Header("Transfer Rules")]
    public float transferCooldown = 0.3f;

    [Header("Meter Source")]
    public BombTugOfWarUI tugOfWarUI;           // assign your UI bar here

    [Header("Debug")]
    public bool debugLogging = true;

    public BombCarrier CurrentCarrier { get; private set; }
    public float RemainingTime => Mathf.Max(timer, 0f);
    public float Remaining01 => roundBombTime > 0 ? Mathf.Clamp01(timer / roundBombTime) : 0f;

    // Events
    public static event Action<float> OnBombTick;
    public static event Action<BombCarrier> OnCarrierSet;
    public static event Action<BombCarrier> OnBombExplode;
    public static event Action<int, int> OnLivesChanged;
    public static event Action<int> OnEliminated;
    public static event Action<string> OnPhaseChanged;
    public static event Action<float> OnCountdownTick;

    // --- internals ---
    enum Phase { Countdown, Playing, PostExplosion }
    Phase phase = Phase.Countdown;

    float timer;                 // bomb timer during Playing
    float countdownRemaining;    // during Countdown
    float postRemaining;         // during PostExplosion
    float lastTransferTime = -999f;
    int[] lives;

    public string CurrentPhaseName
    {
        get
        {
            switch (phase)
            {
                case Phase.Countdown: return "Countdown";
                case Phase.Playing: return "Playing";
                case Phase.PostExplosion: return "PostExplosion";
                default: return "";
            }
        }
    }

    void Start()
    {
        if (players == null || players.Length == 0)
#if UNITY_2022_1_OR_NEWER
            players = FindObjectsByType<BombCarrier>(FindObjectsSortMode.InstanceID);
#else
            players = FindObjectsOfType<BombCarrier>();
#endif

        lives = new int[players.Length];
        for (int i = 0; i < players.Length; i++)
        {
            lives[i] = startingLives;
            OnLivesChanged?.Invoke(i, lives[i]);
        }

        RespawnAllAtSpawns(true);
        if (players.Length > 0) SetCarrier(players[0]);

        // enter initial countdown
        EnterCountdown();
    }

    void Update()
    {
        switch (phase)
        {
            case Phase.Countdown:
                // continuously broadcast the countdown for UI
                OnCountdownTick?.Invoke(countdownRemaining);
                countdownRemaining -= Time.deltaTime;

                if (countdownRemaining <= 0f)
                {
                    OnCountdownTick?.Invoke(0f); // "GO!"
                    EnterPlaying();
                }
                break;

            case Phase.Playing:
                if (!CurrentCarrier) return;
                timer -= Time.deltaTime;
                OnBombTick?.Invoke(Mathf.Max(timer, 0f));

                // drain while holding (hook your scoreboard here later)
                ScoreWhileHolding(CurrentCarrier, pointsDrainPerSecond * Time.deltaTime);

                if (timer <= 0f)
                {
                    if (debugLogging) Debug.Log("[BM] BOOM");
                    OnBombExplode?.Invoke(CurrentCarrier);
                    ApplyExplosionScore(CurrentCarrier);
                    ResolveExplosionByMeter();   // loser = side with less fill
                    EnterPostExplosion();
                }
                break;

            case Phase.PostExplosion:
                postRemaining -= Time.deltaTime;
                if (postRemaining <= 0f)
                {
                    RespawnAllAtSpawns(true);
                    ChooseAndSetNextRoundStarter();
                    EnterCountdown();           // treat “after respawn” exactly like round start
                }
                break;
        }
    }

    // ---------------- phase helpers ----------------

    void EnterCountdown()
    {
        phase = Phase.Countdown;
        countdownRemaining = Mathf.Max(0f, preRoundCountdown);

        OnPhaseChanged?.Invoke("Countdown");

        if (freezePlayersDuringCountdown) SetPlayerInputEnabled(false);
        // seed the UI on the same frame
        OnCountdownTick?.Invoke(countdownRemaining);

        if (debugLogging) Debug.Log("[BM] Phase=Countdown");
    }

    void EnterPlaying()
    {
        phase = Phase.Playing;
        timer = roundBombTime;

        if (freezePlayersDuringCountdown) SetPlayerInputEnabled(true);
        OnPhaseChanged?.Invoke("Playing");

        if (debugLogging) Debug.Log("[BM] Phase=Playing");
    }

    void EnterPostExplosion()
    {
        phase = Phase.PostExplosion;
        postRemaining = Mathf.Max(0f, postExplosionDelay);

        // free move — do NOT freeze
        OnPhaseChanged?.Invoke("PostExplosion");
        if (debugLogging) Debug.Log("[BM] Phase=PostExplosion (free move)");
    }

    // ---------------- API ----------------

    public bool TryTransferTo(BombCarrier next)
    {
        if (phase != Phase.Playing) return false;
        if (!next || next == CurrentCarrier) return false;
        if (Time.time - lastTransferTime < transferCooldown) return false;

        SetCarrier(next);
        lastTransferTime = Time.time;
        return true;
    }

    void SetCarrier(BombCarrier who)
    {
        foreach (var p in players) p.SetCarrier(false);
        CurrentCarrier = who;
        CurrentCarrier.SetCarrier(true);
        OnCarrierSet?.Invoke(CurrentCarrier);
    }

    // loser = LEAST fill at explosion (per your spec)
    void ResolveExplosionByMeter()
    {
        float v = tugOfWarUI ? tugOfWarUI.value : 0.5f;

        int loserIndex;
        if (v < 0.5f) loserIndex = 0;                // left has less fill
        else if (v > 0.5f) loserIndex = 1;                // right has less fill
        else loserIndex = IndexOf(CurrentCarrier); // tie → carrier loses

        lives[loserIndex] = Mathf.Max(0, lives[loserIndex] - 1);
        OnLivesChanged?.Invoke(loserIndex, lives[loserIndex]);
        if (lives[loserIndex] <= 0) OnEliminated?.Invoke(loserIndex);
    }

    void ChooseAndSetNextRoundStarter()
    {
        if (IsGameOver()) return;

        int leftLives = (players.Length > 0) ? lives[0] : 0;
        int rightLives = (players.Length > 1) ? lives[1] : 0;

        BombCarrier starter =
            (leftLives > rightLives) ? players[0] :
            (rightLives > leftLives) ? players[1] :
            (CurrentCarrier == players[0] ? players[1] : players[0]);

        SetCarrier(starter);
    }

    // ---------------- utils ----------------

    bool IsGameOver()
    {
        int alive = 0;
        foreach (int l in lives) if (l > 0) alive++;
        return alive <= 1;
    }

    void RespawnAllAtSpawns(bool resetVelocity)
    {
        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i];
            if (!p) continue;

            if (spawnPoints != null && i < spawnPoints.Length && spawnPoints[i])
                p.transform.position = spawnPoints[i].position;

            var rb = p.GetComponent<Rigidbody2D>();
            if (rb && resetVelocity)
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector2.zero;
#else
                rb.velocity = Vector2.zero;
#endif
                rb.angularVelocity = 0f;
            }
        }
    }

    void SetPlayerInputEnabled(bool enabled)
    {
        for (int i = 0; i < players.Length; i++)
        {
            var pm = players[i].GetComponent<PlayerMovement>();
            if (pm) pm.enabled = enabled;

            var rb = players[i].GetComponent<Rigidbody2D>();
            if (rb)
            {
#if UNITY_6000_0_OR_NEWER
                if (!enabled) rb.linearVelocity = Vector2.zero;
#else
                if (!enabled) rb.velocity = Vector2.zero;
#endif
                rb.angularVelocity = 0f;
                rb.constraints = enabled
                    ? RigidbodyConstraints2D.FreezeRotation
                    : RigidbodyConstraints2D.FreezeAll; // hard freeze during countdown
            }
        }
    }

    int IndexOf(BombCarrier c)
    {
        for (int i = 0; i < players.Length; i++)
            if (players[i] == c) return i;
        return -1;
    }

    // stubs
    void ScoreWhileHolding(BombCarrier carrier, float delta) { }
    void ApplyExplosionScore(BombCarrier carrier) { }
}
