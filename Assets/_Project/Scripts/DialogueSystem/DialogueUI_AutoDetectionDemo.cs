using UnityEngine;
using DialogueSystem;

namespace DialogueSystem
{
    /// <summary>
    /// Demo script để test tính năng Auto-Detection của DialogueUI
    /// Chỉ cần kéo Canvas tổng vào 1 field, script sẽ tự detect tất cả components
    /// </summary>
    public class DialogueUI_AutoDetectionDemo : MonoBehaviour
    {
        [Header("Auto-Detection Setup")]
        [SerializeField] private GameObject dialogueCanvas;
        [SerializeField] private bool autoDetectOnStart = true;

        [Header("Demo Dialogue")]
        [SerializeField] private Sprite demoNPCPortrait;
        [SerializeField] private AudioClip demoVoiceClip;

        private DialogueUI dialogueUI;
        private DialogueManager dialogueManager;

        private void Start()
        {
            if (autoDetectOnStart && dialogueCanvas != null)
            {
                SetupAutoDetection();
                RunDemo();
            }
        }

        /// <summary>
        /// Setup auto-detection cho DialogueUI
        /// </summary>
        [ContextMenu("Setup Auto-Detection")]
        public void SetupAutoDetection()
        {
            if (dialogueCanvas == null)
            {
                Debug.LogError("❌ Dialogue Canvas chưa được gán!");
                return;
            }

            Debug.Log("🚀 Setting up DialogueUI with Auto-Detection...");

            // Thêm DialogueUI script nếu chưa có
            dialogueUI = dialogueCanvas.GetComponent<DialogueUI>();
            if (dialogueUI == null)
            {
                dialogueUI = dialogueCanvas.AddComponent<DialogueUI>();
                Debug.Log("✅ Đã thêm DialogueUI script");
            }

            // Bật auto-detection
            dialogueUI.SetAutoDetectCanvas(dialogueCanvas);
            dialogueUI.AutoDetectComponents();
            dialogueUI.ValidateSetup();

            // Khởi tạo DialogueManager nếu chưa có
            dialogueManager = FindObjectOfType<DialogueManager>();
            if (dialogueManager == null)
            {
                GameObject managerObj = new GameObject("DialogueManager");
                dialogueManager = managerObj.AddComponent<DialogueManager>();
                Debug.Log("✅ Đã tạo DialogueManager");
            }

            Debug.Log("🎉 Auto-Detection setup hoàn thành!");
        }

        /// <summary>
        /// Chạy demo dialogue
        /// </summary>
        [ContextMenu("Run Demo")]
        public void RunDemo()
        {
            if (dialogueUI == null)
            {
                Debug.LogError("❌ DialogueUI chưa được setup!");
                return;
            }

            Debug.Log("🎬 Running Dialogue Demo...");

            // Tạo dialogue data mẫu
            DialogueData dialogue = CreateDemoDialogue();

            // Bắt đầu dialogue
            DialogueManager.Instance.StartDialogue(dialogue);

            Debug.Log("✅ Demo dialogue đã bắt đầu!");
        }

        /// <summary>
        /// Tạo dialogue data mẫu
        /// </summary>
        private DialogueData CreateDemoDialogue()
        {
            DialogueData dialogue = ScriptableObject.CreateInstance<DialogueData>();

            // Tạo các dòng dialogue mẫu
            DialogueLine line1 = new DialogueLine(
                "Xin chào! Đây là demo Auto-Detection của DialogueUI!",
                "NPC Guide",
                demoNPCPortrait,
                3f
            );

            DialogueLine line2 = new DialogueLine(
                "Hệ thống đã tự động tìm thấy tất cả UI components của bạn.",
                "NPC Guide",
                demoNPCPortrait,
                0f,
                demoVoiceClip
            );

            DialogueLine line3 = new DialogueLine(
                "Bạn chỉ cần kéo Canvas tổng vào 1 field duy nhất!",
                "NPC Guide",
                demoNPCPortrait,
                0f,
                demoVoiceClip
            );

            DialogueLine line4 = new DialogueLine(
                "Thử nghiệm tính năng choice system...",
                "Player",
                null,
                0f
            );

            // Thêm lines vào dialogue
            dialogue.DialogueLines.Add(line1);
            dialogue.DialogueLines.Add(line2);
            dialogue.DialogueLines.Add(line3);
            dialogue.DialogueLines.Add(line4);

            return dialogue;
        }

        /// <summary>
        /// Test choice system
        /// </summary>
        [ContextMenu("Test Choice System")]
        public void TestChoiceSystem()
        {
            if (dialogueUI == null)
            {
                Debug.LogError("❌ DialogueUI chưa được setup!");
                return;
            }

            Debug.Log("🔀 Testing Choice System...");

            dialogueUI.ShowDialoguePanel();
            dialogueUI.UpdateNPCInfo("Choice Test", demoNPCPortrait);
            dialogueUI.UpdateDialogueText("Bạn muốn làm gì tiếp theo?");

            // Tạo choices
            string[] choices = {
                "Choice 1: Tiếp tục demo",
                "Choice 2: Test typing effect",
                "Choice 3: Kết thúc demo"
            };

            dialogueUI.ShowChoices(choices, (choiceIndex) => {
                Debug.Log($"Selected choice: {choices[choiceIndex]}");

                switch (choiceIndex)
                {
                    case 0:
                        RunDemo();
                        break;
                    case 1:
                        TestTypingEffect();
                        break;
                    case 2:
                        dialogueUI.HideDialoguePanel();
                        break;
                }
            });
        }

        /// <summary>
        /// Test typing effect
        /// </summary>
        [ContextMenu("Test Typing Effect")]
        public void TestTypingEffect()
        {
            if (dialogueUI == null)
            {
                Debug.LogError("❌ DialogueUI chưa được setup!");
                return;
            }

            Debug.Log("⌨️ Testing Typing Effect...");

            dialogueUI.ShowDialoguePanel();
            dialogueUI.UpdateNPCInfo("Typing Test", demoNPCPortrait);
            dialogueUI.UpdateDialogueText("Đây là hiệu ứng gõ chữ từng ký tự một cách tự động!");
        }

        /// <summary>
        /// Validate setup
        /// </summary>
        [ContextMenu("Validate Setup")]
        public void ValidateSetup()
        {
            if (dialogueUI == null)
            {
                Debug.LogError("❌ DialogueUI chưa được setup!");
                return;
            }

            dialogueUI.ValidateSetup();
        }

        /// <summary>
        /// Reset và setup lại
        /// </summary>
        [ContextMenu("Reset & Setup Again")]
        public void ResetAndSetup()
        {
            // Destroy existing components
            if (dialogueUI != null)
            {
                Destroy(dialogueUI);
                dialogueUI = null;
            }

            if (dialogueManager != null)
            {
                Destroy(dialogueManager.gameObject);
                dialogueManager = null;
            }

            // Setup lại
            SetupAutoDetection();
        }

        #region Public Methods for Inspector Buttons

        /// <summary>
        /// Set dialogue canvas (for Inspector)
        /// </summary>
        public void SetDialogueCanvas(GameObject canvas)
        {
            dialogueCanvas = canvas;
        }

        /// <summary>
        /// Get dialogue canvas (for Inspector)
        /// </summary>
        public GameObject GetDialogueCanvas()
        {
            return dialogueCanvas;
        }

        #endregion

        #region Debug Methods

        /// <summary>
        /// Log setup info
        /// </summary>
        [ContextMenu("Log Setup Info")]
        private void LogSetupInfo()
        {
            Debug.Log("📋 DialogueUI Auto-Detection Demo Setup Info:");
            Debug.Log($"Dialogue Canvas: {dialogueCanvas?.name ?? "Not set"}");
            Debug.Log($"Dialogue UI: {dialogueUI?.name ?? "Not created"}");
            Debug.Log($"Dialogue Manager: {dialogueManager?.name ?? "Not created"}");
            Debug.Log($"Auto Detect on Start: {autoDetectOnStart}");
        }

        /// <summary>
        /// Show help
        /// </summary>
        [ContextMenu("Show Help")]
        private void ShowHelp()
        {
            Debug.Log("🎮 DialogueUI Auto-Detection Demo Help:");
            Debug.Log("1. Kéo Canvas tổng vào field 'Dialogue Canvas'");
            Debug.Log("2. Click 'Setup Auto-Detection' để tự động setup");
            Debug.Log("3. Click 'Run Demo' để test dialogue");
            Debug.Log("4. Click 'Validate Setup' để kiểm tra setup");
            Debug.Log("5. Sử dụng các test buttons khác để test tính năng");
        }

        #endregion
    }
}
