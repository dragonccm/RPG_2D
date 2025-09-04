using UnityEngine;
#if CINEMACHINE_PRESENT
using Cinemachine;
#endif

/// <summary>
/// Unified camera system to replace multiple camera controllers
/// Consolidates PlayerCameraController, CinematicCamera, and camera managers
/// </summary>
public class UnifiedCamera : MonoBehaviour
{
    [Header("Camera Settings")]
#if CINEMACHINE_PRESENT
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
#endif
    [SerializeField] private Transform target;
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -10f);

    [Header("Camera Bounds")]
    [SerializeField] private bool useBounds = true;
    [SerializeField] private Vector2 minBounds = new Vector2(-50f, -50f);
    [SerializeField] private Vector2 maxBounds = new Vector2(50f, 50f);

    [Header("Shake Settings")]
    [SerializeField] private float shakeAmplitude = 1f;
    [SerializeField] private float shakeFrequency = 1f;
    [SerializeField] private float shakeDuration = 0.5f;

    [Header("Zoom Settings")]
    [SerializeField] private float defaultZoom = 5f;
    [SerializeField] private float minZoom = 2f;
    [SerializeField] private float maxZoom = 10f;
    [SerializeField] private float zoomSpeed = 2f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogging = false;

#if CINEMACHINE_PRESENT
    private CinemachineBasicMultiChannelPerlin noise;
#endif
    private Coroutine shakeCoroutine;
    private float currentZoom;
    private Camera mainCamera;

    private void Awake()
    {
        InitializeCamera();
        ServiceLocator.RegisterService(this);
    }

    private void Start()
    {
        currentZoom = defaultZoom;
        UpdateZoom();
    }

    private void LateUpdate()
    {
#if CINEMACHINE_PRESENT
        if (target != null && virtualCamera == null)
#endif
        {
            FollowTarget();
        }
    }

    private void InitializeCamera()
    {
        mainCamera = Camera.main;

#if CINEMACHINE_PRESENT
        if (virtualCamera == null)
        {
            virtualCamera = GetComponent<CinemachineVirtualCamera>();
        }

        if (virtualCamera != null)
        {
            // Get noise component for screen shake
            noise = virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

            // Set initial target
            if (target != null)
            {
                virtualCamera.Follow = target;
                virtualCamera.LookAt = target;
            }
        }
        else
#endif
        {
            // Fallback to manual camera control
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }
    }

    /// <summary>
    /// Set camera target to follow
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

#if CINEMACHINE_PRESENT
        if (virtualCamera != null)
        {
            virtualCamera.Follow = newTarget;
            virtualCamera.LookAt = newTarget;
        }
#endif

        if (enableDebugLogging && newTarget != null)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("📷 Camera target set to: {0}", newTarget.name));
        }
    }

    /// <summary>
    /// Set camera offset
    /// </summary>
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;

#if CINEMACHINE_PRESENT
        if (virtualCamera != null)
        {
            var transposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
            if (transposer != null)
            {
                transposer.m_FollowOffset = offset;
            }
        }
#endif

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("📷 Camera offset set to: {0}", newOffset));
        }
    }

    /// <summary>
    /// Follow target with manual camera control (fallback)
    /// </summary>
    private void FollowTarget()
    {
        if (target == null || mainCamera == null) return;

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(mainCamera.transform.position, desiredPosition, followSpeed * Time.deltaTime);

        // Apply bounds if enabled
        if (useBounds)
        {
            smoothedPosition.x = Mathf.Clamp(smoothedPosition.x, minBounds.x, maxBounds.x);
            smoothedPosition.y = Mathf.Clamp(smoothedPosition.y, minBounds.y, maxBounds.y);
        }

        mainCamera.transform.position = smoothedPosition;
    }

    /// <summary>
    /// Shake camera
    /// </summary>
    public void Shake(float amplitude = -1f, float frequency = -1f, float duration = -1f)
    {
#if CINEMACHINE_PRESENT
        if (noise == null) return;

        // Use default values if not specified
        float amp = amplitude > 0 ? amplitude : shakeAmplitude;
        float freq = frequency > 0 ? frequency : shakeFrequency;
        float dur = duration > 0 ? duration : shakeDuration;

        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }

        shakeCoroutine = StartCoroutine(ShakeCoroutine(amp, freq, dur));

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("📳 Camera shake: {0}s", dur));
        }
#else
        if (enableDebugLogging)
        {
            PerformanceUtils.Log("📳 Camera shake requested but Cinemachine not available");
        }
#endif
    }

#if CINEMACHINE_PRESENT
    private System.Collections.IEnumerator ShakeCoroutine(float amplitude, float frequency, float duration)
    {
        noise.m_AmplitudeGain = amplitude;
        noise.m_FrequencyGain = frequency;

        yield return new WaitForSeconds(duration);

        noise.m_AmplitudeGain = 0f;
        noise.m_FrequencyGain = 0f;
    }
#endif

    /// <summary>
    /// Zoom camera in/out
    /// </summary>
    public void Zoom(float zoomAmount)
    {
        currentZoom = Mathf.Clamp(currentZoom - zoomAmount * zoomSpeed * Time.deltaTime, minZoom, maxZoom);
        UpdateZoom();

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("🔍 Camera zoom: {0}", currentZoom));
        }
    }

    /// <summary>
    /// Set camera zoom directly
    /// </summary>
    public void SetZoom(float zoom)
    {
        currentZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
        UpdateZoom();
    }

    /// <summary>
    /// Reset camera zoom to default
    /// </summary>
    public void ResetZoom()
    {
        currentZoom = defaultZoom;
        UpdateZoom();
    }

    private void UpdateZoom()
    {
#if CINEMACHINE_PRESENT
        if (virtualCamera != null)
        {
            var framingTransposer = virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
            if (framingTransposer != null)
            {
                framingTransposer.m_CameraDistance = currentZoom;
            }
        }
        else
#endif
        if (mainCamera != null)
        {
            // Manual zoom for perspective camera
            mainCamera.orthographicSize = currentZoom;
        }
    }

    /// <summary>
    /// Move camera to specific position
    /// </summary>
    public void MoveToPosition(Vector3 position, float duration = 1f)
    {
        StartCoroutine(MoveToPositionCoroutine(position, duration));
    }

    private System.Collections.IEnumerator MoveToPositionCoroutine(Vector3 targetPosition, float duration)
    {
        if (mainCamera == null) yield break;

        Vector3 startPosition = mainCamera.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Smooth step for better easing
            t = t * t * (3f - 2f * t);

            mainCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        mainCamera.transform.position = targetPosition;
    }

    /// <summary>
    /// Rotate camera around target
    /// </summary>
    public void RotateAroundTarget(float angle, float duration = 1f)
    {
        StartCoroutine(RotateAroundTargetCoroutine(angle, duration));
    }

    private System.Collections.IEnumerator RotateAroundTargetCoroutine(float angle, float duration)
    {
        if (target == null || mainCamera == null) yield break;

        Quaternion startRotation = mainCamera.transform.rotation;
        Quaternion endRotation = Quaternion.Euler(0f, angle, 0f) * startRotation;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            mainCamera.transform.rotation = Quaternion.Lerp(startRotation, endRotation, t);
            yield return null;
        }

        mainCamera.transform.rotation = endRotation;
    }

    /// <summary>
    /// Set camera bounds
    /// </summary>
    public void SetBounds(Vector2 min, Vector2 max)
    {
        minBounds = min;
        maxBounds = max;

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("📐 Camera bounds set: {0} to {1}", min, max));
        }
    }

    /// <summary>
    /// Enable/disable camera bounds
    /// </summary>
    public void EnableBounds(bool enable)
    {
        useBounds = enable;

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("📐 Camera bounds: {0}", enable ? "enabled" : "disabled"));
        }
    }

    /// <summary>
    /// Get world point from screen point
    /// </summary>
    public Vector3 ScreenToWorldPoint(Vector3 screenPoint)
    {
        if (mainCamera != null)
        {
            return mainCamera.ScreenToWorldPoint(screenPoint);
        }
        return screenPoint;
    }

    /// <summary>
    /// Get screen point from world point
    /// </summary>
    public Vector3 WorldToScreenPoint(Vector3 worldPoint)
    {
        if (mainCamera != null)
        {
            return mainCamera.WorldToScreenPoint(worldPoint);
        }
        return worldPoint;
    }

    /// <summary>
    /// Check if point is visible on screen
    /// </summary>
    public bool IsPointVisible(Vector3 worldPoint)
    {
        if (mainCamera == null) return false;

        Vector3 screenPoint = mainCamera.WorldToViewportPoint(worldPoint);
        return screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1;
    }

    /// <summary>
    /// Get camera frustum planes
    /// </summary>
    public Plane[] GetFrustumPlanes()
    {
        if (mainCamera != null)
        {
            return GeometryUtility.CalculateFrustumPlanes(mainCamera);
        }
        return new Plane[0];
    }

    /// <summary>
    /// Focus camera on multiple targets
    /// </summary>
    public void FocusOnGroup(Transform[] targets, float padding = 1f)
    {
        if (targets == null || targets.Length == 0) return;

        // Calculate bounds of all targets
        Bounds bounds = new Bounds(targets[0].position, Vector3.zero);
        foreach (var t in targets)
        {
            bounds.Encapsulate(t.position);
        }

        // Add padding
        bounds.Expand(padding);

        // Calculate optimal camera position and zoom
        Vector3 center = bounds.center;
        float maxExtent = Mathf.Max(bounds.extents.x, bounds.extents.y);

        // Position camera to frame all targets
        Vector3 cameraPosition = center + offset.normalized * (maxExtent * 2f);
        MoveToPosition(cameraPosition, 1f);

        // Adjust zoom to fit all targets
        float requiredZoom = maxExtent * 2f;
        SetZoom(Mathf.Clamp(requiredZoom, minZoom, maxZoom));

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("🎯 Camera focused on {0} targets", targets.Length));
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize camera bounds
        if (useBounds)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(new Vector3(minBounds.x, minBounds.y, 0), new Vector3(maxBounds.x, minBounds.y, 0));
            Gizmos.DrawLine(new Vector3(maxBounds.x, minBounds.y, 0), new Vector3(maxBounds.x, maxBounds.y, 0));
            Gizmos.DrawLine(new Vector3(maxBounds.x, maxBounds.y, 0), new Vector3(minBounds.x, maxBounds.y, 0));
            Gizmos.DrawLine(new Vector3(minBounds.x, maxBounds.y, 0), new Vector3(minBounds.x, minBounds.y, 0));
        }

        // Visualize camera offset
        if (target != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(target.position, target.position + offset);
            Gizmos.DrawSphere(target.position + offset, 0.5f);
        }
    }
}
