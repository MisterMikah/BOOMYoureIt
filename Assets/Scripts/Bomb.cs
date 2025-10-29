using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Bomb : MonoBehaviour
{
    public GameObject explosionEffect;
    public GameObject bomb;
    public GameObject bombGlow;
    public AudioClip explosionSound;

    AudioSource audioSource;

    [Header("Optional Mixer")]
    public UnityEngine.Audio.AudioMixerGroup sfxGroup;

    private void Awake()
    {
        if (explosionSound && !explosionSound.preloadAudioData)
            explosionSound.LoadAudioData();
    }
    public bool IsVisible()
    {
        var sr = GetComponent<SpriteRenderer>();
        return sr && sr.enabled && gameObject.activeInHierarchy;
        
    }

    public void Show()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr) sr.enabled = true;

        var pulse = bomb ? bomb.GetComponent<BombGlowPulse>() : null;
        if (pulse) pulse.enabled = true;

        var l = bombGlow ? bombGlow.GetComponent<Light2D>() : null;
        if (l) l.enabled = true;
    }

    public void Hide()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr) sr.enabled = false;
        bomb.GetComponent<BombGlowPulse>().enabled = false;
        bombGlow.GetComponent<Light2D>().enabled = false;
    }

    public void ExplodeHere()
    {
        // VFX
        if (explosionEffect)
            Instantiate(explosionEffect, transform.position, transform.rotation);

        // SFX (2D, always audible)
        if (explosionSound)
        {
            var go = new GameObject("SFX_Explosion");
            var src = go.AddComponent<AudioSource>();
            src.clip = explosionSound;
            src.playOnAwake = false;
            src.spatialBlend = 0f;                  // 2D sound (no distance falloff)
            src.volume = 1f;
            if (sfxGroup) src.outputAudioMixerGroup = sfxGroup;
            src.Play();
            Destroy(go, explosionSound.length + 0.05f);
        }

        Hide(); // your existing method that hides sprite/glow/light
    }
}