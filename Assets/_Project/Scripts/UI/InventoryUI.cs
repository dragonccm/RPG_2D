using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Giao diện Túi đồ (Inventory UI) 16 ô dạng lưới (Grid 4x4).
/// - Phím I hoặc B để đóng/mở.
/// - Hiển thị danh sách vật phẩm, số lượng, tooltip mô tả.
/// - Hỗ trợ bấm "Sử dụng" để uống bình máu hoặc lĩnh ngộ Cuộn bí kíp võ công.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    private GameObject panel;
    private Transform gridRoot;
    private Text titleText;
    private Text detailNameText;
    private Text detailDescText;
    private Button useButton;
    private Text useButtonText;

    private int selectedSlotIndex = -1;
    private readonly List<GameObject> slotObjects = new List<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        BuildPanel();
        if (panel != null) panel.SetActive(false);
    }

    private void Start()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += Refresh;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= Refresh;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.B))
        {
            TogglePanel();
        }
    }

    public void TogglePanel()
    {
        if (DialogueUI.Instance != null && DialogueUI.Instance.IsOpen) return;
        if (panel == null) return;

        bool active = !panel.activeSelf;
        panel.SetActive(active);

        if (active)
        {
            selectedSlotIndex = -1;
            Refresh();
        }
    }

    private void BuildPanel()
    {
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        // Khung nền chính
        panel = new GameObject("InventoryPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);

        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(520, 380);

        var bg = panel.GetComponent<Image>();
        bg.color = new Color(0.1f, 0.12f, 0.16f, 0.95f);

        // Tiêu đề
        var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
        titleGo.transform.SetParent(panel.transform, false);
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchoredPosition = new Vector2(0, 160);
        titleRt.sizeDelta = new Vector2(480, 40);

        titleText = titleGo.GetComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 20;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(1f, 0.85f, 0.4f);
        titleText.text = Loc.T("inv.title");

        // Lưới 16 ô đồ (bên trái)
        var gridGo = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup));
        gridGo.transform.SetParent(panel.transform, false);
        var gridRt = gridGo.GetComponent<RectTransform>();
        gridRt.anchoredPosition = new Vector2(-100, -10);
        gridRt.sizeDelta = new Vector2(260, 260);

        var gridLayout = gridGo.GetComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(58, 58);
        gridLayout.spacing = new Vector2(8, 8);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 4;
        gridRoot = gridGo.transform;

        // Khung chi tiết vật phẩm (bên phải)
        var detailGo = new GameObject("Details", typeof(RectTransform), typeof(Image));
        detailGo.transform.SetParent(panel.transform, false);
        var detailRt = detailGo.GetComponent<RectTransform>();
        detailRt.anchoredPosition = new Vector2(145, -10);
        detailRt.sizeDelta = new Vector2(180, 260);
        var detailBg = detailGo.GetComponent<Image>();
        detailBg.color = new Color(0.06f, 0.08f, 0.1f, 0.85f);

        // Tên item
        var nameGo = new GameObject("ItemName", typeof(RectTransform), typeof(Text));
        nameGo.transform.SetParent(detailGo.transform, false);
        var nameRt = nameGo.GetComponent<RectTransform>();
        nameRt.anchoredPosition = new Vector2(0, 95);
        nameRt.sizeDelta = new Vector2(160, 40);
        detailNameText = nameGo.GetComponent<Text>();
        detailNameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        detailNameText.fontSize = 14;
        detailNameText.fontStyle = FontStyle.Bold;
        detailNameText.alignment = TextAnchor.MiddleCenter;
        detailNameText.color = Color.white;
        detailNameText.text = Loc.T("inv.pick");

        // Mô tả item
        var descGo = new GameObject("ItemDesc", typeof(RectTransform), typeof(Text));
        descGo.transform.SetParent(detailGo.transform, false);
        var descRt = descGo.GetComponent<RectTransform>();
        descRt.anchoredPosition = new Vector2(0, 15);
        descRt.sizeDelta = new Vector2(160, 110);
        detailDescText = descGo.GetComponent<Text>();
        detailDescText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        detailDescText.fontSize = 12;
        detailDescText.alignment = TextAnchor.UpperLeft;
        detailDescText.color = new Color(0.8f, 0.8f, 0.8f);
        detailDescText.text = Loc.T("inv.pick.hint");

        // Nút dùng
        var btnGo = new GameObject("UseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(detailGo.transform, false);
        var btnRt = btnGo.GetComponent<RectTransform>();
        btnRt.anchoredPosition = new Vector2(0, -95);
        btnRt.sizeDelta = new Vector2(140, 35);
        var btnImg = btnGo.GetComponent<Image>();
        btnImg.color = new Color(0.2f, 0.6f, 0.3f);
        useButton = btnGo.GetComponent<Button>();
        useButton.onClick.AddListener(OnUseButtonClicked);

        var btnTextGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        btnTextGo.transform.SetParent(btnGo.transform, false);
        var btnTextRt = btnTextGo.GetComponent<RectTransform>();
        btnTextRt.sizeDelta = btnRt.sizeDelta;
        useButtonText = btnTextGo.GetComponent<Text>();
        useButtonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        useButtonText.fontSize = 13;
        useButtonText.alignment = TextAnchor.MiddleCenter;
        useButtonText.color = Color.white;
        useButtonText.text = Loc.T("inv.use");

        btnGo.SetActive(false);
    }

    public void Refresh()
    {
        if (gridRoot == null || InventoryManager.Instance == null) return;

        // Xây 16 ô slot nếu chưa đủ
        while (slotObjects.Count < InventoryManager.TOTAL_SLOTS)
        {
            int index = slotObjects.Count;
            var slotGo = new GameObject($"Slot_{index}", typeof(RectTransform), typeof(Image), typeof(Button));
            slotGo.transform.SetParent(gridRoot, false);
            var img = slotGo.GetComponent<Image>();
            img.color = new Color(0.18f, 0.22f, 0.28f);

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(slotGo.transform, false);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.sizeDelta = new Vector2(44, 44);
            var iconImg = iconGo.GetComponent<Image>();
            iconImg.color = Color.clear;

            var qtyGo = new GameObject("Qty", typeof(RectTransform), typeof(Text));
            qtyGo.transform.SetParent(slotGo.transform, false);
            var qtyRt = qtyGo.GetComponent<RectTransform>();
            qtyRt.anchoredPosition = new Vector2(16, -16);
            qtyRt.sizeDelta = new Vector2(24, 20);
            var qtyText = qtyGo.GetComponent<Text>();
            qtyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            qtyText.fontSize = 11;
            qtyText.fontStyle = FontStyle.Bold;
            qtyText.alignment = TextAnchor.LowerRight;
            qtyText.color = Color.yellow;
            qtyText.text = "";

            var btn = slotGo.GetComponent<Button>();
            btn.onClick.AddListener(() => OnSlotClicked(index));

            slotObjects.Add(slotGo);
        }

        // Cập nhật từng ô
        for (int i = 0; i < InventoryManager.TOTAL_SLOTS; i++)
        {
            var slot = InventoryManager.Instance.GetSlot(i);
            var slotGo = slotObjects[i];
            var img = slotGo.GetComponent<Image>();
            var iconImg = slotGo.transform.Find("Icon").GetComponent<Image>();
            var qtyText = slotGo.transform.Find("Qty").GetComponent<Text>();

            // Viền ô đang chọn
            img.color = (i == selectedSlotIndex)
                ? new Color(0.35f, 0.5f, 0.7f)
                : new Color(0.18f, 0.22f, 0.28f);

            if (slot != null && !slot.IsEmpty)
            {
                if (slot.item.icon != null)
                {
                    iconImg.sprite = slot.item.icon;
                    iconImg.color = Color.white;
                }
                else
                {
                    iconImg.color = Color.white;
                }
                qtyText.text = slot.quantity > 1 ? slot.quantity.ToString() : "";
            }
            else
            {
                iconImg.sprite = null;
                iconImg.color = Color.clear;
                qtyText.text = "";
            }
        }

        UpdateDetails();
    }

    private void OnSlotClicked(int index)
    {
        selectedSlotIndex = index;
        Refresh();
    }

    private void UpdateDetails()
    {
        if (selectedSlotIndex < 0 || selectedSlotIndex >= InventoryManager.TOTAL_SLOTS || InventoryManager.Instance == null)
        {
            detailNameText.text = Loc.T("inv.pick");
            detailDescText.text = Loc.T("inv.pick.hint");
            useButton.gameObject.SetActive(false);
            return;
        }

        var slot = InventoryManager.Instance.GetSlot(selectedSlotIndex);
        if (slot == null || slot.IsEmpty)
        {
            detailNameText.text = Loc.T("inv.empty");
            detailDescText.text = Loc.T("inv.empty.hint");
            useButton.gameObject.SetActive(false);
            return;
        }

        detailNameText.text = Loc.TOr("item." + slot.item.itemId + ".name", slot.item.itemName);
        detailDescText.text = Loc.T("inv.detail", Loc.TOr("item." + slot.item.itemId + ".desc", slot.item.description), GetTypeName(slot.item.itemType), slot.quantity);

        if (slot.item.isUsable)
        {
            useButton.gameObject.SetActive(true);
            useButtonText.text = slot.item.itemType == ItemType.SkillScroll ? Loc.T("inv.learn") : Loc.T("inv.use");
        }
        else
        {
            useButton.gameObject.SetActive(false);
        }
    }

    private string GetTypeName(ItemType type)
    {
        switch (type)
        {
            case ItemType.Consumable: return Loc.T("item.type.consumable");
            case ItemType.SkillScroll: return Loc.T("item.type.scroll");
            case ItemType.KeyItem: return Loc.T("item.type.key");
            case ItemType.Material: return Loc.T("item.type.material");
            default: return Loc.T("item.type.other");
        }
    }

    private void OnUseButtonClicked()
    {
        if (selectedSlotIndex < 0 || InventoryManager.Instance == null) return;
        bool ok = InventoryManager.Instance.UseItem(selectedSlotIndex);
        if (ok)
        {
            Refresh();
        }
    }
}
