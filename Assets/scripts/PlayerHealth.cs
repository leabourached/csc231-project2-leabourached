
using UnityEngine;
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

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 1.2f;

    [Header("Death UI")]
    [SerializeField] private GameObject deathPanel;

    private int currentHealth;
    private bool isInvincible;
    private bool isDead;
    private bool flickerVisible = true;
    private bool waitingForRevive;

    private Animator anim;
    private Rigidbody2D body;
    private PlayerController controller;

    private SpriteRenderer[] sprites;
    private float[] originalOpacity;

    public bool IsDead => isDead;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        currentHealth = maxHealth;

        anim = GetComponent<Animator>();
        body = GetComponent<Rigidbody2D>();
        controller = GetComponent<PlayerController>();

        sprites = GetComponentsInChildren<SpriteRenderer>(true);

        originalOpacity = new float[sprites.Length];

        for (int i = 0; i < sprites.Length; i++)
        {
            originalOpacity[i] = sprites[i].color.a;
        }

        if (deathPanel != null)
            deathPanel.SetActive(false);
    }

    private void LateUpdate()
    {
        if (isInvincible && !isDead)
        {
            float opacity = flickerVisible
                ? 1f
                : flickerOpacity;

            SetOpacity(opacity);
        }
    }

    public void HealToFull()
    {
        if (isDead)
            return;

        currentHealth = maxHealth;

        Debug.Log(
            "Fern healed: " +
            currentHealth + "/" + maxHealth
        );
    }

    public void TakeDamage(int damage)
    {
        if (isDead || isInvincible || damage <= 0)
            return;

        currentHealth = Mathf.Max(
            0, currentHealth - damage
        );

        Debug.Log("Fern Health: " + currentHealth);

        if (controller != null)
            controller.CancelAttack();

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        // Play hurt sound for nonfatal damage.
        AudioManager.Instance?.PlayHurt();

        anim.SetTrigger("Hurt");

        isInvincible = true;

        StartCoroutine(InvincibilityFlicker());
    }

    public void KillFromLantern()
    {
        if (isDead)
            return;

        Debug.Log("Fern's flame went out!");

        Die();
    }

    public void Kill()
    {
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

            color.a = originalOpacity[i] * multiplier;

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
        currentHealth = 0;
        isInvincible = false;

        StopAllCoroutines();

        flickerVisible = true;

        RestoreOpacity();

        // Play death sound once, regardless of death cause.
        AudioManager.Instance?.PlayDeath();

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

        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        int deathState = Animator.StringToHash(
            "Base Layer.fern_death"
        );

        if (anim.HasState(0, deathState))
        {
            anim.speed = 1f;

            anim.Play(deathState, 0, 0f);
            anim.Update(0f);

            float elapsed = 0f;

            while (elapsed < 5f)
            {
                yield return null;

                elapsed += Time.deltaTime;

                AnimatorStateInfo state =
                    anim.GetCurrentAnimatorStateInfo(0);

                if (!state.IsName("Base Layer.fern_death"))
                    break;

                if (state.normalizedTime >= 0.95f)
                    break;
            }

            anim.Play(deathState, 0, 0.999f);
            anim.Update(0f);

            anim.speed = 0f;
        }

        yield return new WaitForSeconds(respawnDelay);

        ShowDeathPanel();
    }

    private void ShowDeathPanel()
    {
        if (deathPanel == null)
        {
            Debug.LogError(
                "DeathPanel is not assigned in PlayerHealth!"
            );

            return;
        }

        waitingForRevive = true;

        deathPanel.SetActive(true);

        Time.timeScale = 0f;
    }

    public void Revive()
    {
        if (!isDead || !waitingForRevive)
            return;

        waitingForRevive = false;

        if (deathPanel != null)
            deathPanel.SetActive(false);

        Time.timeScale = 1f;

        ShrineCheckpoint.Respawn();
    }
}
