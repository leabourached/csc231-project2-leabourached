
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform target;

    [Header("Camera Position")]
    [SerializeField] private float verticalOffset = 1f;
    [SerializeField] private float lookAheadDistance = 1.5f;

    [Header("Camera Smoothness")]
    [SerializeField] private float horizontalSmoothTime = 0.15f;
    [SerializeField] private float verticalSmoothTime = 0.25f;
    [SerializeField] private float lookAheadSmoothTime = 0.3f;

    private Rigidbody2D targetBody;

    private float horizontalVelocity;
    private float verticalVelocity;
    private float lookAheadVelocity;

    private float currentLookAhead;
    private float facingDirection = 1f;
    private float cameraZ;

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("CameraFollow: Assign Fern as Target!");
            enabled = false;
            return;
        }

        targetBody = target.GetComponent<Rigidbody2D>();

        cameraZ = transform.position.z;

        currentLookAhead = lookAheadDistance;

        // Start at the player's position
        transform.position = new Vector3(
            target.position.x + currentLookAhead,
            target.position.y + verticalOffset,
            cameraZ
        );
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        // Detect movement direction
        if (targetBody != null)
        {
            float movement = targetBody.linearVelocity.x;

            if (Mathf.Abs(movement) > 0.1f)
            {
                facingDirection = Mathf.Sign(movement);
            }
        }

        // Smoothly look ahead
        float desiredLookAhead =
            facingDirection * lookAheadDistance;

        currentLookAhead = Mathf.SmoothDamp(
            currentLookAhead,
            desiredLookAhead,
            ref lookAheadVelocity,
            lookAheadSmoothTime
        );

        // Desired camera position
        float targetX =
            target.position.x + currentLookAhead;

        float targetY =
            target.position.y + verticalOffset;

        // Smooth horizontal following
        float smoothX = Mathf.SmoothDamp(
            transform.position.x,
            targetX,
            ref horizontalVelocity,
            horizontalSmoothTime
        );

        // Smooth vertical following
        float smoothY = Mathf.SmoothDamp(
            transform.position.y,
            targetY,
            ref verticalVelocity,
            verticalSmoothTime
        );

        transform.position = new Vector3(
            smoothX,
            smoothY,
            cameraZ
        );
    }
}
