
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 3;

    [Header("Invincibility")]
    [SerializeField] private float invincibilityTime = 1.5f;

    [Header("Flicker Effect")]
    [SerializeField] private float flickerSpeed = 0.1f;

    [Range(0f, 1f)]
    [SerializeField] private float flickerOpacity = 0.3f;

    private int currentHealth;

    private bool isInvincible = false;
    private bool isDead = false;
    private bool flickerVisible = true;

    private Animator anim;
    private Rigidbody2D body;
    private PlayerController controller;

    private SpriteRenderer[] sprites;
    private float[] originalOpacity;

    public bool IsDead => isDead;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    void Awake()
    {
        currentHealth = maxHealth;

        anim = GetComponent<Animator>();
        body = GetComponent<Rigidbody2D>();
        controller = GetComponent<PlayerController>();

        sprites =
            GetComponentsInChildren<SpriteRenderer>(true);

        originalOpacity = new float[sprites.Length];

        for (int i = 0; i < sprites.Length; i++)
            originalOpacity[i] = sprites[i].color.a;
    }

    void Update()
    {
        // Temporary damage testing key.
        if (Keyboard.current != null &&
            Keyboard.current.hKey.wasPressedThisFrame)
        {
            TakeDamage(1);
        }
    }

    void LateUpdate()
    {
        if (isInvincible && !isDead)
        {
            float opacity =
                flickerVisible ? 1f : flickerOpacity;

            SetOpacity(opacity);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead || isInvincible || damage <= 0)
            return;

        currentHealth = Mathf.Max(
            0,
            currentHealth - damage
        );

        Debug.Log("Fern Health: " + currentHealth);

        if (controller != null)
            controller.CancelAttack();

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        anim.SetTrigger("Hurt");

        isInvincible = true;

        StartCoroutine(InvincibilityFlicker());
    }

    public void KillFromLantern()
    {
        if (isDead)
            return;

        Debug.Log("Fern died because his flame went out!");

        Die();
    }

    private IEnumerator InvincibilityFlicker()
    {
        float elapsed = 0f;
        float flickerTimer = 0f;

        flickerVisible = false;

        while (elapsed < invincibilityTime)
        {
            elapsed += Time.deltaTime;
            flickerTimer += Time.deltaTime;

            if (flickerTimer >= flickerSpeed)
            {
                flickerVisible = !flickerVisible;
                flickerTimer = 0f;
            }

            yield return null;
        }

        isInvincible = false;
        flickerVisible = true;

        RestoreOpacity();
    }

    private void SetOpacity(float multiplier)
    {
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] == null)
                continue;

            Color color = sprites[i].color;

            color.a =
                originalOpacity[i] * multiplier;

            sprites[i].color = color;
        }
    }

    private void RestoreOpacity()
    {
        SetOpacity(1f);
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;
        isInvincible = false;

        StopAllCoroutines();

        flickerVisible = true;
        RestoreOpacity();

        if (controller != null)
        {
            controller.CancelAttack();
            controller.enabled = false;
        }

        if (body != null)
            body.linearVelocity = Vector2.zero;

        anim.ResetTrigger("Hurt");
        anim.ResetTrigger("Attack1");
        anim.ResetTrigger("Attack2");
        anim.ResetTrigger("Death");

        anim.SetFloat("Speed", 0f);

        Debug.Log("Fern has died!");

        StartCoroutine(PlayDeathOnce());
    }

    private IEnumerator PlayDeathOnce()
    {
        int deathState = Animator.StringToHash(
            "Base Layer.fern_death"
        );

        if (!anim.HasState(0, deathState))
        {
            Debug.LogError("Cannot find fern_death!");
            yield break;
        }

        anim.speed = 1f;

        anim.Play(deathState, 0, 0f);
        anim.Update(0f);

        while (true)
        {
            yield return null;

            AnimatorStateInfo state =
                anim.GetCurrentAnimatorStateInfo(0);

            if (!state.IsName("Base Layer.fern_death"))
            {
                Debug.LogWarning(
                    "Something interrupted fern_death!"
                );

                yield break;
            }

            if (state.normalizedTime >= 0.95f)
                break;
        }

        anim.Play(deathState, 0, 0.999f);
        anim.Update(0f);

        anim.speed = 0f;

        Debug.Log("Fern's death animation is finished!");
    }
}
