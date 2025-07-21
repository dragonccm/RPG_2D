using UnityEngine;

/// <summary>
/// Lớp máy trạng thái để quản lý các trạng thái AI của kẻ địch.
/// </summary>
public class StateMachine
{
    public State currentState { get; private set; }

    /// <summary>
    /// Khởi tạo máy trạng thái với trạng thái ban đầu.
    /// </summary>
    public void Initialize(State startingState)
    {
        currentState = startingState;
        currentState.Enter();
    }

    /// <summary>
    /// Thay đổi trạng thái hiện tại của máy trạng thái.
    /// </summary>
    public void ChangeState(State newState)
    {
        currentState?.Exit(); // Đảm bảo gọi Exit của trạng thái cũ
        currentState = newState;
        currentState.Enter(); // Gọi Enter của trạng thái mới
    }
}
