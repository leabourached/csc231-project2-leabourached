
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Pyrka : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private int pyrkaID = 1;

    [Header("Following")]
    [SerializeField] private float followDistance = 1.2f;
    [SerializeField] private float spacing = 0.8f;
    [SerializeField] private float followHeight = 0.8f;
    [SerializeField] private float smoothTime = 0.18f;

    [Header("Floating")]
    [SerializeField] private float bobHeight = 0.15f;
    [SerializeField] private float bobSpeed = 4f;

    [Header("Sprite Direction")]
    [SerializeField] private bool spriteFacesRight = true;

    [Header("Shrine Delivery")]
    [SerializeField] private float deliveryDuration = 0.6f;

    private static List<Pyrka> collectedPyrkas =
        new List<Pyrka>();

    private static HashSet<int> deliveredIDs =
        new HashSet<int>();

    public static int Count => collectedPyrkas.Count;
    public static int DeliveredCount => deliveredIDs.Count;
    public static int RequiredPyrkas => 3;

    public static bool HasAllPyrkas =>
        Count == RequiredPyrkas;

    private Transform player;
    private PlayerHealth playerHealth;
    private Animator anim;
    private Collider2D pickupCollider;
    private SpriteRenderer sprite;

    private bool collected;
    private bool delivering;

    private Vector3 followVelocity;
    private Vector3 startPosition;

    public bool IsCollected => collected;

    public static void ResetProgress()
    {
        collectedPyrkas.Clear();
        deliveredIDs.Clear();
    }

    private void Awake()
    {
        anim = GetComponent<Animator>();
        pickupCollider = GetComponent<Collider2D>();
        sprite = GetComponentInChildren<SpriteRenderer>();

        startPosition = transform.position;

        if (deliveredIDs.Contains(pyrkaID))
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }

    private void LateUpdate()
    {
        if (delivering)
            return;

        int followIndex = collected
            ? collectedPyrkas.IndexOf(this)
            : 0;

        float bob = Mathf.Sin(
            Time.time * bobSpeed + followIndex
        ) * bobHeight;

        if (!collected)
        {
            transform.position =
                startPosition + Vector3.up * bob;

            return;
        }

        if (player == null)
            return;

        if (playerHealth != null && playerHealth.IsDead)
            return;

        float facing = Mathf.Sign(player.localScale.x);

        if (sprite != null)
        {
            sprite.flipX = spriteFacesRight
                ? facing < 0f
                : facing > 0f;
        }

        float distance =
            followDistance + followIndex * spacing;

        Vector3 targetPosition = new Vector3(
            player.position.x - facing * distance,
            player.position.y + followHeight + bob,
            transform.position.z
        );

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref followVelocity,
            smoothTime
        );
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryCollect(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryCollect(other);
    }

    private void TryCollect(Collider2D other)
    {
        if (collected || delivering)
            return;

        if (Count >= RequiredPyrkas)
            return;

        PlayerHealth health =
            other.GetComponentInParent<PlayerHealth>();

        if (health == null || health.IsDead)
            return;

        if (deliveredIDs.Contains(pyrkaID))
            return;

        foreach (Pyrka pyrka in collectedPyrkas)
        {
            if (pyrka.pyrkaID == pyrkaID)
                return;
        }

        player = health.transform;
        playerHealth = health;

        collected = true;
        collectedPyrkas.Add(this);

        if (pickupCollider != null)
            pickupCollider.enabled = false;

        PlayAnimation("pyrka_follow");

        // Play collection sound once.
        AudioManager.Instance?.PlayPyrkaCollect();

        Debug.Log("Pyrka rescued!");
        Debug.Log("Pyrkas: " + Count + "/3");
    }

    public static bool DeliverThree(Transform target)
    {
        if (Count != RequiredPyrkas)
            return false;

        Pyrka[] pyrkas = collectedPyrkas.ToArray();

        foreach (Pyrka pyrka in pyrkas)
        {
            if (pyrka == null)
                return false;
        }

        foreach (Pyrka pyrka in pyrkas)
        {
            pyrka.Deliver(target);
        }

        Debug.Log("3 Pyrkas delivered!");
        Debug.Log("Pyrkas: " + Count + "/3");
        Debug.Log("Total rescued: " + DeliveredCount);

        return true;
    }

    private void Deliver(Transform target)
    {
        if (delivering)
            return;

        delivering = true;

        collectedPyrkas.Remove(this);
        deliveredIDs.Add(pyrkaID);

        StartCoroutine(EnterShrine(target));
    }

    private IEnumerator EnterShrine(Transform target)
    {
        Vector3 start = transform.position;
        float elapsed = 0f;

        while (elapsed < deliveryDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / deliveryDuration
            );

            t = Mathf.SmoothStep(0f, 1f, t);

            if (target != null)
            {
                transform.position = Vector3.Lerp(
                    start,
                    target.position,
                    t
                );
            }

            yield return null;
        }

        if (PlayAnimation("pyrka_disappear"))
        {
            elapsed = 0f;

            yield return null;

            while (elapsed < 5f)
            {
                AnimatorStateInfo state =
                    anim.GetCurrentAnimatorStateInfo(0);

                if (state.IsName(
                    "Base Layer.pyrka_disappear"
                    ) && state.normalizedTime >= 1f)
                {
                    break;
                }

                elapsed += Time.deltaTime;

                yield return null;
            }
        }

        Destroy(gameObject);
    }

    private bool PlayAnimation(string stateName)
    {
        if (anim == null)
            return false;

        int hash = Animator.StringToHash(
            "Base Layer." + stateName
        );

        if (!anim.HasState(0, hash))
            return false;

        anim.Play(hash, 0, 0f);

        return true;
    }

    private void OnDestroy()
    {
        collectedPyrkas.Remove(this);
    }
}
