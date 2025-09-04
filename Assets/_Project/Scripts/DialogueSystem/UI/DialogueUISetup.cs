using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace DialogueSystem
{
    /// <summary>
    /// Script hỗ trợ tạo UI cho Dialogue System
    /// Tự động tạo các thành phần cần thiết nếu chưa có
    /// </summary>
    public class DialogueUISetup : MonoBehaviour
    {
        [Header("Auto Setup")]
        [SerializeField] private bool autoSetupOnAwake = true;
        [SerializeField] private bool createMissingComponents = true;

        [Header("UI Templates")]
        [SerializeField] private Sprite defaultPortraitSprite;
        [SerializeField] private TMP_FontAsset defaultFont;
        [SerializeField] private Font defaultLegacyFont;

        private void Awake()
        {
            if (autoSetupOnAwake)
            {
                SetupDialogueUI();
            }
        }

        /// <summary>
        /// Thiết lập hoàn chỉnh Dialogue UI
        /// </summary>
        public void SetupDialogueUI()
        {
            Debug.Log("🎨 Setting up Dialogue UI...");

            // Tạo Canvas nếu chưa có
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.pixelPerfect = true;
            }

            // Thêm Canvas Scaler
            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }

            // Thêm Graphic Raycaster
            GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            // Tạo DialogueUI component
            DialogueUI dialogueUI = GetComponent<DialogueUI>();
            if (dialogueUI == null)
            {
                dialogueUI = gameObject.AddComponent<DialogueUI>();
            }

            // Tạo các UI elements
            CreateDialoguePanel(dialogueUI);
            CreateBackgroundImage(dialogueUI);

            Debug.Log("✅ Dialogue UI setup completed!");
        }

        /// <summary>
        /// Tạo Dialogue Panel chính
        /// </summary>
        private void CreateDialoguePanel(DialogueUI dialogueUI)
        {
            // Tạo Dialogue Panel
            GameObject dialoguePanel = new GameObject("DialoguePanel");
            dialoguePanel.transform.SetParent(transform, false);

            // Setup RectTransform
            RectTransform panelRect = dialoguePanel.AddComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(1200, 300);
            panelRect.anchorMin = new Vector2(0.5f, 0);
            panelRect.anchorMax = new Vector2(0.5f, 0);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = new Vector2(0, 100);

            // Thêm Image và CanvasGroup
            Image panelImage = dialoguePanel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.9f);

            CanvasGroup canvasGroup = dialoguePanel.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0; // Bắt đầu ẩn

            // Tạo các text elements
            CreateTextElements(dialoguePanel);

            // Tạo control elements
            CreateControlElements(dialoguePanel);

            // Tạo choice system
            CreateChoiceSystem(dialoguePanel);

            // Gán references cho DialogueUI
            AssignReferences(dialogueUI, dialoguePanel);
        }

        /// <summary>
        /// Tạo các text elements
        /// </summary>
        private void CreateTextElements(GameObject dialoguePanel)
        {
            // NPC Name Text
            GameObject npcNameObj = CreateTextObject("NPCNameText", dialoguePanel,
                new Vector2(300, 50), new Vector2(50, -25), new Vector2(0, 1));
            TextMeshProUGUI npcNameText = npcNameObj.GetComponent<TextMeshProUGUI>();
            npcNameText.fontSize = 24;
            npcNameText.color = Color.yellow;
            npcNameText.alignment = TextAlignmentOptions.Left;
            npcNameText.fontStyle = FontStyles.Bold;
            npcNameText.text = "NPC Name";

            // Dialogue Text
            GameObject dialogueTextObj = CreateTextObject("DialogueText", dialoguePanel,
                new Vector2(1000, 150), Vector2.zero, new Vector2(0.5f, 0.5f));
            TextMeshProUGUI dialogueText = dialogueTextObj.GetComponent<TextMeshProUGUI>();
            dialogueText.fontSize = 18;
            dialogueText.color = Color.white;
            dialogueText.alignment = TextAlignmentOptions.TopLeft;
            dialogueText.enableWordWrapping = true;
            dialogueText.text = "Dialogue text will appear here...";

            // Continue Text
            GameObject continueTextObj = CreateTextObject("ContinueText", dialoguePanel,
                new Vector2(200, 30), new Vector2(-50, 25), new Vector2(1, 0));
            TextMeshProUGUI continueText = continueTextObj.GetComponent<TextMeshProUGUI>();
            continueText.fontSize = 14;
            continueText.color = new Color(0.7f, 0.7f, 0.7f);
            continueText.alignment = TextAlignmentOptions.Right;
            continueText.text = "Nhấn SPACE hoặc ENTER để tiếp tục...";
        }

        /// <summary>
        /// Tạo control elements
        /// </summary>
        private void CreateControlElements(GameObject dialoguePanel)
        {
            // Continue Button
            GameObject continueButtonObj = new GameObject("ContinueButton");
            continueButtonObj.transform.SetParent(dialoguePanel.transform, false);

            RectTransform buttonRect = continueButtonObj.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(50, 50);
            buttonRect.anchorMin = new Vector2(1, 0);
            buttonRect.anchorMax = new Vector2(1, 0);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(-25, 25);

            Button continueButton = continueButtonObj.AddComponent<Button>();
            Image buttonImage = continueButtonObj.AddComponent<Image>();
            buttonImage.color = new Color(1, 1, 1, 0.1f);

            // Add button text
            GameObject buttonTextObj = new GameObject("Text");
            buttonTextObj.transform.SetParent(continueButtonObj.transform, false);
            TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
            buttonText.fontSize = 16;
            buttonText.color = Color.white;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.text = "▶";

            RectTransform textRect = buttonTextObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            // NPC Portrait
            GameObject portraitObj = new GameObject("NPCPortrait");
            portraitObj.transform.SetParent(dialoguePanel.transform, false);

            RectTransform portraitRect = portraitObj.AddComponent<RectTransform>();
            portraitRect.sizeDelta = new Vector2(120, 120);
            portraitRect.anchorMin = new Vector2(0, 1);
            portraitRect.anchorMax = new Vector2(0, 1);
            portraitRect.pivot = new Vector2(0.5f, 0.5f);
            portraitRect.anchoredPosition = new Vector2(50, -75);

            Image portraitImage = portraitObj.AddComponent<Image>();
            portraitImage.color = Color.white;
            if (defaultPortraitSprite != null)
            {
                portraitImage.sprite = defaultPortraitSprite;
            }
        }

        /// <summary>
        /// Tạo choice system
        /// </summary>
        private void CreateChoiceSystem(GameObject dialoguePanel)
        {
            // Choice Panel
            GameObject choicePanel = new GameObject("ChoicePanel");
            choicePanel.transform.SetParent(dialoguePanel.transform, false);
            choicePanel.SetActive(false);

            RectTransform choicePanelRect = choicePanel.AddComponent<RectTransform>();
            choicePanelRect.sizeDelta = new Vector2(1000, 200);
            choicePanelRect.anchorMin = new Vector2(0.5f, 0);
            choicePanelRect.anchorMax = new Vector2(0.5f, 0);
            choicePanelRect.pivot = new Vector2(0.5f, 0.5f);
            choicePanelRect.anchoredPosition = new Vector2(0, 150);

            Image choicePanelImage = choicePanel.AddComponent<Image>();
            choicePanelImage.color = new Color(0, 0, 0, 0.8f);

            CanvasGroup choiceCanvasGroup = choicePanel.AddComponent<CanvasGroup>();

            // Choice Container
            GameObject choiceContainer = new GameObject("ChoiceContainer");
            choiceContainer.transform.SetParent(choicePanel.transform, false);

            RectTransform containerRect = choiceContainer.AddComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(1000, 200);
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layoutGroup = choiceContainer.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = 10;
            layoutGroup.padding = new RectOffset(20, 20, 20, 20);
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;

            // Create Choice Button Prefab
            CreateChoiceButtonPrefab();
        }

        /// <summary>
        /// Tạo choice button prefab
        /// </summary>
        private void CreateChoiceButtonPrefab()
        {
            GameObject prefab = new GameObject("ChoiceButtonPrefab");

            // Setup RectTransform
            RectTransform prefabRect = prefab.AddComponent<RectTransform>();
            prefabRect.sizeDelta = new Vector2(900, 40);

            // Add Button
            Button button = prefab.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            colors.pressedColor = new Color(0.1f, 0.1f, 0.1f, 1f);
            button.colors = colors;

            // Add Image
            Image buttonImage = prefab.AddComponent<Image>();
            buttonImage.color = colors.normalColor;

            // Add Layout Element
            LayoutElement layoutElement = prefab.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 900;
            layoutElement.preferredHeight = 40;

            // Add Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(prefab.transform, false);
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.fontSize = 16;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Left;
            text.text = "Choice Text";

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.offsetMin = new Vector2(20, 0);
            textRect.offsetMax = new Vector2(-20, 0);

            // Save as prefab
            #if UNITY_EDITOR
            UnityEditor.PrefabUtility.SaveAsPrefabAsset(prefab, "Assets/_Project/Prefabs/ChoiceButtonPrefab.prefab");
            #endif

            Destroy(prefab);
        }

        /// <summary>
        /// Tạo background image
        /// </summary>
        private void CreateBackgroundImage(DialogueUI dialogueUI)
        {
            GameObject backgroundObj = new GameObject("BackgroundImage");
            backgroundObj.transform.SetParent(transform, false);

            RectTransform bgRect = backgroundObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            Image bgImage = backgroundObj.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.5f);

            // Move to back
            backgroundObj.transform.SetAsFirstSibling();
        }

        /// <summary>
        /// Helper method để tạo text object
        /// </summary>
        private GameObject CreateTextObject(string name, GameObject parent, Vector2 size, Vector2 position, Vector2 anchors)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent.transform, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchorMin = anchors;
            rect.anchorMax = anchors;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;

            TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null)
            {
                tmpText.font = defaultFont;
            }

            return textObj;
        }

        /// <summary>
        /// Gán references cho DialogueUI component
        /// </summary>
        private void AssignReferences(DialogueUI dialogueUI, GameObject dialoguePanel)
        {
            // Tìm các components
            dialogueUI.GetType().GetField("dialoguePanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(dialogueUI, dialoguePanel);

            CanvasGroup canvasGroup = dialoguePanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                dialogueUI.GetType().GetField("canvasGroup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(dialogueUI, canvasGroup);
            }

            // Tìm text elements
            TextMeshProUGUI[] texts = dialoguePanel.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (TextMeshProUGUI text in texts)
            {
                switch (text.gameObject.name)
                {
                    case "DialogueText":
                        dialogueUI.GetType().GetField("dialogueText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                            ?.SetValue(dialogueUI, text);
                        break;
                    case "NPCNameText":
                        dialogueUI.GetType().GetField("npcNameText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                            ?.SetValue(dialogueUI, text);
                        break;
                    case "ContinueText":
                        dialogueUI.GetType().GetField("continueText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                            ?.SetValue(dialogueUI, text);
                        break;
                }
            }

            // Tìm portrait
            Image portrait = dialoguePanel.transform.Find("NPCPortrait")?.GetComponent<Image>();
            if (portrait != null)
            {
                dialogueUI.GetType().GetField("npcPortrait", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(dialogueUI, portrait);
            }

            // Tìm continue button
            Button continueButton = dialoguePanel.transform.Find("ContinueButton")?.GetComponent<Button>();
            if (continueButton != null)
            {
                dialogueUI.GetType().GetField("continueButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(dialogueUI, continueButton);
            }

            // Tìm choice panel
            GameObject choicePanel = dialoguePanel.transform.Find("ChoicePanel")?.gameObject;
            if (choicePanel != null)
            {
                dialogueUI.GetType().GetField("choicePanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(dialogueUI, choicePanel);

                Transform choiceContainer = choicePanel.transform.Find("ChoiceContainer");
                if (choiceContainer != null)
                {
                    dialogueUI.GetType().GetField("choiceContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.SetValue(dialogueUI, choiceContainer);
                }
            }

            Debug.Log("✅ DialogueUI references assigned!");
        }

        /// <summary>
        /// Reset UI về trạng thái ban đầu
        /// </summary>
        public void ResetUI()
        {
            DialogueUI dialogueUI = GetComponent<DialogueUI>();
            if (dialogueUI != null)
            {
                // Reset canvas group
                CanvasGroup canvasGroup = dialogueUI.GetType().GetField("canvasGroup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(dialogueUI) as CanvasGroup;
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0;
                }

                // Reset dialogue panel
                GameObject dialoguePanel = dialogueUI.GetType().GetField("dialoguePanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(dialogueUI) as GameObject;
                if (dialoguePanel != null)
                {
                    dialoguePanel.SetActive(false);
                }

                // Reset choice panel
                GameObject choicePanel = dialogueUI.GetType().GetField("choicePanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(dialogueUI) as GameObject;
                if (choicePanel != null)
                {
                    choicePanel.SetActive(false);
                }
            }

            Debug.Log("🔄 Dialogue UI reset!");
        }

        /// <summary>
        /// Validate UI setup
        /// </summary>
        public void ValidateSetup()
        {
            DialogueUI dialogueUI = GetComponent<DialogueUI>();
            if (dialogueUI == null)
            {
                Debug.LogError("❌ DialogueUI component not found!");
                return;
            }

            System.Reflection.FieldInfo[] fields = dialogueUI.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            int missingReferences = 0;

            foreach (System.Reflection.FieldInfo field in fields)
            {
                if (field.FieldType == typeof(GameObject) || field.FieldType == typeof(TextMeshProUGUI) ||
                    field.FieldType == typeof(Image) || field.FieldType == typeof(Button) ||
                    field.FieldType == typeof(Transform) || field.FieldType == typeof(CanvasGroup))
                {
                    object value = field.GetValue(dialogueUI);
                    if (value == null)
                    {
                        Debug.LogWarning($"⚠️ Missing reference: {field.Name}");
                        missingReferences++;
                    }
                }
            }

            if (missingReferences == 0)
            {
                Debug.Log("✅ All DialogueUI references are properly assigned!");
            }
            else
            {
                Debug.LogWarning($"⚠️ {missingReferences} references are missing. Run SetupDialogueUI() to fix.");
            }
        }
    }
}
