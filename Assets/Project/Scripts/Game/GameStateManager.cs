using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStateManager : MonoBehaviour
{
    [SerializeField] private PlayerInputReader input;
    [SerializeField] private PlayerLook look;
    [SerializeField] private PlayerDeath playerDeath;
    [SerializeField] private CheckpointManager checkpoints;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;
    [SerializeField] private TMP_Text victoryStatsText;
    [SerializeField] private float defeatScreenDelay = 2.5f;
    [SerializeField] private float victoryScreenDelay = 1.2f;

    private float startTime;
    private int kills;

    public static GameStateManager Instance { get; private set; }
    public bool IsPaused { get; private set; }
    public bool IsGameOver { get; private set; }

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);
        startTime = Time.time;
    }

    private void OnEnable()
    {
        input.PausePressed += TogglePause;
        if (playerDeath != null) playerDeath.PlayerDied += OnPlayerDied;
        EnemyAI.AnyKilled += OnEnemyKilled;
    }

    private void OnDisable()
    {
        input.PausePressed -= TogglePause;
        if (playerDeath != null) playerDeath.PlayerDied -= OnPlayerDied;
        EnemyAI.AnyKilled -= OnEnemyKilled;
        if (Instance == this) Instance = null;
    }

    public void TogglePause()
    {
        if (IsGameOver || IntroSequence.IsPlaying) return;
        SetPaused(!IsPaused);
    }

    public void Resume() => SetPaused(false);

    private void SetPaused(bool paused)
    {
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
        AudioListener.pause = paused;
        if (pausePanel != null) pausePanel.SetActive(paused);
        input.SetGameplayEnabled(!paused);
        look.SetCursorLocked(!paused);
    }

    public void Win()
    {
        if (IsGameOver) return;
        IsGameOver = true;

        float elapsed = Time.time - startTime;
        int minutes = Mathf.FloorToInt(elapsed / 60f);
        int seconds = Mathf.FloorToInt(elapsed % 60f);
        if (victoryStatsText != null) victoryStatsText.text = $"Tiempo: {minutes:00}:{seconds:00}    Forajidos abatidos: {kills}";

        StartCoroutine(ShowEndScreen(victoryPanel, victoryScreenDelay));
    }

    private void OnPlayerDied()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        StartCoroutine(ShowEndScreen(defeatPanel, defeatScreenDelay));
    }

    private void OnEnemyKilled(EnemyAI enemy) => kills++;

    private IEnumerator ShowEndScreen(GameObject panel, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        input.SetGameplayEnabled(false);
        look.SetCursorLocked(false);
        Time.timeScale = 0f;
        if (panel != null) panel.SetActive(true);
    }

    public void RespawnAtCheckpoint()
    {
        if (checkpoints == null)
        {
            Restart();
            return;
        }

        StopAllCoroutines();
        IsGameOver = false;
        if (defeatPanel != null) defeatPanel.SetActive(false);
        Time.timeScale = 1f;
        AudioListener.pause = false;
        checkpoints.Respawn();
        input.SetGameplayEnabled(true);
        look.SetCursorLocked(true);
    }

    public void Restart()
    {
        StopAllCoroutines();
        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Quit()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
