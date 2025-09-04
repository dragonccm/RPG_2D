using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace DialogueSystem
{
    /// <summary>
    /// Demo script để test Dialogue System
    /// Tạo sample dialogue và test các tính năng
    /// </summary>
    public class DialogueDemo : MonoBehaviour
    {
        [Header("Demo Settings")]
        [SerializeField] private DialogueUI dialogueUI;
        [SerializeField] private DialogueManager dialogueManager;
        [SerializeField] private bool startDemoOnAwake = false;

        [Header("Sample Data")]
        [SerializeField] private Sprite npcPortrait;
        [SerializeField] private AudioClip voiceClip;

        private void Awake()
        {
            if (startDemoOnAwake)
            {
                StartCoroutine(RunDemo());
            }
        }

        private void Update()
        {
            // Test keys
            if (Input.GetKeyDown(KeyCode.D))
            {
                StartDemo();
            }

            if (Input.GetKeyDown(KeyCode.T))
            {
                TestTypingEffect();
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                TestChoices();
            }
        }

        /// <summary>
        /// Bắt đầu demo dialogue
        /// </summary>
        public void StartDemo()
        {
            StartCoroutine(RunDemo());
        }

        /// <summary>
        /// Chạy demo hoàn chỉnh
        /// </summary>
        private IEnumerator RunDemo()
        {
            Debug.Log("🎬 Starting Dialogue Demo...");

            // Khởi tạo managers nếu chưa có
            if (dialogueManager == null)
            {
                dialogueManager = FindObjectOfType<DialogueManager>();
                if (dialogueManager == null)
                {
                    GameObject managerObj = new GameObject("DialogueManager");
                    dialogueManager = managerObj.AddComponent<DialogueManager>();
                }
            }

            if (dialogueUI == null)
            {
                dialogueUI = FindObjectOfType<DialogueUI>();
            }

            // Tạo sample dialogue data
            DialogueData sampleDialogue = CreateSampleDialogue();

            // Bắt đầu dialogue
            dialogueManager.StartDialogue(sampleDialogue);

            yield return new WaitForSeconds(2f);
            Debug.Log("✅ Demo completed!");
        }

        /// <summary>
        /// Tạo sample dialogue data
        /// </summary>
        private DialogueData CreateSampleDialogue()
        {
            DialogueData dialogue = ScriptableObject.CreateInstance<DialogueData>();

            // Tạo dialogue lines
            DialogueLine line1 = new DialogueLine("Welcome to the Dialogue System Demo!", "Narrator", null, 3f);
            DialogueLine line2 = new DialogueLine("Hello there! This is a demonstration of our dialogue system.", "NPC", npcPortrait, 0f, voiceClip);
            DialogueLine line3 = new DialogueLine("You can see the typing effect in action...", "NPC", npcPortrait, 0f, voiceClip);
            DialogueLine line4 = new DialogueLine("This looks great! Can you show me the choice system?", "Player", null, 0f);

            // Add lines to dialogue
            dialogue.DialogueLines.Add(line1);
            dialogue.DialogueLines.Add(line2);
            dialogue.DialogueLines.Add(line3);
            dialogue.DialogueLines.Add(line4);

            return dialogue;
        }

        /// <summary>
        /// Test typing effect
        /// </summary>
        private void TestTypingEffect()
        {
            if (dialogueUI == null) return;

            dialogueUI.ShowDialoguePanel();
            dialogueUI.UpdateNPCInfo("Typing Test", null);
            dialogueUI.UpdateDialogueText("This is a test of the typing effect! You can see each character appearing one by one...");
        }

        /// <summary>
        /// Test choice system
        /// </summary>
        private void TestChoices()
        {
            if (dialogueUI == null) return;

            dialogueUI.ShowDialoguePanel();
            dialogueUI.UpdateNPCInfo("Choice Test", null);

            List<string> testChoices = new List<string>
            {
                "Choice 1: Attack the dragon!",
                "Choice 2: Try to negotiate",
                "Choice 3: Run away!",
                "Choice 4: Use magic spell"
            };

            dialogueUI.ShowChoices(testChoices.ToArray(), (choiceIndex) =>
            {
                Debug.Log($"Selected choice {choiceIndex + 1}: {testChoices[choiceIndex]}");
                dialogueUI.HideDialoguePanel();
            });
        }

        /// <summary>
        /// Test performance với nhiều text
        /// </summary>
        public void TestPerformance()
        {
            StartCoroutine(TestPerformanceCoroutine());
        }

        private IEnumerator TestPerformanceCoroutine()
        {
            string longText = "This is a very long piece of text designed to test the performance of our dialogue system. " +
                            "It contains multiple sentences and should help us see how well the typing effect handles " +
                            "longer pieces of dialogue content. The system should be able to handle text of various " +
                            "lengths without causing performance issues or memory leaks.";

            dialogueUI.ShowDialoguePanel();
            dialogueUI.UpdateNPCInfo("Performance Test", null);
            dialogueUI.UpdateDialogueText(longText);

            yield return new WaitForSeconds(5f);

            dialogueUI.HideDialoguePanel();
        }

        /// <summary>
        /// Test fade effects
        /// </summary>
        public void TestFadeEffects()
        {
            StartCoroutine(TestFadeCoroutine());
        }

        private IEnumerator TestFadeCoroutine()
        {
            dialogueUI.ShowDialoguePanel();
            dialogueUI.UpdateNPCInfo("Fade Test", null);
            dialogueUI.UpdateDialogueText("Testing fade in effect...");

            yield return new WaitForSeconds(2f);

            dialogueUI.UpdateDialogueText("Now testing fade out...");

            yield return new WaitForSeconds(2f);

            dialogueUI.HideDialoguePanel();
        }

        /// <summary>
        /// Setup demo scene
        /// </summary>
        [ContextMenu("Setup Demo Scene")]
        private void SetupDemoScene()
        {
            // Tạo Canvas
            GameObject canvasObj = new GameObject("DialogueCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // Thêm DialogueUI
            DialogueUI ui = canvasObj.AddComponent<DialogueUI>();

            // Thêm DialogueUISetup để auto setup
            DialogueUISetup setup = canvasObj.AddComponent<DialogueUISetup>();
            setup.SetupDialogueUI();

            // Setup demo script
            this.dialogueUI = ui;

            Debug.Log("🎬 Demo scene setup completed!");
        }

        /// <summary>
        /// Log demo instructions
        /// </summary>
        [ContextMenu("Show Demo Instructions")]
        private void ShowDemoInstructions()
        {
            Debug.Log("🎮 Dialogue Demo Instructions:");
            Debug.Log("Press 'D' - Start full demo");
            Debug.Log("Press 'T' - Test typing effect");
            Debug.Log("Press 'C' - Test choice system");
            Debug.Log("Use Setup Demo Scene to create UI automatically");
        }
    }
}
