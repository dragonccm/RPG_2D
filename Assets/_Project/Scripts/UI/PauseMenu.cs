using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Linq;

namespace UI
{
    public class PauseMenu : MonoBehaviour
    {
        [Header("Pause Menu Settings")]
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
        
        [Header("Audio Settings")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip pauseSound;
        [SerializeField] private AudioClip resumeSound;
        
        [Header("Visual Settings")]
        [SerializeField] private bool showCursorOnPause = true;
        [SerializeField] private bool useTimeScale = true;
        [SerializeField] private float pauseTransitionDuration = 0.2f;
        
        private bool isPaused = false;
        private float previousTimeScale = 1f;
        private Coroutine transitionCoroutine;
        
        public static PauseMenu Instance { get; private set; }
        
        public bool IsPaused => isPaused;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }
        
        private void Start()
        {
            InitializePauseMenu();
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(pauseKey))
            {
                TogglePause();
            }
        }
        
        private void InitializePauseMenu()
        {
            if (pauseMenuPanel == null)
            {
                Debug.LogError("PauseMenu: pauseMenuPanel chưa được gán!");
                return;
            }
            
            // Thiết lập các button listeners
            if (resumeButton != null)
                resumeButton.onClick.AddListener(ResumeGame);
            
            if (restartButton != null)
                restartButton.onClick.AddListener(RestartGame);
            
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(GoToMainMenu);
            
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OpenSettings);
            
            // Đảm bảo menu bắt đầu ẩn
            pauseMenuPanel.SetActive(false);
            
            // Tìm audio source nếu chưa có
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }
        
        public void TogglePause()
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
        
        public void PauseGame()
        {
            if (isPaused) return;
            
            isPaused = true;
            
            // Lưu time scale hiện tại
            if (useTimeScale)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            
            // Hiển thị menu
            ShowPauseMenu();
            
            // Hiển thị cursor
            if (showCursorOnPause)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            
            // Phát âm thanh
            if (audioSource != null && pauseSound != null)
                audioSource.PlayOneShot(pauseSound);
            
            // Gọi sự kiện pause
            OnGamePaused();
            
            Debug.Log("Game Paused");
        }
        
        public void ResumeGame()
        {
            if (!isPaused) return;
            
            isPaused = false;
            
            // Khôi phục time scale
            if (useTimeScale)
            {
                Time.timeScale = previousTimeScale;
            }
            
            // Ẩn menu
            HidePauseMenu();
            
            // Ẩn cursor
            if (showCursorOnPause)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            
            // Phát âm thanh
            if (audioSource != null && resumeSound != null)
                audioSource.PlayOneShot(resumeSound);
            
            // Gọi sự kiện resume
            OnGameResumed();
            
            Debug.Log("Game Resumed");
        }
        
        private void ShowPauseMenu()
        {
            if (pauseMenuPanel == null) return;
            
            pauseMenuPanel.SetActive(true);
            
            if (transitionCoroutine != null)
                StopCoroutine(transitionCoroutine);
            
            transitionCoroutine = StartCoroutine(TransitionIn());
        }
        
        private void HidePauseMenu()
        {
            if (pauseMenuPanel == null) return;
            
            if (transitionCoroutine != null)
                StopCoroutine(transitionCoroutine);
            
            transitionCoroutine = StartCoroutine(TransitionOut());
        }
        
        private IEnumerator TransitionIn()
        {
            var canvasGroup = pauseMenuPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = pauseMenuPanel.AddComponent<CanvasGroup>();
            }
            
            canvasGroup.alpha = 0f;
            float elapsed = 0f;
            
            while (elapsed < pauseTransitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / pauseTransitionDuration);
                yield return null;
            }
            
            canvasGroup.alpha = 1f;
        }
        
        private IEnumerator TransitionOut()
        {
            var canvasGroup = pauseMenuPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                pauseMenuPanel.SetActive(false);
                yield break;
            }
            
            float elapsed = 0f;
            
            while (elapsed < pauseTransitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / pauseTransitionDuration);
                yield return null;
            }
            
            canvasGroup.alpha = 0f;
            pauseMenuPanel.SetActive(false);
        }
        
        public void RestartGame()
        {
            ResumeGame();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            Debug.Log("Game Restarted");
        }
        
        public void GoToMainMenu()
        {
            ResumeGame();
            SceneManager.LoadScene("MainMenu");
            Debug.Log("Going to Main Menu");
        }
        
        public void OpenSettings()
        {
            Debug.Log("Opening Settings Menu");
            // Có thể mở một settings menu khác
        }
        
        private void OnGamePaused()
        {
            // Thông báo cho các system khác rằng game đã pause
            var pauseListeners = FindObjectsOfType<MonoBehaviour>().OfType<IPauseListener>();
            foreach (var listener in pauseListeners)
            {
                listener.OnGamePaused();
            }
        }
        
        private void OnGameResumed()
        {
            // Thông báo cho các system khác rằng game đã resume
            var pauseListeners = FindObjectsOfType<MonoBehaviour>().OfType<IPauseListener>();
            foreach (var listener in pauseListeners)
            {
                listener.OnGameResumed();
            }
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            // Xử lý khi ứng dụng pause (mobile)
            if (pauseStatus && !isPaused)
            {
                PauseGame();
            }
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            // Xử lý khi mất focus (PC)
            if (!hasFocus && !isPaused)
            {
                PauseGame();
            }
        }
        
        [ContextMenu("Test Pause")]
        public void TestPause()
        {
            PauseGame();
        }
        
        [ContextMenu("Test Resume")]
        public void TestResume()
        {
            ResumeGame();
        }
    }
    
    public interface IPauseListener
    {
        void OnGamePaused();
        void OnGameResumed();
    }
}