
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float doubleJumpForce = 9f;

    [Header("Jump Feel")]
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Header("Ground Detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.1f;

    private Rigidbody2D body;
    private Animator anim;

    private bool isAttacking = false;
    private bool grounded;

    private float moveInput;
    private float coyoteUntil = -1f;
    private float jumpQueuedUntil = -1f;

    private int jumpsUsed = 0;
    private const int maxJumps = 2;

    private float originalScaleX;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        originalScaleX = Mathf.Abs(transform.localScale.x);
    }

    void Update()
    {
        if (Keyboard.current == null)
        {
            moveInput = 0f;
            return;
        }

        // MOVEMENT INPUT
        moveInput = 0f;

        if (Keyboard.current.aKey.isPressed ||
            Keyboard.current.leftArrowKey.isPressed)
        {
            moveInput = -1f;
        }
        else if (Keyboard.current.dKey.isPressed ||
                 Keyboard.current.rightArrowKey.isPressed)
        {
            moveInput = 1f;
        }

        if (!isAttacking)
        {
            // JUMP BUFFER
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                jumpQueuedUntil = Time.time + jumpBufferTime;
            }

            // FLIP CHARACTER
            if (moveInput != 0f)
            {
                Vector3 scale = transform.localScale;

                scale.x = originalScaleX * Mathf.Sign(moveInput);

                transform.localScale = scale;
            }

            // STAFF ATTACK - J
            if (Keyboard.current.jKey.wasPressedThisFrame)
            {
                StartAttack("Attack1");
            }

            // LANTERN ATTACK - K
            else if (Keyboard.current.kKey.wasPressedThisFrame)
            {
                StartAttack("Attack2");
            }
        }
    }

    void FixedUpdate()
    {
        // GROUND DETECTION
        grounded = IsGrounded() &&
                   body.linearVelocity.y <= 0.05f;

        // RESET JUMPS WHEN LANDING
        if (grounded)
        {
            jumpsUsed = 0;
            coyoteUntil = Time.time + coyoteTime;
        }
        else if (Time.time > coyoteUntil && jumpsUsed == 0)
        {
            // Walking off an edge uses the first jump
            // after coyote time expires.
            jumpsUsed = 1;
        }

        if (!isAttacking)
        {
            // HORIZONTAL MOVEMENT
            body.linearVelocity = new Vector2(
                moveInput * moveSpeed,
                body.linearVelocity.y
            );

            // NORMAL JUMP AND DOUBLE JUMP
            if (jumpQueuedUntil >= Time.time &&
                jumpsUsed < maxJumps)
            {
                PerformJump();
            }
        }

        // ANIMATIONS
        anim.SetFloat(
            "Speed",
            isAttacking ? 0f : Mathf.Abs(moveInput)
        );

        anim.SetBool("IsGrounded", grounded);
    }

    private void PerformJump()
    {
        float force = jumpsUsed == 0
            ? jumpForce
            : doubleJumpForce;

        body.linearVelocity = new Vector2(
            body.linearVelocity.x,
            force
        );

        jumpsUsed++;

        grounded = false;
        coyoteUntil = -1f;
        jumpQueuedUntil = -1f;
    }

    private void StartAttack(string attackTrigger)
    {
        isAttacking = true;

        jumpQueuedUntil = -1f;

        // Stop horizontal movement but preserve gravity
        body.linearVelocity = new Vector2(
            0f,
            body.linearVelocity.y
        );

        anim.SetTrigger(attackTrigger);
    }

    // CALLED WHEN AN ATTACK ANIMATION FINISHES
    public void EndAttack()
    {
        isAttacking = false;
    }

    // CALLED WHEN FERN TAKES DAMAGE
    public void CancelAttack()
    {
        isAttacking = false;

        jumpQueuedUntil = -1f;

        anim.ResetTrigger("Attack1");
        anim.ResetTrigger("Attack2");
    }

    // CHECK IF FERN IS TOUCHING THE GROUND
    private bool IsGrounded()
    {
        if (groundCheck == null)
            return false;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            groundCheck.position,
            groundCheckRadius
        );

        foreach (Collider2D hit in hits)
        {
            if (!hit.isTrigger &&
                hit.attachedRigidbody != body &&
                hit.CompareTag("Ground"))
            {
                return true;
            }
        }

        return false;
    }

    // DISPLAY GROUND CHECK IN SCENE VIEW
    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
            return;

        Gizmos.color = Color.green;

        Gizmos.DrawWireSphere(
            groundCheck.position,
            groundCheckRadius
        );
    }
}
