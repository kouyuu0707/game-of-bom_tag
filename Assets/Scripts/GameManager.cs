using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject resultText;
    [SerializeField] private GameObject retryButton;
    public bool IsFinished { get; private set; }
    private PlayerController[] players;
    private EnemyController[] enemies;

    private void Start()
    {
        players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
    }

    private void LateUpdate()
    {
        if (IsFinished || players.Length == 0 || enemies.Length == 0)
            return;

        bool anyoneAlive = false;
        foreach (PlayerController player in players)
        {
            if (player != null && player.TryGetComponent(out PlayerHealth health) && !health.IsDead)
                anyoneAlive = true;
        }
        // If the final explosion kills everyone as well as the last enemy, defeat wins.
        if (!anyoneAlive)
        {
            Finish("Game Over");
            return;
        }
        foreach (EnemyController enemy in enemies)
        {
            if (enemy != null && !enemy.IsDead)
                return;
        }
        Finish("Game Clear!");
    }

    private void Finish(string message)
    {
        IsFinished = true;
        foreach (PlayerController player in players)
        {
            if (player != null)
                player.canMove = false;
        }
        if (resultText != null)
        {
            if (resultText.TryGetComponent(out TMP_Text label))
                label.text = message;
            resultText.SetActive(true);
        }
        if (retryButton != null)
            retryButton.SetActive(true);
    }

    public void RetryGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
