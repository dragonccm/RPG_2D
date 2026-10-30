using System;
using UnityEngine;

/// <summary>Sự kiện âm thanh của game. Gameplay chỉ gọi AudioService.Play(SfxId) — không tự giữ AudioClip.</summary>
public enum SfxId
{
    SwordSwing, SwordHit, SwordMiss, EnemySwing, PlayerHurt, EnemyHurt, PlayerDeath, EnemyDeath,
    ChestOpen, Dash, Charge, Teleport, ScrollLearn, DoorOpen, BossRoar, BossDeath, Pickup, MagicCast, StoneHit
}

/// <summary>Bảng âm thanh dữ liệu (Resources/Audio/AudioBank). Sinh bởi Tools/RPG/Audio/Build Audio Bank.</summary>
[CreateAssetMenu(fileName = "AudioBank", menuName = "RPG/Audio Bank")]
public class SfxBank : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public SfxId id;
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 0.8f;
        [Range(0f, 0.3f)] public float pitchJitter = 0.05f;
    }

    public Entry[] entries;
    public AudioClip mapMusic;
    public AudioClip battleMusic;

    public Entry Find(SfxId id)
    {
        if (entries == null) return null;
        foreach (var e in entries) if (e != null && e.id == id) return e;
        return null;
    }
}
