
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class ShadowChaser : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform player;

    [Header("Flying")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private bool spriteFacesRight = true;

    [Header("Swoop Attack")]
    [SerializeField] private float dashStartRange = 3f;
    [SerializeField] private float dashSpeed = 9f;
    [SerializeField] private float passDistance = 2.5f;
    [SerializeField] private float turnPause = 0.35f;
    [SerializeField] private int attackDamage = 1;

    [Header("Health")]
    [SerializeField] private int maxHealth = 2;

    [Header("Recoil")]
    [SerializeField] private float recoilSpeed = 7f;
    [SerializeField] private float recoilDuration = 0.22f;

    private Rigidbody2D body;
    private SpriteRenderer sprite;
    private Animator anim;

    private PlayerHealth playerHealth;
    private PlayerController playerController;
    private Collider2D playerCollider;

    private int currentHealth;
    private bool hitPlayerThisPass;

    private Vector2 passDirection;
    private Vector2 recoilDirection;

    private float passRemaining;
    private float stateTimer;
    private float facingX = 1f;

    private string currentAnimation;

    private enum State
    {
        Idle,
        Seek,
        Pass,
        Turn,
        Recoil,
        Dead
    }

    private State state = State.Idle;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>();

        body.gravityScale = 0f;
        currentHealth = maxHealth;
    }

    void Start()
    {
        if (player != null)
        {
            playerHealth =
                player.GetComponent<PlayerHealth>();

            playerController =
                player.GetComponent<PlayerController>();

            playerCollider =
                player.GetComponent<Collider2D>();
        }
        else
        {
            Debug.LogError("Assign Fern to ShadowChaser!");
        }

        PlayAnimation("shadow_idle");
    }

    void FixedUpdate()
    {
        if (state == State.Dead)
            return;

        if (player == null ||
            (playerController != null &&
             !playerController.enabled))
        {
            EnterIdle();
            return;
        }

        Vector2 difference =
            GetPlayerPosition() - body.position;

        float distance = difference.magnitude;

        switch (state)
        {
            case State.Idle:

                body.linearVelocity = Vector2.zero;

                if (distance <= detectionRange)
                    BeginSeek();

                break;

            case State.Seek:

                if (distance > detectionRange)
                {
                    EnterIdle();
                    break;
                }

                if (distance <= dashStartRange)
                {
                    BeginPass(difference);
                    MoveThroughPlayer();
                }
                else
                {
                    Vector2 direction =
                        difference.normalized;

                    FaceDirection(direction);

                    body.linearVelocity =
                        direction * moveSpeed;
                }

                break;

            case State.Pass:

                MoveThroughPlayer();
                break;

            case State.Turn:

                body.linearVelocity = Vector2.zero;
                stateTimer -= Time.fixedDeltaTime;

                if (stateTimer <= 0f)
                    BeginSeek();

                break;

            case State.Recoil:

                body.linearVelocity =
                    recoilDirection * recoilSpeed;

                stateTimer -= Time.fixedDeltaTime;

                if (stateTimer <= 0f)
                    BeginSeek();

                break;
        }
    }

    private Vector2 GetPlayerPosition()
    {
        if (playerCollider != null)
            return playerCollider.bounds.center;

        return player.position;
    }

    private void EnterIdle()
    {
        state = State.Idle;
        body.linearVelocity = Vector2.zero;

        PlayAnimation("shadow_idle");
    }

    private void BeginSeek()
    {
        state = State.Seek;
        hitPlayerThisPass = false;

        body.linearVelocity = Vector2.zero;

        PlayAnimation("shadow_seek");
    }

    private void BeginPass(Vector2 difference)
    {
        state = State.Pass;

        passDirection = difference.normalized;

        if (passDirection.sqrMagnitude < 0.01f)
        {
            passDirection =
                new Vector2(facingX, 0f);
        }

        passRemaining =
            difference.magnitude + passDistance;

        FaceDirection(passDirection);
    }

    private void MoveThroughPlayer()
    {
        if (passRemaining <= 0.01f)
        {
            state = State.Turn;
            stateTimer = turnPause;

            body.linearVelocity = Vector2.zero;

            PlayAnimation("shadow_idle");
            return;
        }

        float speed = Mathf.Min(
            dashSpeed,
            passRemaining / Time.fixedDeltaTime
        );

        body.linearVelocity =
            passDirection * speed;

        passRemaining -=
            speed * Time.fixedDeltaTime;
    }

    private void FaceDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) < 0.05f)
            return;

        facingX = Mathf.Sign(direction.x);

        if (sprite != null)
        {
            sprite.flipX = spriteFacesRight
                ? facingX < 0f
                : facingX > 0f;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryDamagePlayer(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        TryDamagePlayer(other);
    }

    private void TryDamagePlayer(Collider2D other)
    {
        if (state != State.Seek &&
            state != State.Pass)
        {
            return;
        }

        if (hitPlayerThisPass ||
            playerHealth == null)
        {
            return;
        }

        PlayerHealth target =
            other.GetComponentInParent<PlayerHealth>();

        if (target != playerHealth)
            return;

        hitPlayerThisPass = true;

        playerHealth.TakeDamage(attackDamage);
    }

    public void TakeDamage(int damage)
    {
        if (state == State.Dead || damage <= 0)
            return;

        currentHealth -= damage;

        Debug.Log("Shadow health: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        // Knock the monster away from Fern.
        if (player != null)
        {
            recoilDirection =
                (body.position -
                 GetPlayerPosition()).normalized;
        }
        else
        {
            recoilDirection =
                new Vector2(-facingX, 0f);
        }

        if (recoilDirection.sqrMagnitude < 0.01f)
        {
            recoilDirection =
                new Vector2(-facingX, 0f);
        }

        state = State.Recoil;
        stateTimer = recoilDuration;

        body.linearVelocity =
            recoilDirection * recoilSpeed;

        Debug.Log("Shadow knocked back!");
    }

    private void Die()
    {
        state = State.Dead;

        body.linearVelocity = Vector2.zero;

        foreach (Collider2D col in
                 GetComponentsInChildren<Collider2D>())
        {
            col.enabled = false;
        }

        Debug.Log("Shadow defeated!");

        if (PlayAnimation("shadow_death"))
        {
            StartCoroutine(WaitForDeath());
        }
        else
        {
            Destroy(gameObject, 0.15f);
        }
    }

    private IEnumerator WaitForDeath()
    {
        float elapsed = 0f;

        yield return null;

        while (elapsed < 5f)
        {
            AnimatorStateInfo info =
                anim.GetCurrentAnimatorStateInfo(0);

            if (info.IsName("Base Layer.shadow_death") &&
                info.normalizedTime >= 1f)
            {
                break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

    private bool PlayAnimation(string animationName)
    {
        if (anim == null)
            return false;

        int hash = Animator.StringToHash(
            "Base Layer." + animationName
        );

        if (!anim.HasState(0, hash))
            return false;

        if (currentAnimation != animationName)
        {
            currentAnimation = animationName;
            anim.Play(hash, 0, 0f);
        }

        return true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            detectionRange
        );

        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            dashStartRange
        );
    }
}
