using UnityEngine;
using UnityEngine.Rendering.Universal; // Light2D

public class BombGlowPulse : MonoBehaviour
{
    [Header("Links")]
    public BombManager bombManager;         // drag your BombManager here (or Find in Awake)
    public Light2D glowLight;               // the Light2D on child "GlowLight"
    public SpriteRenderer bombSprite;       // the BombIcon's SpriteRenderer

    [Header("Pulse Speed (Hz)")]
    public float minHz = 1f;                // start of round
    public float maxHz = 7f;                // near explosion

    [Header("Light Intensity")]
    public float minIntensity = 0.7f;
    public float maxIntensity = 2.2f;

    [Header("Sprite Flash (alpha)")]
    public float minAlpha = 0.7f;
    public float maxAlpha = 1f;

    float phase; // keeps time for the sine

    void Awake()
    {
        if (!bombManager) bombManager = FindFirstObjectByType<BombManager>();
        if (!bombSprite) bombSprite = GetComponent<SpriteRenderer>();
        if (!glowLight) glowLight = GetComponentInChildren<Light2D>(true);
    }

    void Update()
    {
        if (!bombManager || !bombManager.CurrentCarrier) return;

        // urgency 0→1 as timer runs out
        float urgency = 1f - bombManager.Remaining01;

        // ramp frequency with urgency
        float hz = Mathf.Lerp(minHz, maxHz, urgency);
        phase += hz * Time.deltaTime;

        // 0..1 ping value
        float p = 0.5f + 0.5f * Mathf.Sin(phase * Mathf.PI * 2f);

        // drive light
        if (glowLight)
        {
            glowLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, p);
        }

        // drive sprite flash (alpha only; stays red)
        if (bombSprite)
        {
            var c = bombSprite.color;
            c.a = Mathf.Lerp(minAlpha, maxAlpha, p);
            bombSprite.color = c;
        }
    }
}
