
using UnityEngine;

[DefaultExecutionOrder(110)]
public class FogDrift : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera followCamera;

    [Header("Fog Movement")]
    [SerializeField] private float driftSpeed = 0.25f;

    private Vector3 startPosition;
    private float spriteWidth;
    private float centerOffsetX;
    private float driftOffset;

    void Start()
    {
        if (followCamera == null)
            followCamera = Camera.main;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        if (sr == null || sr.sprite == null ||
            followCamera == null)
        {
            Debug.LogError("FogDrift: Missing sprite or camera!");
            enabled = false;
            return;
        }

        startPosition = transform.position;
        spriteWidth = sr.bounds.size.x;

        centerOffsetX =
            sr.bounds.center.x - transform.position.x;

        if (spriteWidth <= 0f)
        {
            enabled = false;
            return;
        }

        float cameraHalfWidth =
            followCamera.orthographicSize *
            followCamera.aspect;

        int copies = Mathf.CeilToInt(
            cameraHalfWidth / spriteWidth
        ) + 1;

        // Create repeating fog copies
        for (int i = 1; i <= copies; i++)
        {
            CreateCopy(sr, -i);
            CreateCopy(sr, i);
        }
    }

    void LateUpdate()
    {
        // Constant movement independent of Fern
        driftOffset = Mathf.Repeat(
            driftOffset + driftSpeed * Time.deltaTime,
            spriteWidth
        );

        float x = startPosition.x + driftOffset;

        // Recycle fog around the camera
        float difference =
            followCamera.transform.position.x -
            (x + centerOffsetX);

        x += Mathf.Round(
            difference / spriteWidth
        ) * spriteWidth;

        // Keep the original vertical position
        transform.position = new Vector3(
            x,
            startPosition.y,
            startPosition.z
        );
    }

    private void CreateCopy(
        SpriteRenderer original,
        int index
    )
    {
        GameObject copy = new GameObject(
            "Fog Copy " + index
        );

        copy.transform.SetParent(transform, false);

        copy.transform.localPosition = new Vector3(
            index * spriteWidth / transform.lossyScale.x,
            0f,
            0f
        );

        SpriteRenderer sr =
            copy.AddComponent<SpriteRenderer>();

        sr.sprite = original.sprite;
        sr.sharedMaterial = original.sharedMaterial;
        sr.sortingLayerID = original.sortingLayerID;
        sr.sortingOrder = original.sortingOrder;
        sr.color = original.color;
        sr.flipX = original.flipX;
        sr.flipY = original.flipY;
    }
}
