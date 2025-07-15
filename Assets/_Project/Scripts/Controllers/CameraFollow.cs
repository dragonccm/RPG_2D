using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // Đối tượng cần theo dõi (ví dụ: nhân vật)
    public float followSpeed = 5f; // Tốc độ theo dõi
    public Vector3 offset = new Vector3(0, 0, -10); // Độ lệch giữa camera và đối tượng

    void LateUpdate()
    {
        if (target != null)
        {
            Vector3 desiredPosition = target.position + offset;
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
            transform.position = smoothedPosition;
        }
    }
}
