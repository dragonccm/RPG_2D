using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// AI cho Boss. Kế thừa EnemyAIController, chỉ cần gắn script này lên prefab là đủ cho AI Boss.
/// Luôn ưu tiên player máu thấp nhất, chỉ truy đuổi khi player trong vùng detection.
/// </summary>
[DisallowMultipleComponent]
public class BossAI : EnemyAIController
{
    void Reset()
    {
        enemyType = EnemyType.Boss;
    }

    /// <summary>
    /// Kiểm tra playerTarget có còn trong vùng detection không (nếu ra khỏi thì dừng truy đuổi).
    /// </summary>
    private bool IsPlayerInDetectionRange()
    {
        var enemy = GetComponent<Enemy>();
        float detectionRange = enemy != null ? enemy.detectionRange : 10f;
        if (playerTarget == null) return false;
        float distance = Vector3.Distance(transform.position, playerTarget.position);
        return distance <= detectionRange;
    }

    /// <summary>
    /// Xử lý khi AI được "báo động" về một mục tiêu (chỉ nhận player).
    /// </summary>
    public override void Alert(Transform target)
    {
        Debug.Log($"[BossAI] Alerted to target: {target?.name}");
        if (target != null && target.CompareTag("Player"))
        {
            var enemy = GetComponent<Enemy>();
            float detectionRange = enemy != null ? enemy.detectionRange : 10f;
            float dist = Vector3.Distance(transform.position, target.position);
            if (dist <= detectionRange)
            {
                playerTarget = target;
                ChangeState(chaseState);
            }
        }
    }

    /// <summary>
    /// Ưu tiên player máu thấp nhất trong vùng detection (override logic mặc định).
    /// </summary>
    public override Transform GetPriorityTarget(List<Transform> availableTargets)
    {
        // CHANGED: Luôn ưu tiên player máu thấp nhất trong vùng detection
        Transform priority = null;
        float minHP = float.MaxValue;
        foreach (var t in availableTargets)
        {
            if (t != null && t.CompareTag("Player"))
            {
                var c = t.GetComponent<Character>();
                float hp = c != null ? c.CurrentHealth : 0f;
                if (hp < minHP)
                {
                    minHP = hp;
                    priority = t;
                }
            }
        }
        return priority;
    }

    /// <summary>
    /// Update: Nếu playerTarget ra khỏi detection thì dừng truy đuổi, chuyển Idle.
    /// </summary>
    protected override void Update()
    {
        if (playerTarget != null && !IsPlayerInDetectionRange())
        {
            Debug.Log("[BossAI] Player out of detection range, stop chasing.");
            playerTarget = null;
            ChangeState(idleState);
        }
        base.Update();
    }

    private void OnEnable()
    {
        enabled = true;
    }
}