using UnityEngine;
using System.Collections.Generic;

namespace DialogueSystem
{
    /// <summary>
    /// Dữ liệu cho một dòng hội thoại
    /// </summary>
    [System.Serializable]
    public class DialogueLine
    {
        [Header("Dialogue Content")]
        [TextArea(3, 10)]
        [SerializeField] private string text;

        [Header("Speaker Information")]
        [SerializeField] private string speakerName;
        [SerializeField] private Sprite speakerPortrait;

        [Header("Timing")]
        [SerializeField] private float typingSpeed = 0.05f;
        [SerializeField] private float autoAdvanceDelay = 2f;

        [Header("Choices")]
        [SerializeField] private bool hasChoices = false;
        [SerializeField] private string[] choices;
        [SerializeField] private int[] choiceNextIndices;

        [Header("Next Dialogue")]
        [SerializeField] private int nextDialogueIndex = -1;
        [SerializeField] private DialogueEvent dialogueEvent = DialogueEvent.None;

        [Header("Audio")]
        [SerializeField] private AudioClip voiceClip;
        [SerializeField] private AudioClip backgroundMusic;

        #region Properties
        public string Text { get => text; set => text = value; }
        public string SpeakerName { get => speakerName; set => speakerName = value; }
        public Sprite SpeakerPortrait { get => speakerPortrait; set => speakerPortrait = value; }
        public float TypingSpeed { get => typingSpeed; set => typingSpeed = value; }
        public float AutoAdvanceDelay { get => autoAdvanceDelay; set => autoAdvanceDelay = value; }
        public bool HasChoices { get => hasChoices; set => hasChoices = value; }
        public string[] Choices { get => choices; set => choices = value; }
        public int[] ChoiceNextIndices { get => choiceNextIndices; set => choiceNextIndices = value; }
        public int NextDialogueIndex { get => nextDialogueIndex; set => nextDialogueIndex = value; }
        public DialogueEvent Event { get => dialogueEvent; set => dialogueEvent = value; }
        public AudioClip VoiceClip { get => voiceClip; set => voiceClip = value; }
        public AudioClip BackgroundMusic { get => backgroundMusic; set => backgroundMusic = value; }
        #endregion

        #region Constructors
        public DialogueLine()
        {
            text = "";
            speakerName = "";
            typingSpeed = 0.05f;
            autoAdvanceDelay = 2f;
            hasChoices = false;
            choices = new string[0];
            choiceNextIndices = new int[0];
            nextDialogueIndex = -1;
            dialogueEvent = DialogueEvent.None;
        }

        public DialogueLine(string text, string speakerName = "", Sprite portrait = null)
        {
            this.text = text;
            this.speakerName = speakerName;
            this.speakerPortrait = portrait;
            typingSpeed = 0.05f;
            autoAdvanceDelay = 2f;
            hasChoices = false;
            choices = new string[0];
            choiceNextIndices = new int[0];
            nextDialogueIndex = -1;
            dialogueEvent = DialogueEvent.None;
        }

        public DialogueLine(string text, string speakerName, Sprite portrait, float autoAdvanceDelay, AudioClip voiceClip = null)
        {
            this.text = text;
            this.speakerName = speakerName;
            this.speakerPortrait = portrait;
            this.autoAdvanceDelay = autoAdvanceDelay;
            this.voiceClip = voiceClip;
            typingSpeed = 0.05f;
            hasChoices = false;
            choices = new string[0];
            choiceNextIndices = new int[0];
            nextDialogueIndex = -1;
            dialogueEvent = DialogueEvent.None;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Thiết lập lựa chọn cho dòng hội thoại này
        /// </summary>
        public void SetChoices(string[] newChoices, int[] nextIndices)
        {
            if (newChoices.Length != nextIndices.Length)
            {
                Debug.LogError("Choices and next indices arrays must have the same length!");
                return;
            }

            hasChoices = true;
            choices = newChoices;
            choiceNextIndices = nextIndices;
        }

        /// <summary>
        /// Xóa tất cả lựa chọn
        /// </summary>
        public void ClearChoices()
        {
            hasChoices = false;
            choices = new string[0];
            choiceNextIndices = new int[0];
        }

        /// <summary>
        /// Kiểm tra tính hợp lệ của dữ liệu
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(text))
            {
                Debug.LogWarning("Dialogue line has empty text!");
                return false;
            }

            if (hasChoices)
            {
                if (choices == null || choices.Length == 0)
                {
                    Debug.LogWarning("Dialogue line has choices enabled but no choices defined!");
                    return false;
                }

                if (choiceNextIndices == null || choiceNextIndices.Length != choices.Length)
                {
                    Debug.LogWarning("Choice next indices array doesn't match choices array length!");
                    return false;
                }
            }

            return true;
        }
        #endregion
    }

    /// <summary>
    /// Sự kiện hội thoại có thể xảy ra
    /// </summary>
    public enum DialogueEvent
    {
        None,
        StartQuest,
        CompleteQuest,
        GiveItem,
        TakeItem,
        ChangeScene,
        PlayAnimation,
        CustomEvent
    }

    /// <summary>
    /// ScriptableObject chứa dữ liệu hội thoại
    /// </summary>
    [CreateAssetMenu(fileName = "New Dialogue", menuName = "Dialogue System/Dialogue Data")]
    public class DialogueData : ScriptableObject
    {
        [Header("Dialogue Information")]
        [SerializeField] private string dialogueName = "New Dialogue";
        [SerializeField] private string description = "";

        [Header("Dialogue Lines")]
        [SerializeField] private List<DialogueLine> dialogueLines = new List<DialogueLine>();

        [Header("Settings")]
        [SerializeField] private bool loopDialogue = false;
        [SerializeField] private bool autoAdvance = true;
        [SerializeField] private float defaultTypingSpeed = 0.05f;

        #region Properties
        public string DialogueName => dialogueName;
        public string Description => description;
        public List<DialogueLine> DialogueLines => dialogueLines;
        public bool LoopDialogue => loopDialogue;
        public bool AutoAdvance => autoAdvance;
        public float DefaultTypingSpeed => defaultTypingSpeed;
        public int LineCount => dialogueLines.Count;
        #endregion

        #region Public Methods
        /// <summary>
        /// Lấy dòng hội thoại theo index
        /// </summary>
        public DialogueLine GetLine(int index)
        {
            if (index < 0 || index >= dialogueLines.Count)
            {
                Debug.LogError($"Dialogue line index {index} is out of range!");
                return null;
            }

            return dialogueLines[index];
        }

        /// <summary>
        /// Thêm dòng hội thoại mới
        /// </summary>
        public void AddLine(DialogueLine line)
        {
            if (line != null)
            {
                dialogueLines.Add(line);
            }
        }

        /// <summary>
        /// Thêm dòng hội thoại mới với text
        /// </summary>
        public void AddLine(string text, string speakerName = "", Sprite portrait = null)
        {
            DialogueLine newLine = new DialogueLine(text, speakerName, portrait);
            dialogueLines.Add(newLine);
        }

        /// <summary>
        /// Xóa dòng hội thoại
        /// </summary>
        public void RemoveLine(int index)
        {
            if (index >= 0 && index < dialogueLines.Count)
            {
                dialogueLines.RemoveAt(index);
            }
        }

        /// <summary>
        /// Xóa tất cả dòng hội thoại
        /// </summary>
        public void ClearLines()
        {
            dialogueLines.Clear();
        }

        /// <summary>
        /// Chèn dòng hội thoại vào vị trí cụ thể
        /// </summary>
        public void InsertLine(int index, DialogueLine line)
        {
            if (line != null && index >= 0 && index <= dialogueLines.Count)
            {
                dialogueLines.Insert(index, line);
            }
        }

        /// <summary>
        /// Kiểm tra tính hợp lệ của toàn bộ dữ liệu hội thoại
        /// </summary>
        public bool IsValid()
        {
            if (dialogueLines == null || dialogueLines.Count == 0)
            {
                Debug.LogWarning($"Dialogue '{dialogueName}' has no lines!");
                return false;
            }

            for (int i = 0; i < dialogueLines.Count; i++)
            {
                if (!dialogueLines[i].IsValid())
                {
                    Debug.LogWarning($"Dialogue '{dialogueName}' has invalid line at index {i}");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Tạo bản sao của dữ liệu hội thoại
        /// </summary>
        public DialogueData Clone()
        {
            DialogueData clone = CreateInstance<DialogueData>();
            clone.dialogueName = this.dialogueName + " (Clone)";
            clone.description = this.description;
            clone.loopDialogue = this.loopDialogue;
            clone.autoAdvance = this.autoAdvance;
            clone.defaultTypingSpeed = this.defaultTypingSpeed;

            foreach (DialogueLine line in dialogueLines)
            {
                DialogueLine clonedLine = new DialogueLine();
                // Copy all properties (this would need to be expanded for full deep clone)
                clone.dialogueLines.Add(clonedLine);
            }

            return clone;
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// Lấy tổng thời gian ước tính của hội thoại
        /// </summary>
        public float GetEstimatedDuration()
        {
            float totalTime = 0f;

            foreach (DialogueLine line in dialogueLines)
            {
                // Estimate typing time
                float typingTime = line.Text.Length * line.TypingSpeed;
                totalTime += typingTime;

                // Add auto advance delay if applicable
                if (line.AutoAdvanceDelay > 0)
                {
                    totalTime += line.AutoAdvanceDelay;
                }
                else
                {
                    // Assume 3 seconds for player to read and continue
                    totalTime += 3f;
                }
            }

            return totalTime;
        }

        /// <summary>
        /// Lấy số lượng lựa chọn trong toàn bộ hội thoại
        /// </summary>
        public int GetTotalChoices()
        {
            int totalChoices = 0;

            foreach (DialogueLine line in dialogueLines)
            {
                if (line.HasChoices)
                {
                    totalChoices += line.Choices.Length;
                }
            }

            return totalChoices;
        }

        /// <summary>
        /// Tìm dòng hội thoại đầu tiên có lựa chọn
        /// </summary>
        public int FindFirstChoiceLine()
        {
            for (int i = 0; i < dialogueLines.Count; i++)
            {
                if (dialogueLines[i].HasChoices)
                {
                    return i;
                }
            }

            return -1;
        }
        #endregion
    }
}
