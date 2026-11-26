
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Camera gameCamera;

    [Header("Health UI")]
    [SerializeField] private Image healthImage;
    [SerializeField] private Sprite[] healthSprites;

    [Header("Shrine Counter UI")]
    [SerializeField] private Image shrineCounterImage;
    [SerializeField] private Sprite[] shrineCounterSprites;

    [Header("Shrine Messages")]
    [SerializeField] private Image shrineRequiredMessage;
    [SerializeField] private Image shrineUnlockedMessage;
    [SerializeField] private Image allShrinesRestoredMessage;

    [Header("Victory UI")]
    [SerializeField] private GameObject levelCompletePanel;

    [Header("Pause Menu")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private string introSceneName = "IntroScreen";

    [Header("Message Settings")]
    [SerializeField] private float detectionDistance = 3f;
    [SerializeField] private float messageHeight = 2.5f;
    [SerializeField] private float fadeDuration = 0.4f;
    [SerializeField] private float unlockedMessageDuration = 3f;

    private ShrineCheckpoint[] shrines;
    private bool[] previousShrineStates;
    private ShrineCheckpoint lastUnlockedShrine;
    private float unlockedTimer;
    private bool allShrinesRestored;

    private PlayerController playerController;
    private bool isPaused;

    private void Start()
    {
        Time.timeScale = 1f;

        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (gameCamera == null)
            gameCamera = Camera.main;

        if (playerHealth != null)
            playerController = playerHealth.GetComponent<PlayerController>();

        if (pausePanel != null)
            pausePanel.SetActive(false);

        shrines = FindObjectsByType<ShrineCheckpoint>(
            FindObjectsSortMode.None);

        previousShrineStates = new bool[shrines.Length];

        int unlockedCount = 0;

        for (int i = 0; i < shrines.Length; i++)
        {
            previousShrineStates[i] = shrines[i].IsUnlocked;

            if (previousShrineStates[i])
                unlockedCount++;
        }

        allShrinesRestored =
            shrines.Length > 0 &&
            unlockedCount == shrines.Length;

        InitializeMessage(shrineRequiredMessage);
        InitializeMessage(shrineUnlockedMessage);
        InitializeMessage(allShrinesRestoredMessage);

        UpdateHUD();
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void PauseGame()
    {
        if (isPaused || pausePanel == null)
            return;

        if (playerHealth != null && playerHealth.IsDead)
            return;

        if (levelCompletePanel != null &&
            levelCompletePanel.activeInHierarchy)
            return;

        if (Time.timeScale == 0f)
            return;

        isPaused = true;

        if (playerController != null)
            playerController.enabled = false;

        pausePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        if (!isPaused)
            return;

        isPaused = false;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        Time.timeScale = 1f;

        StartCoroutine(EnableControlsNextFrame());
    }

    private IEnumerator EnableControlsNextFrame()
    {
        yield return null;

        if (!isPaused &&
            playerController != null &&
            playerHealth != null &&
            !playerHealth.IsDead &&
            (levelCompletePanel == null ||
             !levelCompletePanel.activeInHierarchy))
        {
            playerController.enabled = true;
        }
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        ShrineCheckpoint.ResetProgress();

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex);
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        ShrineCheckpoint.ResetProgress();

        SceneManager.LoadScene(introSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Game closed.");
        Application.Quit();
    }

    private void LateUpdate()
    {
        if (levelCompletePanel != null &&
            levelCompletePanel.activeInHierarchy)
        {
            HideGameplayUI();
            return;
        }

        if (isPaused)
            return;

        UpdateHUD();

        if (playerHealth == null)
            return;

        CheckShrineProgress();
        UpdateShrineMessages();
    }

    private void HideGameplayUI()
    {
        if (healthImage != null)
            healthImage.gameObject.SetActive(false);

        if (shrineCounterImage != null)
            shrineCounterImage.gameObject.SetActive(false);

        if (shrineRequiredMessage != null)
            shrineRequiredMessage.gameObject.SetActive(false);

        if (shrineUnlockedMessage != null)
            shrineUnlockedMessage.gameObject.SetActive(false);

        if (allShrinesRestoredMessage != null)
            allShrinesRestoredMessage.gameObject.SetActive(false);
    }

    private void UpdateHUD()
    {
        if (playerHealth != null &&
            healthImage != null &&
            healthSprites.Length == 4)
        {
            int health = Mathf.Clamp(
                playerHealth.CurrentHealth, 0, 3);

            healthImage.sprite = healthSprites[health];
        }

        if (shrineCounterImage != null &&
            shrineCounterSprites.Length == 5)
        {
            int unlockedCount = 0;

            foreach (ShrineCheckpoint shrine in shrines)
            {
                if (shrine.IsUnlocked)
                    unlockedCount++;
            }

            int count = Mathf.Clamp(unlockedCount, 0, 4);

            shrineCounterImage.sprite =
                shrineCounterSprites[count];
        }
    }

    private void CheckShrineProgress()
    {
        int unlockedCount = 0;

        for (int i = 0; i < shrines.Length; i++)
        {
            bool unlocked = shrines[i].IsUnlocked;

            if (unlocked)
                unlockedCount++;

            if (unlocked && !previousShrineStates[i])
            {
                lastUnlockedShrine = shrines[i];
                unlockedTimer = unlockedMessageDuration;
            }

            previousShrineStates[i] = unlocked;
        }

        allShrinesRestored =
            shrines.Length > 0 &&
            unlockedCount == shrines.Length;
    }

    private void UpdateShrineMessages()
    {
        ShrineCheckpoint nearestShrine = null;
        float nearestDistance = detectionDistance;
        bool playerAlive = !playerHealth.IsDead;

        if (playerAlive)
        {
            foreach (ShrineCheckpoint shrine in shrines)
            {
                if (shrine.IsUnlocked)
                    continue;

                float distance = Vector2.Distance(
                    playerHealth.transform.position,
                    shrine.transform.position);

                if (distance <= nearestDistance)
                {
                    nearestDistance = distance;
                    nearestShrine = shrine;
                }
            }
        }

        bool showRequired =
            playerAlive &&
            nearestShrine != null &&
            Pyrka.Count < 3;

        if (showRequired)
            PositionAboveShrine(
                shrineRequiredMessage, nearestShrine);

        FadeMessage(shrineRequiredMessage, showRequired);

        unlockedTimer -= Time.deltaTime;

        bool showUnlocked =
            playerAlive &&
            unlockedTimer > 0f &&
            lastUnlockedShrine != null;

        if (showUnlocked)
            PositionAboveShrine(
                shrineUnlockedMessage, lastUnlockedShrine);

        FadeMessage(shrineUnlockedMessage, showUnlocked);

        FadeMessage(
            allShrinesRestoredMessage, allShrinesRestored);
    }

    private void PositionAboveShrine(
        Image message, ShrineCheckpoint shrine)
    {
        if (message == null || gameCamera == null)
            return;

        Vector3 worldPosition =
            shrine.transform.position +
            Vector3.up * messageHeight;

        Vector3 screenPosition =
            gameCamera.WorldToScreenPoint(worldPosition);

        message.rectTransform.position = screenPosition;
    }

    private void InitializeMessage(Image message)
    {
        if (message == null)
            return;

        Color color = message.color;
        color.a = 0f;
        message.color = color;

        message.raycastTarget = false;
        message.gameObject.SetActive(false);
    }

    private void FadeMessage(Image message, bool show)
    {
        if (message == null)
            return;

        if (show)
            message.gameObject.SetActive(true);

        Color color = message.color;

        color.a = Mathf.MoveTowards(
            color.a,
            show ? 1f : 0f,
            Time.deltaTime / Mathf.Max(0.01f, fadeDuration));

        message.color = color;

        if (!show && color.a <= 0f)
            message.gameObject.SetActive(false);
    }
}
