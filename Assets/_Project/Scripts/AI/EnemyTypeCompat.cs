using RPGGame.AI;

/// <summary>
/// Legacy EnemyType enum for backward compatibility
/// Maps to the main EnemyType enum
/// </summary>
public class EnemyTypeCompat
{
    public enum Type
    {
        Melee,
        Ranged,
        Support,
        Boss,
        Flying,
        Summoner
    }
    
    public static EnemyType ToMainEnum(Type type)
    {
        return (EnemyType)((int)type);
    }
    
    public static Type FromMainEnum(EnemyType type)
    {
        return (Type)((int)type);
    }
}