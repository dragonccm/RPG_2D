using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Animation môi trường rẻ (CORE_PROJECT_GUIDELINES 6.8): cây đung đưa, bụi lay, lửa, bọt nước...
/// Không dùng Animator cho từng bụi — mọi SpriteLoop được 1 SpriteLoopSystem cập nhật trong 1 Update,
/// chỉ đổi sprite khi tới frame mới. Pha lệch ngẫu nhiên để cả rừng không lắc cùng nhịp.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteLoop : MonoBehaviour
{
    public Sprite[] frames;
    public float fps = 8f;
    [Tooltip("Pha bắt đầu ngẫu nhiên (0..1 chu kỳ)")]
    public bool randomPhase = true;

    [System.NonSerialized] public SpriteRenderer sr;
    [System.NonSerialized] public float phase;
    [System.NonSerialized] public int current = -1;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        phase = randomPhase ? Random.value * 10f : 0f;
    }

    private void OnEnable() { SpriteLoopSystem.Register(this); }
    private void OnDisable() { SpriteLoopSystem.Unregister(this); }
}

public class SpriteLoopSystem : MonoBehaviour
{
    private static SpriteLoopSystem instance;
    private static readonly List<SpriteLoop> loops = new List<SpriteLoop>(256);

    public static int Count => loops.Count;

    public static void Register(SpriteLoop loop)
    {
        if (loop.frames == null || loop.frames.Length < 2) return;
        loops.Add(loop);
        if (instance == null && Application.isPlaying)
        {
            var go = new GameObject("SpriteLoopSystem");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<SpriteLoopSystem>();
        }
    }

    public static void Unregister(SpriteLoop loop) { loops.Remove(loop); }

    /// <summary>Frame ứng với thời điểm t (test được).</summary>
    public static int FrameAt(float t, float phase, float fps, int count) =>
        count <= 0 ? 0 : (int)((t + phase) * fps) % count;

    private void Update()
    {
        float t = Time.time;
        for (int i = 0; i < loops.Count; i++)
        {
            var l = loops[i];
            int f = FrameAt(t, l.phase, l.fps, l.frames.Length);
            if (f == l.current) continue;
            l.current = f;
            l.sr.sprite = l.frames[f];
        }
    }
}
