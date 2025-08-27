using System.Collections;\using System.Collections.Generic;\using UnityEngine;

public enum SpecialMoveType
{
    Dash,
    Teleport,
    ChargeAttack
}

public class special_move : MonoBehaviour
{
    [Header("Special Move Settings")]
    public KeyCode dashKey = KeyCode.LeftShift;
    public KeyCode teleportKey = KeyCode.Q;
    public KeyCode chargeAttackKey = KeyCode.E;
    
    [Header("Dash Settings")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.3f;
    public float dashCooldown = 1f;
    public bool isInvulnerableDuringDash = true;
    
    [Header("Teleport Settings")]
    public float teleportRange = 5f;
    public float teleportCooldown = 2f;
    public LayerMask teleportObstacleMask = -1;
    
    [Header("Charge Attack Settings")]
    public float chargeDistance = 4f;
    public float chargeSpeed = 12f;
    public float chargeDamage = 50f;
    public float chargeCooldown = 1.5f;
    public float knockbackForce = 10f;
    public LayerMask enemyLayer = -1;

    private CharacterController controller;
    private PlayerController playerController;
    private bool isPerformingSpecialMove = false;
    private bool canDash = true;
    private bool canTeleport = true;
    private bool canChargeAttack = true;
    private Vector3 lastMoveDirection;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerController = GetComponent<PlayerController>();
        if (enemyLayer == -1)
            enemyLayer = LayerMask.GetMask("Enemy");
        if (teleportObstacleMask == -1)
            teleportObstacleMask = LayerMask.GetMask("Default", "Enemy");
    }

    void Update()
    {
        if (isPerformingSpecialMove) return;

        // Cập nhật hướng theo chuột
        UpdateMouseDirection();

        // Kiểm tra input cho các loại di chuyển đặc biệt
        if (Input.GetKeyDown(dashKey) && canDash)
        {
            StartCoroutine(PerformDash());
        }
        else if (Input.GetKeyDown(teleportKey) && canTeleport)
        {
            StartCoroutine(PerformTeleport());
        }
        else if (Input.GetKeyDown(chargeAttackKey) && canChargeAttack)
        {
            StartCoroutine(PerformChargeAttack());
        }
    }

    void UpdateMouseDirection()
    {
        // Lấy vị trí chuột trên màn hình
        Vector3 mousePosition = Input.mousePosition;
        
        // Tạo ray từ camera đến vị trí chuột
        Ray ray = Camera.main.ScreenPointToRay(mousePosition);
        
        // Tạo một plane ở độ cao của player
        Plane groundPlane = new Plane(Vector3.up, transform.position);
        
        float rayDistance;
        if (groundPlane.Raycast(ray, out rayDistance))
        {
            // Lấy điểm va chạm trên plane
            Vector3 targetPoint = ray.GetPoint(rayDistance);
            
            // Tính hướng từ player đến điểm chuột
            Vector3 direction = targetPoint - transform.position;
            direction.y = 0; // Đảm bảo chỉ di chuyển trên mặt phẳng ngang
            
            if (direction.magnitude > 0)
            {
                lastMoveDirection = direction.normalized;
            }
        }
    }

    IEnumerator PerformDash()
    {
        isPerformingSpecialMove = true;
        canDash = false;
        float startTime = Time.time;
        Vector3 dashDirection = lastMoveDirection;

        // Tạm thời vô hiệu hóa sát thương nếu cần
        if (isInvulnerableDuringDash)
        {
            // Thêm logic vô hiệu hóa sát thương ở đây
            // Ví dụ: GetComponent<HealthSystem>()?.SetInvulnerable(true);
        }

        while (Time.time < startTime + dashDuration)
        {
            float progress = (Time.time - startTime) / dashDuration;
            float currentSpeed = dashSpeed * (1f - progress * 0.5f); // Giảm dần tốc độ
            
            controller.Move(dashDirection * currentSpeed * Time.deltaTime);
            yield return null;
        }

        if (isInvulnerableDuringDash)
        {
            // Khôi phục sát thương
            // Ví dụ: GetComponent<HealthSystem>()?.SetInvulnerable(false);
        }

        isPerformingSpecialMove = false;
        
        // Cooldown
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    IEnumerator PerformTeleport()
    {
        isPerformingSpecialMove = true;
        canTeleport = false;

        Vector3 teleportDirection = lastMoveDirection;
        Vector3 targetPosition = transform.position + teleportDirection * teleportRange;

        // Kiểm tra va chạm với vật cản
        if (Physics.Raycast(transform.position, teleportDirection, out RaycastHit hit, teleportRange, teleportObstacleMask))
        {
            targetPosition = hit.point - teleportDirection * 0.5f; // Đặt cách vật cản một khoảng
        }

        // Thực hiện dịch chuyển tức thời
        controller.enabled = false;
        transform.position = targetPosition;
        controller.enabled = true;

        isPerformingSpecialMove = false;

        // Cooldown
        yield return new WaitForSeconds(teleportCooldown);
        canTeleport = true;
    }

    IEnumerator PerformChargeAttack()
    {
        isPerformingSpecialMove = true;
        canChargeAttack = false;

        Vector3 chargeDirection = lastMoveDirection;
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + chargeDirection * chargeDistance;

        float chargeStartTime = Time.time;
        float chargeDuration = chargeDistance / chargeSpeed;

        // Di chuyển về phía trước
        while (Time.time < chargeStartTime + chargeDuration)
        {
            float progress = (Time.time - chargeStartTime) / chargeDuration;
            Vector3 newPosition = Vector3.Lerp(startPosition, targetPosition, progress);
            
            // Kiểm tra va chạm với vật cản
            if (Physics.Raycast(transform.position, chargeDirection, 0.5f, teleportObstacleMask))
            {
                break; // Dừng nếu gặp vật cản
            }
            
            controller.Move((newPosition - transform.position));
            yield return null;
        }

        // Gây sát thương và knockback cho kẻ địch trong phạm vi
        PerformChargeAttackDamage();

        isPerformingSpecialMove = false;

        // Cooldown
        yield return new WaitForSeconds(chargeCooldown);
        canChargeAttack = true;
    }

    void PerformChargeAttackDamage()
    {
        // Tạo một vùng kiểm tra hình cầu ở vị trí hiện tại
        Collider[] hitEnemies = Physics.OverlapSphere(transform.position, 2f, enemyLayer);

        foreach (Collider enemy in hitEnemies)
        {
            // Gây sát thương
            IDamageable damageable = enemy.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(chargeDamage);
            }

            // Áp dụng knockback
            Rigidbody enemyRb = enemy.GetComponent<Rigidbody>();
            if (enemyRb != null)
            {
                Vector3 knockbackDirection = (enemy.transform.position - transform.position).normalized;
                knockbackDirection.y = 0.5f; // Hướng lên một chút
                enemyRb.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);
            }
        }
    }

    // Getter cho UI hoặc hệ thống khác
    public bool IsPerformingSpecialMove()
    {
        return isPerformingSpecialMove;
    }

    public float GetDashCooldownProgress()
    {
        return canDash ? 0f : 1f;
    }

    public float GetTeleportCooldownProgress()
    {
        return canTeleport ? 0f : 1f;
    }

    public float GetChargeAttackCooldownProgress()
    {
        return canChargeAttack ? 0f : 1f;
    }

    public SpecialMoveType GetCurrentMoveType()
    {
        if (!isPerformingSpecialMove) return (SpecialMoveType)(-1);
        
        // Logic để xác định loại move hiện tại
        // Có thể cần thêm biến tracking riêng
        return SpecialMoveType.Dash; // Placeholder
    }

    // Vẽ Gizmos để debug
    void OnDrawGizmosSelected()
    {
        // Vẽ phạm vi teleport
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, teleportRange);

        // Vẽ phạm vi charge attack
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 2f);

        // Vẽ hướng di chuyển
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, lastMoveDirection * 3f);
    }
}