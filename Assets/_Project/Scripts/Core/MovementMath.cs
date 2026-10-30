using UnityEngine;

/// <summary>
/// Nguồn duy nhất cho mọi phép tính vector di chuyển.
/// Mọi hệ thống lấy input nhân tốc độ (Player, Enemy, Dash/Teleport/Charge,
/// knockback) PHẢI đi qua ClampDiagonal — cấm nhân vector input trực tiếp.
/// (Xem docs/GAME_MASTER_PLAN.md mục 3.1)
/// </summary>
public static class MovementMath
{
    /// <summary>
    /// Chuẩn hoá vector chéo: magnitude luôn ≤ 1.
    /// - Input digital (1,1) → normalized (0.707, 0.707) → đi chéo KHÔNG nhanh hơn trục.
    /// - Input analog nhẹ (0.4, 0.4) giữ nguyên để preserve walking speed Analog.
    /// </summary>
    public static Vector2 ClampDiagonal(Vector2 raw)
    {
        if (raw.sqrMagnitude > 1f) return raw.normalized;
        return raw;
    }

    /// <summary>Vector 3D overload — dùng cho knockback/dash trong không gian 2.5D.</summary>
    public static Vector3 ClampDiagonal(Vector3 raw)
    {
        var v2 = ClampDiagonal(new Vector2(raw.x, raw.y));
        return new Vector3(v2.x, v2.y, raw.z);
    }
}
