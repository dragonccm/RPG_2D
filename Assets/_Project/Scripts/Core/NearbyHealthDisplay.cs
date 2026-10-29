/// <summary>
/// File: NearbyHealthDisplay.cs
/// Author: Unity 2D RPG Refactoring Agent  
/// Description: Display health bars for nearby enemies with proper API usage
/// </summary>

using UnityEngine;
using System.Collections.Generic;

public class NearbyHealthDisplay : MonoBehaviour
{
    [SerializeField] private float displayRange = 10f;
    [SerializeField] private int maxDisplayCount = 5;
    [SerializeField] private bool showWorldSpaceHealthBars = true;
    [SerializeField] private string enemyHealthBarPrefabPath = "EnemyHealthBar";
    
    private List<GameObject> healthBars = new List<GameObject>();
    private Canvas uiCanvas;

    void Start()
    {
        uiCanvas = GameObject.Find("UICanvas")?.GetComponent<Canvas>();
        if (uiCanvas == null)
            uiCanvas = FindFirstObjectByType<Canvas>();
        
        if (uiCanvas == null)
        {
            Debug.LogError("Kh�ng t�m th?y Canvas n�o trong scene!");
        }
    }

    private const float ScanInterval = 0.2f;
    private float nextScanTime;
    private readonly List<Character> nearby = new List<Character>(8);   // mục tiêu đang hiển thị
    private readonly List<HealthBar> barComponents = new List<HealthBar>(8);
    private GameObject healthBarPrefab;
    private Camera cam;

    void Update()
    {
        if (!showWorldSpaceHealthBars || uiCanvas == null) return;

        // Chọn lại tập kẻ địch gần theo nhịp; vị trí thanh máu vẫn cập nhật mỗi frame
        if (Time.time >= nextScanTime)
        {
            nextScanTime = Time.time + ScanInterval;
            SelectNearbyEnemies();
        }
        UpdateNearbyHealthBars();
    }

    /// <summary>Lấy tối đa maxDisplayCount kẻ địch gần nhất — chèn có thứ tự, không cấp phát.</summary>
    void SelectNearbyEnemies()
    {
        nearby.Clear();
        float rangeSqr = displayRange * displayRange;
        Vector2 self = transform.position;
        var all = Character.Active;
        for (int i = 0; i < all.Count; i++)
        {
            var c = all[i];
            if (!c.CompareTag("Enemy") || !c.IsAlive) continue;
            float d = ((Vector2)c.transform.position - self).sqrMagnitude;
            if (d > rangeSqr) continue;

            int at = nearby.Count;
            while (at > 0 && ((Vector2)nearby[at - 1].transform.position - self).sqrMagnitude > d) at--;
            if (at >= maxDisplayCount) continue;
            nearby.Insert(at, c);
            if (nearby.Count > maxDisplayCount) nearby.RemoveAt(nearby.Count - 1);
        }
    }

    void UpdateNearbyHealthBars()
    {
        int shown = 0;
        for (int i = 0; i < nearby.Count; i++)
        {
            var enemy = nearby[i];
            if (enemy == null || !enemy.IsAlive) continue;

            if (shown >= healthBars.Count && !CreateNewHealthBar()) break;
            var bar = healthBars[shown];
            var hb = barComponents[shown];
            if (bar == null) { shown++; continue; }

            if (hb != null && hb.target != enemy)
            {
                hb.Initialize(enemy);
                hb.SetupForEnemy();
            }
            UpdateHealthBarPosition(bar, enemy.transform);
            shown++;
        }

        for (int i = shown; i < healthBars.Count; i++)
        {
            if (healthBars[i] != null && healthBars[i].activeSelf) healthBars[i].SetActive(false);
        }
    }

    bool CreateNewHealthBar()
    {
        if (healthBarPrefab == null) healthBarPrefab = Resources.Load<GameObject>(enemyHealthBarPrefabPath);
        if (healthBarPrefab == null) return false;
        GameObject instance = Instantiate(healthBarPrefab, uiCanvas.transform);
        healthBars.Add(instance);
        barComponents.Add(instance.GetComponent<HealthBar>());
        return true;
    }

    void UpdateHealthBarPosition(GameObject healthBar, Transform target)
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 screenPos = cam.WorldToScreenPoint(target.position + Vector3.up * 1.5f);
        bool visible = screenPos.z > 0;
        if (visible) healthBar.transform.position = screenPos;
        if (healthBar.activeSelf != visible) healthBar.SetActive(visible);
    }

    void OnDestroy()
    {
        foreach (GameObject healthBar in healthBars)
        {
            if (healthBar != null)
                Destroy(healthBar);
        }
        healthBars.Clear();
    }

    public void SetDisplayRange(float range)
    {
        displayRange = range;
    }

    public void SetMaxDisplayCount(int count)
    {
        maxDisplayCount = count;
    }

    public void SetShowWorldSpaceHealthBars(bool show)
    {
        showWorldSpaceHealthBars = show;
        
        foreach (GameObject healthBar in healthBars)
        {
            if (healthBar != null)
                healthBar.SetActive(show);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, displayRange);
    }
}