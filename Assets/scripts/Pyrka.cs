
using UnityEngine;
using System.Collections.Generic;

public class Pyrka : MonoBehaviour
{
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

    private static List<Pyrka> collectedPyrkas =
        new List<Pyrka>();

    public static int Count => collectedPyrkas.Count;
    public static int RequiredPyrkas => 3;
    public static bool HasAllPyrkas => Count >= RequiredPyrkas;

    private Transform player;
    private PlayerHealth playerHealth;
    private Animator anim;
    private Collider2D pickupCollider;
    private SpriteRenderer sprite;

    private bool collected = false;

    private Vector3 followVelocity;
    private Vector3 startPosition;

    public bool IsCollected => collected;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInventory()
    {
        collectedPyrkas.Clear();
    }

    void Awake()
    {
        anim = GetComponent<Animator>();
        pickupCollider = GetComponent<Collider2D>();
        sprite = GetComponentInChildren<SpriteRenderer>();

        startPosition = transform.position;
    }

    void LateUpdate()
    {
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

        // Face the same direction as Fern.
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

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected)
            return;

        PlayerHealth health =
            other.GetComponentInParent<PlayerHealth>();

        if (health == null || health.IsDead)
            return;

        player = health.transform;
        playerHealth = health;

        collected = true;

        collectedPyrkas.Add(this);

        if (pickupCollider != null)
            pickupCollider.enabled = false;

        if (anim != null)
        {
            int followState = Animator.StringToHash(
                "Base Layer.pyrka_follow"
            );

            if (anim.HasState(0, followState))
                anim.Play(followState, 0, 0f);
        }

        Debug.Log("Pyrka rescued!");

        Debug.Log("Pyrkas collected: " +
            Count + "/" + RequiredPyrkas);
    }

    void OnDestroy()
    {
        collectedPyrkas.Remove(this);
    }
}
