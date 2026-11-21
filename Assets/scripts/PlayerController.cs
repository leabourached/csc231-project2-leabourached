
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;

    [Header("Ground Detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.1f;

    private Rigidbody2D body;
    private Animator anim;

    private bool isAttacking = false;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (Keyboard.current == null)
            return;

        // MOVEMENT
        float moveInput = 0f;

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

        bool grounded = IsGrounded();

        if (!isAttacking)
        {
            body.linearVelocity = new Vector2(
                moveInput * moveSpeed,
                body.linearVelocity.y
            );

            // JUMP
            if (Keyboard.current.spaceKey.wasPressedThisFrame &&
                grounded)
            {
                body.linearVelocity = new Vector2(
                    body.linearVelocity.x,
                    jumpForce
                );
            }
        }

        // ANIMATIONS
        anim.SetFloat("Speed", Mathf.Abs(moveInput));
        anim.SetBool("IsGrounded", grounded);

        // FLIP CHARACTER
        if (moveInput > 0)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        else if (moveInput < 0)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }

        // ATTACK 1 - STAFF SWING (J)
        if (Keyboard.current.jKey.wasPressedThisFrame &&
            !isAttacking)
        {
            isAttacking = true;

            body.linearVelocity = Vector2.zero;

            anim.SetTrigger("Attack1");
        }

        // ATTACK 2 - LANTERN MAGIC (K)
        if (Keyboard.current.kKey.wasPressedThisFrame &&
            !isAttacking)
        {
            isAttacking = true;

            body.linearVelocity = Vector2.zero;

            anim.SetTrigger("Attack2");
        }
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
            if (hit.CompareTag("Ground"))
            {
                return true;
            }
        }

        return false;
    }
}
