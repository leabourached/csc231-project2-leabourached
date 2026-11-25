
using UnityEngine;

[RequireComponent(typeof(Camera))]
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

    [Header("Camera Restrictions")]
    [SerializeField] private BoxCollider2D levelBounds;

    private Camera cam;
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

        cam = GetComponent<Camera>();

        targetBody = target.GetComponent<Rigidbody2D>();

        cameraZ = transform.position.z;

        currentLookAhead = lookAheadDistance;

        Vector3 startPosition = new Vector3(
            target.position.x + currentLookAhead,
            target.position.y + verticalOffset,
            cameraZ
        );

        transform.position = RestrictPosition(startPosition);
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
        Vector3 desiredPosition = new Vector3(
            target.position.x + currentLookAhead,
            target.position.y + verticalOffset,
            cameraZ
        );

        // Keep desired position inside the level
        desiredPosition = RestrictPosition(desiredPosition);

        // Smooth horizontal following
        float smoothX = Mathf.SmoothDamp(
            transform.position.x,
            desiredPosition.x,
            ref horizontalVelocity,
            horizontalSmoothTime
        );

        // Smooth vertical following
        float smoothY = Mathf.SmoothDamp(
            transform.position.y,
            desiredPosition.y,
            ref verticalVelocity,
            verticalSmoothTime
        );

        Vector3 smoothPosition = new Vector3(
            smoothX,
            smoothY,
            cameraZ
        );

        // Ensure the camera never crosses the boundaries
        Vector3 restrictedPosition =
            RestrictPosition(smoothPosition);

        if (restrictedPosition.x != smoothPosition.x)
            horizontalVelocity = 0f;

        if (restrictedPosition.y != smoothPosition.y)
            verticalVelocity = 0f;

        transform.position = restrictedPosition;
    }

    private Vector3 RestrictPosition(Vector3 position)
    {
        if (levelBounds == null || cam == null)
            return position;

        Transform boundsTransform = levelBounds.transform;

        Vector3 center = boundsTransform.TransformPoint(
            levelBounds.offset
        );

        Vector3 scale = boundsTransform.lossyScale;

        float boundsHalfWidth =
            levelBounds.size.x * Mathf.Abs(scale.x) / 2f;

        float boundsHalfHeight =
            levelBounds.size.y * Mathf.Abs(scale.y) / 2f;

        float cameraHalfHeight = cam.orthographicSize;

        float cameraHalfWidth =
            cameraHalfHeight * cam.aspect;

        float minX = center.x - boundsHalfWidth
                     + cameraHalfWidth;

        float maxX = center.x + boundsHalfWidth
                     - cameraHalfWidth;

        float minY = center.y - boundsHalfHeight
                     + cameraHalfHeight;

        float maxY = center.y + boundsHalfHeight
                     - cameraHalfHeight;

        position.x = minX <= maxX
            ? Mathf.Clamp(position.x, minX, maxX)
            : center.x;

        position.y = minY <= maxY
            ? Mathf.Clamp(position.y, minY, maxY)
            : center.y;

        position.z = cameraZ;

        return position;
    }
}
