using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace DialogueSystem
{
    /// <summary>
    /// Script test để kiểm tra hệ thống hội thoại
    /// Tạo dữ liệu hội thoại mẫu và test các chức năng
    /// </summary>
    public class DialogueTest : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool runOnStart = false;
        [SerializeField] private KeyCode testKey = KeyCode.T;

        [Header("Test Dialogue Data")]
        [SerializeField] private DialogueData testDialogueData;

        private DialogueManager dialogueManager;

        #region Unity Methods
        private void Awake()
        {
            dialogueManager = DialogueManager.Instance;
        }

        private void Start()
        {
            if (runOnStart)
            {
                StartCoroutine(RunDialogueTest());
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(testKey))
            {
                StartTestDialogue();
            }
        }
        #endregion

        #region Public Test Methods
        /// <summary>
        /// Bắt đầu test hội thoại
        /// </summary>
        public void StartTestDialogue()
        {
            if (dialogueManager == null)
            {
                Debug.LogError("DialogueManager not found!");
                return;
            }

            if (testDialogueData != null)
            {
                dialogueManager.StartDialogue(testDialogueData, "Test NPC", null);
            }
            else
            {
                // Tạo dữ liệu hội thoại mẫu
                CreateSampleDialogue();
            }
        }

        /// <summary>
        /// Test các chức năng khác nhau của hệ thống hội thoại
        /// </summary>
        public void RunFullTest()
        {
            StartCoroutine(RunFullTestCoroutine());
        }
        #endregion

        #region Private Test Methods
        private IEnumerator RunDialogueTest()
        {
            Debug.Log("🎭 Starting Dialogue System Test...");

            yield return new WaitForSeconds(1f);

            // Test 1: Basic dialogue
            Debug.Log("Test 1: Basic dialogue");
            CreateSampleDialogue();
            yield return new WaitForSeconds(2f);

            // Test 2: Choice dialogue
            Debug.Log("Test 2: Choice dialogue");
            CreateChoiceDialogue();
            yield return new WaitForSeconds(2f);

            // Test 3: Long dialogue
            Debug.Log("Test 3: Long dialogue");
            CreateLongDialogue();
            yield return new WaitForSeconds(2f);

            Debug.Log("✅ Dialogue System Test Complete!");
        }

        private IEnumerator RunFullTestCoroutine()
        {
            Debug.Log("🎭 Running Full Dialogue System Test...");

            // Test dialogue manager functions
            if (dialogueManager != null)
            {
                // Test 1: Check if manager is initialized
                Debug.Log($"Test 1: DialogueManager initialized - {dialogueManager != null}");

                // Test 2: Create and start dialogue
                DialogueData sampleData = CreateSampleDialogue();
                dialogueManager.StartDialogue(sampleData, "Test NPC", null);
                yield return new WaitForSeconds(3f);

                // Test 3: Test choice dialogue
                DialogueData choiceData = CreateChoiceDialogue();
                dialogueManager.StartDialogue(choiceData, "Choice NPC", null);
                yield return new WaitForSeconds(5f);

                // Test 4: Test dialogue interruption
                dialogueManager.EndDialogue();
                yield return new WaitForSeconds(1f);

                Debug.Log("✅ Full Dialogue System Test Complete!");
            }
            else
            {
                Debug.LogError("❌ DialogueManager not found!");
            }
        }

        private DialogueData CreateSampleDialogue()
        {
            DialogueData dialogueData = ScriptableObject.CreateInstance<DialogueData>();
            dialogueData.name = "Sample Dialogue";
            dialogueData.AddLine("Xin chào! Tôi là NPC test.", "Test NPC");
            dialogueData.AddLine("Hôm nay thời tiết thật đẹp phải không?", "Test NPC");
            dialogueData.AddLine("Tôi hy vọng bạn đang có một ngày tốt lành!", "Test NPC");

            // Start dialogue
            if (dialogueManager != null)
            {
                dialogueManager.StartDialogue(dialogueData, "Test NPC", null);
            }

            return dialogueData;
        }

        private DialogueData CreateChoiceDialogue()
        {
            DialogueData dialogueData = ScriptableObject.CreateInstance<DialogueData>();
            dialogueData.name = "Choice Dialogue";

            // First line with choices
            DialogueLine choiceLine = new DialogueLine(
                "Bạn muốn biết gì về tôi?",
                "Test NPC"
            );
            choiceLine.SetChoices(
                new string[] { "Tên của bạn là gì?", "Bạn làm nghề gì?", "Tạm biệt!" },
                new int[] { 1, 2, 3 }
            );
            dialogueData.AddLine(choiceLine);

            // Response lines
            dialogueData.AddLine("Tên tôi là Test NPC! Rất vui được gặp bạn.", "Test NPC");
            dialogueData.AddLine("Tôi là một NPC test trong game này.", "Test NPC");
            dialogueData.AddLine("Tạm biệt! Hẹn gặp lại bạn sau.", "Test NPC");

            // Start dialogue
            if (dialogueManager != null)
            {
                dialogueManager.StartDialogue(dialogueData, "Test NPC", null);
            }

            return dialogueData;
        }

        private DialogueData CreateLongDialogue()
        {
            DialogueData dialogueData = ScriptableObject.CreateInstance<DialogueData>();
            dialogueData.name = "Long Dialogue";

            dialogueData.AddLine("Đây là một đoạn hội thoại rất dài để test khả năng hiển thị text của hệ thống.", "Long NPC");
            dialogueData.AddLine("Hệ thống hội thoại này được thiết kế để xử lý các đoạn text có độ dài khác nhau, từ ngắn đến rất dài.", "Long NPC");
            dialogueData.AddLine("Với tính năng typing effect, text sẽ xuất hiện dần dần, tạo cảm giác tự nhiên và hấp dẫn hơn cho người chơi.", "Long NPC");
            dialogueData.AddLine("Ngoài ra, hệ thống còn hỗ trợ lựa chọn, giúp tạo ra các nhánh hội thoại phức tạp và đa dạng.", "Long NPC");
            dialogueData.AddLine("Tôi hy vọng bạn thích hệ thống hội thoại này!", "Long NPC");

            // Start dialogue
            if (dialogueManager != null)
            {
                dialogueManager.StartDialogue(dialogueData, "Long NPC", null);
            }

            return dialogueData;
        }

        private DialogueData CreateComplexDialogue()
        {
            DialogueData dialogueData = ScriptableObject.CreateInstance<DialogueData>();
            dialogueData.name = "Complex Dialogue";

            // Introduction
            dialogueData.AddLine("Chào mừng bạn đến với thế giới của chúng tôi!", "Guide NPC");
            dialogueData.AddLine("Tôi sẽ hướng dẫn bạn về các tính năng của game.", "Guide NPC");

            // First choice
            DialogueLine choice1 = new DialogueLine(
                "Bạn muốn tìm hiểu điều gì trước?",
                "Guide NPC"
            );
            choice1.SetChoices(
                new string[] { "Hệ thống chiến đấu", "Hệ thống nhiệm vụ", "Hệ thống kỹ năng", "Kết thúc hướng dẫn" },
                new int[] { 2, 3, 4, 5 }
            );
            dialogueData.AddLine(choice1);

            // Combat explanation
            dialogueData.AddLine("Hệ thống chiến đấu sử dụng lượt, bạn có thể tấn công, phòng thủ hoặc sử dụng kỹ năng.", "Guide NPC");

            // Quest explanation
            dialogueData.AddLine("Nhiệm vụ được chia thành nhiều loại: chính tuyến, nhánh, và nhiệm vụ hàng ngày.", "Guide NPC");

            // Skill explanation
            dialogueData.AddLine("Kỹ năng có thể nâng cấp bằng điểm kinh nghiệm hoặc vật phẩm đặc biệt.", "Guide NPC");

            // End
            dialogueData.AddLine("Cảm ơn bạn đã lắng nghe hướng dẫn. Chúc bạn chơi game vui vẻ!", "Guide NPC");

            return dialogueData;
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// Tạo tất cả các loại dialogue mẫu
        /// </summary>
        public void CreateAllSampleDialogues()
        {
            CreateSampleDialogue();
            CreateChoiceDialogue();
            CreateLongDialogue();
            CreateComplexDialogue();

            Debug.Log("✅ All sample dialogues created!");
        }

        /// <summary>
        /// Test performance với nhiều dialogue
        /// </summary>
        public void TestPerformance()
        {
            StartCoroutine(TestPerformanceCoroutine());
        }

        private IEnumerator TestPerformanceCoroutine()
        {
            Debug.Log("⚡ Starting Performance Test...");

            float startTime = Time.time;

            // Create 10 dialogues
            for (int i = 0; i < 10; i++)
            {
                DialogueData data = CreateSampleDialogue();
                Destroy(data); // Clean up
                yield return null;
            }

            float endTime = Time.time;
            Debug.Log($"⚡ Performance Test Complete! Time: {endTime - startTime}s");
        }

        /// <summary>
        /// Log thông tin hệ thống hội thoại
        /// </summary>
        public void LogSystemInfo()
        {
            Debug.Log("📊 Dialogue System Information:");
            Debug.Log($"- DialogueManager: {(dialogueManager != null ? "Found" : "Not Found")}");
            Debug.Log($"- Test Dialogue Data: {(testDialogueData != null ? "Set" : "Not Set")}");
            Debug.Log($"- Run On Start: {runOnStart}");
            Debug.Log($"- Test Key: {testKey}");
        }
        #endregion
    }
}
