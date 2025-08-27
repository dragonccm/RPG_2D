using UnityEngine;

namespace RPG.Animation
{
    /// <summary>
    /// Centralized animation parameter management using StringToHash for performance optimization
    /// </summary>
    public static class AnimationParameters
    {
        // Common parameters
        public static readonly int Speed = Animator.StringToHash("Speed");
        public static readonly int IsMoving = Animator.StringToHash("IsMoving");
        public static readonly int IsDead = Animator.StringToHash("IsDead");
        public static readonly int Attack = Animator.StringToHash("Attack");
        public static readonly int Hurt = Animator.StringToHash("Hurt");
        public static readonly int Die = Animator.StringToHash("Die");
        public static readonly int Skill = Animator.StringToHash("Skill");
        public static readonly int Berserk = Animator.StringToHash("Berserk");
        public static readonly int Teleport = Animator.StringToHash("Teleport");
        
        // 4-Directional attack parameters
        public static readonly int AttackUp = Animator.StringToHash("AttackUp");
        public static readonly int AttackDown = Animator.StringToHash("AttackDown");
        public static readonly int AttackLeft = Animator.StringToHash("AttackLeft");
        public static readonly int AttackRight = Animator.StringToHash("AttackRight");
        public static readonly int FacingDirection = Animator.StringToHash("FacingDirection");
        
        // Boss specific parameters
        public static readonly int CastFireball = Animator.StringToHash("CastFireball");
        public static readonly int AOECast = Animator.StringToHash("AOECast");
        public static readonly int EnterBerserk = Animator.StringToHash("EnterBerserk");
        public static readonly int Death = Animator.StringToHash("Death");
        public static readonly int IsBerserk = Animator.StringToHash("IsBerserk");
        
        // Special move parameters
        public static readonly int Dash = Animator.StringToHash("Dash");
        public static readonly int ChargeAttack = Animator.StringToHash("ChargeAttack");
        
        // Movement parameters
        public static readonly int MoveSpeed = Animator.StringToHash("MoveSpeed");
        public static readonly int MoveX = Animator.StringToHash("MoveX");
        public static readonly int MoveY = Animator.StringToHash("MoveY");
        
        // Shield parameters
        public static readonly int ShieldActive = Animator.StringToHash("ShieldActive");
        
        // Health parameters
        public static readonly int HealthPercent = Animator.StringToHash("HealthPercent");
        public static readonly int PhaseNumber = Animator.StringToHash("PhaseNumber");
    }
}