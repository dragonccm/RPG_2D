# Hướng dẫn sử dụng Special Move System

## Tổng quan
System này cung cấp 3 loại di chuyển đặc biệt cho player: Dash, Teleport và Charge Attack.

## Cách sử dụng

### 1. Cài đặt
- Attach script `special_move.cs` vào Player GameObject
- Đảm bảo Player có CharacterController component
- Đảm bảo Player có PlayerController component

### 2. Các phím điều khiển mặc định
- **Dash**: Left Shift
- **Teleport**: Q
- **Charge Attack**: E

### 2.1 Hướng di chuyển
**Tất cả các loại di chuyển đặc biệt sẽ theo hướng chuột**, không phụ thuộc vào phím di chuyển WASD. Player sẽ luôn di chuyển về phía vị trí chuột đang trỏ tới.

### 3. Các loại di chuyển đặc biệt

#### Dash
- **Tính năng**: Lướt nhanh theo hướng di chuyển
- **Đặc điểm**: Miễn nhiễm sát thương trong thời gian dash
- **Cooldown**: 1 giây
- **Khoảng cách**: 15 units trong 0.3 giây

#### Teleport
- **Tính năng**: Dịch chuyển tức thời đến vị trí mới
- **Phạm vi**: 5 units theo hướng hiện tại
- **Cooldown**: 2 giây
- **Va chạm**: Tự động dừng trước vật cản

#### Charge Attack
- **Tính năng**: Lao về phía trước, húc bay kẻ địch
- **Sát thương**: 50 damage
- **Khoảng cách**: 4 units
- **Knockback**: 10 units
- **Cooldown**: 1.5 giây

### 4. Tùy chỉnh trong Inspector

#### Dash Settings
- `dashSpeed`: Tốc độ dash
- `dashDuration`: Thời gian dash kéo dài
- `dashCooldown`: Thời gian hồi chiêu
- `isInvulnerableDuringDash`: Bật/tắt miễn nhiễm sát thương

#### Teleport Settings
- `teleportRange`: Khoảng cách teleport tối đa
- `teleportCooldown`: Thời gian hồi chiêu
- `teleportObstacleMask`: Layer mask cho vật cản

#### Charge Attack Settings
- `chargeDistance`: Khoảng cách lao tới
- `chargeSpeed`: Tốc độ lao
- `chargeDamage`: Sát thương gây ra
- `chargeCooldown`: Thời gian hồi chiêu
- `knockbackForce`: Lực hất văng
- `enemyLayer`: Layer của kẻ địch

### 5. Integration với các system khác

#### Health System
Để hỗ trợ miễn nhiễm sát thương trong dash, cần implement interface sau trong Health System:
```csharp
public interface IInvulnerable
{
    void SetInvulnerable(bool invulnerable);
}
```

#### Damage System
Để Charge Attack có thể gây sát thương, kẻ địch cần implement:
```csharp
public interface IDamageable
{
    void TakeDamage(float damage);
}
```

### 6. Debug
- Gizmos được vẽ trong Scene view để hiển thị:
  - Phạm vi teleport (màu xanh)
  - Phạm vi charge attack (màu đỏ)
  - Hướng di chuyển hiện tại (màu xanh lá)

### 7. Tips
- Có thể thay đổi các phím điều khiển trong Inspector
- Có thể tắt bất kỳ loại move nào bằng cách không gán phím
- System tự động kiểm tra va chạm để tránh bug di chuyển qua tường

### 8. Script API
```csharp
// Kiểm tra có đang thực hiện move đặc biệt không
bool isMoving = specialMove.IsPerformingSpecialMove();

// Lấy tiến trình cooldown (0-1)
float dashProgress = specialMove.GetDashCooldownProgress();
```