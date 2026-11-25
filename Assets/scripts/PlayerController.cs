
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
    [SerializeField] private float fallGravityMultiplier = 2.2f;

    [Header("Ground Detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.1f;

    [Header("Staff Combat")]
    [SerializeField] private Transform staffHitPoint;
    [SerializeField] private float attackRadius = 0.8f;
    [SerializeField] private int attackDamage = 1;

    private Rigidbody2D body;
    private Animator anim;
    private LanternController lantern;

    private bool isAttacking;
    private bool isStaffAttack;
    private bool staffHitUsed;
    private bool grounded;

    private bool attackEndRequested;
    private int attackStateHash;
    private int attackStartFrame;

    private float moveInput;
    private float coyoteUntil = -1f;
    private float jumpQueuedUntil = -1f;

    private int jumpsUsed;
    private const int maxJumps = 2;

    private float originalScaleX;
    private float normalGravityScale;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        lantern = GetComponent<LanternController>();

        originalScaleX = Mathf.Abs(transform.localScale.x);
        normalGravityScale = body.gravityScale;
    }

    void Update()
    {
        moveInput = 0f;

        if (Keyboard.current != null)
        {
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
        }

        CheckAttackFinished();

        if (Keyboard.current == null)
            return;

        if (!isAttacking)
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                jumpQueuedUntil = Time.time + jumpBufferTime;
            }

            if (moveInput != 0f)
            {
                Vector3 scale = transform.localScale;

                scale.x = originalScaleX *
                    Mathf.Sign(moveInput);

                transform.localScale = scale;
            }

            if (Keyboard.current.jKey.wasPressedThisFrame)
            {
                StartAttack(true);
            }
            else if (Keyboard.current.kKey.wasPressedThisFrame)
            {
                StartAttack(false);
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

        float speedMultiplier = 1f;
        float pullVelocity = 0f;

        if (lantern != null)
        {
            speedMultiplier = lantern.SpeedMultiplier;
            pullVelocity = lantern.GetPullVelocity();
        }

        // Allow movement during airborne attacks.
        if (!isAttacking || !grounded)
        {
            float horizontalSpeed =
                moveInput * moveSpeed * speedMultiplier;

            body.linearVelocity = new Vector2(
                horizontalSpeed + pullVelocity,
                body.linearVelocity.y
            );
        }
        else
        {
            body.linearVelocity = new Vector2(
                pullVelocity,
                body.linearVelocity.y
            );
        }

        if (!isAttacking &&
            jumpQueuedUntil >= Time.time &&
            jumpsUsed < maxJumps)
        {
            PerformJump();
        }

        // Faster falling without reducing jump height.
        if (!grounded && body.linearVelocity.y < 0f)
        {
            body.gravityScale =
                normalGravityScale * fallGravityMultiplier;
        }
        else
        {
            body.gravityScale = normalGravityScale;
        }

        anim.SetFloat(
            "Speed",
            isAttacking && grounded
                ? 0f
                : Mathf.Abs(moveInput)
        );

        anim.SetBool("IsGrounded", grounded);
    }

    private void PerformJump()
    {
        float force = jumpsUsed == 0
            ? jumpForce
            : doubleJumpForce;

        body.gravityScale = normalGravityScale;

        body.linearVelocity = new Vector2(
            body.linearVelocity.x,
            force
        );

        jumpsUsed++;
        grounded = false;

        coyoteUntil = -1f;
        jumpQueuedUntil = -1f;
    }

    private void StartAttack(bool staffAttack)
    {
        string stateName = staffAttack
            ? "Base Layer.fern_attack"
            : "Base Layer.fern_lantern";

        int stateHash = Animator.StringToHash(stateName);

        if (!anim.HasState(0, stateHash))
        {
            Debug.LogError("Missing attack state: " + stateName);
            return;
        }

        isAttacking = true;
        isStaffAttack = staffAttack;
        staffHitUsed = false;

        attackEndRequested = false;
        attackStateHash = stateHash;
        attackStartFrame = Time.frameCount;

        jumpQueuedUntil = -1f;

        if (grounded)
        {
            body.linearVelocity = new Vector2(
                0f,
                body.linearVelocity.y
            );
        }

        anim.ResetTrigger("Attack1");
        anim.ResetTrigger("Attack2");

        // Immediately interrupt the current animation.
        anim.Play(attackStateHash, 0, 0f);
    }

    private void CheckAttackFinished()
    {
        if (!isAttacking)
            return;

        AnimatorStateInfo state =
            anim.GetCurrentAnimatorStateInfo(0);

        if (state.fullPathHash == attackStateHash)
        {
            // Backup if EndAttack is missing.
            if (attackEndRequested ||
                state.normalizedTime >= 1f)
            {
                FinishAttack();
            }
        }
        else if (Time.frameCount > attackStartFrame + 1)
        {
            // Prevent freezing if an attack is interrupted.
            CancelAttack();
        }
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

            if (enemy != null && damagedEnemies.Add(enemy))
            {
                enemy.TakeDamage(attackDamage);

                Debug.Log("Fern hit a shadow monster!");
            }
        }
    }

    public void EndAttack()
    {
        if (isAttacking)
        {
            attackEndRequested = true;
        }
    }

    private void FinishAttack()
    {
        CancelAttack();

        grounded = IsGrounded() &&
            body.linearVelocity.y <= 0.05f;

        anim.SetBool("IsGrounded", grounded);
        anim.SetFloat("Speed", Mathf.Abs(moveInput));

        // Return to the correct animation.
        if (!grounded)
        {
            anim.Play("Base Layer.fern_jump", 0, 0f);
        }
        else if (Mathf.Abs(moveInput) > 0.01f)
        {
            anim.Play("Base Layer.fern_run", 0, 0f);
        }
        else
        {
            anim.Play("Base Layer.fern_idle", 0, 0f);
        }
    }

    public void CancelAttack()
    {
        isAttacking = false;
        isStaffAttack = false;
        staffHitUsed = false;
        attackEndRequested = false;

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

    void OnDisable()
    {
        if (body != null)
        {
            body.gravityScale = normalGravityScale;
        }
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
