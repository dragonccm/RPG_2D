using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Sinh AttackDef (Resources/Attacks) cho Player, quái và Agis, rồi gắn vào prefab địch
/// (EnemyAttackController.attack) và bỏ kỹ năng cũ của quái (EnemySkillManager) để không đánh 2 lần.
/// Số liệu theo GAME_MASTER_PLAN 5.3: windup quái 0.4–0.7s, boss ≥ 0.4s (AoE ≥ 0.8s), mọi đòn quái/boss có telegraph.
/// Chạy: Tools/RPG/Combat/Build Attack Defs (cũng chạy trong MapBuilder.BuildAll).
/// </summary>
public static class AttackDefBuilder
{
    public const string Folder = "Assets/_Project/Resources/Attacks";

    [MenuItem("Tools/RPG/Combat/Build Attack Defs")]
    public static void BuildAll()
    {
        MapArt.EnsureFolder(Folder);

        // ── Player: combo 3 nhịp (chuỗi next) ──
        var s3 = Make("Player_Slash3", d =>
        {
            d.team = AttackTeam.Player; d.shape = AttackShape.Circle;
            d.windup = 0.25f; d.active = 0.1f; d.recovery = 0.4f; d.cooldown = 0f;
            d.reach = 0.7f; d.radius = 1.5f; d.damageMultiplier = 1.5f; d.knockback = 4f; d.canCrit = true;
            d.telegraph = false; d.feedback = HitFeedback.Heavy; d.slashColor = new Color(1f, 0.9f, 0.5f); d.slashSize = 1.3f;
            d.comboIndex = 2; d.next = null;
        });
        var s2 = Make("Player_Slash2", d =>
        {
            d.team = AttackTeam.Player; d.shape = AttackShape.Circle;
            d.windup = 0.18f; d.active = 0.08f; d.recovery = 0.24f; d.cooldown = 0f;
            d.reach = 0.7f; d.radius = 1.5f; d.damageMultiplier = 1f; d.knockback = 1.5f; d.canCrit = true;
            d.telegraph = false; d.feedback = HitFeedback.Light; d.comboIndex = 1; d.next = s3;
            d.cancelFrom = 0.35f; d.chainWindow = 0.4f;
        });
        Make("Player_Slash1", d =>
        {
            d.team = AttackTeam.Player; d.shape = AttackShape.Circle;
            d.windup = 0.2f; d.active = 0.08f; d.recovery = 0.24f; d.cooldown = 0f;
            d.reach = 0.7f; d.radius = 1.5f; d.damageMultiplier = 1f; d.knockback = 1.5f; d.canCrit = true;
            d.telegraph = false; d.feedback = HitFeedback.Light; d.comboIndex = 0; d.next = s2;
            d.cancelFrom = 0.35f; d.chainWindow = 0.4f;
        });

        // ── Quái ──
        var goblin = Make("Goblin_Club", d =>
        {
            d.team = AttackTeam.Enemy; d.shape = AttackShape.Circle;
            d.windup = 0.5f; d.active = 0.1f; d.recovery = 0.5f; d.cooldown = 0.7f;
            d.reach = 0.85f; d.radius = 0.7f; d.damageMultiplier = 0.8f; d.knockback = 3f;
        });
        var sheep = Make("Sheep_Ram", d =>
        {
            d.team = AttackTeam.Enemy; d.shape = AttackShape.Line;
            d.windup = 0.6f; d.active = 0.12f; d.recovery = 0.7f; d.cooldown = 1f;
            d.reach = 1.3f; d.radius = 0.45f; d.lungeDistance = 1.2f; d.damageMultiplier = 0.6f; d.knockback = 4f;
        });
        Make("Rat_Bite", d =>
        {
            d.team = AttackTeam.Enemy; d.shape = AttackShape.Circle;
            d.windup = 0.4f; d.active = 0.08f; d.recovery = 0.4f; d.cooldown = 0.5f;
            d.reach = 0.55f; d.radius = 0.5f; d.damageMultiplier = 0.6f;
        });
        var archer = Make("Archer_Shot", d =>
        {
            d.team = AttackTeam.Enemy; d.shape = AttackShape.Projectile;
            d.windup = 0.7f; d.active = 0.1f; d.recovery = 0.5f; d.cooldown = 0.9f;
            d.radius = 0.25f; d.projectileSpeed = 9f; d.projectileRange = 9f; d.projectileStyle = ProjectileStyle.Dart;
            d.damageMultiplier = 0.8f; d.knockback = 1.5f;
        });
        var lancer = Make("Lancer_Thrust", d =>
        {
            d.team = AttackTeam.Enemy; d.shape = AttackShape.Line;
            d.windup = 0.7f; d.active = 0.12f; d.recovery = 0.6f; d.cooldown = 0.8f;
            d.reach = 2f; d.radius = 0.45f; d.lungeDistance = 0.6f; d.damageMultiplier = 1.2f; d.knockback = 5f;
        });

        // ── Agis (boss) ──
        Make("Agis_Fireball", d =>
        {
            d.team = AttackTeam.Boss; d.animTrigger = ""; d.shape = AttackShape.Projectile;
            d.windup = 0.45f; d.active = 0.1f; d.recovery = 0.3f; d.cooldown = 0f;
            d.radius = 0.35f; d.projectileSpeed = 10f; d.projectileRange = 12f; d.projectileColor = new Color(1f, 0.5f, 0.12f);
            d.damageMultiplier = 2.5f; d.knockback = 3f;
            d.telegraphColor = new Color(1f, 0.8f, 0.2f, 0.8f);
        });
        Make("Agis_FireballBarrage", d =>
        {
            d.team = AttackTeam.Boss; d.animTrigger = ""; d.shape = AttackShape.Projectile;
            d.windup = 0.4f; d.active = 0.08f; d.recovery = 0.08f; d.cooldown = 0f;
            d.radius = 0.35f; d.projectileSpeed = 11f; d.projectileRange = 12f; d.projectileColor = new Color(1f, 0.25f, 0.1f);
            d.damageMultiplier = 2f; d.knockback = 2f;
            d.telegraphColor = new Color(1f, 0.5f, 0.2f, 0.8f);
        });
        Make("Agis_Slam", d =>
        {
            d.team = AttackTeam.Boss; d.animTrigger = ""; d.shape = AttackShape.Circle;
            d.windup = 1f; d.active = 0.15f; d.recovery = 0.5f; d.cooldown = 0f;
            d.reach = 0.1f; d.radius = 3f; d.damageMultiplier = 5f; d.knockback = 6f; d.stunSeconds = 0.4f;
            d.telegraphColor = new Color(1f, 0.15f, 0.15f, 0.8f);
        });

        AssetDatabase.SaveAssets();

        // ── Gắn vào prefab ──
        Wire("Assets/_Project/Art/animation/Goblin/Goblin.prefab", goblin);
        Wire("Assets/_Project/Art/animation/sheep/Enemy_Sheep.prefab", sheep);
        Wire("Assets/_Project/Art/animation/Archer/Archer.prefab", archer);
        Wire("Assets/_Project/Art/animation/Lancer/Lancer.prefab", lancer);
        AssetDatabase.SaveAssets();
    }

    /// <summary>Gắn AttackDef vào EnemyAttackController của prefab; bỏ kỹ năng cũ của quái.</summary>
    public static void Wire(string prefabPath, AttackDef def)
    {
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var atk = root.GetComponent<EnemyAttackController>();
            if (atk == null) { Debug.LogWarning($"[AttackDefBuilder] {prefabPath} không có EnemyAttackController"); return; }
            var so = new SerializedObject(atk);
            so.FindProperty("attack").objectReferenceValue = def;
            so.FindProperty("attackRange").floatValue = def.TriggerRange;
            so.ApplyModifiedPropertiesWithoutUndo();

            var skills = root.GetComponent<EnemySkillManager>();
            if (skills != null)
            {
                var ss = new SerializedObject(skills);
                ss.FindProperty("skillModules").arraySize = 0;
                ss.ApplyModifiedPropertiesWithoutUndo();
            }
            if (root.GetComponent<AttackRunner>() == null) root.AddComponent<AttackRunner>();
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static AttackDef Make(string name, Action<AttackDef> setup)
    {
        string path = $"{Folder}/{name}.asset";
        var d = AssetDatabase.LoadAssetAtPath<AttackDef>(path);
        if (d == null)
        {
            d = ScriptableObject.CreateInstance<AttackDef>();
            AssetDatabase.CreateAsset(d, path);
        }
        d.attackId = name.ToLowerInvariant();
        setup(d);
        EditorUtility.SetDirty(d);
        return d;
    }
}
