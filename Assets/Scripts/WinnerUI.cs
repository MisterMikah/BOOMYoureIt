using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class WinnerUI : MonoBehaviour
{
    public TextMeshProUGUI title;
    public TextMeshProUGUI stats;
    public Button playAgainBtn;
    public Button menuBtn;

    [Header("Optional")]
    public string gameSceneName = "Game";
    public string menuSceneName = "Menu";
    public AudioClip victoryFanfare;

    void Start()
    {
        // Title
        string who = GameResult.winnerIndex == 0 ? "PLAYER 1" :
                     GameResult.winnerIndex == 1 ? "PLAYER 2" : "No one";
        if (title) title.text = $"{who} Wins!";

        // Stats
        if (stats)
            stats.text = $"Games Played: {GameResult.gamesPlayed}\n \n" +
                         $"PLAYER 1 Lives: {GameResult.leftLives}\n \n" +
                         $"PLAYER 2 Lives: {GameResult.rightLives}";

        // Music / fanfare
        if (victoryFanfare)
        {
            var sfx = new GameObject("VictorySFX").AddComponent<AudioSource>();
            sfx.spatialBlend = 0f; sfx.playOnAwake = false; sfx.volume = 1f;
            sfx.clip = victoryFanfare; sfx.Play();
            Destroy(sfx.gameObject, victoryFanfare.length + 0.1f);
        }
        else if (MusicManager.Instance)
        {
            // reuse your menu loop here if you want
            MusicManager.Instance.PlayMenuMusic();
        }

        // Buttons
        if (playAgainBtn) playAgainBtn.onClick.AddListener(() => SceneManager.LoadScene(gameSceneName));
        if (menuBtn) menuBtn.onClick.AddListener(() => SceneManager.LoadScene(menuSceneName));
    }
}
