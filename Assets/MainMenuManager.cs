using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [Header("=== PANELS ===")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject chapterPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("=== TIEP TUC (SAVE) ===")]
    [Tooltip("Nút Tiếp tục — tự mờ đi khi chưa có file save")]
    [SerializeField] private Button continueButton;
    [Tooltip("Chữ hiện thời gian lưu gần nhất (không bắt buộc)")]
    [SerializeField] private TMP_Text continueInfoText;

    [Header("=== SCENES ===")]
    [SerializeField] private string[] chapterSceneNames = { "SampleScene", "Chapter2", "Chapter3" };

    [Header("=== HIEU UNG ===")]
    [SerializeField] private CanvasGroup rootCanvasGroup;
    [SerializeField] private float fadeInDuration  = 1.2f;
    [SerializeField] private float fadeOutDuration = 0.8f;

    [Header("=== NHAC NEN ===")]
    [SerializeField] private AudioSource bgmSource;

    [Header("=== SETTINGS: AUDIO ===")]
    [SerializeField] private AudioMixer mixer; // MainMixer (MasterVol / MusicVol / SFXVol)
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("=== SETTINGS: GRAPHICS ===")]
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle       fullscreenToggle;
    Resolution[] resolutions;
    bool isLoading = false; // chặn event khi đang load settings

    [Header("=== SETTINGS: VISUAL ===")]
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private Toggle  vsyncToggle;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = 0f;
            StartCoroutine(FadeCanvas(rootCanvasGroup, 0f, 1f, fadeInDuration));
        }
        LoadSettings();
        InitResolutions();
        RefreshContinueButton();
        ShowPanel(mainPanel);
        if (bgmSource != null && !bgmSource.isPlaying) bgmSource.Play();
    }

    // MAIN PANEL
    public void OnNewGameButton()
    {
        SaveSystem.LoadOnNextScene = false;
        StartCoroutine(LoadSceneCoroutine(chapterSceneNames[0]));
    }

    public void OnContinueButton()
    {
        SaveData d = SaveSystem.ReadSave();
        if (d == null || string.IsNullOrEmpty(d.scene)) { RefreshContinueButton(); return; }
        SaveSystem.LoadOnNextScene = true;
        StartCoroutine(LoadSceneCoroutine(d.scene));
    }

    void RefreshContinueButton()
    {
        SaveData d = SaveSystem.ReadSave();
        if (continueButton != null) continueButton.interactable = d != null;
        if (continueInfoText != null) continueInfoText.text = d != null ? "Lưu lúc " + d.savedAt : "";
    }
    public void OnChapterSelectButton()  => ShowPanel(chapterPanel);
    public void OnSettingsButton()       => ShowPanel(settingsPanel);
    public void OnQuitButton()           => StartCoroutine(QuitCoroutine());

    // CHAPTER PANEL
    public void OnChapterButton(int chapterIndex)
    {
        if (chapterIndex < 0 || chapterIndex >= chapterSceneNames.Length)
        {
            Debug.LogWarning("[MainMenu] Chapter " + (chapterIndex + 1) + " chua co scene!");
            return;
        }
        StartCoroutine(LoadSceneCoroutine(chapterSceneNames[chapterIndex]));
    }

    // SETTINGS - AUDIO
    public void OnMasterVolumeChanged(float val) { if (isLoading) return; AudioInit.SetMixerVolume(mixer, "MasterVol", val); PlayerPrefs.SetFloat("MasterVolume", val); }
    public void OnMusicVolumeChanged(float val)  { if (isLoading) return; AudioInit.SetMixerVolume(mixer, "MusicVol",  val); PlayerPrefs.SetFloat("MusicVolume",  val); }
    public void OnSFXVolumeChanged(float val)    { if (isLoading) return; AudioInit.SetMixerVolume(mixer, "SFXVol",    val); PlayerPrefs.SetFloat("SFXVolume",    val); }

    // SETTINGS - GRAPHICS
    public void OnQualityChanged(int index)      { if (isLoading) return; QualitySettings.SetQualityLevel(index); PlayerPrefs.SetInt("Quality", index); }
    public void OnResolutionChanged(int index)
    {
        if (isLoading) return;
        if (resolutions == null || index >= resolutions.Length) return;
        Resolution r = resolutions[index];
        Screen.SetResolution(r.width, r.height, Screen.fullScreen);
        PlayerPrefs.SetInt("Resolution", index);
    }
    public void OnFullscreenChanged(bool fs)     { if (isLoading) return; Screen.fullScreen = fs; PlayerPrefs.SetInt("Fullscreen", fs ? 1 : 0); }

    // SETTINGS - VISUAL
    public void OnBrightnessChanged(float val)   { if (isLoading) return; PlayerPrefs.SetFloat("Brightness", val); }
    public void OnVSyncChanged(bool enabled)     { if (isLoading) return; QualitySettings.vSyncCount = enabled ? 1 : 0; PlayerPrefs.SetInt("VSync", enabled ? 1 : 0); }

    // BACK
    public void OnBackButton() { SaveSettings(); ShowPanel(mainPanel); }

    // HELPERS
    void ShowPanel(GameObject target)
    {
        mainPanel?.SetActive(target == mainPanel);
        chapterPanel?.SetActive(target == chapterPanel);
        settingsPanel?.SetActive(target == settingsPanel);
    }

    void InitResolutions()
    {
        if (resolutionDropdown == null) return;
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();
        int currentIndex = 0;
        var options = new System.Collections.Generic.List<string>();
        for (int i = 0; i < resolutions.Length; i++)
        {
            options.Add(resolutions[i].width + " x " + resolutions[i].height);
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
                currentIndex = i;
        }
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = PlayerPrefs.GetInt("Resolution", currentIndex);
        resolutionDropdown.RefreshShownValue();
    }

    void LoadSettings()
    {
        isLoading = true; // chặn events
        float master = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float music  = PlayerPrefs.GetFloat("MusicVolume",  0.8f);
        float sfx    = PlayerPrefs.GetFloat("SFXVolume",    1f);
        float bright = PlayerPrefs.GetFloat("Brightness",   0.5f);
        int   qual   = PlayerPrefs.GetInt("Quality", QualitySettings.GetQualityLevel());
        bool  fs     = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
        bool  vsync  = PlayerPrefs.GetInt("VSync", QualitySettings.vSyncCount > 0 ? 1 : 0) == 1;

        AudioInit.SetMixerVolume(mixer, "MasterVol", master);
        AudioInit.SetMixerVolume(mixer, "MusicVol",  music);
        AudioInit.SetMixerVolume(mixer, "SFXVol",    sfx);
        QualitySettings.SetQualityLevel(qual);
        Screen.fullScreen          = fs;
        QualitySettings.vSyncCount = vsync ? 1 : 0;

        if (masterVolumeSlider != null)  masterVolumeSlider.value  = master;
        if (musicVolumeSlider  != null)  musicVolumeSlider.value   = music;
        if (sfxVolumeSlider    != null)  sfxVolumeSlider.value     = sfx;
        if (brightnessSlider   != null)  brightnessSlider.value    = bright;
        if (qualityDropdown    != null)  qualityDropdown.value     = qual;
        if (fullscreenToggle   != null)  fullscreenToggle.isOn     = fs;
        if (vsyncToggle        != null)  vsyncToggle.isOn          = vsync;
        isLoading = false; // mở lại events
    }

    void SaveSettings() => PlayerPrefs.Save();

    IEnumerator LoadSceneCoroutine(string sceneName)
    {
        if (rootCanvasGroup != null)
            yield return StartCoroutine(FadeCanvas(rootCanvasGroup, 1f, 0f, fadeOutDuration));
        if (bgmSource != null)
            yield return StartCoroutine(FadeBGM(bgmSource, fadeOutDuration * 0.5f));
        SceneManager.LoadScene(sceneName);
    }

    IEnumerator QuitCoroutine()
    {
        if (rootCanvasGroup != null)
            yield return StartCoroutine(FadeCanvas(rootCanvasGroup, 1f, 0f, fadeOutDuration * 0.5f));
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    static IEnumerator FadeCanvas(CanvasGroup cg, float from, float to, float duration)
    {
        float elapsed = 0f; cg.alpha = from;
        while (elapsed < duration) { elapsed += Time.unscaledDeltaTime; cg.alpha = Mathf.Lerp(from, to, elapsed / duration); yield return null; }
        cg.alpha = to;
    }

    static IEnumerator FadeBGM(AudioSource src, float duration)
    {
        float start = src.volume, elapsed = 0f;
        while (elapsed < duration) { elapsed += Time.unscaledDeltaTime; src.volume = Mathf.Lerp(start, 0f, elapsed / duration); yield return null; }
        src.Stop();
    }
}
