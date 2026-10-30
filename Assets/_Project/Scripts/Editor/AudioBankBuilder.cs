using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Sinh Resources/Audio/AudioBank từ bộ SFX/nhạc có sẵn trong Audio/. Chạy: Tools/RPG/Audio/Build Audio Bank (và trong MapBuilder.BuildAll).</summary>
public static class AudioBankBuilder
{
    private const string Sfx = "Assets/_Project/Audio/Minifantasy_Dungeon_SFX/";
    private const string Rpg = "Assets/_Project/Audio/rpg/";
    private const string Music = "Assets/_Project/Audio/Minifantasy_Dungeon_Music/Music/";
    private const string Folder = "Assets/_Project/Resources/Audio";

    [MenuItem("Tools/RPG/Audio/Build Audio Bank")]
    public static void Build()
    {
        MapArt.EnsureFolder(Folder);
        string path = Folder + "/AudioBank.asset";
        var bank = AssetDatabase.LoadAssetAtPath<SfxBank>(path);
        if (bank == null) { bank = ScriptableObject.CreateInstance<SfxBank>(); AssetDatabase.CreateAsset(bank, path); }

        bank.entries = new[]
        {
            E(SfxId.SwordSwing, 0.7f, Sfx + "07_human_atk_sword_1.wav", Sfx + "07_human_atk_sword_2.wav", Sfx + "07_human_atk_sword_3.wav"),
            E(SfxId.SwordHit, 0.8f, Sfx + "26_sword_hit_1.wav", Sfx + "26_sword_hit_2.wav", Sfx + "26_sword_hit_3.wav"),
            E(SfxId.SwordMiss, 0.5f, Sfx + "27_sword_miss_1.wav", Sfx + "27_sword_miss_2.wav", Sfx + "27_sword_miss_3.wav"),
            E(SfxId.EnemySwing, 0.6f, Sfx + "17_orc_atk_sword_1.wav", Sfx + "17_orc_atk_sword_2.wav", Sfx + "17_orc_atk_sword_3.wav"),
            E(SfxId.PlayerHurt, 0.8f, Sfx + "11_human_damage_1.wav", Sfx + "11_human_damage_2.wav", Sfx + "11_human_damage_3.wav"),
            E(SfxId.EnemyHurt, 0.7f, Sfx + "21_orc_damage_1.wav", Sfx + "21_orc_damage_2.wav", Sfx + "21_orc_damage_3.wav"),
            E(SfxId.PlayerDeath, 0.9f, Sfx + "14_human_death_spin.wav"),
            E(SfxId.EnemyDeath, 0.8f, Sfx + "24_orc_death_spin.wav", Rpg + "die_monster.wav"),
            E(SfxId.ChestOpen, 0.8f, Sfx + "01_chest_open_1.wav", Sfx + "01_chest_open_2.wav", Sfx + "01_chest_open_3.wav", Sfx + "01_chest_open_4.wav"),
            E(SfxId.Dash, 0.7f, Sfx + "15_human_dash_1.wav", Sfx + "15_human_dash_2.wav"),
            E(SfxId.Charge, 0.8f, Sfx + "08_human_charge_1.wav", Sfx + "08_human_charge_2.wav"),
            E(SfxId.Teleport, 0.7f, Sfx + "10_human_special_atk_1.wav", Sfx + "10_human_special_atk_2.wav"),
            E(SfxId.ScrollLearn, 0.9f, Sfx + "10_human_special_atk_2.wav"),
            E(SfxId.DoorOpen, 0.6f, Sfx + "05_door_open_1.mp3", Sfx + "05_door_open_2.mp3"),
            E(SfxId.BossRoar, 0.9f, Rpg + "roar_monster.wav", Rpg + "roar2_monster.wav", Rpg + "roar3_monster.wav"),
            E(SfxId.BossDeath, 1f, Rpg + "die_boss.wav"),
            E(SfxId.Pickup, 0.7f, Rpg + "coin.wav", Rpg + "gem.wav"),
            E(SfxId.MagicCast, 0.7f, Sfx + "20_orc_special_atk.wav", Sfx + "10_human_special_atk_1.wav"),
            E(SfxId.StoneHit, 0.6f, Sfx + "03_crate_open_1.wav", Sfx + "03_crate_open_2.wav", Sfx + "03_crate_open_3.wav"),
        };
        bank.mapMusic = AssetDatabase.LoadAssetAtPath<AudioClip>(Music + "Goblins_Den_(Regular).wav");
        bank.battleMusic = AssetDatabase.LoadAssetAtPath<AudioClip>(Music + "Goblins_Dance_(Battle).wav");
        EditorUtility.SetDirty(bank);
        AssetDatabase.SaveAssets();
    }

    private static SfxBank.Entry E(SfxId id, float volume, params string[] paths)
    {
        var clips = paths.Select(p => AssetDatabase.LoadAssetAtPath<AudioClip>(p)).Where(c => c != null).ToArray();
        if (clips.Length != paths.Length) Debug.LogWarning($"[AudioBankBuilder] {id}: thiếu clip ({clips.Length}/{paths.Length})");
        return new SfxBank.Entry { id = id, clips = clips, volume = volume };
    }
}
