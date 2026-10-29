using UnityEngine;

/// <summary>
/// Interface cho các ??i t??ng có th? b? t?n công
/// Dùng ?? nh?n di?n enemy cho cursor system
/// </summary>
public interface IAttackable
{
    /// <summary>
    /// Ki?m tra xem ??i t??ng có th? b? t?n công không
    /// </summary>
    /// <returns>True n?u có th? t?n công</returns>
    bool CanBeAttacked();
    
    /// <summary>
    /// L?y position c?a ??i t??ng
    /// </summary>
    /// <returns>World position</returns>
    Vector2 GetPosition();
    
    /// <summary>
    /// L?y tên c?a ??i t??ng
    /// </summary>
    /// <returns>Tên ??i t??ng</returns>
    string GetName();
}