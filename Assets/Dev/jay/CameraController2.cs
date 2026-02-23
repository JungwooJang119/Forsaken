using UnityEngine;

[DisallowMultipleComponent]
public class CameraController2 : MonoBehaviour
{
    [Header("Refs")]
    public Transform player1;
    public Transform player2;

    [Header("Smoothing (same style as CameraController1)")]
    [Tooltip("Higher = faster (converted internally to SmoothDamp time). 0 = instant.")]
    public float smoothSpeedX = 5f;
    [Tooltip("Higher = faster (converted internally to SmoothDamp time). 0 = instant.")]
    public float smoothSpeedY = 5f;
    [Tooltip("Higher = faster (converted internally to SmoothDamp time). 0 = instant.")]
    public float smoothSpeedZ = 5f;

    [Header("Zoom")]
    [Tooltip("Higher = faster (converted internally to SmoothDamp time). 0 = instant.")]
    public float zoomSpeed = 10f;
    public float minZoom = 2f;
    public float maxZoom = 15f;
    public float zoomMultiplier = 1.5f;

    [Header("Bounds")]
    public Transform leftBound;
    public Transform rightBound;

    private Vector3 initialOffset;
    private Vector3 offsetDir;

    private float velocityX;
    private float velocityY;
    private float velocityZ;
    private float velocityZoom;

    private float currentZoom;

    private void Start()
    {
        if (player1 == null || player2 == null)
        {
            Debug.LogError("Camera Controller: Players not assigned!");
            enabled = false;
            return;
        }

        Vector3 midpoint = GetMidpoint();
        initialOffset = transform.position - midpoint;

        // Direction we "zoom" along (keeps controller2 behavior: zoom is distance along initial direction).
        offsetDir = initialOffset.sqrMagnitude > 0.000001f ? initialOffset.normalized : Vector3.back;

        // Initialize zoom to match the current camera position relative to midpoint.
        currentZoom = Vector3.Dot(transform.position - midpoint, offsetDir);
        if (currentZoom <= 0f) currentZoom = initialOffset.magnitude;
    }

    private void LateUpdate()
    {
        if (player1 == null || player2 == null) return;

        Vector3 midpoint = GetMidpoint();

        float playerDistance = Vector3.Distance(player1.position, player2.position);
        float desiredZoom = Mathf.Clamp(playerDistance * zoomMultiplier, minZoom, maxZoom);

        // Smooth zoom like CameraController1 does with orthographic size.
        float zoomSmoothTime = SpeedToSmoothTime(zoomSpeed);
        currentZoom = zoomSmoothTime <= 0f
            ? desiredZoom
            : Mathf.SmoothDamp(currentZoom, desiredZoom, ref velocityZoom, zoomSmoothTime);

        Vector3 desiredPosition = midpoint + offsetDir * currentZoom;

        // Same clamp behavior as your original controller2 (X only).
        if (leftBound != null && rightBound != null)
        {
            float minX = leftBound.position.x;
            float maxX = rightBound.position.x;
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
        }

        Vector3 current = transform.position;

        float smoothTimeX = SpeedToSmoothTime(smoothSpeedX);
        float smoothTimeY = SpeedToSmoothTime(smoothSpeedY);
        float smoothTimeZ = SpeedToSmoothTime(smoothSpeedZ);

        float x = smoothTimeX <= 0f ? desiredPosition.x : Mathf.SmoothDamp(current.x, desiredPosition.x, ref velocityX, smoothTimeX);
        float y = smoothTimeY <= 0f ? desiredPosition.y : Mathf.SmoothDamp(current.y, desiredPosition.y, ref velocityY, smoothTimeY);
        float z = smoothTimeZ <= 0f ? desiredPosition.z : Mathf.SmoothDamp(current.z, desiredPosition.z, ref velocityZ, smoothTimeZ);

        transform.position = new Vector3(x, y, z);
    }

    private Vector3 GetMidpoint()
    {
        return (player1.position + player2.position) * 0.5f;
    }

    // Converts "speed-like" values (bigger = faster) into SmoothDamp smoothTime (smaller = faster),
    // preserving the feel of your original Lerp(t = speed * dt) setup.
    private static float SpeedToSmoothTime(float speed)
    {
        if (speed <= 0f) return 0f;
        return 1f / speed;
    }
}