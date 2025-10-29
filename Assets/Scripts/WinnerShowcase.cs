using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class WinnerShowcase : MonoBehaviour
{

    [Header("Spawn")]
    public Transform spawnPoint;          // empty transform in the scene
    public GameObject leftPlayerPrefab;   // your left player prefab
    public GameObject rightPlayerPrefab;  // your right player prefab
    public bool faceRight = true;         // flip if needed

    void Start()
    {

        // Pick prefab
        GameObject prefab =
            GameResult.winnerIndex == 0 ? leftPlayerPrefab :
            GameResult.winnerIndex == 1 ? rightPlayerPrefab : null;

        if (!prefab || !spawnPoint) return;

        // Spawn
        var go = Instantiate(prefab, spawnPoint.position, Quaternion.identity);

        // Kill gameplay components so it’s a mannequin, not a menace
        var rb = go.GetComponent<Rigidbody2D>(); if (rb) Destroy(rb);
        var pm = go.GetComponent<PlayerMovement>(); if (pm) Destroy(pm);
        var carrier = go.GetComponent<BombCarrier>(); if (carrier) Destroy(carrier);

        // Face camera if needed (assumes +X is “right”)
        if (!faceRight)
        {
            var s = go.transform.localScale;
            s.x = Mathf.Abs(s.x) * -1f;
            go.transform.localScale = s;
        }

        // Trigger the dance
        var anim = go.GetComponentInChildren<Animator>() ?? go.GetComponent<Animator>();
        if (anim) anim.SetTrigger("Dance");
    }

    // Optional buttons
    public void PlayAgain() => SceneManager.LoadScene("Game");
    public void MainMenu() => SceneManager.LoadScene("Menu");
}
