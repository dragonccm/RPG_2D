using System;
using UnityEngine;

/// <summary>
/// Logic thuần (test được) của câu đố "thắp N đuốc trong T giây": mỗi đuốc sau khi thắp
/// cháy trong `window` giây rồi tắt; cùng lúc tất cả đều cháy → giải xong (vĩnh viễn).
/// </summary>
public class TimedTorchLogic
{
    private readonly float[] litUntil;
    private readonly float window;

    public bool Solved { get; private set; }
    public int Count => litUntil.Length;

    public TimedTorchLogic(int count, float window)
    {
        litUntil = new float[count];
        for (int i = 0; i < count; i++) litUntil[i] = float.NegativeInfinity;
        this.window = window;
    }

    public bool IsLit(int i, float now) => Solved || now < litUntil[i];

    /// <summary>0..1 thời gian cháy còn lại (dùng cho ngọn lửa nhỏ dần).</summary>
    public float Remaining01(int i, float now) =>
        Solved ? 1f : Mathf.Clamp01((litUntil[i] - now) / window);

    /// <summary>Thắp đuốc i. Trả về true nếu lần thắp này giải xong câu đố.</summary>
    public bool Light(int i, float now)
    {
        if (Solved || i < 0 || i >= litUntil.Length) return false;
        litUntil[i] = now + window;
        for (int k = 0; k < litUntil.Length; k++)
            if (now >= litUntil[k]) return false;
        Solved = true;
        return true;
    }

    public void ForceSolved() => Solved = true;
}

/// <summary>
/// Câu đố đuốc (LEVEL_DESIGN 3.3 / 4). Gắn lên object cha; các PuzzleTorch con tự đăng ký.
/// Giải xong: bật flag, ẩn các rào chắn, báo quest. Trạng thái lưu qua WorldFlags ("torches:id").
/// </summary>
public class TorchPuzzle : MonoBehaviour
{
    [SerializeField] private string puzzleId = "torches_1";
    [SerializeField] private float window = 6f;
    [SerializeField] private PuzzleTorch[] torches;
    [Tooltip("Ẩn các object này khi giải xong (cổng, đá chắn, mực nước...)")]
    [SerializeField] private GameObject[] disableOnSolved;
    [Tooltip("Hiện các object này khi giải xong (rương, cầu...)")]
    [SerializeField] private GameObject[] enableOnSolved;

    [Header("Gắn nhiệm vụ (tuỳ chọn)")]
    [SerializeField] private string questId = "";
    [SerializeField] private string objectiveId = "";

    public string Flag => "torches:" + puzzleId;
    public event Action<TorchPuzzle> OnSolved;

    private TimedTorchLogic logic;

    private void Awake()
    {
        if (torches == null || torches.Length == 0) torches = GetComponentsInChildren<PuzzleTorch>();
        logic = new TimedTorchLogic(torches.Length, window);
        for (int i = 0; i < torches.Length; i++) torches[i].Bind(this, i);
    }

    private void Start()
    {
        if (WorldFlags.Has(Flag)) ApplySolved(false);
    }

    public void Light(int index)
    {
        if (logic.Light(index, Time.time))
        {
            WorldFlags.Set(Flag);
            ApplySolved(true);
        }
    }

    public bool IsLit(int index) => logic.IsLit(index, Time.time);
    public float Remaining01(int index) => logic.Remaining01(index, Time.time);
    public bool IsSolved => logic.Solved;

    private void ApplySolved(bool fresh)
    {
        logic.ForceSolved();
        if (disableOnSolved != null) foreach (var go in disableOnSolved) if (go != null) go.SetActive(false);
        if (enableOnSolved != null) foreach (var go in enableOnSolved) if (go != null) go.SetActive(true);
        if (!fresh) return;

        VfxService.Impact(transform.position, new Color(1f, 0.65f, 0.2f), 2f);
        VfxService.Shake(ShakeLevel.Light);
        if (!string.IsNullOrEmpty(questId) && QuestManager.Instance != null)
            QuestManager.Instance.ReportProgress(questId, objectiveId);
        OnSolved?.Invoke(this);
    }
}
