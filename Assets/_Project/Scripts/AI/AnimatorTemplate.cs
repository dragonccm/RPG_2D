using UnityEngine;

/// <summary>
/// ScriptableObject định nghĩa template cho việc tạo Animator Controller tự động
/// </summary>
[CreateAssetMenu(fileName = "AnimatorTemplate", menuName = "RPG/Animator Template", order = 1)]
public class AnimatorTemplate : ScriptableObject
{
    [Header("🔧 Thông Tin Template")]
    [Tooltip("Tên template này - dùng để nhận diện template khi chọn trong menu")]
    public string templateName = "Kẻ Địch Cơ Bản";
    
    [Tooltip("Mô tả chi tiết về cách sử dụng template này và loại enemy phù hợp")]
    [TextArea(2, 3)]
    public string description = "Template cho enemy cơ bản với 5 trạng thái: đứng yên, di chuyển, tấn công, bị thương và chết. Phù hợp cho goblin, orc, skeleton thường.";
    
    [Header("🎬 Animation Clips")]
    [Tooltip("Animation khi enemy đứng yên tại chỗ. Nên có loop để liên tục lặp lại.")]
    public AnimationClip idleClip;
    
    [Tooltip("Animation khi enemy di chuyển. Nên có loop và footstep events để đồng bộ âm thanh bước chân.")]
    public AnimationClip walkClip;
    
    [Tooltip("Animation tấn công cơ bản. Nên có Attack Hit event ở khoảng 60-70% của animation.")]
    public AnimationClip attackClip;
    
    [Tooltip("Animation khi enemy bị tấn công và nhận sát thương. Nên có thời gian ngắn (0.3-0.5s).")]
    public AnimationClip hurtClip;
    
    [Tooltip("Animation khi enemy chết. Nên có Death event ở cuối để trigger destroy.")]
    public AnimationClip deathClip;
    
    [Header("⚔️ Boss Special Animations (Tùy chọn)")]
    [Tooltip("Animation skill đặc biệt 1 - ví dụ: Fireball, Lightning Strike")]
    public AnimationClip skill1Clip;
    
    [Tooltip("Animation skill đặc biệt 2 - ví dụ: AOE Attack, Summon Minions")]
    public AnimationClip skill2Clip;
    
    [Tooltip("Animation skill đặc biệt 3 - ví dụ: Buff, Shield")]
    public AnimationClip skill3Clip;
    
    [Tooltip("Animation ultimate skill - skill mạnh nhất của boss")]
    public AnimationClip ultimateClip;
    
    [Tooltip("Animation teleport - boss biến mất/xuất hiện tại vị trí khác")]
    public AnimationClip teleportClip;
    
    [Tooltip("Animation berserk transformation - boss chuyển sang phase cuối")]
    public AnimationClip berserkClip;
    
    [Header("⚙️ Cài đặt Template")]
    [Tooltip("Loại enemy: Basic Enemy (thường), Elite Enemy (mạnh hơn), Boss (trùm), Advanced Boss (trùm phức tạp)")]
    public AnimatorType animatorType = AnimatorType.BasicEnemy;
    
    [Tooltip("Tự động tạo transitions giữa các states. Giúp tiết kiệm thời gian setup thủ công.")]
    public bool autoSetupTransitions = true;
    
    [Tooltip("Tạo Sub-State Machines cho Boss phức tạp. Chỉ dùng cho Boss có nhiều phase.")]
    public bool createSubStateMachines = true;
    
    [Tooltip("Thời gian chuyển tiếp giữa các animation (giây). Giá trị càng cao càng mượt.")]
    [Range(0f, 1f)]
    public float defaultTransitionDuration = 0.1f;
    
    [Header("🎯 Animation Events")]
    [Tooltip("Tự động thêm Animation Events cho Attack Hit, Footstep, Death. Tiết kiệm thời gian setup.")]
    public bool autoAddAnimationEvents = true;
    
    [Tooltip("Thời điểm Attack Hit được trigger (% animation). 0.6 = 60% animation.")]
    [Range(0f, 1f)]
    public float attackHitFrame = 0.6f;
    
    [Tooltip("Thời điểm Footstep Sound được trigger (% animation). Dùng cho walk/run.")]
    [Range(0f, 1f)]
    public float footstepFrame = 0.5f;
}

public enum AnimatorType
{
    Player,
    BasicEnemy,
    EliteEnemy,
    Boss,
    AdvancedBoss
}