
using UnityEngine;
using TMPro;

public class LevelEnd : MonoBehaviour
{
    [Header("Victory UI")]
    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private TMP_Text requirementMessage;

    private ShrineCheckpoint[] shrines;
    private bool levelCompleted;

    private void Start()
    {
        shrines = FindObjectsByType<ShrineCheckpoint>(
            FindObjectsSortMode.None
        );

        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);

        if (requirementMessage != null)
            requirementMessage.gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        CheckVictory(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        CheckVictory(other);
    }

    private void CheckVictory(Collider2D other)
    {
        if (levelCompleted)
            return;

        PlayerHealth player =
            other.GetComponentInParent<PlayerHealth>();

        if (player == null || player.IsDead)
            return;

        int unlocked = 0;

        foreach (ShrineCheckpoint shrine in shrines)
        {
            if (shrine.IsUnlocked)
                unlocked++;
        }

        if (shrines.Length == 0 ||
            unlocked < shrines.Length)
        {
            if (requirementMessage != null)
            {
                requirementMessage.text =
                    "Restore all shrines before leaving!\n" +
                    unlocked + "/" + shrines.Length +
                    " shrines restored";

                requirementMessage.gameObject.SetActive(true);
            }

            return;
        }

        levelCompleted = true;

        // Disable Fern's controls completely.
        PlayerController controller =
            player.GetComponent<PlayerController>();

        if (controller != null)
        {
            controller.CancelAttack();
            controller.enabled = false;
        }

        // Stop Fern's remaining movement.
        Rigidbody2D body =
            player.GetComponent<Rigidbody2D>();

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }

        if (requirementMessage != null)
            requirementMessage.gameObject.SetActive(false);

        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(true);

        Debug.Log("LEVEL COMPLETE!");

        Time.timeScale = 0f;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerHealth>() != null)
        {
            if (requirementMessage != null)
                requirementMessage.gameObject.SetActive(false);
        }
    }
}
