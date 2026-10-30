using UnityEngine;

/// <summary>Tiện ích chung cho bẫy/câu đố: nhận diện player từ collider.</summary>
public static class WorldTriggers
{
    public static bool TryGetPlayer(Component c, out PlayerController player)
    {
        player = null;
        if (c == null) return false;
        player = c.GetComponentInParent<PlayerController>();
        return player != null;
    }
}
