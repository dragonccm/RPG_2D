using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace DialogueSystem
{
    /// <summary>
    /// Component xử lý tương tác với NPC
    /// Phát hiện player trong vùng tương tác và khởi tạo hội thoại
    /// </summary>
    public class NPCController : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Interaction Settings")]
        [SerializeField] private float interactionRadius = 2f;
        [SerializeField] private LayerMask playerLayer;
        [SerializeField] private KeyCode interactionKey = KeyCode.E;

        [Header("NPC Information")]
        [SerializeField] private string npcName = "NPC";
        [SerializeField] private Sprite npcPortrait;
        [SerializeField] private DialogueData dialogueData;

        [Header("Visual Feedback")]
        [SerializeField] private GameObject interactionIndicator;
        [SerializeField] private Animator npcAnimator;
        [SerializeField] private string idleAnimation = "Idle";
        [SerializeField] private string talkingAnimation = "Talking";

        [Header("Audio")]
        [SerializeField] private AudioClip interactionSound;
        [SerializeField] private AudioSource audioSource;
        #endregion

        #region Private Fields
        private bool playerInRange = false;
        private bool isTalking = false;
        private Transform playerTransform;
        private DialogueManager dialogueManager;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            dialogueManager = DialogueManager.Instance;
            if (dialogueManager == null)
            {
                Debug.LogError("DialogueManager not found in scene!");
            }

            // Hide interaction indicator initially
            if (interactionIndicator != null)
            {
                interactionIndicator.SetActive(false);
            }
        }

        private void Update()
        {
            if (playerInRange && !isTalking)
            {
                // Check for interaction input
                if (Input.GetKeyDown(interactionKey))
                {
                    StartDialogue();
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (((1 << other.gameObject.layer) & playerLayer) != 0)
            {
                playerInRange = true;
                playerTransform = other.transform;
                ShowInteractionIndicator();

                Debug.Log($"Player entered interaction range of {npcName}");
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (((1 << other.gameObject.layer) & playerLayer) != 0)
            {
                playerInRange = false;
                playerTransform = null;
                HideInteractionIndicator();

                Debug.Log($"Player left interaction range of {npcName}");
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw interaction radius in editor
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }
        #endregion

        #region Initialization
        private void InitializeComponents()
        {
            // Setup collider if not present
            CircleCollider2D collider = GetComponent<CircleCollider2D>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<CircleCollider2D>();
                collider.isTrigger = true;
                collider.radius = interactionRadius;
            }
            else
            {
                collider.radius = interactionRadius;
            }

            // Setup audio source
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            Debug.Log($"NPC {npcName} initialized successfully!");
        }
        #endregion

        #region Dialogue Methods
        /// <summary>
        /// Bắt đầu hội thoại với NPC
        /// </summary>
        public void StartDialogue()
        {
            if (dialogueManager == null || dialogueData == null)
            {
                Debug.LogError("Cannot start dialogue: DialogueManager or DialogueData is missing!");
                return;
            }

            if (isTalking)
            {
                Debug.LogWarning("Already in dialogue with this NPC!");
                return;
            }

            isTalking = true;
            PlayInteractionSound();
            SetTalkingAnimation(true);

            // Start dialogue with NPC information
            dialogueManager.StartDialogue(dialogueData, npcName, npcPortrait);

            Debug.Log($"Started dialogue with {npcName}");
        }

        /// <summary>
        /// Kết thúc hội thoại
        /// </summary>
        public void EndDialogue()
        {
            if (!isTalking)
                return;

            isTalking = false;
            SetTalkingAnimation(false);

            Debug.Log($"Ended dialogue with {npcName}");
        }

        /// <summary>
        /// Khởi tạo hội thoại với dữ liệu tùy chỉnh
        /// </summary>
        public void StartDialogue(DialogueData customDialogueData, string customName = null, Sprite customPortrait = null)
        {
            if (dialogueManager == null)
            {
                Debug.LogError("Cannot start dialogue: DialogueManager is missing!");
                return;
            }

            if (isTalking)
            {
                Debug.LogWarning("Already in dialogue with this NPC!");
                return;
            }

            isTalking = true;
            PlayInteractionSound();
            SetTalkingAnimation(true);

            string displayName = customName ?? npcName;
            Sprite displayPortrait = customPortrait ?? npcPortrait;

            dialogueManager.StartDialogue(customDialogueData, displayName, displayPortrait);

            Debug.Log($"Started custom dialogue with {displayName}");
        }
        #endregion

        #region Visual Feedback
        private void ShowInteractionIndicator()
        {
            if (interactionIndicator != null)
            {
                interactionIndicator.SetActive(true);
            }
        }

        private void HideInteractionIndicator()
        {
            if (interactionIndicator != null)
            {
                interactionIndicator.SetActive(false);
            }
        }

        private void SetTalkingAnimation(bool talking)
        {
            if (npcAnimator != null)
            {
                if (talking)
                {
                    npcAnimator.Play(talkingAnimation);
                }
                else
                {
                    npcAnimator.Play(idleAnimation);
                }
            }
        }
        #endregion

        #region Audio
        private void PlayInteractionSound()
        {
            if (interactionSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(interactionSound);
            }
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Kiểm tra player có trong vùng tương tác không
        /// </summary>
        public bool IsPlayerInRange()
        {
            return playerInRange;
        }

        /// <summary>
        /// Kiểm tra NPC đang nói chuyện không
        /// </summary>
        public bool IsTalking()
        {
            return isTalking;
        }

        /// <summary>
        /// Lấy tên NPC
        /// </summary>
        public string GetNPCName()
        {
            return npcName;
        }

        /// <summary>
        /// Lấy portrait của NPC
        /// </summary>
        public Sprite GetNPCPortrait()
        {
            return npcPortrait;
        }

        /// <summary>
        /// Lấy dữ liệu hội thoại
        /// </summary>
        public DialogueData GetDialogueData()
        {
            return dialogueData;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Thiết lập dữ liệu hội thoại mới
        /// </summary>
        public void SetDialogueData(DialogueData newDialogueData)
        {
            dialogueData = newDialogueData;
        }

        /// <summary>
        /// Thiết lập tên NPC
        /// </summary>
        public void SetNPCName(string newName)
        {
            npcName = newName;
        }

        /// <summary>
        /// Thiết lập portrait NPC
        /// </summary>
        public void SetNPCPortrait(Sprite newPortrait)
        {
            npcPortrait = newPortrait;
        }

        /// <summary>
        /// Thiết lập bán kính tương tác
        /// </summary>
        public void SetInteractionRadius(float radius)
        {
            interactionRadius = radius;

            CircleCollider2D collider = GetComponent<CircleCollider2D>();
            if (collider != null)
            {
                collider.radius = interactionRadius;
            }
        }

        /// <summary>
        /// Thiết lập phím tương tác
        /// </summary>
        public void SetInteractionKey(KeyCode key)
        {
            interactionKey = key;
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// Tìm player trong vùng tương tác
        /// </summary>
        public GameObject FindPlayerInRange()
        {
            if (!playerInRange || playerTransform == null)
                return null;

            return playerTransform.gameObject;
        }

        /// <summary>
        /// Lấy khoảng cách đến player
        /// </summary>
        public float GetDistanceToPlayer()
        {
            if (playerTransform == null)
                return float.MaxValue;

            return Vector3.Distance(transform.position, playerTransform.position);
        }

        /// <summary>
        /// Xoay NPC về phía player
        /// </summary>
        public void FacePlayer()
        {
            if (playerTransform == null)
                return;

            Vector3 direction = playerTransform.position - transform.position;
            if (direction.x > 0)
            {
                // Player ở bên phải
                transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }
            else if (direction.x < 0)
            {
                // Player ở bên trái
                transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }
        }
        #endregion
    }
}
