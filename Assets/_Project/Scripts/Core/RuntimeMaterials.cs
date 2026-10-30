using UnityEngine;

/// <summary>
/// Material dùng cho sprite sinh lúc chạy. VFX, đom đóm, tàn lửa dùng Unlit: tự phát sáng, không bị
/// đêm (Global Light 2D) làm tối. Sprite thế giới để mặc định (Sprite-Lit-Default của URP 2D).
/// Asset nằm ở Resources/Materials (MapBuilder sao chép từ package URP).
/// </summary>
public static class RuntimeMaterials
{
    private static Material spriteUnlit;
    private static bool loaded;

    public static Material SpriteUnlit
    {
        get
        {
            if (!loaded)
            {
                loaded = true;
                spriteUnlit = Resources.Load<Material>("Materials/Sprite-Unlit");
            }
            return spriteUnlit;
        }
    }

    /// <summary>Gán material unlit nếu có (không có → giữ mặc định, vẫn hiển thị).</summary>
    public static void MakeUnlit(Renderer r)
    {
        if (r != null && SpriteUnlit != null) r.sharedMaterial = SpriteUnlit;
    }
}
