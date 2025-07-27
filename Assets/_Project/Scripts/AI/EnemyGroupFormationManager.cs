using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

public enum FormationType { Line, V, Circle, Custom }

[DisallowMultipleComponent]
public class EnemyGroupFormationManager : MonoBehaviour
{
    [Header("Nhóm kẻ địch")]
    public List<EnemyAIController> members = new List<EnemyAIController>();

    [Header("Formation")]
    public FormationType formationType = FormationType.Line;
    public float formationSpacing = 2f;
    public Transform anchor; // Anchor hoặc leader
    public List<Vector3> customOffsets; // Nếu formationType = Custom

    [Header("Patrol Route")]
    public AIPatrolRoute patrolRoute; // Cho phép thiết lập lộ trình tuần tra
    private int currentPatrolIndex = 0;
    private int patrolDirection = 1;
    private float pauseTimer = 0f;

    [Header("Fortress Patrol Settings")]
    public float fortressRadius = 5f; // Bán kính tuần quanh điểm gốc nếu là fortress

    [Header("Avoidance")]
    public bool useNavMeshAvoidance = true;
    public float staggeredPathInterval = 0.15f;
    private float nextPathUpdateTime = 0f;
    private Dictionary<EnemyAIController, Vector3> lastMemberPositions = new Dictionary<EnemyAIController, Vector3>();
    private Dictionary<EnemyAIController, float> lastPathUpdateTime = new Dictionary<EnemyAIController, float>();
    private const float MIN_PATH_UPDATE_INTERVAL = 0.5f;
    
    // Biến theo dõi vị trí và thời gian kiểm tra của anchor
    private Vector3 lastAnchorPosition = Vector3.zero;
    private float lastAnchorCheckTime = 0f;

    [Header("Combat")]
    public float detectionRadius = 10f;
    public LayerMask playerLayer;
    public Transform groupTarget;
    private bool isInCombat = false;
    public bool IsInCombat => isInCombat;

    [Header("Movement Settings")]
    public float groupMoveSpeed = 5f;
    public float groupAcceleration = 8f;
    public float groupStoppingDistance = 1f;

    void Start()
    {
        CalculateFormationOffsets();
        AssignInitialPositions();
        
        // Đảm bảo anchor được thiết lập
        if (anchor == null)
        {
            anchor = transform;
        }

        // Thiết lập NavMeshAgent cho các thành viên
        foreach (var member in members)
        {
            if (member != null)
            {
                var agent = member.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    agent.speed = groupMoveSpeed;
                    agent.acceleration = groupAcceleration;
                    agent.stoppingDistance = groupStoppingDistance;
                    agent.updateRotation = true;
                    agent.updatePosition = true;
                }
            }
        }
    }

    void Update()
    {
        DetectPlayer();
        if (isInCombat)
        {
            UpdateGroupCombatPositions();
            return;
        }

        // Kiểm tra trạng thái NavMeshAgent của các thành viên
        bool allAgentsValid = true;
        foreach (var member in members)
        {
            if (member == null) continue;
            var agent = member.GetComponent<NavMeshAgent>();
            if (agent == null || !agent.isOnNavMesh)
            {
                allAgentsValid = false;
                Debug.LogError($"[EnemyGroupFormationManager] Thành viên {member.name} không có NavMeshAgent hợp lệ!");
            }
        }

        if (!allAgentsValid)
        {
            Debug.LogError("[EnemyGroupFormationManager] Một số thành viên không có NavMeshAgent hợp lệ, tạm dừng tuần tra.");
            return;
        }

        if (Time.time >= nextPathUpdateTime)
        {
            UpdateGroupMovement();
            nextPathUpdateTime = Time.time + staggeredPathInterval;

            // Debug thông tin tuần tra
            if (patrolRoute != null && patrolRoute.patrolPoints != null && patrolRoute.patrolPoints.Length > 0)
            {
                string patrolMode = patrolRoute.isFortress ? "Fortress" : patrolRoute.patrolMode.ToString();
                Debug.Log($"[EnemyGroupFormationManager] Đang tuần tra: Điểm hiện tại = {currentPatrolIndex}, Chế độ = {patrolMode}, Thời gian chờ = {pauseTimer:F1}s");
            }
        }
    }

    void DetectPlayer()
    {
        // Sử dụng vị trí trung tâm của nhóm để phát hiện người chơi
        Vector3 detectionCenter = transform.position;
        
        // Tính toán vị trí trung tâm dựa trên anchor và các thành viên
        if (anchor != null)
        {
            // Ưu tiên sử dụng vị trí anchor
            detectionCenter = anchor.position;
        }
        else if (members.Count > 0)
        {
            // Tính toán vị trí trung tâm của các thành viên
            Vector3 groupCenter = Vector3.zero;
            int validMembers = 0;
            
            foreach (var member in members)
            {
                if (member != null)
                {
                    groupCenter += member.transform.position;
                    validMembers++;
                }
            }
            
            if (validMembers > 0)
            {
                detectionCenter = groupCenter / validMembers;
            }
        }
        
        // Tính toán bán kính phát hiện dựa trên số lượng thành viên
        float effectiveDetectionRadius = detectionRadius;
        int activeMembers = members.Count;
        if (activeMembers > 1)
        {
            // Tăng bán kính phát hiện khi có nhiều thành viên
            effectiveDetectionRadius += (activeMembers - 1) * 0.5f;
            effectiveDetectionRadius = Mathf.Min(effectiveDetectionRadius, detectionRadius * 1.5f); // Giới hạn tối đa
        }
        
        // Phát hiện người chơi trong vùng phát hiện
        Collider[] hitColliders = Physics.OverlapSphere(detectionCenter, effectiveDetectionRadius, playerLayer);
        
        // Kiểm tra xem có thành viên nào đang trong trạng thái chiến đấu không
        bool anyMemberInCombat = false;
        foreach (var member in members)
        {
            if (member != null && member.stateMachine.currentState == member.attackState)
            {
                anyMemberInCombat = true;
                break;
            }
        }
        
        // Nếu có thành viên đang tấn công, tăng bán kính phát hiện để duy trì trạng thái chiến đấu
        if (anyMemberInCombat)
        {
            effectiveDetectionRadius *= 1.3f;
        }
        
        Debug.Log($"[EnemyGroupFormationManager] Phát hiện {hitColliders.Length} người chơi trong vùng phát hiện (bán kính: {effectiveDetectionRadius:F2}m).");
        
        if (hitColliders.Length > 0)
        {
            // Tìm người chơi gần nhất và có thể nhìn thấy
            Transform bestTarget = null;
            float bestScore = float.MinValue;
            
            foreach (var hit in hitColliders)
            {
                if (hit != null && hit.CompareTag("Player"))
                {
                    // Kiểm tra tầm nhìn
                    bool canSeePlayer = true;
                    Vector3 directionToPlayer = (hit.transform.position - detectionCenter).normalized;
                    float distanceToPlayer = Vector3.Distance(detectionCenter, hit.transform.position);
                    
                    // Kiểm tra xem có vật cản giữa nhóm và người chơi không
                    if (Physics.Raycast(detectionCenter, directionToPlayer, out RaycastHit rayHit, distanceToPlayer))
                    {
                        if (!rayHit.transform.CompareTag("Player"))
                        {
                            canSeePlayer = false;
                            Debug.Log($"[EnemyGroupFormationManager] Không thể nhìn thấy người chơi {hit.transform.name} do bị chặn bởi {rayHit.transform.name}.");
                        }
                    }
                    
                    if (canSeePlayer)
                    {
                        // Tính điểm ưu tiên dựa trên khoảng cách và tầm nhìn
                        float distanceScore = 1.0f - (distanceToPlayer / effectiveDetectionRadius); // 0-1, càng gần càng cao
                        float score = distanceScore;
                        
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestTarget = hit.transform;
                        }
                    }
                }
            }
            
            if (bestTarget != null)
            {
                float distanceToBest = Vector3.Distance(detectionCenter, bestTarget.position);
                
                // Nếu chưa trong chiến đấu hoặc mục tiêu thay đổi
                if (!isInCombat || groupTarget != bestTarget)
                {
                    Debug.Log($"[EnemyGroupFormationManager] Phát hiện người chơi {bestTarget.name} ở khoảng cách {distanceToBest:F2}m.");
                    groupTarget = bestTarget;
                    ActivateCombatMode();
                }
            }
        }
        else
        {
            // Kiểm tra xem người chơi đã ra khỏi vùng phát hiện chưa
            if (isInCombat && groupTarget != null)
            {
                float distanceToTarget = Vector3.Distance(detectionCenter, groupTarget.position);
                float deactivationThreshold = effectiveDetectionRadius * 1.2f; // Thêm buffer để tránh chuyển đổi liên tục
                
                if (distanceToTarget > deactivationThreshold)
                {
                    Debug.Log($"[EnemyGroupFormationManager] Người chơi {groupTarget.name} đã ra khỏi vùng phát hiện ({distanceToTarget:F2}m > {deactivationThreshold:F2}m).");
                    
                    // Thêm độ trễ trước khi hủy chế độ chiến đấu
                    StartCoroutine(DelayedDeactivateCombat(1.5f));
                }
            }
            else if (isInCombat && groupTarget == null)
            {
                Debug.Log("[EnemyGroupFormationManager] Không còn phát hiện người chơi, quay lại tuần tra.");
                DeactivateCombatMode();
            }
        }
    }
    
    // Coroutine để tạo độ trễ trước khi hủy chế độ chiến đấu
    System.Collections.IEnumerator DelayedDeactivateCombat(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Kiểm tra lại xem người chơi có quay lại vùng phát hiện không
        if (groupTarget != null)
        {
            Vector3 detectionCenter = anchor != null ? anchor.position : transform.position;
            float distanceToTarget = Vector3.Distance(detectionCenter, groupTarget.position);
            
            if (distanceToTarget > detectionRadius * 1.2f)
            {
                Debug.Log($"[EnemyGroupFormationManager] Xác nhận người chơi đã ra khỏi vùng phát hiện sau {delay}s, hủy chế độ chiến đấu.");
                groupTarget = null;
                DeactivateCombatMode();
            }
            else
            {
                Debug.Log($"[EnemyGroupFormationManager] Người chơi đã quay lại vùng phát hiện, duy trì chế độ chiến đấu.");
            }
        }
        else
        {
            DeactivateCombatMode();
        }
    }

    void ActivateCombatMode()
    {
        isInCombat = true;
        foreach (var member in members)
        {
            member.playerTarget = groupTarget;
            member.stateMachine.ChangeState(member.chaseState);
        }
    }

    void DeactivateCombatMode()
    {
        isInCombat = false;
        Debug.Log("[EnemyGroupFormationManager] Deactivating combat mode, returning to patrol.");
        
        // Reset các biến theo dõi vị trí để đảm bảo cập nhật đường đi mới
        lastMemberPositions.Clear();
        lastPathUpdateTime.Clear();
        
        foreach (var member in members)
        {
            if (member == null) continue;
            
            // Xóa target
            member.playerTarget = null;
            
            // Kiểm tra khoảng cách đến anchor
            float distToAnchor = Vector3.Distance(member.transform.position, anchor != null ? anchor.position : transform.position);
            
            // Nếu quá xa anchor, chuyển sang ReturnToAnchorState
            if (distToAnchor > groupStoppingDistance * 3f)
            {
                Debug.Log($"[EnemyGroupFormationManager] Thành viên {member.name} quá xa anchor ({distToAnchor:F2}m), chuyển sang ReturnToAnchorState.");
                member.stateMachine.ChangeState(new ReturnToAnchorState(member, member.stateMachine, groupStoppingDistance));
            }
            // Nếu đủ gần và có patrolState, chuyển sang patrolState
            else if (member.patrolState != null)
            {
                Debug.Log($"[EnemyGroupFormationManager] Thành viên {member.name} chuyển sang PatrolState.");
                member.stateMachine.ChangeState(member.patrolState);
            }
            // Nếu không có patrolState, chuyển sang idleState
            else
            {
                Debug.Log($"[EnemyGroupFormationManager] Thành viên {member.name} không có PatrolState, chuyển sang IdleState.");
                member.stateMachine.ChangeState(member.idleState);
            }
            
            // Reset NavMeshAgent để đảm bảo cập nhật đường đi mới
            var agent = member.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null && agent.isOnNavMesh)
            {
                agent.ResetPath();
            }
        }
        
        // Cập nhật ngay lập tức để áp dụng formation mới
        nextPathUpdateTime = Time.time;
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
                // Sử dụng customOffsets đã thiết lập
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
        Debug.Log($"[EnemyGroupFormationManager] Bắt đầu tuần tra - Anchor: {(anchor != null ? anchor.name : "null")} | Số thành viên: {members.Count}");
        if (patrolRoute == null || patrolRoute.patrolPoints == null || patrolRoute.patrolPoints.Length == 0)
        {
            Debug.LogWarning("[EnemyGroupFormationManager] Không tìm thấy patrolRoute hoặc patrolPoints!");
            return;
        }

        // Cập nhật vị trí anchor
        if (anchor != null)
        {
            Vector3 currentTargetPos = patrolRoute.patrolPoints[currentPatrolIndex].position;
            NavMeshAgent anchorAgent = anchor.GetComponent<NavMeshAgent>();
            if (anchorAgent != null)
            {
                // Vẽ đường từ anchor đến điểm đích để debug
                Debug.DrawLine(anchor.position, currentTargetPos, Color.blue, 0.1f);
                
                // Kiểm tra xem anchor có bị kẹt không
                bool anchorStuck = false;
                if (lastAnchorPosition != Vector3.zero)
                {
                    float distanceMoved = Vector3.Distance(anchor.position, lastAnchorPosition);
                    float timeSinceLastCheck = Time.time - lastAnchorCheckTime;
                    
                    // Nếu anchor di chuyển quá ít trong khoảng thời gian đáng kể và chưa đến đích
                    if (distanceMoved < 0.05f && timeSinceLastCheck > 1.0f && 
                        Vector3.Distance(anchor.position, currentTargetPos) > anchorAgent.stoppingDistance * 1.5f)
                    {
                        anchorStuck = true;
                        Debug.LogWarning($"[EnemyGroupFormationManager] Anchor có vẻ bị kẹt: Di chuyển {distanceMoved:F2}m trong {timeSinceLastCheck:F2}s");
                        Debug.DrawRay(anchor.position, Vector3.up * 3f, Color.red, 0.5f);
                    }
                }
                
                // Cập nhật vị trí và thời gian kiểm tra anchor
                lastAnchorPosition = anchor.position;
                lastAnchorCheckTime = Time.time;
                
                // Chỉ cập nhật đích đến nếu chưa có đường đi, đích đến thay đổi đáng kể, hoặc anchor bị kẹt
                bool shouldUpdatePath = !anchorAgent.hasPath || 
                                        Vector3.Distance(anchorAgent.destination, currentTargetPos) > 0.1f || 
                                        anchorStuck;
                
                if (shouldUpdatePath)
                {
                    // Nếu anchor bị kẹt, reset đường đi hiện tại
                    if (anchorStuck)
                    {
                        anchorAgent.ResetPath();
                        
                        // Thử nhiều hướng khác nhau để tìm đường đi
                        Vector3 directionToTarget = (currentTargetPos - anchor.position).normalized;
                        Vector3[] alternativeDirections = new Vector3[] {
                            directionToTarget,
                            Quaternion.Euler(0, 45, 0) * directionToTarget,
                            Quaternion.Euler(0, -45, 0) * directionToTarget,
                            Quaternion.Euler(0, 90, 0) * directionToTarget,
                            Quaternion.Euler(0, -90, 0) * directionToTarget
                        };
                        
                        bool foundPath = false;
                        foreach (Vector3 direction in alternativeDirections)
                        {
                            Vector3 alternativePos = anchor.position + direction * 3f;
                            NavMeshHit altNavHit;
                            
                            if (NavMesh.SamplePosition(alternativePos, out altNavHit, 3f, NavMesh.AllAreas))
                            {
                                NavMeshPath altPath = new NavMeshPath();
                                if (NavMesh.CalculatePath(anchor.position, altNavHit.position, NavMesh.AllAreas, altPath) && 
                                    altPath.status == NavMeshPathStatus.PathComplete)
                                {
                                    anchorAgent.SetDestination(altNavHit.position);
                                    Debug.Log($"[EnemyGroupFormationManager] Anchor di chuyển tới vị trí thay thế: {altNavHit.position}");
                                    Debug.DrawLine(anchor.position, altNavHit.position, Color.yellow, 0.5f);
                                    foundPath = true;
                                    break;
                                }
                            }
                        }
                        
                        // Nếu không tìm được đường đi thay thế, thử lùi lại
                        if (!foundPath)
                        {
                            Vector3 backupPos = anchor.position - directionToTarget * 2f;
                            NavMeshHit backupNavHit;
                            
                            if (NavMesh.SamplePosition(backupPos, out backupNavHit, 3f, NavMesh.AllAreas))
                            {
                                anchorAgent.SetDestination(backupNavHit.position);
                                Debug.LogWarning($"[EnemyGroupFormationManager] Anchor lùi lại để tìm đường đi mới: {backupNavHit.position}");
                                Debug.DrawLine(anchor.position, backupNavHit.position, Color.red, 0.5f);
                            }
                            else
                            {
                                // Nếu tất cả đều thất bại, thử lại với điểm đích ban đầu
                                anchorAgent.SetDestination(currentTargetPos);
                            }
                        }
                    }
                    else
                    {
                        // Kiểm tra xem có thể đi đến đích không
                        NavMeshPath path = new NavMeshPath();
                        if (NavMesh.CalculatePath(anchor.position, currentTargetPos, NavMesh.AllAreas, path))
                        {
                            if (path.status == NavMeshPathStatus.PathComplete)
                            {
                                anchorAgent.SetDestination(currentTargetPos);
                                Debug.Log($"[EnemyGroupFormationManager] Anchor đang di chuyển tới: {currentTargetPos}");
                                
                                // Vẽ đường đi để debug
                                if (path.corners.Length > 1)
                                {
                                    for (int j = 0; j < path.corners.Length - 1; j++)
                                    {
                                        Debug.DrawLine(path.corners[j], path.corners[j + 1], Color.blue, 0.1f);
                                    }
                                }
                            }
                            else
                            {
                                Debug.LogWarning("[EnemyGroupFormationManager] Không thể tìm đường đi hoàn chỉnh cho anchor!");
                                // Tìm điểm gần nhất trên NavMesh
                                NavMeshHit hit;
                                if (NavMesh.SamplePosition(currentTargetPos, out hit, 5f, NavMesh.AllAreas))
                                {
                                    anchorAgent.SetDestination(hit.position);
                                    Debug.Log($"[EnemyGroupFormationManager] Anchor di chuyển tới vị trí thay thế: {hit.position}");
                                    Debug.DrawLine(anchor.position, hit.position, Color.yellow, 0.1f);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                // Nếu không có NavMeshAgent, tìm điểm gần nhất trên NavMesh trước khi di chuyển
                NavMeshHit targetNavHit;
                Vector3 targetPosition = currentTargetPos;
                
                // Kiểm tra xem điểm đích có nằm trên NavMesh không
                if (NavMesh.SamplePosition(currentTargetPos, out targetNavHit, 5f, NavMesh.AllAreas))
                {
                    targetPosition = targetNavHit.position;
                    Debug.Log($"[EnemyGroupFormationManager] Anchor không có NavMeshAgent, di chuyển tới điểm NavMesh gần nhất: {targetNavHit.position}");
                    Debug.DrawLine(anchor.position, targetNavHit.position, Color.magenta, 0.1f);
                }
                else
                {
                    Debug.LogWarning($"[EnemyGroupFormationManager] Không tìm thấy điểm NavMesh gần với waypoint {currentPatrolIndex}!");
                    // Tìm điểm NavMesh gần với vị trí hiện tại của anchor
                    NavMeshHit anchorNavHit;
                    if (NavMesh.SamplePosition(anchor.position, out anchorNavHit, 3f, NavMesh.AllAreas))
                    {
                        targetPosition = anchorNavHit.position;
                        Debug.LogWarning($"[EnemyGroupFormationManager] Di chuyển anchor đến điểm NavMesh gần nhất với vị trí hiện tại: {anchorNavHit.position}");
                    }
                }
                
                // Di chuyển anchor đến điểm đã xác định
                anchor.position = Vector3.MoveTowards(anchor.position, targetPosition, Time.deltaTime * (groupMoveSpeed * 0.8f));
            }
        }

        Vector3 anchorPos = anchor != null ? anchor.position : transform.position;
        Vector3 targetPos = patrolRoute.patrolPoints[currentPatrolIndex].position;

        if (patrolRoute.isFortress)
        {
            float angleStep = 360f / Mathf.Max(1, members.Count);
            for (int i = 0; i < members.Count; i++)
            {
                float angle = angleStep * i + Time.time * 20f;
                Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0, Mathf.Sin(angle * Mathf.Deg2Rad)) * fortressRadius;
                Vector3 fortressPos = patrolRoute.patrolPoints[0].position + offset;
                Debug.Log($"[EnemyGroupFormationManager] Thành viên {i} di chuyển tới vị trí pháo đài: {fortressPos}");
                members[i].movementController?.MoveTo(fortressPos);
                SetNavMeshAvoidance(members[i], i);
            }
            return;
        }

        float distanceToTarget = Vector3.Distance(anchorPos, targetPos);
        Debug.Log($"[EnemyGroupFormationManager] AnchorPos: {anchorPos} | TargetPos: {targetPos} | Khoảng cách: {distanceToTarget}");

        // Vẽ debug lines để kiểm tra đường đi
        Debug.DrawLine(anchorPos, targetPos, Color.yellow);
        
        // Kiểm tra trạng thái của các thành viên
        int activeMembers = 0;
        int membersNearAnchor = 0;
        int membersNearWaypoint = 0;
        float maxDistanceToAnchor = 0f;
        float maxDistanceToWaypoint = 0f;
        
        foreach (var member in members)
        {
            if (member == null) continue;
            activeMembers++;
            
            // Kiểm tra khoảng cách đến anchor
            float distToAnchor = Vector3.Distance(member.transform.position, anchorPos);
            maxDistanceToAnchor = Mathf.Max(maxDistanceToAnchor, distToAnchor);
            if (distToAnchor <= formationSpacing * 2f)
            {
                membersNearAnchor++;
            }
            
            // Kiểm tra khoảng cách đến điểm tuần tra
            float distToWaypoint = Vector3.Distance(member.transform.position, targetPos);
            maxDistanceToWaypoint = Mathf.Max(maxDistanceToWaypoint, distToWaypoint);
            if (distToWaypoint <= groupStoppingDistance * 3f)
            {
                membersNearWaypoint++;
            }
            
            // Vẽ đường đi của thành viên
            NavMeshAgent memberAgent = member.GetComponent<NavMeshAgent>();
            if (memberAgent != null && memberAgent.hasPath)
            {
                Debug.DrawLine(member.transform.position, memberAgent.destination, Color.green, 0.1f);
                
                // Vẽ đường đi chi tiết nếu có path
                if (memberAgent.hasPath && memberAgent.path.corners.Length > 1)
                {
                    for (int i = 0; i < memberAgent.path.corners.Length - 1; i++)
                    {
                        Debug.DrawLine(memberAgent.path.corners[i], memberAgent.path.corners[i + 1], new Color(0, 0.5f, 0, 0.5f), 0.1f);
                    }
                }
            }
        }
        
        // Hiển thị thông tin debug
        Debug.Log($"[EnemyGroupFormationManager] Thành viên gần anchor: {membersNearAnchor}/{activeMembers}, gần waypoint: {membersNearWaypoint}/{activeMembers}, khoảng cách max đến anchor: {maxDistanceToAnchor:F2}m");
        
        // Chỉ chuyển sang điểm tiếp theo khi anchor đủ gần điểm NavMesh gần nhất và đủ số thành viên đã đến gần
        NavMeshHit navHit;
        bool isTargetOnNavMesh = NavMesh.SamplePosition(targetPos, out navHit, 5f, NavMesh.AllAreas);
        float distanceToNavMeshPoint = isTargetOnNavMesh ? Vector3.Distance(anchorPos, navHit.position) : float.MaxValue;
        
        // Vẽ điểm NavMesh gần nhất để debug
        if (isTargetOnNavMesh) {
            Debug.DrawLine(targetPos, navHit.position, Color.cyan, 0.1f);
            // Vẽ một hình chữ thập tại điểm NavMesh gần nhất
            Vector3 pos = navHit.position;
            float size = 0.5f;
            Debug.DrawLine(pos + Vector3.up * size, pos - Vector3.up * size, Color.cyan, 0.1f);
            Debug.DrawLine(pos + Vector3.right * size, pos - Vector3.right * size, Color.cyan, 0.1f);
            Debug.DrawLine(pos + Vector3.forward * size, pos - Vector3.forward * size, Color.cyan, 0.1f);
        }
        
        bool readyToAdvance = (isTargetOnNavMesh && distanceToNavMeshPoint <= groupStoppingDistance * 2f) && 
                             (membersNearWaypoint >= activeMembers * 0.75f || maxDistanceToWaypoint <= groupStoppingDistance * 4f);
        
        Debug.Log($"[EnemyGroupFormationManager] Khoảng cách đến điểm NavMesh gần nhất: {distanceToNavMeshPoint:F2}m, Sẵn sàng chuyển điểm: {readyToAdvance}");
        
        if (readyToAdvance)
        {
            if (pauseTimer <= 0)
            {
                Debug.Log($"[EnemyGroupFormationManager] Đã đến waypoint {currentPatrolIndex}, chuyển sang waypoint tiếp theo.");
                
                // Lưu lại điểm tuần tra hiện tại trước khi chuyển
                int previousPatrolIndex = currentPatrolIndex;
                
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
                
                // Kiểm tra nếu điểm tuần tra mới giống với điểm cũ (có thể xảy ra với Once mode ở điểm cuối)
                if (previousPatrolIndex == currentPatrolIndex)
                {
                    Debug.Log($"[EnemyGroupFormationManager] Đã đến điểm tuần tra cuối cùng, dừng lại.");
                }
                
                pauseTimer = patrolRoute.pauseTimeAtPoint;
                
                // Reset các biến theo dõi vị trí của thành viên
                lastMemberPositions.Clear();
                lastPathUpdateTime.Clear();
            }
            else
            {
                Debug.Log($"[EnemyGroupFormationManager] Đang dừng tại waypoint {currentPatrolIndex}, còn {pauseTimer:F2}s");
                pauseTimer -= Time.deltaTime;
                
                // Khi đang dừng, vẫn áp dụng formation để giữ các thành viên ở đúng vị trí
                ApplyFormation(targetPos);
                return;
            }
        }
        
        // Kiểm tra xem có thành viên nào quá xa anchor không
        bool anyMemberTooFar = maxDistanceToAnchor > formationSpacing * 4f;
        
        if (anyMemberTooFar)
        {
            Debug.LogWarning($"[EnemyGroupFormationManager] Có thành viên quá xa anchor ({maxDistanceToAnchor:F2}m), ưu tiên di chuyển về gần anchor.");
            
            // Nếu có thành viên quá xa, ưu tiên di chuyển về gần anchor trước
            foreach (var member in members)
            {
                if (member == null) continue;
                
                float distToAnchor = Vector3.Distance(member.transform.position, anchorPos);
                if (distToAnchor > formationSpacing * 3f)
                {
                    // Tính toán vị trí gần anchor hơn
                    Vector3 dirToAnchor = (anchorPos - member.transform.position).normalized;
                    Vector3 closerPos = member.transform.position + dirToAnchor * Mathf.Min(distToAnchor * 0.5f, formationSpacing * 2f);
                    
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(closerPos, out hit, 3f, NavMesh.AllAreas))
                    {
                        member.movementController?.MoveTo(hit.position);
                        Debug.DrawLine(member.transform.position, hit.position, Color.red, 0.1f);
                    }
                }
            }
        }
        else
        {
            // Cập nhật vị trí của các thành viên trong nhóm theo formation
            ApplyFormation(targetPos);
        }
    }

    void UpdateGroupCombatPositions()
    {
        if (groupTarget == null) return;
        
        // Kiểm tra xem groupTarget có hợp lệ không
        if (!groupTarget.gameObject.activeInHierarchy)
        {
            Debug.LogWarning("[EnemyGroupFormationManager] groupTarget không còn active, deactivating combat mode.");
            groupTarget = null;
            DeactivateCombatMode();
            return;
        }
        
        // Tính toán vị trí chiến đấu cho từng thành viên
        float angleStep = 360f / Mathf.Max(1, members.Count);
        float combatRadius = 2.5f + formationSpacing; // Đảm bảo không chồng lấn
        
        // Tính toán vị trí trung tâm của nhóm
        Vector3 groupCenter = Vector3.zero;
        int validMemberCount = 0;
        
        foreach (var member in members)
        {
            if (member != null)
            {
                groupCenter += member.transform.position;
                validMemberCount++;
            }
        }
        
        if (validMemberCount > 0)
        {
            groupCenter /= validMemberCount;
        }
        
        // Điều chỉnh vị trí chiến đấu dựa trên loại kẻ địch và khoảng cách
        for (int i = 0; i < members.Count; i++)
        {
            if (members[i] == null) continue;
            
            float angle = angleStep * i;
            float memberCombatRadius = combatRadius;
            
            // Điều chỉnh bán kính dựa trên loại kẻ địch
            if (members[i].enemyType == EnemyType.Ranged)
            {
                // Kẻ địch tầm xa đứng xa hơn
                memberCombatRadius += 2f;
            }
            else if (members[i].enemyType == EnemyType.Melee)
            {
                // Kẻ địch cận chiến đứng gần hơn
                memberCombatRadius -= 0.5f;
            }
            
            Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0, Mathf.Sin(angle * Mathf.Deg2Rad)) * memberCombatRadius;
            Vector3 combatPos = groupTarget.position + offset;
            
            // Kiểm tra vị trí trên NavMesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(combatPos, out hit, 5f, NavMesh.AllAreas))
            {
                combatPos = hit.position;
            }
            
            // Kiểm tra khoảng cách từ vị trí hiện tại đến vị trí chiến đấu
            float distToCombatPos = Vector3.Distance(members[i].transform.position, combatPos);
            
            // Nếu quá xa, tìm vị trí trung gian
            if (distToCombatPos > formationSpacing * 5f)
            {
                Vector3 directionToCombat = (combatPos - members[i].transform.position).normalized;
                Vector3 intermediatePos = members[i].transform.position + directionToCombat * formationSpacing * 3f;
                
                if (NavMesh.SamplePosition(intermediatePos, out hit, 3f, NavMesh.AllAreas))
                {
                    Debug.Log($"[EnemyGroupFormationManager] Thành viên {i} quá xa vị trí chiến đấu, di chuyển tới vị trí trung gian.");
                    combatPos = hit.position;
                }
            }
            
                            members[i].movementController?.MoveTo(combatPos);
            SetNavMeshAvoidance(members[i], i);
        }
    }

    void ApplyFormation(Vector3 targetPos)
    {
        Debug.Log($"[EnemyGroupFormationManager] ApplyFormation tới targetPos: {targetPos}");
        
        // Lấy vị trí anchor
        Vector3 anchorPosition = anchor != null ? anchor.position : transform.position;
        
        // Tính toán hướng di chuyển
        Vector3 directionToTarget = (targetPos - anchorPosition).normalized;
        Quaternion rotationToTarget = Quaternion.LookRotation(directionToTarget);
        
        // Tính toán vị trí trung tâm của nhóm
        Vector3 groupCenter = Vector3.zero;
        int validMemberCount = 0;
        
        foreach (var member in members)
        {
            if (member != null)
            {
                groupCenter += member.transform.position;
                validMemberCount++;
            }
        }
        
        if (validMemberCount > 0)
        {
            groupCenter /= validMemberCount;
        }
        else
        {
            groupCenter = anchorPosition;
        }
        
        // Vẽ debug để hiển thị trung tâm nhóm và hướng di chuyển
        Debug.DrawLine(groupCenter, targetPos, Color.blue, 0.1f);
        Debug.DrawLine(anchorPosition, anchorPosition + directionToTarget * 3f, Color.red, 0.1f);

        for (int i = 0; i < members.Count; i++)
        {
            if (members[i] != null)
            {
                var agent = members[i].GetComponent<NavMeshAgent>();
                if (agent == null) continue;

                // Xoay offset theo hướng di chuyển
                Vector3 rotatedOffset = rotationToTarget * customOffsets[i];
                Vector3 memberTargetPos = targetPos + rotatedOffset;
                
                // Vẽ debug để hiển thị vị trí mục tiêu ban đầu
                Debug.DrawLine(members[i].transform.position, memberTargetPos, Color.cyan, 0.1f);
                
                // Điều chỉnh vị trí dựa trên khoảng cách từ trung tâm nhóm
                float distanceFromCenter = Vector3.Distance(members[i].transform.position, groupCenter);
                float distanceFromAnchor = Vector3.Distance(members[i].transform.position, anchorPosition);
                
                // Nếu thành viên quá xa trung tâm hoặc anchor, điều chỉnh vị trí mục tiêu
                if (distanceFromCenter > formationSpacing * 3f || distanceFromAnchor > formationSpacing * 4f)
                {
                    // Ưu tiên di chuyển về phía anchor nếu quá xa
                    Vector3 directionToAnchor = (anchorPosition - members[i].transform.position).normalized;
                    Vector3 directionToCenter = (groupCenter - members[i].transform.position).normalized;
                    
                    // Kết hợp cả hai hướng, nhưng ưu tiên hướng về anchor nếu quá xa
                    Vector3 combinedDirection = Vector3.Lerp(directionToCenter, directionToAnchor, 
                                                            Mathf.Clamp01(distanceFromAnchor / (formationSpacing * 5f)));
                    
                    // Tính toán vị trí mục tiêu mới
                    Vector3 adjustedPos = members[i].transform.position + combinedDirection * formationSpacing * 2f;
                    memberTargetPos = Vector3.Lerp(memberTargetPos, adjustedPos, 0.7f);
                    
                    Debug.Log($"[EnemyGroupFormationManager] Thành viên {i} quá xa (Trung tâm: {distanceFromCenter:F2}m, Anchor: {distanceFromAnchor:F2}m), điều chỉnh vị trí mục tiêu.");
                    
                    // Vẽ debug để hiển thị vị trí mục tiêu đã điều chỉnh
                    Debug.DrawLine(members[i].transform.position, memberTargetPos, Color.yellow, 0.1f);
                }

                // Kiểm tra và điều chỉnh vị trí trên NavMesh
                NavMeshHit hit;
                bool positionValid = NavMesh.SamplePosition(memberTargetPos, out hit, 10f, NavMesh.AllAreas);
                
                if (positionValid)
                {
                    memberTargetPos = hit.position;
                    Debug.DrawRay(memberTargetPos, Vector3.up * 2f, Color.green, 0.1f); // Đánh dấu vị trí hợp lệ
                }
                else
                {
                    Debug.LogWarning($"[EnemyGroupFormationManager] Không tìm thấy vị trí NavMesh hợp lệ cho thành viên {i}, tìm vị trí thay thế.");
                    
                    // Thử tìm vị trí gần nhất trên NavMesh từ vị trí của anchor
                    Vector3 fallbackDirection = (memberTargetPos - anchorPosition).normalized;
                    
                    // Thử nhiều khoảng cách khác nhau từ anchor
                    for (float distance = formationSpacing; distance <= formationSpacing * 3f; distance += formationSpacing * 0.5f)
                    {
                        Vector3 fallbackPos = anchorPosition + fallbackDirection * distance;
                        if (NavMesh.SamplePosition(fallbackPos, out hit, 5f, NavMesh.AllAreas))
                        {
                            memberTargetPos = hit.position;
                            Debug.LogWarning($"[EnemyGroupFormationManager] Tìm thấy vị trí thay thế cho thành viên {i} ở khoảng cách {distance:F2}m từ anchor.");
                            Debug.DrawRay(memberTargetPos, Vector3.up * 2f, Color.yellow, 0.1f); // Đánh dấu vị trí thay thế
                            positionValid = true;
                            break;
                        }
                    }
                    
                    // Nếu vẫn không tìm được vị trí, thử lấy vị trí gần nhất với vị trí hiện tại của thành viên
                    if (!positionValid)
                    {
                        Vector3 currentToTarget = (memberTargetPos - members[i].transform.position).normalized;
                        Vector3 nearbyPos = members[i].transform.position + currentToTarget * formationSpacing;
                        
                        if (NavMesh.SamplePosition(nearbyPos, out hit, 5f, NavMesh.AllAreas))
                        {
                            memberTargetPos = hit.position;
                            Debug.LogWarning($"[EnemyGroupFormationManager] Sử dụng vị trí gần với vị trí hiện tại cho thành viên {i}.");
                            Debug.DrawRay(memberTargetPos, Vector3.up * 2f, Color.red, 0.1f); // Đánh dấu vị trí cuối cùng
                        }
                        else
                        {
                            // Nếu tất cả đều thất bại, sử dụng vị trí hiện tại
                            memberTargetPos = members[i].transform.position;
                            Debug.LogError($"[EnemyGroupFormationManager] Không thể tìm thấy bất kỳ vị trí NavMesh nào cho thành viên {i}, giữ nguyên vị trí.");
                        }
                    }
                }

                // Cập nhật đường đi cho NavMeshAgent
                if (!lastPathUpdateTime.ContainsKey(members[i]))
                {
                    lastPathUpdateTime[members[i]] = 0f;
                    lastMemberPositions[members[i]] = members[i].transform.position;
                }

                float timeSinceLastUpdate = Time.time - lastPathUpdateTime[members[i]];
                float distanceFromLastPosition = Vector3.Distance(members[i].transform.position, lastMemberPositions[members[i]]);
                
                // Kiểm tra xem agent có bị kẹt không
                bool agentStuck = distanceFromLastPosition < 0.1f && timeSinceLastUpdate > 1.0f;
                if (agentStuck)
                {
                    Debug.DrawLine(members[i].transform.position, members[i].transform.position + Vector3.up * 3f, Color.red, 0.5f);
                    Debug.LogWarning($"[EnemyGroupFormationManager] Thành viên {i} có thể bị kẹt: Di chuyển {distanceFromLastPosition:F2}m trong {timeSinceLastUpdate:F2}s");
                }
                
                // Chỉ cập nhật đích đến nếu:
                // 1. Chưa có path hoặc đích đến thay đổi đáng kể
                // 2. Đã đủ thời gian từ lần cập nhật trước
                // 3. Agent đã di chuyển được một khoảng cách nhất định
                // 4. Agent bị kẹt
                // 5. Agent quá xa anchor hoặc vị trí mục tiêu
                bool pathInvalid = !agent.hasPath || agent.pathStatus != NavMeshPathStatus.PathComplete;
                bool destinationChanged = Vector3.Distance(agent.destination, memberTargetPos) > 0.5f;
                bool timeToUpdate = timeSinceLastUpdate >= MIN_PATH_UPDATE_INTERVAL || distanceFromLastPosition > formationSpacing * 0.5f;
                bool agentIdle = agent.velocity.magnitude < 0.05f;
                bool tooFarFromTarget = Vector3.Distance(members[i].transform.position, memberTargetPos) > formationSpacing * 3f;
                bool tooFarFromAnchor = Vector3.Distance(members[i].transform.position, anchorPosition) > formationSpacing * 4f;
                
                if ((pathInvalid || destinationChanged) && timeToUpdate || agentStuck || agentIdle || tooFarFromTarget || tooFarFromAnchor)
                {
                    // Kiểm tra xem có thể đi đến đích không
                    NavMeshPath path = new NavMeshPath();
                    bool pathValid = NavMesh.CalculatePath(members[i].transform.position, memberTargetPos, NavMesh.AllAreas, path);
                    
                    if (pathValid && path.status == NavMeshPathStatus.PathComplete)
                    {
                        Debug.Log($"[EnemyGroupFormationManager] Thành viên {i} ({members[i].gameObject.name}) di chuyển tới: {memberTargetPos}");
                        members[i].movementController?.MoveTo(memberTargetPos);
                        
                        // Vẽ đường đi để debug
                        if (path.corners.Length > 1)
                        {
                            for (int j = 0; j < path.corners.Length - 1; j++)
                            {
                                Debug.DrawLine(path.corners[j], path.corners[j + 1], Color.green, 0.1f);
                            }
                        }
                    }
                    else
                    {
                        // Thử nhiều hướng khác nhau nếu hướng trực tiếp không hoạt động
                        Vector3 dirToTarget = (memberTargetPos - members[i].transform.position).normalized;
                        Vector3[] alternativeDirections = new Vector3[] {
                            dirToTarget,
                            Quaternion.Euler(0, 45, 0) * dirToTarget,
                            Quaternion.Euler(0, -45, 0) * dirToTarget,
                            Quaternion.Euler(0, 90, 0) * dirToTarget,
                            Quaternion.Euler(0, -90, 0) * dirToTarget
                        };
                        
                        bool foundPath = false;
                        foreach (Vector3 direction in alternativeDirections)
                        {
                            float stepDistance = formationSpacing * 0.5f;
                            Vector3 stepPos = members[i].transform.position + direction * stepDistance;
                            
                            NavMeshHit stepHit;
                            if (NavMesh.SamplePosition(stepPos, out stepHit, 2f, NavMesh.AllAreas))
                            {
                                if (NavMesh.CalculatePath(members[i].transform.position, stepHit.position, NavMesh.AllAreas, path) && 
                                    path.status == NavMeshPathStatus.PathComplete)
                                {
                                    Debug.Log($"[EnemyGroupFormationManager] Thành viên {i} không thể đi thẳng tới đích, di chuyển theo hướng thay thế.");
                                    members[i].movementController?.MoveTo(stepHit.position);
                                    Debug.DrawLine(members[i].transform.position, stepHit.position, Color.yellow, 0.5f);
                                    foundPath = true;
                                    break;
                                }
                            }
                        }
                        
                        // Nếu không tìm được đường đi thay thế, di chuyển về phía anchor
                        if (!foundPath)
                        {
                            Vector3 toAnchor = (anchorPosition - members[i].transform.position).normalized;
                            Vector3 fallbackPos = members[i].transform.position + toAnchor * 2f;
                            NavMeshHit fallbackHit;
                            
                            if (NavMesh.SamplePosition(fallbackPos, out fallbackHit, 3f, NavMesh.AllAreas))
                            {
                                Debug.LogError($"[EnemyGroupFormationManager] Không tìm được đường đi cho thành viên {i}, di chuyển về phía anchor.");
                                members[i].movementController?.MoveTo(fallbackHit.position);
                                Debug.DrawLine(members[i].transform.position, fallbackHit.position, Color.red, 0.5f);
                            }
                        }
                    }
                    
                    lastPathUpdateTime[members[i]] = Time.time;
                    lastMemberPositions[members[i]] = members[i].transform.position;
                }

                // Kiểm tra nếu thành viên bị kẹt
                if (agent.velocity.magnitude < 0.1f && Vector3.Distance(members[i].transform.position, memberTargetPos) > agent.stoppingDistance)
                {
                    Debug.LogWarning($"[EnemyGroupFormationManager] Thành viên {i} có vẻ bị kẹt, thử tìm đường đi mới.");
                    agent.ResetPath();
                    
                    // Tìm vị trí thay thế gần hơn với anchor
                    Vector3 directionToAnchor = (anchor.position - members[i].transform.position).normalized;
                    Vector3 alternativePos = members[i].transform.position + directionToAnchor * formationSpacing;
                    
                    // Kiểm tra vị trí thay thế trên NavMesh
                    NavMeshHit altHit;
                    if (NavMesh.SamplePosition(alternativePos, out altHit, 3f, NavMesh.AllAreas))
                    {
                        memberTargetPos = altHit.position;
                        Debug.Log($"[EnemyGroupFormationManager] Thành viên {i} di chuyển tới vị trí thay thế: {memberTargetPos}");
                    }
                    
                    members[i].movementController?.MoveTo(memberTargetPos);
                }

                SetNavMeshAvoidance(members[i], i);
            }
        }
    }

    void SetNavMeshAvoidance(EnemyAIController member, int index)
    {
        var agent = member.GetComponent<NavMeshAgent>();
        if (agent != null && useNavMeshAvoidance)
        {
            // Thiết lập avoidancePriority dựa trên loại kẻ địch và index
            // Kẻ địch tầm xa có ưu tiên cao hơn để tránh bị kẹt
            if (member.enemyType == EnemyType.Ranged)
            {
                agent.avoidancePriority = 40 + index; // Ưu tiên cao hơn (số thấp hơn = ưu tiên cao hơn)
            }
            else if (member.enemyType == EnemyType.Melee)
            {
                agent.avoidancePriority = 50 + index; // Ưu tiên trung bình
            }
            else if (member.enemyType == EnemyType.Support)
            {
                agent.avoidancePriority = 30 + index; // Ưu tiên cao nhất cho support
            }
            else
            {
                agent.avoidancePriority = 50 + index; // Mặc định
            }
            
            // Điều chỉnh các tham số tránh va chạm khác
            // agent.avoidancePredictionTime = 2.0f; // Thuộc tính này không tồn tại trong NavMeshAgent
            
            // Điều chỉnh bán kính tránh va chạm dựa trên loại kẻ địch
            if (member.enemyType == EnemyType.Ranged)
            {
                agent.radius = 0.5f; // Kẻ địch tầm xa có bán kính nhỏ hơn
            }
            else if (member.enemyType == EnemyType.Melee)
            {
                agent.radius = 0.7f; // Kẻ địch cận chiến có bán kính lớn hơn
            }
            
            // Đảm bảo NavMeshAgent đang hoạt động
            if (!agent.enabled)
            {
                agent.enabled = true;
            }
            
            // Đảm bảo updatePosition và updateRotation được bật
            agent.updatePosition = true;
            agent.updateRotation = true;
        }
    }
}