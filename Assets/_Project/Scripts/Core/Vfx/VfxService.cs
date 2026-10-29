using System.Collections.Generic;
using UnityEngine;

/// <summary>Chất liệu bị đánh trúng — quyết định hiệu ứng: máu / tia lửa / bụi / ma thuật.</summary>
public enum HitMaterial { Flesh, Metal, Stone, Magic }

/// <summary>Mức rung + hit-stop chuẩn (CORE_PROJECT_GUIDELINES 6.4).</summary>
public enum ShakeLevel { Light, Heavy, Hurt, Boss }

/// <summary>
/// Cổng VFX duy nhất của game (CORE_PROJECT_GUIDELINES 6.1). Thay CombatEffectsManager,
/// EnhancedEffectManager, Effect2DManager, VisualEffects.
/// Gameplay chỉ gọi VfxService.*, không tự Instantiate effect.
/// Hạt/vòng/số sát thương do 1 host Update điều khiển, tái sử dụng bằng pool (0 coroutine, 0 GC khi ổn định).
/// </summary>
public static class VfxService
{
    // Bảng chuẩn rung/hit-stop: (biên độ, thời gian, hit-stop)
    private static readonly (float amp, float dur, float stop)[] ShakeTable =
    {
        (0.05f, 0.08f, 0.04f), // Light  — đòn thường trúng
        (0.12f, 0.15f, 0.06f), // Heavy  — finisher / chí mạng
        (0.08f, 0.10f, 0.00f), // Hurt   — người chơi nhận sát thương
        (0.20f, 0.25f, 0.00f), // Boss   — slam / nổ / boss gục
    };

    private const float MaxShakeAmp = 0.25f;
    private const float MaxShakeDur = 0.3f;
    private const float MaxHitStop = 0.08f;

    /// <summary>Hệ số rung 0..1 (cài đặt cho người dễ say). Mặc định 1.</summary>
    public static float ShakeScale = 1f;

    /// <summary>Độ lệch camera do rung ở frame hiện tại. CameraFollow cộng vào vị trí cuối.</summary>
    public static Vector2 ShakeOffset => host != null ? host.ShakeOffset : Vector2.zero;

    private static VfxHost host;

    private static VfxHost Host
    {
        get
        {
            if (!Application.isPlaying) return null;
            if (host == null)
            {
                var go = new GameObject("VfxService");
                Object.DontDestroyOnLoad(go);
                host = go.AddComponent<VfxHost>();
            }
            return host;
        }
    }

    // ───────────────────────── Sự kiện chiến đấu ─────────────────────────

    /// <summary>
    /// Đòn trúng đích: hạt văng theo hướng đòn (±30°) theo chất liệu. dir = 0 → văng tỏa ngẫu nhiên.
    /// </summary>
    public static void Hit(Vector3 position, Vector2 dir, HitMaterial material, bool heavy = false)
    {
        var h = Host;
        if (h == null) return;
        int count = heavy ? Random.Range(5, 8) : Random.Range(3, 6);
        bool aimed = dir.sqrMagnitude > 0.0001f;
        if (aimed) dir.Normalize();

        for (int i = 0; i < count; i++)
        {
            Vector2 d = aimed ? Rotate(dir, Random.Range(-30f, 30f)) : Random.insideUnitCircle.normalized;
            float speed = Random.Range(2.5f, 5.5f) * (heavy ? 1.25f : 1f);
            switch (material)
            {
                case HitMaterial.Flesh:
                    h.Spawn(PixelSprites.Pixel, position, d * speed, 0.18f + Random.value * 0.1f,
                        Random.value < 0.5f ? new Color(0.6f, 0.05f, 0.08f) : new Color(0.85f, 0.12f, 0.12f),
                        Random.value < 0.4f ? 4f : 3f, 1f, drag: 6f, gravity: 6f, rotate: false);
                    break;
                case HitMaterial.Metal:
                    h.Spawn(PixelSprites.Spark, position, d * (speed + 1.5f), 0.12f + Random.value * 0.08f,
                        Random.value < 0.5f ? new Color(1f, 0.95f, 0.7f) : Color.white,
                        2f, 1f, drag: 5f, gravity: 0f, rotate: true);
                    break;
                case HitMaterial.Stone:
                    h.Spawn(PixelSprites.Pixel, position, d * speed * 0.7f, 0.22f + Random.value * 0.1f,
                        new Color(0.55f, 0.52f, 0.48f), 4f, 2f, drag: 7f, gravity: 5f, rotate: false);
                    break;
                default:
                    h.Spawn(PixelSprites.Pixel, position, d * speed * 0.8f, 0.25f,
                        new Color(0.7f, 0.4f, 1f), 3f, 1f, drag: 4f, gravity: 0f, rotate: false);
                    break;
            }
        }
    }

    /// <summary>Lưỡi liềm chém pixel khớp hướng vung (CORE 2.1). Sống ~0.12s, bước nhảy 3 nấc.</summary>
    public static void Slash(Vector3 origin, Vector2 dir, Color tint, float size = 1f)
    {
        var h = Host;
        if (h == null) return;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        h.SpawnStatic(PixelSprites.Arc(1.5f * size), origin, Quaternion.Euler(0f, 0f, angle), 0.12f, tint,
            1f, 1f, stepped: 3);
    }

    /// <summary>
    /// Sự kiện chung (checkpoint, mở rương, công tắc, dash...): vòng pixel nở 5 frame + mảnh pixel tỏa ra.
    /// Vòng cuối có bán kính 0.9 × scale (đơn vị thế giới).
    /// </summary>
    public static void Impact(Vector3 position, Color tint, float scale = 1f)
    {
        var h = Host;
        if (h == null) return;
        scale = Mathf.Max(0.2f, scale);
        h.SpawnFrames(RingFrames(scale), position, 0.3f, tint);

        int shards = Mathf.Clamp(Mathf.RoundToInt(4 + scale * 2f), 4, 10);
        for (int i = 0; i < shards; i++)
        {
            Vector2 d = Random.insideUnitCircle.normalized;
            h.Spawn(PixelSprites.Pixel, position, d * Random.Range(1.5f, 3.5f) * Mathf.Sqrt(scale),
                0.22f + Random.value * 0.12f, tint, Random.value < 0.4f ? 4f : 3f, 1f,
                drag: 5f, gravity: 3f, rotate: false);
        }
    }

    private static readonly Dictionary<int, Sprite[]> ringFrames = new Dictionary<int, Sprite[]>();

    /// <summary>5 frame vòng nở, bán kính 0.3→0.9 × scale (đơn vị thế giới), cache theo scale.</summary>
    private static Sprite[] RingFrames(float scale)
    {
        int key = Mathf.RoundToInt(scale * 10f);
        if (ringFrames.TryGetValue(key, out var frames) && frames[0] != null) return frames;
        frames = new Sprite[5];
        for (int i = 0; i < 5; i++)
            frames[i] = PixelSprites.Ring(Mathf.RoundToInt((0.3f + 0.15f * i) * scale * PixelSprites.PPU));
        ringFrames[key] = frames;
        return frames;
    }

    /// <summary>
    /// Vùng báo hiệu (telegraph) pixel-art: đĩa dither + viền răng cưa, nhấp nháy theo bậc.
    /// duration ≤ 0 hoặc rất lớn = tồn tại tới khi người gọi Destroy. follow != null → bám theo.
    /// </summary>
    public static GameObject Telegraph(Vector3 position, float radius, float duration, Color tint, Transform follow = null)
    {
        if (!Application.isPlaying) return null;
        radius = Mathf.Max(0.25f, radius);
        int px = Mathf.RoundToInt(radius * PixelSprites.PPU);
        var go = new GameObject("VfxTelegraph");
        go.transform.position = position;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PixelSprites.DitherDisc(px);
        sr.color = tint;
        sr.sortingLayerName = "Effects";
        sr.sortingOrder = 4000;
        var t = go.AddComponent<VfxTelegraph>();
        t.Init(sr, tint, duration, follow);
        return go;
    }

    // ───────────────────────── Số sát thương ─────────────────────────

    public static void DamageNumber(float value, Vector3 position, bool critical = false, bool heal = false)
    {
        var h = Host;
        if (h == null) return;
        h.SpawnNumber(Mathf.CeilToInt(value), position, critical, heal);
    }

    // ───────────────────────── Rung màn hình + hit-stop ─────────────────────────

    public static void Shake(ShakeLevel level)
    {
        var s = ShakeTable[(int)level];
        Shake(s.amp, s.dur);
    }

    /// <summary>Rung tuỳ biến — bị chặn trên (≤0.25 / ≤0.3s). Nhiều nguồn cùng lúc: lấy mạnh nhất, không cộng dồn.</summary>
    public static void Shake(float intensity, float duration)
    {
        var h = Host;
        if (h == null) return;
        intensity = Mathf.Min(intensity, MaxShakeAmp) * Mathf.Clamp01(ShakeScale);
        duration = Mathf.Min(duration, MaxShakeDur);
        if (intensity <= 0f || duration <= 0f) return;
        h.RequestShake(intensity, duration);
    }

    public static void HitStop(ShakeLevel level) => HitStop(ShakeTable[(int)level].stop);

    /// <summary>Đóng băng ngắn. Chồng lên nhau chỉ kéo dài, không bao giờ kẹt timeScale = 0.</summary>
    public static void HitStop(float seconds)
    {
        var h = Host;
        if (h == null || seconds <= 0f) return;
        h.RequestHitStop(Mathf.Min(seconds, MaxHitStop));
    }

    // ───────────────────────── Effect từ prefab (art vẽ tay) ─────────────────────────

    /// <summary>
    /// Sinh effect prefab. lifetime &lt; 0: tự đo từ animation/particle; lifetime = 0: tồn tại tới khi
    /// người gọi tự Destroy; lifetime &gt; 0: hủy sau chừng đó giây.
    /// </summary>
    public static GameObject PlayPrefab(GameObject prefab, Vector3 position, Quaternion rotation = default,
        float scale = 1f, float lifetime = -1f)
    {
        if (prefab == null || !Application.isPlaying) return null;
        var go = Object.Instantiate(prefab, position, rotation == default ? Quaternion.identity : rotation);
        if (!Mathf.Approximately(scale, 1f)) go.transform.localScale = Vector3.one * scale;
        if (lifetime < 0f) lifetime = MeasureLifetime(go);
        if (lifetime > 0f) Object.Destroy(go, lifetime);
        return go;
    }

    /// <summary>Effect prefab xoay theo hướng đòn (2D: trục +Y của prefab hướng theo dir).</summary>
    public static GameObject PlayPrefabAimed(GameObject prefab, Vector3 position, Vector2 dir,
        float scale = 1f, float lifetime = -1f)
    {
        var rot = dir.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(Vector3.forward, dir) : Quaternion.identity;
        return PlayPrefab(prefab, position, rot, scale, lifetime);
    }

    /// <summary>Effect prefab bám theo target; tự hủy khi target mất.</summary>
    public static GameObject FollowPrefab(GameObject prefab, Transform target, Vector3 offset = default,
        float scale = 1f, float lifetime = -1f)
    {
        if (target == null) return null;
        var go = PlayPrefab(prefab, target.position + offset, Quaternion.identity, scale, lifetime);
        if (go != null) go.AddComponent<VfxFollow>().Init(target, offset);
        return go;
    }

    private static float MeasureLifetime(GameObject go)
    {
        float max = 0.5f;
        foreach (var animator in go.GetComponentsInChildren<Animator>())
        {
            if (animator.runtimeAnimatorController == null) continue;
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
                max = Mathf.Max(max, clip.length);
        }
        foreach (var ps in go.GetComponentsInChildren<ParticleSystem>())
        {
            var main = ps.main;
            max = Mathf.Max(max, main.loop ? 3f : main.startLifetime.constantMax + main.duration);
        }
        return max + 0.25f;
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float r = degrees * Mathf.Deg2Rad;
        float c = Mathf.Cos(r), s = Mathf.Sin(r);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }

    // ───────────────────────── Host ─────────────────────────

    private sealed class Item
    {
        public Transform tr;
        public SpriteRenderer sr;
        public Vector2 vel;
        public float drag, gravity;
        public float age, life;
        public float s0, s1;
        public int stepped;      // 0 = mượt theo pixel; >0 = số nấc
        public bool intScale;    // scale làm tròn nguyên (hạt)
        public bool faceVelocity;
        public Sprite[] frames;  // != null: đổi sprite theo bậc thay vì scale
        public Color color;
    }

    private sealed class NumberItem
    {
        public Transform tr;
        public TextMesh text;
        public float age;
        public Vector3 start;
        public Color color;
    }

    private sealed class VfxHost : MonoBehaviour
    {
        private const float NumberLife = 0.9f;

        private readonly List<Item> active = new List<Item>(64);
        private readonly Stack<Item> free = new Stack<Item>(64);
        private readonly List<NumberItem> numbers = new List<NumberItem>(16);
        private readonly Stack<NumberItem> freeNumbers = new Stack<NumberItem>(16);

        private float shakeAmp, shakeDur, shakeAge;
        private bool stopped;
        private float savedTimeScale = 1f;
        private float stopUntil;

        public Vector2 ShakeOffset { get; private set; }

        // ── tạo item ──
        private Item Acquire()
        {
            Item it = null;
            while (free.Count > 0)
            {
                it = free.Pop();
                if (it.tr != null) break;
                it = null;
            }
            if (it == null)
            {
                var go = new GameObject("Vfx");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingLayerName = "Effects";
                sr.sortingOrder = 5000;
                it = new Item { tr = go.transform, sr = sr };
            }
            it.tr.gameObject.SetActive(true);
            it.age = 0f;
            it.vel = Vector2.zero;
            it.drag = it.gravity = 0f;
            it.stepped = 0;
            it.intScale = false;
            it.faceVelocity = false;
            it.frames = null;
            it.tr.rotation = Quaternion.identity;
            return it;
        }

        public void Spawn(Sprite sprite, Vector3 pos, Vector2 vel, float life, Color color,
            float startScale, float endScale, float drag, float gravity, bool rotate)
        {
            var it = Acquire();
            it.sr.sprite = sprite;
            it.tr.position = Snap(pos);
            it.vel = vel;
            it.life = life;
            it.color = color;
            it.s0 = startScale;
            it.s1 = endScale;
            it.drag = drag;
            it.gravity = gravity;
            it.intScale = true;
            it.faceVelocity = rotate;
            Apply(it, 0f);
            active.Add(it);
        }

        public void SpawnStatic(Sprite sprite, Vector3 pos, Quaternion rot, float life, Color color,
            float startScale, float endScale, int stepped)
        {
            var it = Acquire();
            it.sr.sprite = sprite;
            it.tr.position = Snap(pos);
            it.tr.rotation = rot;
            it.life = life;
            it.color = color;
            it.s0 = startScale;
            it.s1 = endScale;
            it.stepped = stepped;
            Apply(it, 0f);
            active.Add(it);
        }

        public void SpawnFrames(Sprite[] frames, Vector3 pos, float life, Color color)
        {
            var it = Acquire();
            it.frames = frames;
            it.sr.sprite = frames[0];
            it.tr.position = Snap(pos);
            it.life = life;
            it.color = color;
            it.s0 = it.s1 = 1f;
            Apply(it, 0f);
            active.Add(it);
        }

        public void SpawnNumber(int value, Vector3 pos, bool critical, bool heal)
        {
            NumberItem n = null;
            while (freeNumbers.Count > 0)
            {
                n = freeNumbers.Pop();
                if (n.tr != null) break;
                n = null;
            }
            if (n == null)
            {
                var go = new GameObject("VfxNumber");
                go.transform.SetParent(transform, false);
                var tm = go.AddComponent<TextMesh>();
                tm.anchor = TextAnchor.MiddleCenter;
                tm.characterSize = 0.4f;
                var mr = go.GetComponent<MeshRenderer>();
                mr.sortingLayerName = "Effects";
                mr.sortingOrder = 6000;
                n = new NumberItem { tr = go.transform, text = tm };
            }
            n.tr.gameObject.SetActive(true);
            n.age = 0f;
            n.start = pos;
            n.color = heal ? new Color(0.3f, 0.9f, 0.35f) : critical ? Color.yellow : new Color(0.95f, 0.2f, 0.2f);
            n.text.text = value.ToString();
            n.text.fontSize = critical ? 20 : 14;
            n.text.color = n.color;
            n.tr.position = pos;
            numbers.Add(n);
        }

        // ── rung / hit-stop ──
        public void RequestShake(float amp, float dur)
        {
            float remaining = shakeDur > 0f ? shakeAmp * Mathf.Pow(Mathf.Clamp01(1f - shakeAge / shakeDur), 2f) : 0f;
            if (amp < remaining) return; // chỉ nhận nguồn mạnh hơn, không cộng dồn
            shakeAmp = amp;
            shakeDur = dur;
            shakeAge = 0f;
        }

        public void RequestHitStop(float seconds)
        {
            if (!stopped)
            {
                if (Time.timeScale <= 0f) return; // đang pause — không can thiệp
                savedTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                stopped = true;
            }
            stopUntil = Mathf.Max(stopUntil, Time.unscaledTime + seconds);
        }

        private void ReleaseHitStop()
        {
            if (!stopped) return;
            Time.timeScale = savedTimeScale;
            stopped = false;
        }

        private void OnDisable() { ReleaseHitStop(); }
        private void OnDestroy() { ReleaseHitStop(); }

        // ── vòng lặp ──
        private void Update()
        {
            if (stopped && Time.unscaledTime >= stopUntil) ReleaseHitStop();

            float dt = Time.deltaTime;

            if (shakeDur > 0f)
            {
                shakeAge += Time.unscaledDeltaTime;
                if (shakeAge >= shakeDur)
                {
                    shakeDur = 0f;
                    ShakeOffset = Vector2.zero;
                }
                else
                {
                    float amp = shakeAmp * Mathf.Pow(1f - shakeAge / shakeDur, 2f);
                    ShakeOffset = (Vector2)Snap(Random.insideUnitCircle * amp);
                }
            }

            for (int i = active.Count - 1; i >= 0; i--)
            {
                var it = active[i];
                it.age += dt;
                if (it.age >= it.life)
                {
                    it.tr.gameObject.SetActive(false);
                    free.Push(it);
                    active.RemoveAt(i);
                    continue;
                }
                if (it.drag > 0f) it.vel /= 1f + it.drag * dt;
                it.vel.y -= it.gravity * dt;
                if (it.vel.sqrMagnitude > 0f) it.tr.position = Snap(it.tr.position + (Vector3)(it.vel * dt));
                Apply(it, it.age / it.life);
            }

            for (int i = numbers.Count - 1; i >= 0; i--)
            {
                var n = numbers[i];
                n.age += dt;
                if (n.age >= NumberLife)
                {
                    n.tr.gameObject.SetActive(false);
                    freeNumbers.Push(n);
                    numbers.RemoveAt(i);
                    continue;
                }
                float p = n.age / NumberLife;
                n.tr.position = Snap(n.start + Vector3.up * (1.6f * p));
                var c = n.color;
                c.a = 1f - p * p;
                n.text.color = c;
            }
        }

        private static void Apply(Item it, float t)
        {
            if (it.frames != null)
                it.sr.sprite = it.frames[Mathf.Min(it.frames.Length - 1, (int)(t * it.frames.Length))];
            float st = it.stepped > 0 ? Mathf.Floor(t * it.stepped) / it.stepped : t;
            float s = Mathf.Lerp(it.s0, it.s1, st);
            if (it.intScale) s = Mathf.Max(1f, Mathf.Round(s));
            it.tr.localScale = new Vector3(s, s, 1f);
            if (it.faceVelocity && it.vel.sqrMagnitude > 0.01f)
                it.tr.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(it.vel.y, it.vel.x) * Mathf.Rad2Deg - 90f);
            var c = it.color;
            c.a = it.color.a * (1f - t * t);
            it.sr.color = c;
        }
    }

    private static Vector3 Snap(Vector3 p)
    {
        const float u = PixelSprites.PixelSize;
        return new Vector3(Mathf.Round(p.x / u) * u, Mathf.Round(p.y / u) * u, p.z);
    }

    // ───────────────────────── Component phụ ─────────────────────────

    private sealed class VfxFollow : MonoBehaviour
    {
        private Transform target;
        private Vector3 offset;

        public void Init(Transform t, Vector3 o) { target = t; offset = o; }

        private void LateUpdate()
        {
            if (target == null) { Destroy(gameObject); return; }
            transform.position = target.position + offset;
        }
    }

    private sealed class VfxTelegraph : MonoBehaviour
    {
        private SpriteRenderer sr;
        private Color tint;
        private float duration;
        private float age;
        private Transform follow;
        private bool hadFollow;

        public void Init(SpriteRenderer renderer, Color color, float time, Transform followTarget)
        {
            sr = renderer;
            tint = color;
            duration = time > 0f && time < 3600f ? time : 0f;
            follow = followTarget;
            hadFollow = followTarget != null;
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (hadFollow)
            {
                if (follow == null) { Destroy(gameObject); return; }
                transform.position = follow.position;
            }

            if (duration > 0f && age >= duration) { Destroy(gameObject); return; }

            // Nhấp nháy theo bậc (≈6 Hz), nhanh dần khi sắp hết giờ
            float rate = duration > 0f && duration - age < 0.35f ? 14f : 6f;
            bool bright = Mathf.FloorToInt(age * rate) % 2 == 0;
            var c = tint;
            c.a = tint.a * (bright ? 1f : 0.55f);
            sr.color = c;
        }
    }
}

/// <summary>Làm mờ dần mọi renderer sau một độ trễ (dùng cho chỉ báo vùng của SkillExecutor).</summary>
public class FadeOutEffect : MonoBehaviour
{
    public void StartFadeOut(float delay, float fadeTime)
    {
        StartCoroutine(Run(delay, fadeTime));
    }

    private System.Collections.IEnumerator Run(float delay, float fadeTime)
    {
        yield return new WaitForSeconds(delay);
        var renderers = GetComponentsInChildren<SpriteRenderer>();
        var colors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) colors[i] = renderers[i].color;

        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float a = 1f - Mathf.Clamp01(t / fadeTime);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                var c = colors[i];
                c.a *= a;
                renderers[i].color = c;
            }
            yield return null;
        }
    }
}
