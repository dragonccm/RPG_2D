using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace DialogueSystem
{
    /// <summary>
    /// Component xử lý giao diện người dùng cho hệ thống hội thoại
    /// Quản lý việc hiển thị panel, text, và các element UI
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        #region UI References
        [Header("Auto Detection (Khuyến nghị)")]
        [SerializeField] private bool autoDetectComponents = true;
        [SerializeField] private GameObject dialogueCanvas;
        [SerializeField] private bool showAdvancedSetup = false;

        [Header("Main Panel")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Text Elements")]
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private TextMeshProUGUI npcNameText;
        [SerializeField] private Image npcPortrait;
        [SerializeField] private Image backgroundImage;

        [Header("Control Elements")]
        [SerializeField] private Button continueButton;
        [SerializeField] private TextMeshProUGUI continueText;
        [SerializeField] private GameObject choicePanel;
        [SerializeField] private Transform choiceContainer;
        [SerializeField] private GameObject choiceButtonPrefab;

        [Header("Animation")]
        [SerializeField] private Animator panelAnimator;
        [SerializeField] private string showTrigger = "Show";
        [SerializeField] private string hideTrigger = "Hide";

        [Header("Settings")]
        [SerializeField] private float fadeDuration = 0.3f;
        [SerializeField] private Color backgroundTint = new Color(0, 0, 0, 0.7f);
        #endregion

        #region Private Fields
        private List<Button> choiceButtons = new List<Button>();
        private Coroutine fadeCoroutine;
        private bool isVisible = false;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            InitializeUI();
        }

        private void Start()
        {
            // Subscribe to dialogue manager events
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnDialogueStarted += OnDialogueStarted;
                DialogueManager.Instance.OnDialogueEnded += OnDialogueEnded;
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnDialogueStarted -= OnDialogueStarted;
                DialogueManager.Instance.OnDialogueEnded -= OnDialogueEnded;
            }
        }
        #endregion

        #region Initialization
        private void InitializeUI()
        {
            // Auto detect components if enabled
            if (autoDetectComponents)
            {
                AutoDetectComponents();
            }

            // Setup canvas group
            if (canvasGroup == null && dialoguePanel != null)
            {
                canvasGroup = dialoguePanel.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = dialoguePanel.AddComponent<CanvasGroup>();
                }
            }

            // Setup continue button
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked);
            }

            // Hide initially
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }

            if (choicePanel != null)
            {
                choicePanel.SetActive(false);
            }

            Debug.Log("🎨 DialogueUI initialized successfully!");
        }
        #endregion

        #region Event Handlers
        private void OnDialogueStarted()
        {
            ShowDialoguePanel();
        }

        private void OnDialogueEnded()
        {
            HideDialoguePanel();
        }

        private void OnContinueClicked()
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.DisplayNextLine();
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Hiển thị panel hội thoại
        /// </summary>
        public void ShowDialoguePanel()
        {
            if (dialoguePanel == null)
                return;

            isVisible = true;
            dialoguePanel.SetActive(true);

            // Trigger animation
            if (panelAnimator != null)
            {
                panelAnimator.SetTrigger(showTrigger);
            }
            else
            {
                // Fallback fade in
                StartFade(1f);
            }

            UpdateContinueIndicator(true);
        }

        /// <summary>
        /// Ẩn panel hội thoại
        /// </summary>
        public void HideDialoguePanel()
        {
            if (dialoguePanel == null)
                return;

            isVisible = false;

            // Trigger animation
            if (panelAnimator != null)
            {
                panelAnimator.SetTrigger(hideTrigger);
                // Delay deactivation to allow animation to complete
                StartCoroutine(DelayedDeactivation());
            }
            else
            {
                // Fallback fade out
                StartFade(0f, () => dialoguePanel.SetActive(false));
            }

            UpdateContinueIndicator(false);
        }

        /// <summary>
        /// Cập nhật text hội thoại
        /// </summary>
        public void UpdateDialogueText(string text)
        {
            if (dialogueText != null)
            {
                dialogueText.text = text;
            }
        }

        /// <summary>
        /// Cập nhật thông tin NPC
        /// </summary>
        public void UpdateNPCInfo(string name, Sprite portrait)
        {
            if (npcNameText != null)
            {
                npcNameText.text = name;
            }

            if (npcPortrait != null && portrait != null)
            {
                npcPortrait.sprite = portrait;
                npcPortrait.gameObject.SetActive(true);
            }
            else if (npcPortrait != null)
            {
                npcPortrait.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Hiển thị lựa chọn
        /// </summary>
        public void ShowChoices(string[] choices, System.Action<int> onChoiceSelected)
        {
            if (choicePanel == null || choiceContainer == null)
            {
                Debug.LogError("Choice UI not properly set up");
                return;
            }

            // Clear existing buttons
            ClearChoiceButtons();

            choicePanel.SetActive(true);

            // Create choice buttons
            for (int i = 0; i < choices.Length; i++)
            {
                GameObject buttonObj = CreateChoiceButton(choices[i], i);
                Button button = buttonObj.GetComponent<Button>();

                int choiceIndex = i;
                button.onClick.AddListener(() => {
                    onChoiceSelected(choiceIndex);
                    choicePanel.SetActive(false);
                });

                choiceButtons.Add(button);
            }
        }

        /// <summary>
        /// Ẩn lựa chọn
        /// </summary>
        public void HideChoices()
        {
            if (choicePanel != null)
            {
                choicePanel.SetActive(false);
            }

            ClearChoiceButtons();
        }

        /// <summary>
        /// Cập nhật trạng thái typing
        /// </summary>
        public void SetTypingState(bool isTyping)
        {
            UpdateContinueIndicator(!isTyping);
        }

        /// <summary>
        /// Kiểm tra panel có đang hiển thị không
        /// </summary>
        public bool IsVisible()
        {
            return isVisible;
        }
        #endregion

        #region Private Methods
        private void UpdateContinueIndicator(bool show)
        {
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(show);
            }

            if (continueText != null)
            {
                continueText.gameObject.SetActive(show);
                if (show)
                {
                    continueText.text = "Nhấn SPACE hoặc ENTER để tiếp tục...";
                }
            }
        }

        private GameObject CreateChoiceButton(string choiceText, int index)
        {
            GameObject buttonObj;

            if (choiceButtonPrefab != null)
            {
                buttonObj = Instantiate(choiceButtonPrefab, choiceContainer);
            }
            else
            {
                // Create basic button
                buttonObj = new GameObject($"ChoiceButton_{index}");
                buttonObj.transform.SetParent(choiceContainer, false);

                // Add components
                Button button = buttonObj.AddComponent<Button>();
                Image image = buttonObj.AddComponent<Image>();
                image.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

                // Add text
                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(buttonObj.transform, false);
                TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
                text.text = choiceText;
                text.fontSize = 18;
                text.alignment = TextAlignmentOptions.Center;
                text.color = Color.white;

                // Setup rect transforms
                RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
                buttonRect.sizeDelta = new Vector2(300, 40);

                RectTransform textRect = textObj.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
            }

            // Set button text
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = choiceText;
            }

            return buttonObj;
        }

        private void ClearChoiceButtons()
        {
            foreach (Button button in choiceButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }
            choiceButtons.Clear();
        }

        private void StartFade(float targetAlpha, System.Action onComplete = null)
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }

            fadeCoroutine = StartCoroutine(FadeTo(targetAlpha, onComplete));
        }

        private IEnumerator FadeTo(float targetAlpha, System.Action onComplete = null)
        {
            if (canvasGroup == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
            onComplete?.Invoke();
        }

        private IEnumerator DelayedDeactivation()
        {
            // Wait for animation to complete
            yield return new WaitForSeconds(0.5f);

            if (!isVisible && dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// Lấy reference đến dialogue panel
        /// </summary>
        public GameObject GetDialoguePanel()
        {
            return dialoguePanel;
        }

        /// <summary>
        /// Lấy reference đến dialogue text
        /// </summary>
        public TextMeshProUGUI GetDialogueText()
        {
            return dialogueText;
        }

        /// <summary>
        /// Lấy reference đến NPC name text
        /// </summary>
        public TextMeshProUGUI GetNPCNameText()
        {
            return npcNameText;
        }

        /// <summary>
        /// Lấy reference đến NPC portrait
        /// </summary>
        public Image GetNPCPortrait()
        {
            return npcPortrait;
        }
        #endregion

        #region Auto Detection Methods
        /// <summary>
        /// Tự động detect và gán tất cả UI components
        /// </summary>
        [ContextMenu("Auto Detect Components")]
        public void AutoDetectComponents()
        {
            Debug.Log("🔍 Auto-detecting Dialogue UI components...");

            // Determine search root
            GameObject searchRoot = dialogueCanvas != null ? dialogueCanvas : gameObject;

            // Find main dialogue panel
            dialoguePanel = FindDialoguePanel(searchRoot);
            if (dialoguePanel != null)
            {
                canvasGroup = dialoguePanel.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = dialoguePanel.AddComponent<CanvasGroup>();
                }
            }

            // Find text elements
            dialogueText = FindTextComponent(searchRoot, "DialogueText", "Dialogue", "Text");
            npcNameText = FindTextComponent(searchRoot, "NPCNameText", "NPCName", "NameText", "SpeakerName");
            continueText = FindTextComponent(searchRoot, "ContinueText", "Continue", "NextText");

            // Find image elements
            npcPortrait = FindImageComponent(searchRoot, "NPCPortrait", "Portrait", "Avatar", "NPCImage");
            backgroundImage = FindImageComponent(searchRoot, "BackgroundImage", "Background", "BG");

            // Find control elements
            continueButton = FindButtonComponent(searchRoot, "ContinueButton", "Continue", "NextButton");
            choicePanel = FindPanelComponent(searchRoot, "ChoicePanel", "Choices", "Choice");
            if (choicePanel != null)
            {
                choiceContainer = FindContainerComponent(choicePanel, "ChoiceContainer", "Container", "Choices");
            }

            // Find choice button prefab
            choiceButtonPrefab = FindChoiceButtonPrefab(searchRoot);

            // Find animator
            panelAnimator = FindAnimatorComponent(searchRoot, "DialoguePanel", "Panel");

            Debug.Log("✅ Auto-detection completed!");
            ValidateSetup();
        }

        /// <summary>
        /// Tìm dialogue panel chính
        /// </summary>
        private GameObject FindDialoguePanel(GameObject root)
        {
            // Priority order for finding dialogue panel
            string[] possibleNames = {
                "DialoguePanel", "Dialogue", "Panel", "MainPanel",
                "DialogPanel", "ChatPanel", "ConversationPanel"
            };

            foreach (string name in possibleNames)
            {
                GameObject obj = FindChildByName(root, name);
                if (obj != null)
                {
                    Debug.Log($"📋 Found dialogue panel: {obj.name}");
                    return obj;
                }
            }

            // Fallback: find any object with "dialogue" or "panel" in name (case insensitive)
            return FindChildByPattern(root, "(?i)(dialogue|panel|chat|conversation)");
        }

        /// <summary>
        /// Tìm TextMeshPro component theo tên
        /// </summary>
        private TextMeshProUGUI FindTextComponent(GameObject root, params string[] possibleNames)
        {
            foreach (string name in possibleNames)
            {
                GameObject obj = FindChildByName(root, name);
                if (obj != null)
                {
                    TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
                    if (text != null)
                    {
                        Debug.Log($"📝 Found text component: {obj.name}");
                        return text;
                    }
                }
            }

            // Fallback: find any TextMeshProUGUI in children
            TextMeshProUGUI[] texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            if (texts.Length > 0)
            {
                Debug.Log($"📝 Found fallback text component: {texts[0].name}");
                return texts[0];
            }

            return null;
        }

        /// <summary>
        /// Tìm Image component theo tên
        /// </summary>
        private Image FindImageComponent(GameObject root, params string[] possibleNames)
        {
            foreach (string name in possibleNames)
            {
                GameObject obj = FindChildByName(root, name);
                if (obj != null)
                {
                    Image image = obj.GetComponent<Image>();
                    if (image != null)
                    {
                        Debug.Log($"🖼️ Found image component: {obj.name}");
                        return image;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Tìm Button component theo tên
        /// </summary>
        private Button FindButtonComponent(GameObject root, params string[] possibleNames)
        {
            foreach (string name in possibleNames)
            {
                GameObject obj = FindChildByName(root, name);
                if (obj != null)
                {
                    Button button = obj.GetComponent<Button>();
                    if (button != null)
                    {
                        Debug.Log($"🔘 Found button component: {obj.name}");
                        return button;
                    }
                }
            }

            // Fallback: find any Button in children
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            if (buttons.Length > 0)
            {
                Debug.Log($"🔘 Found fallback button component: {buttons[0].name}");
                return buttons[0];
            }

            return null;
        }

        /// <summary>
        /// Tìm panel component theo tên
        /// </summary>
        private GameObject FindPanelComponent(GameObject root, params string[] possibleNames)
        {
            foreach (string name in possibleNames)
            {
                GameObject obj = FindChildByName(root, name);
                if (obj != null)
                {
                    Debug.Log($"📋 Found panel component: {obj.name}");
                    return obj;
                }
            }

            return null;
        }

        /// <summary>
        /// Tìm container component trong panel
        /// </summary>
        private Transform FindContainerComponent(GameObject panel, params string[] possibleNames)
        {
            if (panel == null) return null;

            foreach (string name in possibleNames)
            {
                GameObject obj = FindChildByName(panel, name);
                if (obj != null)
                {
                    Debug.Log($"📂 Found container component: {obj.name}");
                    return obj.transform;
                }
            }

            // Fallback: return panel's transform
            Debug.Log($"📂 Using panel as container: {panel.name}");
            return panel.transform;
        }

        /// <summary>
        /// Tìm choice button prefab
        /// </summary>
        private GameObject FindChoiceButtonPrefab(GameObject root)
        {
            // Look for existing choice button
            GameObject existingButton = FindChildByName(root, "ChoiceButton", "Choice", "Button");
            if (existingButton != null)
            {
                Debug.Log($"🔘 Found choice button prefab: {existingButton.name}");
                return existingButton;
            }

            // Create default choice button if none found
            return CreateDefaultChoiceButton(root);
        }

        /// <summary>
        /// Tìm Animator component
        /// </summary>
        private Animator FindAnimatorComponent(GameObject root, params string[] possibleNames)
        {
            foreach (string name in possibleNames)
            {
                GameObject obj = FindChildByName(root, name);
                if (obj != null)
                {
                    Animator animator = obj.GetComponent<Animator>();
                    if (animator != null)
                    {
                        Debug.Log($"🎬 Found animator component: {obj.name}");
                        return animator;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Tìm child object theo tên chính xác
        /// </summary>
        private GameObject FindChildByName(GameObject root, params string[] names)
        {
            foreach (string name in names)
            {
                Transform child = root.transform.Find(name);
                if (child != null)
                {
                    return child.gameObject;
                }
            }
            return null;
        }

        /// <summary>
        /// Tìm child object theo pattern (sử dụng regex)
        /// </summary>
        private GameObject FindChildByPattern(GameObject root, string pattern)
        {
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(pattern);

            foreach (Transform child in root.transform)
            {
                if (regex.IsMatch(child.name))
                {
                    return child.gameObject;
                }

                // Search recursively
                GameObject recursiveResult = FindChildByPattern(child.gameObject, pattern);
                if (recursiveResult != null)
                {
                    return recursiveResult;
                }
            }

            return null;
        }

        /// <summary>
        /// Tạo default choice button nếu không tìm thấy
        /// </summary>
        private GameObject CreateDefaultChoiceButton(GameObject root)
        {
            GameObject buttonObj = new GameObject("ChoiceButton");
            buttonObj.transform.SetParent(root.transform, false);

            // Add required components
            Image image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            Button button = buttonObj.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 0.9f);
            colors.pressedColor = new Color(0.1f, 0.1f, 0.1f, 0.7f);
            button.colors = colors;

            // Add text child
            GameObject textObj = new GameObject("ChoiceText");
            textObj.transform.SetParent(buttonObj.transform, false);

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "Choice";
            text.fontSize = 24;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;

            // Setup rect transforms
            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(300, 50);

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(280, 40);

            Debug.Log("🔘 Created default choice button prefab");
            return buttonObj;
        }

        /// <summary>
        /// Validate setup và log kết quả
        /// </summary>
        [ContextMenu("Validate Setup")]
        public void ValidateSetup()
        {
            Debug.Log("🔍 Validating Dialogue UI setup...");

            int missingCount = 0;
            int foundCount = 0;

            // Check required components
            if (dialoguePanel != null) { foundCount++; Debug.Log("✅ Dialogue Panel: Found"); }
            else { missingCount++; Debug.Log("❌ Dialogue Panel: Missing"); }

            if (dialogueText != null) { foundCount++; Debug.Log("✅ Dialogue Text: Found"); }
            else { missingCount++; Debug.Log("❌ Dialogue Text: Missing"); }

            // Check optional components
            if (npcNameText != null) Debug.Log("✅ NPC Name Text: Found");
            else Debug.Log("⚠️ NPC Name Text: Optional (missing)");

            if (npcPortrait != null) Debug.Log("✅ NPC Portrait: Found");
            else Debug.Log("⚠️ NPC Portrait: Optional (missing)");

            if (continueButton != null) Debug.Log("✅ Continue Button: Found");
            else Debug.Log("⚠️ Continue Button: Optional (missing)");

            if (choicePanel != null) Debug.Log("✅ Choice Panel: Found");
            else Debug.Log("⚠️ Choice Panel: Optional (missing)");

            if (choiceContainer != null) Debug.Log("✅ Choice Container: Found");
            else Debug.Log("⚠️ Choice Container: Optional (missing)");

            if (choiceButtonPrefab != null) Debug.Log("✅ Choice Button Prefab: Found");
            else Debug.Log("⚠️ Choice Button Prefab: Optional (missing)");

            // Summary
            if (missingCount == 0)
            {
                Debug.Log($"🎉 Setup validation PASSED! Found {foundCount} components.");
            }
            else
            {
                Debug.Log($"⚠️ Setup validation WARNING: {missingCount} required components missing, {foundCount} found.");
            }
        }

        /// <summary>
        /// Set dialogue canvas for auto-detection
        /// </summary>
        public void SetAutoDetectCanvas(GameObject canvas)
        {
            dialogueCanvas = canvas;
            autoDetectComponents = true;
        }

        /// <summary>
        /// Get dialogue canvas
        /// </summary>
        public GameObject GetDialogueCanvas()
        {
            return dialogueCanvas;
        }

        /// <summary>
        /// Enable/disable auto-detection
        /// </summary>
        public void SetAutoDetectComponents(bool enabled)
        {
            autoDetectComponents = enabled;
        }

        /// <summary>
        /// Check if auto-detection is enabled
        /// </summary>
        public bool IsAutoDetectEnabled()
        {
            return autoDetectComponents;
        }
        #endregion
    }
}
