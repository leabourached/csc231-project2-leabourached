
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

[DefaultExecutionOrder(-100)]
public class ShrineCheckpoint : MonoBehaviour
{
    [Header("Checkpoint")]
    [SerializeField] private int shrineID = 1;
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private Transform pyrkaTarget;

    [Header("Animation and Lighting")]
    [SerializeField] private Animator shrineAnimator;
    [SerializeField] private Light2D shrineLight;

    private static HashSet<int> unlockedShrines =
        new HashSet<int>();

    private static int latestShrineID = -1;
    private static string savedScene;

    public bool IsUnlocked =>
        savedScene == gameObject.scene.path &&
        unlockedShrines.Contains(shrineID);

    public static int UnlockedCount =>
        unlockedShrines.Count;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        ResetProgress();
    }

    public static void ResetProgress()
    {
        unlockedShrines.Clear();
        latestShrineID = -1;
        savedScene = null;

        Pyrka.ResetProgress();
    }

    private void Awake()
    {
        if (shrineAnimator == null)
            shrineAnimator = GetComponent<Animator>();

        if (shrineLight == null)
            shrineLight = GetComponentInChildren<Light2D>(true);

        if (shrineAnimator != null)
            shrineAnimator.enabled = false;

        if (shrineLight != null)
            shrineLight.enabled = false;
    }

    private void Start()
    {
        if (IsUnlocked)
            ActivateVisuals(true);

        if (latestShrineID != shrineID ||
            savedScene != gameObject.scene.path)
            return;

        PlayerHealth player =
            FindFirstObjectByType<PlayerHealth>();

        if (player == null)
            return;

        Vector3 position = respawnPoint != null
            ? respawnPoint.position
            : transform.position;

        player.transform.position = position;

        Rigidbody2D body =
            player.GetComponent<Rigidbody2D>();

        if (body != null)
        {
            body.position = position;
            body.linearVelocity = Vector2.zero;
        }

        Debug.Log("Fern respawned at shrine " + shrineID);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryUnlock(other, true);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryUnlock(other, false);
    }

    private void TryUnlock(
        Collider2D other, bool showMessage)
    {
        PlayerHealth player =
            other.GetComponentInParent<PlayerHealth>();

        if (player == null || player.IsDead)
            return;

        if (IsUnlocked)
            return;

        if (!Pyrka.HasAllPyrkas)
        {
            if (showMessage)
            {
                Debug.Log(
                    "This shrine needs 3 Pyrkas! " +
                    Pyrka.Count + "/3"
                );
            }

            return;
        }

        Transform target = pyrkaTarget != null
            ? pyrkaTarget
            : transform;

        if (!Pyrka.DeliverThree(target))
            return;

        savedScene = gameObject.scene.path;

        unlockedShrines.Add(shrineID);
        latestShrineID = shrineID;

        player.HealToFull();

        ActivateVisuals(false);

        // Play the shrine unlock sound once.
        AudioManager.Instance?.PlayShrineUnlock();

        Debug.Log("Shrine " + shrineID + " unlocked!");
        Debug.Log("Checkpoint saved!");
        Debug.Log("Fern's hearts restored!");
    }

    private void ActivateVisuals(bool restoring)
    {
        if (shrineLight != null)
            shrineLight.enabled = true;

        if (shrineAnimator != null)
        {
            shrineAnimator.enabled = true;

            int state = Animator.StringToHash(
                "Base Layer.shrine_unlocked"
            );

            if (!shrineAnimator.HasState(0, state))
            {
                Debug.LogWarning(
                    "Cannot find shrine_unlocked!"
                );

                return;
            }

            shrineAnimator.speed = 1f;
            shrineAnimator.Play(state, 0, 0f);
            shrineAnimator.Update(0f);

            if (restoring &&
                !shrineAnimator.GetCurrentAnimatorStateInfo(0).loop)
            {
                shrineAnimator.Play(state, 0, 0.9999f);
                shrineAnimator.Update(0f);
                shrineAnimator.speed = 0f;
            }
        }
    }

    public static void Respawn()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (scene.buildIndex < 0)
        {
            Debug.LogError(
                "Add your level to Build Profiles!"
            );

            return;
        }

        SceneManager.LoadScene(scene.buildIndex);
    }
}
