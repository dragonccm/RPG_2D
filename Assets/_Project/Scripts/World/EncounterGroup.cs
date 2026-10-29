using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Một nhóm giao tranh cố định (LEVEL_DESIGN 5): sinh đúng đội hình 1 lần khi player tới gần,
/// không hồi sinh vô hạn như EnemySpawner. Diệt hết → flag "clear:id", mở các rào khoá arena,
/// không xuất hiện lại sau khi load save. Tuỳ chọn khoá arena trong lúc đánh (boss).
/// </summary>
public class EncounterGroup : MonoBehaviour
{
    [Serializable]
    public class Slot
    {
        public GameObject prefab;
        public Vector2 offset;
        [Tooltip("Gắn EliteBuff (mini-boss)")]
        public bool elite;
        public string eliteName = "";
        public string rewardItemId = "";
    }

    [SerializeField] private string encounterId = "e1";
    [SerializeField] private float activateRadius = 11f;
    [SerializeField] private List<Slot> slots = new List<Slot>();
    [Tooltip("Bật khi giao tranh bắt đầu, tắt khi dọn xong (cổng arena)")]
    [SerializeField] private GameObject[] lockWhileActive;
    [Tooltip("Báo tiến độ quest cho mỗi kẻ địch bị hạ (questId:objectiveId)")]
    [SerializeField] private string reportKill = "";
    [SerializeField] private string reportClear = "";

    public string Flag => "clear:" + encounterId;
    public bool IsCleared { get; private set; }
    public bool IsActive { get; private set; }
    public int Alive => alive;

    private int alive;
    private Transform player;
    private float nextCheck;

    public void Configure(string id, float radius, List<Slot> list, GameObject[] locks, string kill, string clear)
    {
        encounterId = id; activateRadius = radius; slots = list; lockWhileActive = locks;
        reportKill = kill; reportClear = clear;
    }

    private void Start()
    {
        SetLocks(false);
        if (WorldFlags.Has(Flag)) IsCleared = true;
    }

    private void Update()
    {
        if (IsCleared || IsActive || Time.time < nextCheck) return;
        nextCheck = Time.time + 0.25f;
        if (player == null)
        {
            var pc = FindAnyObjectByType<PlayerController>();
            if (pc == null) return;
            player = pc.transform;
        }
        if (((Vector2)(player.position - transform.position)).sqrMagnitude <= activateRadius * activateRadius)
            Activate();
    }

    public void Activate()
    {
        if (IsActive || IsCleared) return;
        IsActive = true;
        SetLocks(true);
        foreach (var s in slots)
        {
            if (s.prefab == null) continue;
            var go = Instantiate(s.prefab, transform.position + (Vector3)s.offset, Quaternion.identity);
            if (s.elite)
            {
                var buff = go.AddComponent<EliteBuff>();
                buff.Configure(s.eliteName, s.rewardItemId);
            }
            // Prefab địch gốc nằm ở sorting layer Default — y-sort cùng player/vật thể để không bị nền che
            if (go.GetComponent<YSort>() == null) go.AddComponent<YSort>();
            EnemySpawner.NotifySpawned(go);
            var ch = go.GetComponent<Character>();
            if (ch == null) continue;
            alive++;
            ch.OnDeath += OnMemberDied;
            VfxService.Impact(go.transform.position, new Color(0.6f, 0.1f, 0.15f), 0.8f);
        }
        if (alive == 0) Clear();
    }

    private void OnMemberDied()
    {
        alive--;
        Report(reportKill);
        if (alive <= 0) Clear();
    }

    private void Clear()
    {
        IsActive = false;
        IsCleared = true;
        WorldFlags.Set(Flag);
        SetLocks(false);
        Report(reportClear);
    }

    private void SetLocks(bool on)
    {
        if (lockWhileActive == null) return;
        foreach (var go in lockWhileActive) if (go != null) go.SetActive(on);
    }

    private static void Report(string spec)
    {
        if (string.IsNullOrEmpty(spec) || QuestManager.Instance == null) return;
        var kv = spec.Split(':');
        if (kv.Length == 2) QuestManager.Instance.ReportProgress(kv[0], kv[1]);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, activateRadius);
    }
}
