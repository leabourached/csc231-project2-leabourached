
using UnityEngine;

[DefaultExecutionOrder(100)]
public class ParallaxBackground : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera followCamera;

    [Header("Parallax")]
    [Range(0f, 1f)]
    [SerializeField] private float parallaxX = 0.75f;

    [Range(0f, 1f)]
    [SerializeField] private float parallaxY = 1f;

    [Header("Background Settings")]
    [SerializeField] private bool loopHorizontally = true;

    private Vector3 startPosition;
    private Vector3 cameraStartPosition;

    private float spriteWidth;
    private float spriteHeight;

    private Vector3 centerOffset;

    void Start()
    {
        if (followCamera == null)
            followCamera = Camera.main;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        if (followCamera == null || sr == null ||
            sr.sprite == null)
        {
            Debug.LogError(
                "Parallax: Missing camera or sprite!",
                this
            );

            enabled = false;
            return;
        }

        startPosition = transform.position;
        cameraStartPosition =
            followCamera.transform.position;

        spriteWidth = sr.bounds.size.x;
        spriteHeight = sr.bounds.size.y;

        centerOffset =
            sr.bounds.center - transform.position;

        if (spriteWidth <= 0f)
        {
            enabled = false;
            return;
        }

        if (loopHorizontally)
        {
            float cameraHalfWidth =
                followCamera.orthographicSize *
                followCamera.aspect;

            int copies = Mathf.CeilToInt(
                cameraHalfWidth / spriteWidth
            ) + 1;

            for (int i = 1; i <= copies; i++)
            {
                CreateCopy(sr, -i);
                CreateCopy(sr, i);
            }
        }

        if (spriteHeight <
            followCamera.orthographicSize * 2f)
        {
            Debug.LogWarning(
                name + " is shorter than the camera view.",
                this
            );
        }
    }

    void LateUpdate()
    {
        Vector3 cameraPosition =
            followCamera.transform.position;

        Vector3 distance =
            cameraPosition - cameraStartPosition;

        float x = startPosition.x +
                  distance.x * parallaxX;

        float y = startPosition.y +
                  distance.y * parallaxY;

        if (loopHorizontally)
        {
            float difference =
                cameraPosition.x - (x + centerOffset.x);

            x += Mathf.Round(
                difference / spriteWidth
            ) * spriteWidth;
        }
        else
        {
            // Keep the sky centered on the camera.
            x = cameraPosition.x - centerOffset.x;
        }

        // Prevent the camera from revealing
        // the top or bottom of the background.
        float verticalMargin =
            spriteHeight / 2f -
            followCamera.orthographicSize;

        if (verticalMargin >= 0f)
        {
            y = Mathf.Clamp(
                y,
                cameraPosition.y - centerOffset.y
                    - verticalMargin,
                cameraPosition.y - centerOffset.y
                    + verticalMargin
            );
        }

        transform.position = new Vector3(
            x,
            y,
            startPosition.z
        );
    }

    private void CreateCopy(
        SpriteRenderer original,
        int index
    )
    {
        GameObject copy = new GameObject(
            "Parallax Copy " + index
        );

        copy.transform.SetParent(transform, false);

        copy.transform.localPosition =
            new Vector3(
                index * spriteWidth /
                transform.lossyScale.x,
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
