using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Unified dialogue system to replace multiple dialogue managers
/// Consolidates DialogueManager, ConversationController, and NPC dialogue
/// </summary>
public class UnifiedDialogue : MonoBehaviour
{
    [Header("Dialogue Settings")]
    [SerializeField] private float textSpeed = 0.05f;
    [SerializeField] private bool enableDebugLogging = false;
    [SerializeField] private AudioClip dialogueBlipSound;

    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMPro.TextMeshProUGUI dialogueText;
    [SerializeField] private TMPro.TextMeshProUGUI speakerNameText;
    [SerializeField] private GameObject[] choiceButtons;

    private DialogueTree currentDialogue;
    private DialogueNode currentNode;
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private Queue<DialogueLine> dialogueQueue = new Queue<DialogueLine>();

    private void Awake()
    {
        ServiceLocator.RegisterService(this);
        InitializeDialogueSystem();
    }

    private void InitializeDialogueSystem()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        // Create default dialogue trees
        CreateDefaultDialogues();
    }

    private void CreateDefaultDialogues()
    {
        // Tutorial NPC dialogue
        CreateDialogueTree("tutorial_npc", "Tutorial NPC",
            new DialogueNode
            {
                Id = "greeting",
                Speaker = "Tutorial NPC",
                Text = "Welcome, adventurer! Would you like me to teach you the basics of combat?",
                Choices = new List<DialogueChoice>
                {
                    new DialogueChoice { Text = "Yes, please!", NextNodeId = "combat_lesson" },
                    new DialogueChoice { Text = "Maybe later.", NextNodeId = "goodbye" }
                }
            },
            new DialogueNode
            {
                Id = "combat_lesson",
                Speaker = "Tutorial NPC",
                Text = "Great! To attack, press the attack button when near enemies. Different directions give different attacks!",
                Choices = new List<DialogueChoice>
                {
                    new DialogueChoice { Text = "Got it!", NextNodeId = "end_tutorial" }
                }
            },
            new DialogueNode
            {
                Id = "goodbye",
                Speaker = "Tutorial NPC",
                Text = "Come back anytime you need help!",
                Choices = new List<DialogueChoice>
                {
                    new DialogueChoice { Text = "Goodbye.", NextNodeId = null }
                }
            },
            new DialogueNode
            {
                Id = "end_tutorial",
                Speaker = "Tutorial NPC",
                Text = "Good luck on your adventure!",
                Choices = new List<DialogueChoice>
                {
                    new DialogueChoice { Text = "Thanks!", NextNodeId = null }
                }
            }
        );

        // Shopkeeper dialogue
        CreateDialogueTree("shopkeeper", "Shopkeeper",
            new DialogueNode
            {
                Id = "welcome",
                Speaker = "Shopkeeper",
                Text = "Welcome to my shop! Take a look at my wares.",
                Choices = new List<DialogueChoice>
                {
                    new DialogueChoice { Text = "Show me your goods.", NextNodeId = "show_shop" },
                    new DialogueChoice { Text = "Just browsing.", NextNodeId = "goodbye_shop" }
                }
            },
            new DialogueNode
            {
                Id = "show_shop",
                Speaker = "Shopkeeper",
                Text = "Here are my items. Let me know if you need anything!",
                Choices = new List<DialogueChoice>
                {
                    new DialogueChoice { Text = "Thanks!", NextNodeId = null, Action = "open_shop" }
                }
            },
            new DialogueNode
            {
                Id = "goodbye_shop",
                Speaker = "Shopkeeper",
                Text = "Come back soon!",
                Choices = new List<DialogueChoice>
                {
                    new DialogueChoice { Text = "Goodbye.", NextNodeId = null }
                }
            }
        );
    }

    /// <summary>
    /// Create a dialogue tree
    /// </summary>
    public void CreateDialogueTree(string treeId, string npcName, params DialogueNode[] nodes)
    {
        DialogueTree tree = new DialogueTree
        {
            Id = treeId,
            NpcName = npcName,
            Nodes = new Dictionary<string, DialogueNode>()
        };

        foreach (var node in nodes)
        {
            tree.Nodes.Add(node.Id, node);
        }

        // Store dialogue tree (would typically use a database or file system)
        PlayerPrefs.SetString("dialogue_" + treeId, JsonUtility.ToJson(tree));

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("💬 Created dialogue tree: {0}", npcName));
        }
    }

    /// <summary>
    /// Start dialogue with NPC
    /// </summary>
    public void StartDialogue(string dialogueTreeId, string startNodeId = "greeting")
    {
        string dialogueJson = PlayerPrefs.GetString("dialogue_" + dialogueTreeId);
        if (string.IsNullOrEmpty(dialogueJson))
        {
            PerformanceUtils.LogWarning(PerformanceUtils.FormatString("⚠️ Dialogue tree not found: {0}", dialogueTreeId));
            return;
        }

        currentDialogue = JsonUtility.FromJson<DialogueTree>(dialogueJson);
        if (currentDialogue == null || !currentDialogue.Nodes.TryGetValue(startNodeId, out currentNode))
        {
            PerformanceUtils.LogWarning(PerformanceUtils.FormatString("⚠️ Dialogue node not found: {0}", startNodeId));
            return;
        }

        ShowDialoguePanel();
        DisplayCurrentNode();

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("💬 Started dialogue: {0}", currentDialogue.NpcName));
        }
    }

    /// <summary>
    /// Display current dialogue node
    /// </summary>
    private void DisplayCurrentNode()
    {
        if (currentNode == null) return;

        // Set speaker name
        if (speakerNameText != null)
        {
            speakerNameText.text = currentNode.Speaker;
        }

        // Start typing effect
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine = StartCoroutine(TypeText(currentNode.Text));

        // Display choices
        DisplayChoices();
    }

    /// <summary>
    /// Type text with animation
    /// </summary>
    private System.Collections.IEnumerator TypeText(string text)
    {
        isTyping = true;

        if (dialogueText != null)
        {
            dialogueText.text = "";

            foreach (char letter in text)
            {
                dialogueText.text += letter;

                // Play typing sound
                if (dialogueBlipSound != null)
                {
                    ServiceLocator.GetService<UnifiedAudio>()?.PlaySFX(dialogueBlipSound, 0.3f);
                }

                yield return new WaitForSeconds(textSpeed);
            }
        }

        isTyping = false;
    }

    /// <summary>
    /// Display dialogue choices
    /// </summary>
    private void DisplayChoices()
    {
        if (currentNode.Choices == null || currentNode.Choices.Count == 0)
        {
            // No choices, auto-advance or end dialogue
            HideChoiceButtons();
            return;
        }

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (i < currentNode.Choices.Count)
            {
                choiceButtons[i].SetActive(true);
                var buttonText = choiceButtons[i].GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = currentNode.Choices[i].Text;
                }

                // Add click listener
                int choiceIndex = i; // Capture loop variable
                choiceButtons[i].GetComponent<UnityEngine.UI.Button>()?.onClick.RemoveAllListeners();
                choiceButtons[i].GetComponent<UnityEngine.UI.Button>()?.onClick.AddListener(() => SelectChoice(choiceIndex));
            }
            else
            {
                choiceButtons[i].SetActive(false);
            }
        }
    }

    /// <summary>
    /// Hide all choice buttons
    /// </summary>
    private void HideChoiceButtons()
    {
        foreach (var button in choiceButtons)
        {
            if (button != null)
            {
                button.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Select a dialogue choice
    /// </summary>
    public void SelectChoice(int choiceIndex)
    {
        if (currentNode == null || currentNode.Choices == null || choiceIndex >= currentNode.Choices.Count)
        {
            return;
        }

        var choice = currentNode.Choices[choiceIndex];

        // Execute choice action
        if (!string.IsNullOrEmpty(choice.Action))
        {
            ExecuteDialogueAction(choice.Action);
        }

        // Move to next node
        if (!string.IsNullOrEmpty(choice.NextNodeId))
        {
            if (currentDialogue.Nodes.TryGetValue(choice.NextNodeId, out DialogueNode nextNode))
            {
                currentNode = nextNode;
                DisplayCurrentNode();
            }
            else
            {
                EndDialogue();
            }
        }
        else
        {
            EndDialogue();
        }
    }

    /// <summary>
    /// Execute dialogue action
    /// </summary>
    private void ExecuteDialogueAction(string action)
    {
        switch (action)
        {
            case "open_shop":
                // ServiceLocator.GetService<ShopManager>()?.OpenShop();
                GameEvents.OnShopOpened?.Invoke();
                break;
            case "give_quest":
                // ServiceLocator.GetService<UnifiedQuest>()?.ActivateQuest("tutorial_combat");
                break;
            case "complete_quest":
                // ServiceLocator.GetService<UnifiedQuest>()?.CompleteQuest("tutorial_combat");
                break;
            default:
                PerformanceUtils.LogWarning(PerformanceUtils.FormatString("⚠️ Unknown dialogue action: {0}", action));
                break;
        }
    }

    /// <summary>
    /// Skip typing animation
    /// </summary>
    public void SkipTyping()
    {
        if (isTyping && typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            isTyping = false;

            if (dialogueText != null)
            {
                dialogueText.text = currentNode.Text;
            }
        }
    }

    /// <summary>
    /// Continue to next node (for dialogues without choices)
    /// </summary>
    public void ContinueDialogue()
    {
        if (currentNode == null || currentNode.Choices == null || currentNode.Choices.Count == 0)
        {
            EndDialogue();
            return;
        }

        // Auto-select first choice
        SelectChoice(0);
    }

    /// <summary>
    /// End current dialogue
    /// </summary>
    public void EndDialogue()
    {
        currentDialogue = null;
        currentNode = null;

        HideDialoguePanel();

        if (enableDebugLogging)
        {
            PerformanceUtils.Log("💬 Ended dialogue");
        }
    }

    /// <summary>
    /// Show dialogue panel
    /// </summary>
    private void ShowDialoguePanel()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }

        // Pause game time
        Time.timeScale = 0f;
    }

    /// <summary>
    /// Hide dialogue panel
    /// </summary>
    private void HideDialoguePanel()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        // Resume game time
        Time.timeScale = 1f;
    }

    /// <summary>
    /// Check if dialogue is active
    /// </summary>
    public bool IsDialogueActive()
    {
        return currentDialogue != null;
    }

    /// <summary>
    /// Get current speaker name
    /// </summary>
    public string GetCurrentSpeaker()
    {
        return currentNode?.Speaker ?? "";
    }

    /// <summary>
    /// Get current dialogue text
    /// </summary>
    public string GetCurrentText()
    {
        return currentNode?.Text ?? "";
    }

    /// <summary>
    /// Queue dialogue line for later display
    /// </summary>
    public void QueueDialogue(DialogueLine line)
    {
        dialogueQueue.Enqueue(line);
    }

    /// <summary>
    /// Process dialogue queue
    /// </summary>
    public void ProcessDialogueQueue()
    {
        if (dialogueQueue.Count > 0 && !IsDialogueActive())
        {
            var line = dialogueQueue.Dequeue();
            StartDialogue(line.TreeId, line.NodeId);
        }
    }

    /// <summary>
    /// Clear dialogue queue
    /// </summary>
    public void ClearDialogueQueue()
    {
        dialogueQueue.Clear();
    }

    /// <summary>
    /// Add dialogue node to existing tree
    /// </summary>
    public void AddDialogueNode(string treeId, DialogueNode node)
    {
        string dialogueJson = PlayerPrefs.GetString("dialogue_" + treeId);
        if (string.IsNullOrEmpty(dialogueJson)) return;

        DialogueTree tree = JsonUtility.FromJson<DialogueTree>(dialogueJson);
        if (tree != null)
        {
            tree.Nodes[node.Id] = node;
            PlayerPrefs.SetString("dialogue_" + treeId, JsonUtility.ToJson(tree));
        }
    }

    /// <summary>
    /// Get all available dialogue trees
    /// </summary>
    public List<string> GetAvailableDialogues()
    {
        List<string> dialogues = new List<string>();

        // This would typically scan saved dialogue files
        // For now, return known dialogues
        dialogues.Add("tutorial_npc");
        dialogues.Add("shopkeeper");

        return dialogues;
    }

    private void Update()
    {
        // Handle input
        if (IsDialogueActive())
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                if (isTyping)
                {
                    SkipTyping();
                }
                else
                {
                    ContinueDialogue();
                }
            }
        }
    }
}

/// <summary>
/// Dialogue tree structure
/// </summary>
[System.Serializable]
public class DialogueTree
{
    public string Id;
    public string NpcName;
    public Dictionary<string, DialogueNode> Nodes;
}

/// <summary>
/// Dialogue node structure
/// </summary>
[System.Serializable]
public class DialogueNode
{
    public string Id;
    public string Speaker;
    public string Text;
    public List<DialogueChoice> Choices;
}

/// <summary>
/// Dialogue choice structure
/// </summary>
[System.Serializable]
public class DialogueChoice
{
    public string Text;
    public string NextNodeId;
    public string Action;
}

/// <summary>
/// Dialogue line for queuing
/// </summary>
[System.Serializable]
public class DialogueLine
{
    public string TreeId;
    public string NodeId;
}
