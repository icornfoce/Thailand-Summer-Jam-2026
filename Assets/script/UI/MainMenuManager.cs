using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;

public class MainMenuManager : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // Inspector Slots — Drag your UI objects into these slots
    // ─────────────────────────────────────────────────────────

    [Header("Panels")]
    public GameObject settingsPanel;        // Drag SettingsPanel here
    public GameObject confirmQuitPanel;     // (Optional) Drag ConfirmQuitPanel here

    [Header("Scene to Load")]
    public string gameSceneName = "GameScene";  // Must match your scene file name exactly

    [Header("Fade Transition (Optional)")]
    public CanvasGroup fadeCanvasGroup;     // (Optional) Full-screen black image with CanvasGroup
    public float fadeDuration = 1f;        // Duration of the fade-to-black effect

    [Header("Settings — Audio")]
    public AudioMixer audioMixer;          // (Optional) Drag your AudioMixer here
    public Slider masterVolumeSlider;      // (Optional) Drag master volume Slider here
    public Slider musicVolumeSlider;       // (Optional) Drag music volume Slider here
    public Slider sfxVolumeSlider;         // (Optional) Drag SFX volume Slider here

    [Header("Settings — Display")]
    public Toggle fullscreenToggle;        // (Optional) Drag fullscreen Toggle here
    public Dropdown resolutionDropdown;    // (Optional) Drag resolution Dropdown here

    // ─────────────────────────────────────────────────────────
    // Private fields
    // ─────────────────────────────────────────────────────────
    private Resolution[] resolutions;

    // ─────────────────────────────────────────────────────────
    // Start() — Initialize everything
    // ─────────────────────────────────────────────────────────
    void Start()
    {
        // Hide panels at start
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (confirmQuitPanel != null)
            confirmQuitPanel.SetActive(false);

        // Fade starts fully black, then fades in
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 1f;
            StartCoroutine(FadeIn());
        }

        // ── Setup volume sliders ──
        SetupVolumeSlider(masterVolumeSlider, "MasterVolume");
        SetupVolumeSlider(musicVolumeSlider, "MusicVolume");
        SetupVolumeSlider(sfxVolumeSlider, "SFXVolume");

        // ── Setup fullscreen toggle ──
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        // ── Setup resolution dropdown ──
        SetupResolutionDropdown();
    }

    // ═════════════════════════════════════════════════════════
    //  PLAY BUTTON
    // ═════════════════════════════════════════════════════════

    /// <summary>
    /// Called when the player clicks the Play / Start Game button.
    /// If a fade CanvasGroup is assigned, fades to black first.
    /// </summary>
    public void OnStartGame()
    {
        Debug.Log("Starting game...");

        if (fadeCanvasGroup != null)
        {
            StartCoroutine(FadeOutAndLoadScene());
        }
        else
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    // ═════════════════════════════════════════════════════════
    //  SETTINGS BUTTON
    // ═════════════════════════════════════════════════════════

    /// <summary>
    /// Toggles the settings panel open/closed.
    /// </summary>
    public void OnToggleSettings()
    {
        if (settingsPanel == null) return;

        bool isActive = settingsPanel.activeSelf;
        settingsPanel.SetActive(!isActive);

        Debug.Log("Settings panel: " + (!isActive ? "Opened" : "Closed"));
    }

    /// <summary>
    /// Opens the settings panel.
    /// </summary>
    public void OnOpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    /// <summary>
    /// Closes the settings panel.
    /// </summary>
    public void OnCloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    // ═══════════════════════════════════════════════════════
    //  SETTINGS — AUDIO
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// Sets the Master volume. Hook this to the master volume slider's OnValueChanged,
    /// or it will be hooked automatically in Start().
    /// Slider range should be 0.0001 to 1.
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        SetMixerVolume("MasterVolume", volume);
    }

    /// <summary>
    /// Sets the Music volume.
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        SetMixerVolume("MusicVolume", volume);
    }

    /// <summary>
    /// Sets the SFX volume.
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        SetMixerVolume("SFXVolume", volume);
    }

    // ═══════════════════════════════════════════════════════
    //  SETTINGS — DISPLAY
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// Sets fullscreen mode on/off.
    /// </summary>
    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        Debug.Log("Fullscreen: " + isFullscreen);
    }

    /// <summary>
    /// Sets the resolution based on dropdown index.
    /// </summary>
    public void SetResolution(int resolutionIndex)
    {
        if (resolutions == null || resolutionIndex < 0 || resolutionIndex >= resolutions.Length)
            return;

        Resolution res = resolutions[resolutionIndex];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
        Debug.Log("Resolution set to: " + res.width + " x " + res.height);
    }

    // ═════════════════════════════════════════════════════════
    //  QUIT BUTTON
    // ═════════════════════════════════════════════════════════

    /// <summary>
    /// Called when the player clicks the Quit button.
    /// If a confirmQuitPanel is assigned, shows it first.
    /// Otherwise quits immediately.
    /// </summary>
    public void OnExitGame()
    {
        if (confirmQuitPanel != null)
        {
            confirmQuitPanel.SetActive(true);
            Debug.Log("Showing quit confirmation...");
        }
        else
        {
            QuitGame();
        }
    }

    /// <summary>
    /// Called when the player confirms they want to quit (from the confirmation dialog).
    /// </summary>
    public void OnConfirmQuit()
    {
        QuitGame();
    }

    /// <summary>
    /// Called when the player cancels quitting (from the confirmation dialog).
    /// </summary>
    public void OnCancelQuit()
    {
        if (confirmQuitPanel != null)
            confirmQuitPanel.SetActive(false);
    }

    // ═════════════════════════════════════════════════════════
    //  PRIVATE HELPERS
    // ═════════════════════════════════════════════════════════

    /// <summary>
    /// Actually quits the application (handles Editor vs Build).
    /// </summary>
    private void QuitGame()
    {
        Debug.Log("Exiting game...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// Converts a linear slider value (0.0001–1) to decibels and sets it on the AudioMixer.
    /// </summary>
    private void SetMixerVolume(string parameterName, float linearValue)
    {
        if (audioMixer == null) return;

        // Convert linear (0.0001 to 1) → decibels (-80 to 0)
        float dB = Mathf.Log10(Mathf.Max(linearValue, 0.0001f)) * 20f;
        audioMixer.SetFloat(parameterName, dB);
    }

    /// <summary>
    /// Hooks up a volume slider with saved PlayerPrefs value.
    /// </summary>
    private void SetupVolumeSlider(Slider slider, string parameterName)
    {
        if (slider == null || audioMixer == null) return;

        // Load saved volume or default to 0.75
        float savedVolume = PlayerPrefs.GetFloat(parameterName, 0.75f);
        slider.value = savedVolume;
        SetMixerVolume(parameterName, savedVolume);

        // Listen for changes and save
        slider.onValueChanged.AddListener((value) =>
        {
            SetMixerVolume(parameterName, value);
            PlayerPrefs.SetFloat(parameterName, value);
        });
    }

    /// <summary>
    /// Populates the resolution dropdown with available screen resolutions.
    /// </summary>
    private void SetupResolutionDropdown()
    {
        if (resolutionDropdown == null) return;

        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height
                          + " @ " + resolutions[i].refreshRateRatio + "Hz";
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }

    // ═════════════════════════════════════════════════════════
    //  FADE TRANSITIONS
    // ═════════════════════════════════════════════════════════

    /// <summary>
    /// Fades from black to transparent when the menu loads.
    /// </summary>
    private IEnumerator FadeIn()
    {
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = 1f - (timer / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
    }

    /// <summary>
    /// Fades to black, then loads the game scene.
    /// </summary>
    private IEnumerator FadeOutAndLoadScene()
    {
        fadeCanvasGroup.blocksRaycasts = true;
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = timer / fadeDuration;
            yield return null;
        }
        fadeCanvasGroup.alpha = 1f;

        SceneManager.LoadScene(gameSceneName);
    }
}