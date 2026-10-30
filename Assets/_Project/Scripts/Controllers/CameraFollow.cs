using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // Đối tượng cần theo dõi (ví dụ: nhân vật)
    public float followSpeed = 9f; // Tốc độ theo dõi
    public Vector3 offset = new Vector3(0, 0, -10); // Độ lệch giữa camera và đối tượng

    // Vị trí theo dõi (chưa cộng rung). Tách riêng để rung của VfxService không bị
    // Lerp ăn ngược vào vị trí camera.
    private Vector3 basePosition;
    private bool hasBase;

    void LateUpdate()
    {
        if (!hasBase) { basePosition = transform.position; hasBase = true; }

        if (target != null)
        {
            Vector3 desiredPosition = target.position + offset;
            // Làm mượt độc lập tốc độ khung hình (Lerp(speed*dt) giật khi dt đổi)
            basePosition = Vector3.Lerp(basePosition, desiredPosition, 1f - Mathf.Exp(-followSpeed * Time.deltaTime));
        }
        transform.position = SnapToPixel(basePosition + (Vector3)VfxService.ShakeOffset);
    }

    // Camera đứng đúng lưới pixel art (1/64 đơn vị) → tile/sprite không "bơi" khi camera lerp
    private static Vector3 SnapToPixel(Vector3 p)
    {
        const float u = PixelSprites.PixelSize;
        return new Vector3(Mathf.Round(p.x / u) * u, Mathf.Round(p.y / u) * u, p.z);
    }

    /// <summary>S3 (ScenePortal): đặt camera ngay lập tức tới target — teleport không lerp.</summary>
    public void SnapTo(Vector3 targetPosition)
    {
        basePosition = target != null ? target.position + offset : targetPosition + offset;
        hasBase = true;
        transform.position = basePosition;
    }
}
