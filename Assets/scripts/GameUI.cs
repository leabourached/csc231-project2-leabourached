
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Camera gameCamera;

    [Header("Health UI")]
    [SerializeField] private Image healthImage;
    [SerializeField] private Sprite[] healthSprites;

    [Header("Pyrka UI")]
    [SerializeField] private Image pyrkaImage;
    [SerializeField] private Sprite[] pyrkaSprites;

    [Header("Shrine Messages")]
    [SerializeField] private Image shrineRequiredMessage;
    [SerializeField] private Image shrineUnlockedMessage;
    [SerializeField] private Image allShrinesRestoredMessage;

    [Header("Message Settings")]
    [SerializeField] private float detectionDistance = 3f;
    [SerializeField] private float messageHeight = 2.5f;
    [SerializeField] private float fadeDuration = 0.4f;
    [SerializeField] private float unlockedMessageDuration = 3f;
    [SerializeField] private float allRestoredDuration = 5f;

    private ShrineCheckpoint[] shrines;
    private bool[] previousShrineStates;

    private ShrineCheckpoint lastUnlockedShrine;
    private float unlockedTimer;
    private float allRestoredTimer;

    private bool allRestoredAnnounced;

    private void Start()
    {
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (gameCamera == null)
            gameCamera = Camera.main;

        shrines = FindObjectsByType<ShrineCheckpoint>(
            FindObjectsSortMode.None
        );

        previousShrineStates = new bool[shrines.Length];

        int unlockedCount = 0;

        for (int i = 0; i < shrines.Length; i++)
        {
            previousShrineStates[i] = shrines[i].IsUnlocked;

            if (previousShrineStates[i])
                unlockedCount++;
        }

        allRestoredAnnounced =
            shrines.Length > 0 &&
            unlockedCount == shrines.Length;

        InitializeMessage(shrineRequiredMessage);
        InitializeMessage(shrineUnlockedMessage);
        InitializeMessage(allShrinesRestoredMessage);

        UpdateHUD();
    }

    private void LateUpdate()
    {
        UpdateHUD();

        if (playerHealth == null)
            return;

        CheckShrineProgress();
        UpdateShrineMessages();
    }

    private void UpdateHUD()
    {
        if (playerHealth != null &&
            healthImage != null &&
            healthSprites.Length == 4)
        {
            int health = Mathf.Clamp(
                playerHealth.CurrentHealth, 0, 3
            );

            healthImage.sprite = healthSprites[health];
        }

        if (pyrkaImage != null &&
            pyrkaSprites.Length == 4)
        {
            int count = Mathf.Clamp(Pyrka.Count, 0, 3);

            pyrkaImage.sprite = pyrkaSprites[count];
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

        if (!allRestoredAnnounced &&
            shrines.Length > 0 &&
            unlockedCount == shrines.Length)
        {
            allRestoredAnnounced = true;
            allRestoredTimer = allRestoredDuration;
        }
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
                    shrine.transform.position
                );

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
        {
            PositionAboveShrine(
                shrineRequiredMessage,
                nearestShrine
            );
        }

        FadeMessage(shrineRequiredMessage, showRequired);

        unlockedTimer -= Time.deltaTime;

        bool showUnlocked =
            playerAlive &&
            unlockedTimer > 0f &&
            lastUnlockedShrine != null;

        if (showUnlocked)
        {
            PositionAboveShrine(
                shrineUnlockedMessage,
                lastUnlockedShrine
            );
        }

        FadeMessage(shrineUnlockedMessage, showUnlocked);

        allRestoredTimer -= Time.deltaTime;

        FadeMessage(
            allShrinesRestoredMessage,
            playerAlive && allRestoredTimer > 0f
        );
    }

    private void PositionAboveShrine(
        Image message,
        ShrineCheckpoint shrine)
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
            Time.deltaTime / Mathf.Max(0.01f, fadeDuration)
        );

        message.color = color;

        if (!show && color.a <= 0f)
            message.gameObject.SetActive(false);
    }
}
