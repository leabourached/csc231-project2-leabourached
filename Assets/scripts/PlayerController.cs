
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

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

    [Header("Staff Combat")]
    [SerializeField] private Transform staffHitPoint;
    [SerializeField] private float attackRadius = 0.8f;
    [SerializeField] private int attackDamage = 1;

    private Rigidbody2D body;
    private Animator anim;

    private bool isAttacking = false;
    private bool isStaffAttack = false;
    private bool staffHitUsed = false;

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

        originalScaleX = Mathf.Abs(
            transform.localScale.x
        );
    }

    void Update()
    {
        if (Keyboard.current == null)
        {
            moveInput = 0f;
            return;
        }

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
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                jumpQueuedUntil =
                    Time.time + jumpBufferTime;
            }

            if (moveInput != 0f)
            {
                Vector3 scale = transform.localScale;

                scale.x =
                    originalScaleX * Mathf.Sign(moveInput);

                transform.localScale = scale;
            }

            if (Keyboard.current.jKey.wasPressedThisFrame)
            {
                StartAttack("Attack1");
            }
            else if (Keyboard.current.kKey.wasPressedThisFrame)
            {
                StartAttack("Attack2");
            }
        }
    }

    void FixedUpdate()
    {
        grounded = IsGrounded() &&
                   body.linearVelocity.y <= 0.05f;

        if (grounded)
        {
            jumpsUsed = 0;
            coyoteUntil = Time.time + coyoteTime;
        }
        else if (Time.time > coyoteUntil && jumpsUsed == 0)
        {
            jumpsUsed = 1;
        }

        if (!isAttacking)
        {
            body.linearVelocity = new Vector2(
                moveInput * moveSpeed,
                body.linearVelocity.y
            );

            if (jumpQueuedUntil >= Time.time &&
                jumpsUsed < maxJumps)
            {
                PerformJump();
            }
        }

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
        isStaffAttack = attackTrigger == "Attack1";
        staffHitUsed = false;

        jumpQueuedUntil = -1f;

        body.linearVelocity = new Vector2(
            0f,
            body.linearVelocity.y
        );

        anim.SetTrigger(attackTrigger);
    }

    public void StaffHit()
    {
        if (!isActiveAndEnabled ||
            !isAttacking ||
            !isStaffAttack ||
            staffHitUsed ||
            staffHitPoint == null)
        {
            return;
        }

        staffHitUsed = true;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            staffHitPoint.position,
            attackRadius
        );

        HashSet<ShadowChaser> damagedEnemies =
            new HashSet<ShadowChaser>();

        foreach (Collider2D hit in hits)
        {
            ShadowChaser enemy =
                hit.GetComponentInParent<ShadowChaser>();

            if (enemy != null &&
                damagedEnemies.Add(enemy))
            {
                enemy.TakeDamage(attackDamage);

                Debug.Log("Fern hit a shadow monster!");
            }
        }
    }

    public void EndAttack()
    {
        isAttacking = false;
        isStaffAttack = false;
        staffHitUsed = false;
    }

    public void CancelAttack()
    {
        isAttacking = false;
        isStaffAttack = false;
        staffHitUsed = false;

        jumpQueuedUntil = -1f;

        anim.ResetTrigger("Attack1");
        anim.ResetTrigger("Attack2");
    }

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

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;

            Gizmos.DrawWireSphere(
                groundCheck.position,
                groundCheckRadius
            );
        }

        if (staffHitPoint != null)
        {
            Gizmos.color = Color.red;

            Gizmos.DrawWireSphere(
                staffHitPoint.position,
                attackRadius
            );
        }
    }
}
