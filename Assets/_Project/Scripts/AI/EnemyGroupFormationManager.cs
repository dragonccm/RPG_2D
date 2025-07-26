using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

public enum FormationType { Line, V, Circle, Custom }

[DisallowMultipleComponent]
public class EnemyGroupFormationManager : MonoBehaviour
{
    [Header("Nh�m k? ??ch")]
    public List<EnemyAIController> members = new List<EnemyAIController>();

    [Header("Formation")]
    public FormationType formationType = FormationType.Line;
    public float formationSpacing = 2f;
    public Transform anchor; // Anchor ho?c leader
    public List<Vector3> customOffsets; // N?u formationType = Custom

    [Header("Patrol")]
    public AIPatrolRoute patrolRoute;
    private int currentPatrolIndex = 0;
    private int patrolDirection = 1;
    private float pauseTimer = 0f;

    [Header("Avoidance")]
    public bool useNavMeshAvoidance = true;
    public float staggeredPathInterval = 0.15f;

    private float nextPathUpdateTime = 0f;

    [Header("Combat")]
    public float detectionRadius = 10f;
    public LayerMask playerLayer;
    public Transform groupTarget;

    private bool isInCombat = false;

    void Start()
    {
        CalculateFormationOffsets();
        AssignInitialPositions();
    }

    void Update()
    {
        DetectPlayer();

        if (isInCombat)
        {
            // Logic chiến đấu có thể được thêm vào đây nếu cần
            return; // Không cập nhật di chuyển tuần tra nữa
        }

        if (Time.time >= nextPathUpdateTime)
        {
            UpdateGroupMovement();
            nextPathUpdateTime = Time.time + staggeredPathInterval;
        }
    }

    void DetectPlayer()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius, playerLayer);
        if (hitColliders.Length > 0)
        {
            if (!isInCombat)
            {
                groupTarget = hitColliders[0].transform;
                ActivateCombatMode();
            }
        }
        else
        {
            if (isInCombat)
            {
                groupTarget = null;
                DeactivateCombatMode();
            }
        }
    }

    void ActivateCombatMode()
    {
        isInCombat = true;

        // Ra lệnh cho tất cả các thành viên tấn công
        foreach (var member in members)
        {
            member.stateMachine.ChangeState(member.chaseState);
        }
    }

    void DeactivateCombatMode()
    {
        isInCombat = false;
        // Chuyển tất cả thành viên về trạng thái tuần tra
        foreach (var member in members)
        {
            member.stateMachine.ChangeState(member.patrolState);
        }
    }

    void CalculateFormationOffsets()
    {
        customOffsets = new List<Vector3>();
        int count = members.Count;
        switch (formationType)
        {
            case FormationType.Line:
                for (int i = 0; i < count; i++)
                    customOffsets.Add(Vector3.right * formationSpacing * (i - count / 2f));
                break;
            case FormationType.V:
                for (int i = 0; i < count; i++)
                {
                    float x = (i - count / 2f) * formationSpacing;
                    float z = Mathf.Abs(i - count / 2f) * formationSpacing * 0.5f;
                    customOffsets.Add(new Vector3(x, 0, z));
                }
                break;
            case FormationType.Circle:
                float radius = formationSpacing * count / Mathf.PI;
                for (int i = 0; i < count; i++)
                {
                    float angle = i * Mathf.PI * 2f / count;
                    customOffsets.Add(new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius);
                }
                break;
            case FormationType.Custom:
                break;
        }
    }

    void AssignInitialPositions()
    {
        for (int i = 0; i < members.Count; i++)
        {
            if (anchor != null)
                members[i].transform.position = anchor.position + customOffsets[i];
        }
    }

    void UpdateGroupMovement()
    {
        if (patrolRoute == null || patrolRoute.patrolPoints == null || patrolRoute.patrolPoints.Length == 0) return;

        Vector3 anchorPos = anchor != null ? anchor.position : transform.position;
        Vector3 targetPos;

        if (patrolRoute.isFortress)
        {
            // Chế độ pháo đài: di chuyển xung quanh điểm đầu tiên
            targetPos = patrolRoute.patrolPoints[0].position + (Random.insideUnitSphere * 5f);
            targetPos.y = patrolRoute.patrolPoints[0].position.y;
        }
        else
        {
            targetPos = patrolRoute.patrolPoints[currentPatrolIndex].position;
            if (Vector3.Distance(anchorPos, targetPos) < 1f)
            {
                if (pauseTimer <= 0)
                {
                    // Chuyển đến điểm tiếp theo sau khi đã dừng đủ thời gian
                    switch (patrolRoute.patrolMode)
                    {
                        case AIPatrolRoute.PatrolMode.Loop:
                            currentPatrolIndex = (currentPatrolIndex + 1) % patrolRoute.patrolPoints.Length;
                            break;
                        case AIPatrolRoute.PatrolMode.PingPong:
                            if (currentPatrolIndex >= patrolRoute.patrolPoints.Length - 1) patrolDirection = -1;
                            else if (currentPatrolIndex <= 0) patrolDirection = 1;
                            currentPatrolIndex += patrolDirection;
                            break;
                        case AIPatrolRoute.PatrolMode.Once:
                            if (currentPatrolIndex < patrolRoute.patrolPoints.Length - 1)
                                currentPatrolIndex++;
                            break;
                    }
                    pauseTimer = patrolRoute.pauseTimeAtPoint;
                }
                else
                {
                    pauseTimer -= Time.deltaTime;
                    return; // Đợi tại điểm hiện tại
                }
            }
        }

        ApplyFormation(targetPos);
    }

    void ApplyFormation(Vector3 targetPos)
    {
        for (int i = 0; i < members.Count; i++)
        {
            if (members[i] != null)
            {
                Vector3 formationTarget = targetPos + customOffsets[i];
                members[i].SetNavDestination(formationTarget);

                var agent = members[i].GetComponent<NavMeshAgent>();
                if (agent != null && useNavMeshAvoidance)
                {
                    agent.avoidancePriority = 50 + i;
                }
            }
        }
    }
}