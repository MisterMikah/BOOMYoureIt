using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;
    private AudioSource audioSource;

    [Header("Music Clips")]
    public AudioClip menuMusic;
    public AudioClip gameMusic;

    [SerializeField] private Slider musicSlider;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            audioSource = GetComponent<AudioSource>();
            DontDestroyOnLoad(gameObject);

            //Listen for scene changes
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Automatically play correct music at start
    void Start()
    {
        ChangeMusicForScene(SceneManager.GetActiveScene().name);
    }

    // Change song whenever a new scene loads
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ChangeMusicForScene(scene.name);
    }

    private void ChangeMusicForScene(string sceneName)
    {
        AudioClip newClip = null;

        if (sceneName == "Menu") newClip = menuMusic;
        else if (sceneName == "Game") newClip = gameMusic;

        if (newClip != null && audioSource.clip != newClip)
        {
            audioSource.clip = newClip;
            audioSource.Play();
        }
    }

    public void PlayBackgroundMusic(bool resetSong, AudioClip audioClip = null)
    {
        if (audioClip != null)
        {
            audioSource.clip = audioClip;
        }
        else if (audioSource.clip != null)
        {
            if (resetSong)
            {
                audioSource.Stop();
            }
            audioSource.Play();
        }
    }

    public void PauseBackgroundMusic()
    {
        audioSource.Pause();
    }

    public void PlayGameMusic()
    {
        if (gameMusic == null) return;
        audioSource.loop = false;
        audioSource.clip = gameMusic;
        audioSource.Play();
    }

    public void PlayMenuMusic()
    {
        if (menuMusic == null) return;
        audioSource.loop = true;
        audioSource.clip = menuMusic;
        audioSource.Play();
    }

}
