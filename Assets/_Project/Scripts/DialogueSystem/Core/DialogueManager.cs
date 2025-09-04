using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System;

namespace DialogueSystem
{
    /// <summary>
    /// Quản lý hệ thống hội thoại trong game
    /// Xử lý việc hiển thị text, lựa chọn và luồng hội thoại
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        #region Singleton Pattern
        public static DialogueManager Instance { get; private set; }
        #endregion

        #region Events
        public event Action OnDialogueStarted;
        public event Action OnDialogueEnded;
        public event Action<int> OnChoiceSelected;
        public event Action OnTypingStarted;
        public event Action OnTypingEnded;
        public event Action<DialogueLine> OnLineDisplayed;
        public event Action<string[]> OnChoicesShown;
        public event Action OnChoicesHidden;
        #endregion

        #region UI References
        [Header("UI Panel")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Text Elements")]
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private TextMeshProUGUI npcNameText;
        [SerializeField] private Image npcPortrait;

        [Header("Control Buttons")]
        [SerializeField] private Button continueButton;
        [SerializeField] private GameObject choicePanel;
        [SerializeField] private Button[] choiceButtons;

        [Header("Visual Effects")]
        [SerializeField] private float fadeDuration = 0.3f;
        [SerializeField] private float typingSpeed = 0.05f;
        #endregion

        #region Private Fields
        private Queue<DialogueLine> dialogueQueue;
        private bool isTyping = false;
        private bool isDialogueActive = false;
        private Coroutine typingCoroutine;
        private Coroutine fadeCoroutine;
        private DialogueData currentDialogueData;
        private int currentLineIndex;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            // Singleton setup
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeManager();
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Update()
        {
            // Handle continue input during dialogue
            if (isDialogueActive && !isTyping)
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                {
                    DisplayNextLine();
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
        #endregion

        #region Initialization
        private void InitializeManager()
        {
            dialogueQueue = new Queue<DialogueLine>();

            // Setup UI elements
            if (dialogueCanvasGroup == null && dialoguePanel != null)
            {
                dialogueCanvasGroup = dialoguePanel.GetComponent<CanvasGroup>();
                if (dialogueCanvasGroup == null)
                {
                    dialogueCanvasGroup = dialoguePanel.AddComponent<CanvasGroup>();
                }
            }

            // Hide dialogue panel initially
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }

            // Setup continue button
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(DisplayNextLine);
            }

            Debug.Log("🎭 DialogueManager initialized successfully!");
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Bắt đầu hội thoại với DialogueData
        /// </summary>
        public void StartDialogue(DialogueData dialogueData, string npcName = "", Sprite portrait = null)
        {
            if (isDialogueActive)
            {
                Debug.LogWarning("Dialogue already active, ending current dialogue first");
                EndDialogue();
            }

            if (dialogueData == null || dialogueData.LineCount == 0)
            {
                Debug.LogError("Cannot start dialogue: No dialogue data provided");
                return;
            }

            // Setup dialogue
            isDialogueActive = true;
            dialogueQueue.Clear();
            currentDialogueData = dialogueData;
            currentLineIndex = 0;

            // Set NPC info
            if (npcNameText != null)
                npcNameText.text = npcName;

            if (npcPortrait != null && portrait != null)
                npcPortrait.sprite = portrait;

            // Show dialogue panel
            ShowDialoguePanel();

            // Start displaying first line
            DisplayNextLine();

            // Trigger event
            OnDialogueStarted?.Invoke();

            Debug.Log($"🎭 Started dialogue with {npcName}");
        }

        /// <summary>
        /// Bắt đầu hội thoại với NPC
        /// </summary>
        public void StartDialogue(string npcName, Sprite portrait, DialogueLine[] lines)
        {
            if (isDialogueActive)
            {
                Debug.LogWarning("Dialogue already active, ending current dialogue first");
                EndDialogue();
            }

            if (lines == null || lines.Length == 0)
            {
                Debug.LogError("Cannot start dialogue: No dialogue lines provided");
                return;
            }

            // Setup dialogue
            isDialogueActive = true;
            dialogueQueue.Clear();

            // Set NPC info
            if (npcNameText != null)
                npcNameText.text = npcName;

            if (npcPortrait != null && portrait != null)
                npcPortrait.sprite = portrait;

            // Queue dialogue lines
            foreach (DialogueLine line in lines)
            {
                dialogueQueue.Enqueue(line);
            }

            // Show dialogue panel
            ShowDialoguePanel();

            // Start displaying
            DisplayNextLine();

            // Trigger event
            OnDialogueStarted?.Invoke();

            Debug.Log($"🎭 Started dialogue with {npcName}");
        }

        /// <summary>
        /// Bắt đầu hội thoại đơn giản với text array
        /// </summary>
        public void StartSimpleDialogue(string npcName, Sprite portrait, string[] textLines)
        {
            DialogueLine[] dialogueLines = new DialogueLine[textLines.Length];
            for (int i = 0; i < textLines.Length; i++)
            {
                dialogueLines[i] = new DialogueLine(textLines[i]);
            }

            StartDialogue(npcName, portrait, dialogueLines);
        }

        /// <summary>
        /// Hiển thị lựa chọn cho người chơi
        /// </summary>
        public void ShowChoices(string[] choices, Action<int> onChoiceSelected)
        {
            if (choicePanel == null || choiceButtons == null)
            {
                Debug.LogError("Choice UI not set up properly");
                return;
            }

            choicePanel.SetActive(true);

            // Setup choice buttons
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (i < choices.Length)
                {
                    choiceButtons[i].gameObject.SetActive(true);
                    TextMeshProUGUI buttonText = choiceButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null)
                    {
                        buttonText.text = choices[i];
                    }

                    // Remove previous listeners
                    choiceButtons[i].onClick.RemoveAllListeners();

                    // Add new listener
                    int choiceIndex = i;
                    choiceButtons[i].onClick.AddListener(() => {
                        SelectChoice(choiceIndex, onChoiceSelected);
                    });
                }
                else
                {
                    choiceButtons[i].gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Kết thúc hội thoại
        /// </summary>
        public void EndDialogue()
        {
            if (!isDialogueActive)
                return;

            isDialogueActive = false;

            // Stop typing if active
            if (isTyping && typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                isTyping = false;
            }

            // Hide panels
            HideDialoguePanel();
            if (choicePanel != null)
            {
                choicePanel.SetActive(false);
            }

            // Clear queue and reset data
            dialogueQueue.Clear();
            currentDialogueData = null;
            currentLineIndex = 0;

            // Trigger event
            OnDialogueEnded?.Invoke();

            Debug.Log("🎭 Dialogue ended");
        }

        /// <summary>
        /// Kiểm tra xem hội thoại có đang active không
        /// </summary>
        public bool IsDialogueActive()
        {
            return isDialogueActive;
        }

        /// <summary>
        /// Hiển thị dòng tiếp theo
        /// </summary>
        public void DisplayNextLine()
        {
            if (isTyping)
            {
                // Complete typing immediately
                CompleteTyping();
                return;
            }

            // Check if we have dialogue data
            if (currentDialogueData != null)
            {
                DisplayLineFromData();
            }
            else if (dialogueQueue.Count > 0)
            {
                DisplayLineFromQueue();
            }
            else
            {
                EndDialogue();
            }
        }

        /// <summary>
        /// Skip typing animation
        /// </summary>
        public void SkipTyping()
        {
            if (isTyping)
            {
                CompleteTyping();
            }
        }

        /// <summary>
        /// Lấy danh sách lựa chọn hiện tại
        /// </summary>
        public string[] GetCurrentChoices()
        {
            if (currentDialogueData != null && currentLineIndex < currentDialogueData.LineCount)
            {
                DialogueLine currentLine = currentDialogueData.GetLine(currentLineIndex);
                if (currentLine.HasChoices)
                {
                    return currentLine.Choices;
                }
            }
            return new string[0];
        }

        /// <summary>
        /// Chọn một lựa chọn
        /// </summary>
        public void SelectChoice(int choiceIndex)
        {
            SelectChoice(choiceIndex, null);
        }

        /// <summary>
        /// Chọn một lựa chọn với callback
        /// </summary>
        public void SelectChoice(int choiceIndex, Action<int> callback)
        {
            callback?.Invoke(choiceIndex);

            // Hide choice panel and trigger event
            if (choicePanel != null)
            {
                choicePanel.SetActive(false);
            }
            OnChoicesHidden?.Invoke();
        }
        #endregion

        #region Private Methods
        private void DisplayLineFromData()
        {
            if (currentLineIndex >= currentDialogueData.LineCount)
            {
                EndDialogue();
                return;
            }

            DialogueLine line = currentDialogueData.GetLine(currentLineIndex);

            // Update NPC info if provided
            if (!string.IsNullOrEmpty(line.SpeakerName) && npcNameText != null)
            {
                npcNameText.text = line.SpeakerName;
            }

            if (line.SpeakerPortrait != null && npcPortrait != null)
            {
                npcPortrait.sprite = line.SpeakerPortrait;
            }

            // Handle different line types
            if (line.HasChoices)
            {
                // Show choices instead of text
                ShowChoices(line.Choices, (choiceIndex) => {
                    // Handle choice selection
                    OnChoiceSelected?.Invoke(choiceIndex);

                    // Continue with next line based on choice
                    if (line.ChoiceNextIndices != null && choiceIndex < line.ChoiceNextIndices.Length)
                    {
                        int nextIndex = line.ChoiceNextIndices[choiceIndex];
                        if (nextIndex >= 0 && nextIndex < currentDialogueData.LineCount)
                        {
                            currentLineIndex = nextIndex;
                            DisplayNextLine();
                        }
                        else
                        {
                            EndDialogue();
                        }
                    }
                    else
                    {
                        // Move to next sequential line
                        currentLineIndex++;
                        DisplayNextLine();
                    }
                });

                // Trigger events
                OnLineDisplayed?.Invoke(line);
                OnChoicesShown?.Invoke(line.Choices);
            }
            else
            {
                // Display text with typing effect
                StartTyping(line.Text, line.TypingSpeed);

                // Trigger line displayed event
                OnLineDisplayed?.Invoke(line);

                // Auto advance if no choices
                if (line.NextDialogueIndex >= 0)
                {
                    currentLineIndex = line.NextDialogueIndex;
                }
                else
                {
                    currentLineIndex++;
                }
            }
        }

        private void DisplayLineFromQueue()
        {
            DialogueLine line = dialogueQueue.Dequeue();

            // Update NPC info if provided
            if (!string.IsNullOrEmpty(line.SpeakerName) && npcNameText != null)
            {
                npcNameText.text = line.SpeakerName;
            }

            if (line.SpeakerPortrait != null && npcPortrait != null)
            {
                npcPortrait.sprite = line.SpeakerPortrait;
            }

            // Handle different line types
            if (line.HasChoices)
            {
                // Show choices instead of text
                ShowChoices(line.Choices, (choiceIndex) => {
                    // Handle choice selection
                    OnChoiceSelected?.Invoke(choiceIndex);

                    // Continue with next line based on choice
                    if (line.ChoiceNextIndices != null && choiceIndex < line.ChoiceNextIndices.Length)
                    {
                        int nextIndex = line.ChoiceNextIndices[choiceIndex];
                        if (nextIndex >= 0)
                        {
                            // This would need to be handled by the dialogue data system
                            // For now, just end dialogue
                            EndDialogue();
                        }
                    }
                    else
                    {
                        // No next line specified, end dialogue
                        EndDialogue();
                    }
                });

                // Trigger events
                OnLineDisplayed?.Invoke(line);
                OnChoicesShown?.Invoke(line.Choices);
            }
            else
            {
                // Display text with typing effect
                StartTyping(line.Text, line.TypingSpeed);

                // Trigger line displayed event
                OnLineDisplayed?.Invoke(line);
            }
        }

        private void StartTyping(string text, float speed = 0.05f)
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }

            typingCoroutine = StartCoroutine(TypeText(text, speed));
        }

        private void CompleteTyping()
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }

            if (dialogueQueue.Count > 0)
            {
                DialogueLine currentLine = dialogueQueue.Peek();
                dialogueText.text = currentLine.Text;
            }
            else if (currentDialogueData != null && currentLineIndex < currentDialogueData.LineCount)
            {
                DialogueLine currentLine = currentDialogueData.GetLine(currentLineIndex);
                dialogueText.text = currentLine.Text;
            }

            isTyping = false;
            OnTypingEnded?.Invoke();
        }

        private IEnumerator TypeText(string text, float speed)
        {
            isTyping = true;
            dialogueText.text = "";

            OnTypingStarted?.Invoke();

            foreach (char letter in text.ToCharArray())
            {
                dialogueText.text += letter;
                yield return new WaitForSeconds(speed);
            }

            isTyping = false;
            OnTypingEnded?.Invoke();
        }

        private void ShowDialoguePanel()
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(true);
                if (fadeCoroutine != null)
                {
                    StopCoroutine(fadeCoroutine);
                }
                fadeCoroutine = StartCoroutine(FadeIn());
            }
        }

        private void HideDialoguePanel()
        {
            if (dialoguePanel != null)
            {
                if (fadeCoroutine != null)
                {
                    StopCoroutine(fadeCoroutine);
                }
                fadeCoroutine = StartCoroutine(FadeOut());
            }
        }

        private IEnumerator FadeIn()
        {
            if (dialogueCanvasGroup == null)
                yield break;

            float elapsed = 0f;
            dialogueCanvasGroup.alpha = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                dialogueCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
                yield return null;
            }

            dialogueCanvasGroup.alpha = 1f;
        }

        private IEnumerator FadeOut()
        {
            if (dialogueCanvasGroup == null)
                yield break;

            float elapsed = 0f;
            float startAlpha = dialogueCanvasGroup.alpha;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                dialogueCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDuration);
                yield return null;
            }

            dialogueCanvasGroup.alpha = 0f;
            dialoguePanel.SetActive(false);
        }
        #endregion
    }
}
