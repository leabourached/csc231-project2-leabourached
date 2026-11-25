
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ShadowChaser : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform player;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float detectionRange = 10f;

    [Header("Attack")]
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private int attackDamage = 1;

    [Header("Health")]
    [SerializeField] private int maxHealth = 2;

    private Rigidbody2D body;
    private SpriteRenderer sprite;
    private PlayerHealth playerHealth;
    private PlayerController playerController;

    private int currentHealth;
    private float nextAttackTime;
    private bool isDead;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        sprite = GetComponentInChildren<SpriteRenderer>();
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
        }
    }

    void FixedUpdate()
    {
        if (isDead || player == null ||
            (playerController != null &&
             !playerController.enabled))
        {
            StopMoving();
            return;
        }

        Vector2 difference =
            player.position - transform.position;

        float distance = difference.magnitude;

        if (distance > detectionRange)
        {
            StopMoving();
            return;
        }

        if (sprite != null &&
            Mathf.Abs(difference.x) > 0.05f)
        {
            sprite.flipX = difference.x < 0;
        }

        if (distance <= attackRange)
        {
            StopMoving();

            if (Time.time >= nextAttackTime)
            {
                nextAttackTime =
                    Time.time + attackCooldown;

                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(attackDamage);
                }
            }
        }
        else
        {
            body.linearVelocity = new Vector2(
                Mathf.Sign(difference.x) * moveSpeed,
                body.linearVelocity.y
            );
        }
    }

    void StopMoving()
    {
        body.linearVelocity = new Vector2(
            0f,
            body.linearVelocity.y
        );
    }

    public void TakeDamage(int damage)
    {
        if (isDead || damage <= 0)
            return;

        currentHealth -= damage;

        Debug.Log("Shadow health: " + currentHealth);

        if (currentHealth <= 0)
        {
            isDead = true;
            Debug.Log("Shadow defeated!");
            Destroy(gameObject);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            transform.position, detectionRange
        );

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            transform.position, attackRange
        );
    }
}
