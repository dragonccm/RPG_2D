using System.Collections;
using UnityEngine;

/// <summary>
/// Cổng âm thanh duy nhất (CORE_PROJECT_GUIDELINES 6.9). SFX: 2–3 biến thể ngẫu nhiên, pitch ±5%, tắt tiếng theo khoảng cách
/// tới camera, pool AudioSource (không tạo mới mỗi tiếng). Nhạc: crossfade giữa nhạc map và nhạc trận.
/// Hit-stop không làm méo âm (AudioSource không phụ thuộc timeScale).
/// </summary>
public static class AudioService
{
    public static float SfxVolume
    {
        get => PlayerPrefs.GetFloat("sfxVolume", 1f);
        set => PlayerPrefs.SetFloat("sfxVolume", Mathf.Clamp01(value));
    }
    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat("musicVolume", 0.6f);
        set { PlayerPrefs.SetFloat("musicVolume", Mathf.Clamp01(value)); if (host != null) host.ApplyMusicVolume(); }
    }

    /// <summary>Âm tắt dần tới 0 ở khoảng cách này (đơn vị thế giới) tính từ camera.</summary>
    public const float HearingRange = 18f;

    public static AudioClip CurrentMusic => host != null ? host.TargetMusic : null;

    private static SfxBank bank;
    private static Host host;

    private static SfxBank Bank => bank != null ? bank : (bank = Resources.Load<SfxBank>("Audio/AudioBank"));

    private static Host GetHost()
    {
        if (!Application.isPlaying) return null;
        if (host == null)
        {
            var go = new GameObject("AudioService");
            Object.DontDestroyOnLoad(go);
            host = go.AddComponent<Host>();
        }
        return host;
    }

    /// <summary>Phát SFX tại vị trí (vị trí để tính nhỏ dần theo khoảng cách; Vector3.zero-kiểu "không vị trí" dùng Play(id)).</summary>
    public static void Play(SfxId id, Vector3 position)
    {
        var h = GetHost();
        var entry = Bank != null ? Bank.Find(id) : null;
        if (h == null || entry == null || entry.clips == null || entry.clips.Length == 0) return;
        float vol = entry.volume * SfxVolume * Attenuation(position);
        if (vol <= 0.01f) return;
        h.PlayClip(entry.clips[Random.Range(0, entry.clips.Length)], vol, 1f + Random.Range(-entry.pitchJitter, entry.pitchJitter));
    }

    /// <summary>SFX không gắn vị trí (UI, nhặt đồ) — luôn nghe rõ.</summary>
    public static void Play(SfxId id) => Play(id, CameraPosition());

    public static void PlayMusic(AudioClip clip, float fadeSeconds = 1.5f)
    {
        var h = GetHost();
        if (h != null) h.CrossfadeTo(clip, fadeSeconds);
    }

    public static void PlayMapMusic() { if (Bank != null) PlayMusic(Bank.mapMusic); }
    public static void PlayBattleMusic() { if (Bank != null) PlayMusic(Bank.battleMusic, 0.8f); }

    /// <summary>Hệ số âm lượng theo khoảng cách camera (1 ở sát, 0 ở HearingRange). Thuần — test được.</summary>
    public static float AttenuationFor(float distance) => Mathf.Clamp01(1f - distance / HearingRange);

    private static float Attenuation(Vector3 position)
    {
        var cam = Camera.main;
        if (cam == null) return 1f;
        return AttenuationFor(Vector2.Distance(cam.transform.position, position));
    }

    private static Vector3 CameraPosition() => Camera.main != null ? Camera.main.transform.position : Vector3.zero;

    private sealed class Host : MonoBehaviour
    {
        private const int PoolSize = 12;
        private AudioSource[] pool;
        private int next;
        private AudioSource musicA, musicB, activeMusic;
        private Coroutine fade;
        public AudioClip TargetMusic { get; private set; }

        private void Awake()
        {
            pool = new AudioSource[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                pool[i] = gameObject.AddComponent<AudioSource>();
                pool[i].playOnAwake = false;
                pool[i].spatialBlend = 0f;
            }
            musicA = NewMusicSource();
            musicB = NewMusicSource();
            activeMusic = musicA;
        }

        private AudioSource NewMusicSource()
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = true;
            s.spatialBlend = 0f;
            s.volume = 0f;
            return s;
        }

        public void PlayClip(AudioClip clip, float volume, float pitch)
        {
            var s = pool[next];
            next = (next + 1) % PoolSize;
            s.pitch = pitch;
            s.PlayOneShot(clip, volume);
        }

        public void ApplyMusicVolume()
        {
            if (activeMusic != null && activeMusic.isPlaying) activeMusic.volume = MusicVolume;
        }

        public void CrossfadeTo(AudioClip clip, float seconds)
        {
            if (clip == TargetMusic) return;
            TargetMusic = clip;
            if (fade != null) StopCoroutine(fade);
            fade = StartCoroutine(Fade(clip, Mathf.Max(0.01f, seconds)));
        }

        private IEnumerator Fade(AudioClip clip, float seconds)
        {
            var from = activeMusic;
            var to = activeMusic == musicA ? musicB : musicA;
            if (clip != null)
            {
                to.clip = clip;
                to.volume = 0f;
                to.Play();
            }
            float fromStart = from.volume;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float k = t / seconds;
                from.volume = Mathf.Lerp(fromStart, 0f, k);
                if (clip != null) to.volume = Mathf.Lerp(0f, MusicVolume, k);
                yield return null;
            }
            from.Stop();
            from.volume = 0f;
            if (clip != null) to.volume = MusicVolume;
            activeMusic = to;
            fade = null;
        }
    }
}
