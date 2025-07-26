using UnityEngine;

public class AIPatrolRoute : MonoBehaviour
{
    public enum PatrolMode
    {
        Loop,       // Tuần tra theo vòng tròn
        PingPong,   // Tuần tra qua lại
        Once        // Chỉ đi một lần
    }

    public Transform[] patrolPoints;  // Các điểm tuần tra
    public PatrolMode patrolMode = PatrolMode.Loop;
    public bool isFortress;           // Nếu true, kẻ địch sẽ hoạt động xung quanh điểm đầu tiên
    public float pauseTimeAtPoint = 2f; // Thời gian dừng tại mỗi điểm

    private void OnDrawGizmos()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        // Vẽ các điểm tuần tra
        Gizmos.color = Color.yellow;
        foreach (var point in patrolPoints)
        {
            if (point != null)
            {
                Gizmos.DrawWireSphere(point.position, 0.5f);
            }
        }

        // Vẽ đường nối giữa các điểm
        if (!isFortress && patrolPoints.Length > 1)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < patrolPoints.Length - 1; i++)
            {
                if (patrolPoints[i] != null && patrolPoints[i + 1] != null)
                {
                    Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
                }
            }

            // Nối điểm cuối với điểm đầu nếu là chế độ Loop
            if (patrolMode == PatrolMode.Loop && patrolPoints[0] != null && patrolPoints[patrolPoints.Length - 1] != null)
            {
                Gizmos.DrawLine(patrolPoints[patrolPoints.Length - 1].position, patrolPoints[0].position);
            }
        }
        else if (isFortress && patrolPoints.Length > 0 && patrolPoints[0] != null)
        {
            // Vẽ vòng tròn cho chế độ pháo đài
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(patrolPoints[0].position, 5f);
        }
    }
}