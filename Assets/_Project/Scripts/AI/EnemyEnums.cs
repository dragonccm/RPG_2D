namespace RPGGame.AI
{
    /// <summary>
    /// Enemy type defines the basic combat role and behavior pattern
    /// </summary>
    public enum EnemyType
    {
        Melee,      // Close combat enemies
        Ranged,     // Long range attackers
        Support,    // Healers, buffers
        Boss,       // Special boss enemies
        Flying,     // Aerial enemies
        Summoner    // Enemies that can summon minions
    }

    /// <summary>
    /// Difficulty level affects stats and AI intelligence
    /// </summary>
    public enum EnemyDifficulty
    {
        Easy,       // Reduced stats, simple AI
        Normal,     // Default stats and AI
        Hard,       // Increased stats, smarter AI
        Extreme,    // Significantly buffed
        Elite,      // Special elites with unique abilities
        Champion    // Strongest non-boss enemies
    }

    /// <summary>
    /// Behavior pattern defines how the enemy approaches combat
    /// </summary>
    public enum EnemyBehavior
    {
        Normal,     // Standard behavior
        Aggressive, // Always attacks, never retreats
        Defensive,  // Cautious, retreats when low health
        Cunning,    // Uses terrain and calls for help
        Berserker   // Becomes more dangerous when injured
    }

    /// <summary>
    /// AI personality affects decision making
    /// </summary>
    public enum AIPersonality
    {
        Balanced,   // Even mix of all behaviors
        Aggressive, // Prefers direct confrontation
        Defensive,  // Prioritizes survival
        Cunning,    // Uses tactics and environmental advantages
        Berserker   // Becomes more aggressive when damaged
    }

    /// <summary>
    /// Movement pattern defines how the enemy moves during combat
    /// </summary>
    public enum MovementPattern
    {
        Direct,     // Move directly towards target
        Circular,   // Circle around the target
        Zigzag,     // Move in zigzag pattern
        Ambush,     // Hide and ambush
        Hit_And_Run // Attack and retreat repeatedly
    }

    /// <summary>
    /// AI state for the state machine
    /// </summary>
    public enum EnemyAIState
    {
        Idle,       // Not engaged, patrolling or standing still
        Chase,      // Pursuing a target
        Attack,     // In combat with a target
        Retreat,    // Withdrawing from combat
        Reposition, // Moving to a better position
        Stunned,    // Temporarily disabled
        Dead        // No longer active
    }

    /// <summary>
    /// Patrol modes for enemy movement when not in combat
    /// </summary>
    public enum PatrolMode
    {
        None,                   // No patrolling
        Loop,                   // Move between waypoints in order, then loop back
        PingPong,              // Move between waypoints back and forth
        RandomAroundAnchor,    // Random movement around a central point
        RandomBetweenPoints    // Random movement between defined points
    }
}