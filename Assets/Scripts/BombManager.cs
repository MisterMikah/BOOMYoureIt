using UnityEngine;
using System;

public class BombManager : MonoBehaviour
{
    [Header("Timing & Scoring")]
    public float roundBombTime = 8f;          // time until explosion
    public float pointsDrainPerSecond = 1f;   // lose points while holding
    public float RemainingTime => Mathf.Max(timer, 0f);
    public float Remaining01 => roundBombTime > 0 ? Mathf.Clamp01(timer / roundBombTime) : 0f;

    [Header("Players")]
    public BombCarrier[] players;             // assign all players in scene
    public int startingLives = 5;             // how many lives each starts with

    [Header("Transfer Rules")]
    public float transferCooldown = 0.3f;     // global cooldown after a pass

    public BombCarrier CurrentCarrier { get; private set; }

    // Events for UI or logic
    public static event Action<float> OnBombTick;
    public static event Action<BombCarrier> OnCarrierSet;
    public static event Action<BombCarrier> OnBombExplode;
    public static event Action<int, int> OnLivesChanged; // (playerIndex, newLives)
    public static event Action<int> OnEliminated;        // (playerIndex)

    float timer;
    float lastTransferTime = -999f;
    int[] lives;

    void Start()
    {
        timer = roundBombTime;

        // Ensure players are found
        if (players == null || players.Length == 0)
            players = FindObjectsByType<BombCarrier>(FindObjectsSortMode.InstanceID);

        // Init lives and notify
        lives = new int[players.Length];
        for (int i = 0; i < players.Length; i++)
        {
            lives[i] = startingLives;
            OnLivesChanged?.Invoke(i, lives[i]);
        }

        // Pick starter (first for now)
        if (players.Length > 0)
            SetCarrier(players[0]);
    }

    void Update()
    {
        if (!CurrentCarrier) return;

        timer -= Time.deltaTime;
        OnBombTick?.Invoke(Mathf.Max(timer, 0f));

        // drain points while holding (hook into your scoring later)
        ScoreWhileHolding(CurrentCarrier, pointsDrainPerSecond * Time.deltaTime);

        if (timer <= 0f)
        {
            OnBombExplode?.Invoke(CurrentCarrier);
            ApplyExplosionScore(CurrentCarrier);
            ResolveExplosion(); // life loss + next round
        }
    }

    // Called by tagger when two players touch
    public bool TryTransferTo(BombCarrier next)
    {
        if (!next || next == CurrentCarrier) return false;
        if (Time.time - lastTransferTime < transferCooldown) return false;

        SetCarrier(next);
        lastTransferTime = Time.time;
        return true;
    }

    void SetCarrier(BombCarrier who)
    {
        foreach (var p in players)
            p.SetCarrier(false);

        CurrentCarrier = who;
        CurrentCarrier.SetCarrier(true);
        OnCarrierSet?.Invoke(CurrentCarrier);
    }

    // --- Life / round resolution ---
    void ResolveExplosion()
    {
        BombCarrier loser = CurrentCarrier;
        int li = IndexOf(loser);
        if (li < 0) return;

        // lose a life
        lives[li] = Mathf.Max(0, lives[li] - 1);
        OnLivesChanged?.Invoke(li, lives[li]);

        if (lives[li] <= 0)
        {
            OnEliminated?.Invoke(li);
            Debug.Log($"Player {li} eliminated!");
        }

        // Start next round unless game over
        if (IsGameOver()) return;
        StartNextRound();
    }

    bool IsGameOver()
    {
        int alive = 0;
        foreach (int l in lives)
            if (l > 0) alive++;
        if (alive <= 1)
        {
            Debug.Log("Game over!");
            return true;
        }
        return false;
    }

    void StartNextRound()
    {
        timer = roundBombTime;

        // Example starter: whoever has more lives starts with bomb
        int leftLives = (players.Length > 0) ? lives[0] : 0;
        int rightLives = (players.Length > 1) ? lives[1] : 0;

        BombCarrier starter =
            (leftLives > rightLives) ? players[0] :
            (rightLives > leftLives) ? players[1] :
            (CurrentCarrier == players[0] ? players[1] : players[0]); // tie breaker flips

        SetCarrier(starter);
    }

    int IndexOf(BombCarrier c)
    {
        for (int i = 0; i < players.Length; i++)
            if (players[i] == c) return i;
        return -1;
    }

    // ----- scoring stubs -----
    void ScoreWhileHolding(BombCarrier carrier, float delta)
    {
        // Hook into your scoring UI later
    }

    void ApplyExplosionScore(BombCarrier carrier)
    {
        // Hook into your scoring UI later
    }
}
