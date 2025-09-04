using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace DialogueSystem
{
    /// <summary>
    /// Component xử lý âm thanh cho hệ thống hội thoại
    /// Quản lý voice clips, background music, và sound effects
    /// </summary>
    public class DialogueAudio : MonoBehaviour
    {
        #region Singleton
        private static DialogueAudio instance;
        public static DialogueAudio Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<DialogueAudio>();
                    if (instance == null)
                    {
                        GameObject obj = new GameObject("DialogueAudio");
                        instance = obj.AddComponent<DialogueAudio>();
                        DontDestroyOnLoad(obj);
                    }
                }
                return instance;
            }
        }
        #endregion

        #region Serialized Fields
        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceSource;
        [SerializeField] private AudioSource backgroundMusicSource;
        [SerializeField] private AudioSource soundEffectSource;

        [Header("Default Audio Clips")]
        [SerializeField] private AudioClip defaultTypingSound;
        [SerializeField] private AudioClip dialogueStartSound;
        [SerializeField] private AudioClip dialogueEndSound;
        [SerializeField] private AudioClip choiceSelectSound;
        [SerializeField] private AudioClip choiceHoverSound;

        [Header("Audio Settings")]
        [SerializeField] private float voiceVolume = 1f;
        [SerializeField] private float musicVolume = 0.5f;
        [SerializeField] private float sfxVolume = 0.8f;
        [SerializeField] private bool playTypingSounds = true;
        [SerializeField] private float typingSoundInterval = 0.1f;

        [Header("Background Music")]
        [SerializeField] private AudioClip defaultBackgroundMusic;
        [SerializeField] private bool fadeMusicOnDialogue = true;
        [SerializeField] private float musicFadeDuration = 1f;
        #endregion

        #region Private Fields
        private DialogueManager dialogueManager;
        private Coroutine typingSoundCoroutine;
        private AudioClip previousBackgroundMusic;
        private float previousMusicVolume;
        private bool isDialogueActive = false;
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

            InitializeAudioSources();
        }

        private void Start()
        {
            dialogueManager = DialogueManager.Instance;
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        #endregion

        #region Initialization
        private void InitializeAudioSources()
        {
            // Create voice source
            if (voiceSource == null)
            {
                voiceSource = gameObject.AddComponent<AudioSource>();
                voiceSource.volume = voiceVolume;
                voiceSource.playOnAwake = false;
            }

            // Create background music source
            if (backgroundMusicSource == null)
            {
                backgroundMusicSource = gameObject.AddComponent<AudioSource>();
                backgroundMusicSource.volume = musicVolume;
                backgroundMusicSource.loop = true;
                backgroundMusicSource.playOnAwake = false;
            }

            // Create sound effect source
            if (soundEffectSource == null)
            {
                soundEffectSource = gameObject.AddComponent<AudioSource>();
                soundEffectSource.volume = sfxVolume;
                soundEffectSource.playOnAwake = false;
            }

            Debug.Log("🔊 DialogueAudio initialized successfully!");
        }

        private void SubscribeToEvents()
        {
            if (dialogueManager != null)
            {
                dialogueManager.OnDialogueStarted += OnDialogueStarted;
                dialogueManager.OnDialogueEnded += OnDialogueEnded;
                dialogueManager.OnTypingStarted += OnTypingStarted;
                dialogueManager.OnTypingEnded += OnTypingEnded;
                dialogueManager.OnLineDisplayed += OnLineDisplayed;
                dialogueManager.OnChoiceSelected += OnChoiceSelected;
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
                dialogueManager.OnLineDisplayed -= OnLineDisplayed;
                dialogueManager.OnChoiceSelected -= OnChoiceSelected;
            }
        }
        #endregion

        #region Event Handlers
        private void OnDialogueStarted()
        {
            isDialogueActive = true;
            PlayDialogueStartSound();

            if (fadeMusicOnDialogue)
            {
                HandleBackgroundMusicTransition();
            }
        }

        private void OnDialogueEnded()
        {
            isDialogueActive = false;
            PlayDialogueEndSound();
            StopTypingSounds();

            if (fadeMusicOnDialogue)
            {
                RestoreBackgroundMusic();
            }
        }

        private void OnTypingStarted()
        {
            if (playTypingSounds)
            {
                StartTypingSounds();
            }
        }

        private void OnTypingEnded()
        {
            StopTypingSounds();
        }

        private void OnLineDisplayed(DialogueLine line)
        {
            PlayVoiceClip(line.VoiceClip);
            PlayBackgroundMusic(line.BackgroundMusic);
        }

        private void OnChoiceSelected(int choiceIndex)
        {
            PlayChoiceSelectSound();
        }
        #endregion

        #region Voice Audio
        /// <summary>
        /// Phát voice clip
        /// </summary>
        public void PlayVoiceClip(AudioClip clip)
        {
            if (clip != null && voiceSource != null)
            {
                voiceSource.clip = clip;
                voiceSource.Play();
            }
        }

        /// <summary>
        /// Dừng voice clip
        /// </summary>
        public void StopVoiceClip()
        {
            if (voiceSource != null)
            {
                voiceSource.Stop();
            }
        }

        /// <summary>
        /// Kiểm tra voice clip có đang phát không
        /// </summary>
        public bool IsVoicePlaying()
        {
            return voiceSource != null && voiceSource.isPlaying;
        }
        #endregion

        #region Background Music
        /// <summary>
        /// Phát background music
        /// </summary>
        public void PlayBackgroundMusic(AudioClip clip)
        {
            if (clip != null && backgroundMusicSource != null)
            {
                if (backgroundMusicSource.clip != clip)
                {
                    backgroundMusicSource.clip = clip;
                    backgroundMusicSource.Play();
                }
            }
        }

        /// <summary>
        /// Dừng background music
        /// </summary>
        public void StopBackgroundMusic()
        {
            if (backgroundMusicSource != null)
            {
                backgroundMusicSource.Stop();
            }
        }

        /// <summary>
        /// Fade background music
        /// </summary>
        public void FadeBackgroundMusic(float targetVolume, float duration)
        {
            if (backgroundMusicSource != null)
            {
                StartCoroutine(FadeMusic(targetVolume, duration));
            }
        }

        private void HandleBackgroundMusicTransition()
        {
            // Store current music state
            previousBackgroundMusic = backgroundMusicSource.clip;
            previousMusicVolume = backgroundMusicSource.volume;

            // Play default dialogue music or fade out current music
            if (defaultBackgroundMusic != null)
            {
                backgroundMusicSource.clip = defaultBackgroundMusic;
                backgroundMusicSource.Play();
            }
            else
            {
                // Fade out current music
                FadeBackgroundMusic(0.1f, musicFadeDuration);
            }
        }

        private void RestoreBackgroundMusic()
        {
            if (previousBackgroundMusic != null)
            {
                backgroundMusicSource.clip = previousBackgroundMusic;
                backgroundMusicSource.Play();
                FadeBackgroundMusic(previousMusicVolume, musicFadeDuration);
            }
            else
            {
                // Fade back to normal volume
                FadeBackgroundMusic(musicVolume, musicFadeDuration);
            }
        }
        #endregion

        #region Sound Effects
        /// <summary>
        /// Phát sound effect
        /// </summary>
        public void PlaySoundEffect(AudioClip clip)
        {
            if (clip != null && soundEffectSource != null)
            {
                soundEffectSource.PlayOneShot(clip, sfxVolume);
            }
        }

        /// <summary>
        /// Phát dialogue start sound
        /// </summary>
        public void PlayDialogueStartSound()
        {
            PlaySoundEffect(dialogueStartSound);
        }

        /// <summary>
        /// Phát dialogue end sound
        /// </summary>
        public void PlayDialogueEndSound()
        {
            PlaySoundEffect(dialogueEndSound);
        }

        /// <summary>
        /// Phát choice select sound
        /// </summary>
        public void PlayChoiceSelectSound()
        {
            PlaySoundEffect(choiceSelectSound);
        }

        /// <summary>
        /// Phát choice hover sound
        /// </summary>
        public void PlayChoiceHoverSound()
        {
            PlaySoundEffect(choiceHoverSound);
        }
        #endregion

        #region Typing Sounds
        /// <summary>
        /// Bắt đầu phát âm thanh typing
        /// </summary>
        public void StartTypingSounds()
        {
            if (typingSoundCoroutine != null)
            {
                StopCoroutine(typingSoundCoroutine);
            }

            if (defaultTypingSound != null)
            {
                typingSoundCoroutine = StartCoroutine(PlayTypingSounds());
            }
        }

        /// <summary>
        /// Dừng phát âm thanh typing
        /// </summary>
        public void StopTypingSounds()
        {
            if (typingSoundCoroutine != null)
            {
                StopCoroutine(typingSoundCoroutine);
                typingSoundCoroutine = null;
            }
        }

        private IEnumerator PlayTypingSounds()
        {
            while (true)
            {
                if (soundEffectSource != null && defaultTypingSound != null)
                {
                    soundEffectSource.PlayOneShot(defaultTypingSound, sfxVolume * 0.3f);
                }

                yield return new WaitForSeconds(typingSoundInterval);
            }
        }
        #endregion

        #region Volume Controls
        /// <summary>
        /// Thiết lập volume voice
        /// </summary>
        public void SetVoiceVolume(float volume)
        {
            voiceVolume = Mathf.Clamp01(volume);
            if (voiceSource != null)
            {
                voiceSource.volume = voiceVolume;
            }
        }

        /// <summary>
        /// Thiết lập volume music
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (backgroundMusicSource != null)
            {
                backgroundMusicSource.volume = musicVolume;
            }
        }

        /// <summary>
        /// Thiết lập volume SFX
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            if (soundEffectSource != null)
            {
                soundEffectSource.volume = sfxVolume;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Bật/tắt typing sounds
        /// </summary>
        public void EnableTypingSounds(bool enable)
        {
            playTypingSounds = enable;
            if (!enable)
            {
                StopTypingSounds();
            }
        }

        /// <summary>
        /// Bật/tắt music fade
        /// </summary>
        public void EnableMusicFade(bool enable)
        {
            fadeMusicOnDialogue = enable;
        }

        /// <summary>
        /// Kiểm tra dialogue có đang active không
        /// </summary>
        public bool IsDialogueActive()
        {
            return isDialogueActive;
        }

        /// <summary>
        /// Lấy voice source
        /// </summary>
        public AudioSource GetVoiceSource()
        {
            return voiceSource;
        }

        /// <summary>
        /// Lấy background music source
        /// </summary>
        public AudioSource GetBackgroundMusicSource()
        {
            return backgroundMusicSource;
        }

        /// <summary>
        /// Lấy sound effect source
        /// </summary>
        public AudioSource GetSoundEffectSource()
        {
            return soundEffectSource;
        }
        #endregion

        #region Private Methods
        private IEnumerator FadeMusic(float targetVolume, float duration)
        {
            if (backgroundMusicSource == null)
                yield break;

            float startVolume = backgroundMusicSource.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                backgroundMusicSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
                yield return null;
            }

            backgroundMusicSource.volume = targetVolume;
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// Log thông tin audio settings
        /// </summary>
        public void LogAudioSettings()
        {
            Debug.Log("🔊 Dialogue Audio Settings:");
            Debug.Log($"- Voice Volume: {voiceVolume}");
            Debug.Log($"- Music Volume: {musicVolume}");
            Debug.Log($"- SFX Volume: {sfxVolume}");
            Debug.Log($"- Play Typing Sounds: {playTypingSounds}");
            Debug.Log($"- Typing Sound Interval: {typingSoundInterval}s");
            Debug.Log($"- Fade Music: {fadeMusicOnDialogue}");
            Debug.Log($"- Music Fade Duration: {musicFadeDuration}s");
        }

        /// <summary>
        /// Test audio system
        /// </summary>
        public void TestAudio()
        {
            Debug.Log("🔊 Testing Dialogue Audio System...");

            PlayDialogueStartSound();
            StartCoroutine(TestAudioCoroutine());
        }

        private IEnumerator TestAudioCoroutine()
        {
            yield return new WaitForSeconds(0.5f);
            PlayChoiceSelectSound();
            yield return new WaitForSeconds(0.5f);
            PlayDialogueEndSound();

            Debug.Log("✅ Audio Test Complete!");
        }
        #endregion
    }
}
