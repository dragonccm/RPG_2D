using UnityEngine;
using System.Collections;

namespace DialogueSystem
{
    /// <summary>
    /// Component xử lý input cho hệ thống hội thoại
    /// Quản lý các phím tắt và điều khiển hội thoại
    /// </summary>
    public class DialogueInput : MonoBehaviour
    {
        #region Singleton
        private static DialogueInput instance;
        public static DialogueInput Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<DialogueInput>();
                    if (instance == null)
                    {
                        GameObject obj = new GameObject("DialogueInput");
                        instance = obj.AddComponent<DialogueInput>();
                        DontDestroyOnLoad(obj);
                    }
                }
                return instance;
            }
        }
        #endregion

        #region Serialized Fields
        [Header("Input Settings")]
        [SerializeField] private KeyCode continueKey = KeyCode.Space;
        [SerializeField] private KeyCode continueKeyAlt = KeyCode.Return;
        [SerializeField] private KeyCode skipTypingKey = KeyCode.LeftControl;
        [SerializeField] private KeyCode endDialogueKey = KeyCode.Escape;

        [Header("Choice Input")]
        [SerializeField] private KeyCode[] choiceKeys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4 };
        [SerializeField] private KeyCode choiceUpKey = KeyCode.UpArrow;
        [SerializeField] private KeyCode choiceDownKey = KeyCode.DownArrow;
        [SerializeField] private KeyCode choiceSelectKey = KeyCode.Return;

        [Header("Mouse Input")]
        [SerializeField] private bool enableMouseClick = true;
        [SerializeField] private float doubleClickTime = 0.3f;

        [Header("Controller Support")]
        [SerializeField] private string continueButton = "Submit";
        [SerializeField] private string choiceVerticalAxis = "Vertical";
        [SerializeField] private string choiceSelectButton = "Submit";
        #endregion

        #region Private Fields
        private DialogueManager dialogueManager;
        private DialogueUI dialogueUI;
        private bool isTyping = false;
        private int selectedChoiceIndex = 0;
        private float lastClickTime = 0f;
        private bool waitingForChoice = false;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeComponents();
        }

        private void Start()
        {
            dialogueManager = DialogueManager.Instance;
            dialogueUI = FindObjectOfType<DialogueUI>();

            SubscribeToEvents();
        }

        private void Update()
        {
            if (dialogueManager == null || !dialogueManager.IsDialogueActive())
                return;

            HandleKeyboardInput();
            HandleMouseInput();
            HandleControllerInput();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        #endregion

        #region Initialization
        private void InitializeComponents()
        {
            Debug.Log("🎮 DialogueInput initialized successfully!");
        }

        private void SubscribeToEvents()
        {
            if (dialogueManager != null)
            {
                dialogueManager.OnDialogueStarted += OnDialogueStarted;
                dialogueManager.OnDialogueEnded += OnDialogueEnded;
                dialogueManager.OnTypingStarted += OnTypingStarted;
                dialogueManager.OnTypingEnded += OnTypingEnded;
                dialogueManager.OnChoicesShown += OnChoicesShown;
                dialogueManager.OnChoicesHidden += OnChoicesHidden;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (dialogueManager != null)
            {
                dialogueManager.OnDialogueStarted -= OnDialogueStarted;
                dialogueManager.OnDialogueEnded -= OnDialogueEnded;
                dialogueManager.OnTypingStarted -= OnTypingStarted;
                dialogueManager.OnTypingEnded -= OnTypingEnded;
                dialogueManager.OnChoicesShown -= OnChoicesShown;
                dialogueManager.OnChoicesHidden -= OnChoicesHidden;
            }
        }
        #endregion

        #region Event Handlers
        private void OnDialogueStarted()
        {
            Debug.Log("🎮 Dialogue input enabled");
        }

        private void OnDialogueEnded()
        {
            waitingForChoice = false;
            selectedChoiceIndex = 0;
            Debug.Log("🎮 Dialogue input disabled");
        }

        private void OnTypingStarted()
        {
            isTyping = true;
        }

        private void OnTypingEnded()
        {
            isTyping = false;
        }

        private void OnChoicesShown(string[] choices)
        {
            waitingForChoice = true;
            selectedChoiceIndex = 0;
        }

        private void OnChoicesHidden()
        {
            waitingForChoice = false;
        }
        #endregion

        #region Input Handling
        private void HandleKeyboardInput()
        {
            // Continue dialogue
            if (Input.GetKeyDown(continueKey) || Input.GetKeyDown(continueKeyAlt))
            {
                if (isTyping)
                {
                    // Skip typing
                    if (Input.GetKey(skipTypingKey))
                    {
                        dialogueManager.SkipTyping();
                    }
                }
                else if (!waitingForChoice)
                {
                    // Continue to next line
                    dialogueManager.DisplayNextLine();
                }
            }

            // End dialogue
            if (Input.GetKeyDown(endDialogueKey))
            {
                dialogueManager.EndDialogue();
            }

            // Choice selection
            if (waitingForChoice)
            {
                HandleChoiceInput();
            }
        }

        private void HandleMouseInput()
        {
            if (!enableMouseClick)
                return;

            if (Input.GetMouseButtonDown(0))
            {
                float currentTime = Time.time;
                bool isDoubleClick = (currentTime - lastClickTime) < doubleClickTime;

                if (isTyping && isDoubleClick)
                {
                    // Double click to skip typing
                    dialogueManager.SkipTyping();
                }
                else if (!isTyping && !waitingForChoice)
                {
                    // Single click to continue
                    dialogueManager.DisplayNextLine();
                }

                lastClickTime = currentTime;
            }
        }

        private void HandleControllerInput()
        {
            // Continue with controller button
            if (Input.GetButtonDown(continueButton))
            {
                if (isTyping)
                {
                    dialogueManager.SkipTyping();
                }
                else if (!waitingForChoice)
                {
                    dialogueManager.DisplayNextLine();
                }
            }

            // Choice navigation with controller
            if (waitingForChoice)
            {
                float verticalInput = Input.GetAxis(choiceVerticalAxis);

                if (verticalInput > 0.5f && !Mathf.Approximately(verticalInput, 0))
                {
                    // Move up
                    selectedChoiceIndex = Mathf.Max(0, selectedChoiceIndex - 1);
                    UpdateChoiceSelection();
                }
                else if (verticalInput < -0.5f && !Mathf.Approximately(verticalInput, 0))
                {
                    // Move down
                    int maxChoices = dialogueManager.GetCurrentChoices().Length;
                    selectedChoiceIndex = Mathf.Min(maxChoices - 1, selectedChoiceIndex + 1);
                    UpdateChoiceSelection();
                }

                // Select choice
                if (Input.GetButtonDown(choiceSelectButton))
                {
                    dialogueManager.SelectChoice(selectedChoiceIndex);
                }
            }
        }

        private void HandleChoiceInput()
        {
            // Number keys for choices
            for (int i = 0; i < choiceKeys.Length; i++)
            {
                if (Input.GetKeyDown(choiceKeys[i]))
                {
                    dialogueManager.SelectChoice(i);
                    return;
                }
            }

            // Arrow keys for choice navigation
            if (Input.GetKeyDown(choiceUpKey))
            {
                selectedChoiceIndex = Mathf.Max(0, selectedChoiceIndex - 1);
                UpdateChoiceSelection();
            }
            else if (Input.GetKeyDown(choiceDownKey))
            {
                int maxChoices = dialogueManager.GetCurrentChoices().Length;
                selectedChoiceIndex = Mathf.Min(maxChoices - 1, selectedChoiceIndex + 1);
                UpdateChoiceSelection();
            }

            // Enter to select
            if (Input.GetKeyDown(choiceSelectKey))
            {
                dialogueManager.SelectChoice(selectedChoiceIndex);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Thiết lập phím tiếp tục hội thoại
        /// </summary>
        public void SetContinueKey(KeyCode key)
        {
            continueKey = key;
        }

        /// <summary>
        /// Thiết lập phím kết thúc hội thoại
        /// </summary>
        public void SetEndDialogueKey(KeyCode key)
        {
            endDialogueKey = key;
        }

        /// <summary>
        /// Thiết lập các phím chọn lựa
        /// </summary>
        public void SetChoiceKeys(KeyCode[] keys)
        {
            choiceKeys = keys;
        }

        /// <summary>
        /// Bật/tắt input chuột
        /// </summary>
        public void EnableMouseInput(bool enable)
        {
            enableMouseClick = enable;
        }

        /// <summary>
        /// Lấy index lựa chọn hiện tại
        /// </summary>
        public int GetSelectedChoiceIndex()
        {
            return selectedChoiceIndex;
        }

        /// <summary>
        /// Đặt index lựa chọn
        /// </summary>
        public void SetSelectedChoiceIndex(int index)
        {
            selectedChoiceIndex = index;
            UpdateChoiceSelection();
        }

        /// <summary>
        /// Kiểm tra có đang chờ lựa chọn không
        /// </summary>
        public bool IsWaitingForChoice()
        {
            return waitingForChoice;
        }

        /// <summary>
        /// Kiểm tra có đang typing không
        /// </summary>
        public bool IsTyping()
        {
            return isTyping;
        }
        #endregion

        #region Private Methods
        private void UpdateChoiceSelection()
        {
            // Update UI to show selected choice
            if (dialogueUI != null)
            {
                // This would need to be implemented in DialogueUI to highlight selected choice
                Debug.Log($"Selected choice index: {selectedChoiceIndex}");
            }
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// Lấy thông tin input settings
        /// </summary>
        public void LogInputSettings()
        {
            Debug.Log("🎮 Dialogue Input Settings:");
            Debug.Log($"- Continue Key: {continueKey}");
            Debug.Log($"- Continue Key Alt: {continueKeyAlt}");
            Debug.Log($"- Skip Typing Key: {skipTypingKey}");
            Debug.Log($"- End Dialogue Key: {endDialogueKey}");
            Debug.Log($"- Choice Keys: {string.Join(", ", System.Array.ConvertAll(choiceKeys, k => k.ToString()))}");
            Debug.Log($"- Mouse Input: {enableMouseClick}");
            Debug.Log($"- Double Click Time: {doubleClickTime}s");
        }

        /// <summary>
        /// Reset input state
        /// </summary>
        public void ResetInputState()
        {
            isTyping = false;
            waitingForChoice = false;
            selectedChoiceIndex = 0;
            lastClickTime = 0f;
        }
        #endregion
    }
}
