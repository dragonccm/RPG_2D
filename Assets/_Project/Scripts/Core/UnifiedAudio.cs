using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

/// <summary>
/// Unified audio system to replace multiple audio managers
/// Consolidates AudioManager, SoundManager, and music controllers
/// </summary>
public class UnifiedAudio : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource voiceSource;

    [Header("Audio Settings")]
    [SerializeField] private AudioMixer masterMixer;
    [SerializeField] private string masterVolumeParam = "MasterVolume";
    [SerializeField] private string musicVolumeParam = "MusicVolume";
    [SerializeField] private string sfxVolumeParam = "SFXVolume";
    [SerializeField] private string voiceVolumeParam = "VoiceVolume";

    [Header("Default Audio Clips")]
    [SerializeField] private AudioClip defaultBGM;
    [SerializeField] private AudioClip buttonClickSFX;
    [SerializeField] private AudioClip levelUpSFX;

    [Header("Settings")]
    [SerializeField] private bool enableDebugLogging = false;
    [SerializeField] private float fadeDuration = 1f;

    private Dictionary<string, AudioClip> audioClips = new Dictionary<string, AudioClip>();
    private Coroutine musicFadeCoroutine;

    private void Awake()
    {
        InitializeAudioSources();
        ServiceLocator.RegisterService(this);
        LoadAudioSettings();
    }

    private void InitializeAudioSources()
    {
        // Create audio sources if not assigned
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        if (voiceSource == null)
        {
            voiceSource = gameObject.AddComponent<AudioSource>();
            voiceSource.loop = false;
            voiceSource.playOnAwake = false;
        }

        // Configure audio sources
        ConfigureAudioSource(musicSource, "Music");
        ConfigureAudioSource(sfxSource, "SFX");
        ConfigureAudioSource(voiceSource, "Voice");
    }

    private void ConfigureAudioSource(AudioSource source, string type)
    {
        source.spatialBlend = 0f; // 2D audio
        source.volume = 1f;
        source.pitch = 1f;

        if (masterMixer != null)
        {
            source.outputAudioMixerGroup = masterMixer.FindMatchingGroups(type)[0];
        }
    }

    /// <summary>
    /// Play background music
    /// </summary>
    public void PlayMusic(AudioClip clip, bool fadeIn = true)
    {
        if (clip == null || musicSource == null) return;

        if (fadeIn && musicSource.isPlaying)
        {
            StartCoroutine(FadeMusic(clip));
        }
        else
        {
            musicSource.clip = clip;
            musicSource.Play();

            if (enableDebugLogging)
            {
                PerformanceUtils.Log(PerformanceUtils.FormatString("🎵 Playing music: {0}", clip.name));
            }
        }
    }

    /// <summary>
    /// Play music by name from registered clips
    /// </summary>
    public void PlayMusic(string clipName, bool fadeIn = true)
    {
        if (audioClips.TryGetValue(clipName, out AudioClip clip))
        {
            PlayMusic(clip, fadeIn);
        }
        else
        {
            PerformanceUtils.LogWarning(PerformanceUtils.FormatString("⚠️ Music clip not found: {0}", clipName));
        }
    }

    /// <summary>
    /// Stop background music
    /// </summary>
    public void StopMusic(bool fadeOut = true)
    {
        if (musicSource == null) return;

        if (fadeOut)
        {
            StartCoroutine(FadeOutMusic());
        }
        else
        {
            musicSource.Stop();
        }

        if (enableDebugLogging)
        {
            PerformanceUtils.Log("🎵 Music stopped");
        }
    }

    /// <summary>
    /// Play sound effect
    /// </summary>
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null || sfxSource == null) return;

        sfxSource.PlayOneShot(clip, volume);

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("🔊 Playing SFX: {0}", clip.name));
        }
    }

    /// <summary>
    /// Play sound effect by name
    /// </summary>
    public void PlaySFX(string clipName, float volume = 1f)
    {
        if (audioClips.TryGetValue(clipName, out AudioClip clip))
        {
            PlaySFX(clip, volume);
        }
        else
        {
            PerformanceUtils.LogWarning(PerformanceUtils.FormatString("⚠️ SFX clip not found: {0}", clipName));
        }
    }

    /// <summary>
    /// Play voice clip
    /// </summary>
    public void PlayVoice(AudioClip clip, float volume = 1f)
    {
        if (clip == null || voiceSource == null) return;

        voiceSource.PlayOneShot(clip, volume);

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("🗣️ Playing voice: {0}", clip.name));
        }
    }

    /// <summary>
    /// Play voice by name
    /// </summary>
    public void PlayVoice(string clipName, float volume = 1f)
    {
        if (audioClips.TryGetValue(clipName, out AudioClip clip))
        {
            PlayVoice(clip, volume);
        }
        else
        {
            PerformanceUtils.LogWarning(PerformanceUtils.FormatString("⚠️ Voice clip not found: {0}", clipName));
        }
    }

    /// <summary>
    /// Register audio clip for later use
    /// </summary>
    public void RegisterAudioClip(string name, AudioClip clip)
    {
        if (!audioClips.ContainsKey(name))
        {
            audioClips.Add(name, clip);

            if (enableDebugLogging)
            {
                PerformanceUtils.Log(PerformanceUtils.FormatString("📝 Registered audio clip: {0}", name));
            }
        }
    }

    /// <summary>
    /// Unregister audio clip
    /// </summary>
    public void UnregisterAudioClip(string name)
    {
        if (audioClips.Remove(name))
        {
            if (enableDebugLogging)
            {
                PerformanceUtils.Log(PerformanceUtils.FormatString("🗑️ Unregistered audio clip: {0}", name));
            }
        }
    }

    /// <summary>
    /// Set master volume (0-1)
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        SetVolume(masterVolumeParam, volume);
        PlayerPrefs.SetFloat("MasterVolume", volume);
    }

    /// <summary>
    /// Set music volume (0-1)
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        SetVolume(musicVolumeParam, volume);
        PlayerPrefs.SetFloat("MusicVolume", volume);
    }

    /// <summary>
    /// Set SFX volume (0-1)
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        SetVolume(sfxVolumeParam, volume);
        PlayerPrefs.SetFloat("SFXVolume", volume);
    }

    /// <summary>
    /// Set voice volume (0-1)
    /// </summary>
    public void SetVoiceVolume(float volume)
    {
        SetVolume(voiceVolumeParam, volume);
        PlayerPrefs.SetFloat("VoiceVolume", volume);
    }

    /// <summary>
    /// Get master volume
    /// </summary>
    public float GetMasterVolume()
    {
        return GetVolume(masterVolumeParam);
    }

    /// <summary>
    /// Get music volume
    /// </summary>
    public float GetMusicVolume()
    {
        return GetVolume(musicVolumeParam);
    }

    /// <summary>
    /// Get SFX volume
    /// </summary>
    public float GetSFXVolume()
    {
        return GetVolume(sfxVolumeParam);
    }

    /// <summary>
    /// Get voice volume
    /// </summary>
    public float GetVoiceVolume()
    {
        return GetVolume(voiceVolumeParam);
    }

    private void SetVolume(string parameter, float volume)
    {
        if (masterMixer != null)
        {
            masterMixer.SetFloat(parameter, Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20f);
        }
    }

    private float GetVolume(string parameter)
    {
        if (masterMixer != null && masterMixer.GetFloat(parameter, out float value))
        {
            return Mathf.Pow(10f, value / 20f);
        }
        return 1f;
    }

    private void LoadAudioSettings()
    {
        SetMasterVolume(PlayerPrefs.GetFloat("MasterVolume", 1f));
        SetMusicVolume(PlayerPrefs.GetFloat("MusicVolume", 1f));
        SetSFXVolume(PlayerPrefs.GetFloat("SFXVolume", 1f));
        SetVoiceVolume(PlayerPrefs.GetFloat("VoiceVolume", 1f));
    }

    /// <summary>
    /// Play default button click sound
    /// </summary>
    public void PlayButtonClick()
    {
        if (buttonClickSFX != null)
        {
            PlaySFX(buttonClickSFX);
        }
    }

    /// <summary>
    /// Play level up sound
    /// </summary>
    public void PlayLevelUp()
    {
        if (levelUpSFX != null)
        {
            PlaySFX(levelUpSFX);
        }
    }

    /// <summary>
    /// Play default background music
    /// </summary>
    public void PlayDefaultBGM()
    {
        if (defaultBGM != null)
        {
            PlayMusic(defaultBGM);
        }
    }

    /// <summary>
    /// Fade between music tracks
    /// </summary>
    private System.Collections.IEnumerator FadeMusic(AudioClip newClip)
    {
        // Fade out current music
        float startVolume = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
            yield return null;
        }

        // Switch to new music
        musicSource.clip = newClip;
        musicSource.Play();

        // Fade in new music
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, startVolume, elapsed / fadeDuration);
            yield return null;
        }
    }

    /// <summary>
    /// Fade out music
    /// </summary>
    private System.Collections.IEnumerator FadeOutMusic()
    {
        float startVolume = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
            yield return null;
        }

        musicSource.Stop();
        musicSource.volume = startVolume; // Reset volume for next play
    }

    /// <summary>
    /// Pause all audio
    /// </summary>
    public void PauseAll()
    {
        musicSource?.Pause();
        sfxSource?.Pause();
        voiceSource?.Pause();
    }

    /// <summary>
    /// Resume all audio
    /// </summary>
    public void ResumeAll()
    {
        musicSource?.UnPause();
        sfxSource?.UnPause();
        voiceSource?.UnPause();
    }

    /// <summary>
    /// Stop all audio
    /// </summary>
    public void StopAll()
    {
        musicSource?.Stop();
        sfxSource?.Stop();
        voiceSource?.Stop();
    }
}
