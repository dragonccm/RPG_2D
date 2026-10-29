using UnityEngine;

/// <summary>
/// P1-3 (GAME_MASTER_PLAN 3.3): y-sort cho thực thể 2D.
/// Gán sortingOrder theo toạ độ Y (càng thấp trên màn = càng gần camera = đè lên).
/// Gắn vào Player/Enemy/props lớn; sprite của object (hoặc con) chuyển sang
/// sort layer "Characters"/"Props" và order cập nhật mỗi LateUpdate.
/// </summary>
[DisallowMultipleComponent]
public class YSort : MonoBehaviour
{
    [Header("Cấu hình sort")]
    [SerializeField] private string sortingLayerName = "Characters";
    [SerializeField] private float orderScale = 100f;   // 1 world unit = 100 order
    [SerializeField] private int baseOrder = 1000;      // sau tilemap/props, trước effects

    private SpriteRenderer[] renderers;

    private void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    private void LateUpdate()
    {
        int order = baseOrder - Mathf.RoundToInt(transform.position.y * orderScale);
        foreach (var r in renderers)
        {
            if (r == null || r.name == "ShadowBlob") continue; // bóng do BlobShadow tự quản
            if (r.sortingLayerName != sortingLayerName)
                r.sortingLayerName = sortingLayerName;
            r.sortingOrder = order;
        }
    }
}
