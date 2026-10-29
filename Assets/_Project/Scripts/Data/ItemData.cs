using UnityEngine;

/// <summary>
/// Định nghĩa loại vật phẩm trong game.
/// </summary>
public enum ItemType
{
    Consumable,   // Bình máu, mana, thức ăn
    SkillScroll,  // Cuộn bí kíp võ công (dùng để học chiêu)
    KeyItem,      // Chìa khóa, vật phẩm nhiệm vụ
    Material      // Quặng, gỗ, tiền vàng
}

/// <summary>
/// ScriptableObject chứa dữ liệu gốc của mỗi loại vật phẩm.
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "RPG/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Thông tin cơ bản")]
    public string itemId;
    public string itemName;
    [TextArea(2, 4)]
    public string description;
    public Sprite icon;
    public ItemType itemType;
    public int maxStack = 99;
    public bool isUsable = true;

    [Header("Hiệu ứng tiêu hao (Consumable)")]
    public float healAmount = 0f;
    public float manaAmount = 0f;

    [Header("Bí kíp võ công (Skill Scroll)")]
    [Tooltip("ID kỹ năng trong SkillTree sẽ được mở khóa khi dùng cuộn bí kíp này (ví dụ: gio_1, khong_1, lua_1)")]
    public string unlockSkillId;
}
