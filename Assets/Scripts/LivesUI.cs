using UnityEngine;
using UnityEngine.UI;

public class LivesUI : MonoBehaviour
{
    [Header("Config")]
    public int playerIndex = 0;           // 0 = left, 1 = right
    public int maxIcons = 10;             // pool size (>= startingLives)
    public Color aliveColor = Color.white;
    public Color lostColor = new Color(1f, 1f, 1f, 0.2f);

    [Header("Refs")]
    public Transform container;           // this object (or a child)
    public Image iconPrefab;              // the LifeIcon prefab

    private Image[] pool;

    void Awake()
    {
        if (!container) container = transform;

        pool = new Image[maxIcons];
        for (int i = 0; i < maxIcons; i++)
        {
            var img = Instantiate(iconPrefab, container);
            img.gameObject.SetActive(true);
            img.color = lostColor; // start dim; will brighten on first event
            pool[i] = img;
        }
    }

    void OnEnable()
    {
        BombManager.OnLivesChanged += HandleLivesChanged;
    }

    void OnDisable()
    {
        BombManager.OnLivesChanged -= HandleLivesChanged;
    }

    void HandleLivesChanged(int index, int lives)
    {
        if (index != playerIndex) return;

        // Brighten first `lives` icons, dim the rest
        for (int i = 0; i < pool.Length; i++)
        {
            pool[i].color = (i < lives) ? aliveColor : lostColor;
        }
    }
}
