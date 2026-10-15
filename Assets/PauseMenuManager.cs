using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;   // ← New Input System
using System.Collections;

/// <summary>
/// Quản lý Pause Menu trong Scene game.
/// Setup:
///   1. Trong SampleScene, tạo Canvas (Screen Space - Overlay, Sort Order 10).
///   2. Bên trong Canvas tạo một Panel đặt tên "PausePanel" → chứa các nút.
///   3. Tạo GameObject rỗng "PauseMenuManager", gán script này vào.
///   4. Kéo PausePanel vào field pausePanel trong Inspector.
///   5. Script tự lắng nghe phím ESC.
/// </summary>
public class PauseMenuManager : MonoBehaviour
{
    [Header("=== UI ===")]
    [Tooltip("Panel Pause (chứa các nút Resume / Main Menu / Quit)")]
    [SerializeField] private GameObject pausePanel;

    [Tooltip("CanvasGroup của PausePanel để fade in/out (thêm component CanvasGroup vào PausePanel)")]
    [SerializeField] private CanvasGroup pauseCanvasGroup;

    [Tooltip("Chữ báo kết quả lưu game hiện trong Pause Menu (không bắt buộc)")]
    [SerializeField] private TMPro.TextMeshProUGUI saveStatusText;

    [Header("=== SCENE ===")]
    [Tooltip("Tên Scene Main Menu")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("=== HIỆU ỨNG ===")]
    [Tooltip("Thời gian fade pause panel (giây)")]
    [SerializeField] private float fadeDuration = 0.2f;

    [Header("=== AUDIO ===")]
    [Tooltip("AudioSource nhạc nền trong game — sẽ giảm âm khi pause")]
    [SerializeField] private AudioSource gameBGM;

    [Tooltip("Mức âm lượng giảm còn bao nhiêu % khi pause (0–1)")]
    [SerializeField] [Range(0f, 1f)] private float pausedBGMVolume = 0.2f;

    // ──────────────────────────────────────────────
    private bool  isPaused       = false;
    private float originalVolume = 1f;
    private Coroutine fadeCoroutine;

    // ──────────────────────────────────────────────
    void Awake()
    {
        // Đảm bảo panel ẩn khi bắt đầu
        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (gameBGM != null)
            originalVolume = gameBGM.volume;
    }

    void Update()
    {
        // Dùng New Input System (Keyboard.current) thay cho Input.GetKeyDown cũ
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused) ResumeGame();
            else          PauseGame();
        }
    }

    // ──────────────────────────────────────────────
    //  Public — gán OnClick() cho các nút trong Inspector
    // ──────────────────────────────────────────────

    /// <summary>Gán cho nút RESUME</summary>
    public void ResumeGame()
    {
        if (!isPaused) return;

        isPaused = false;

        // Fade out panel rồi ẩn
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeOutAndHide());

        // Khôi phục thời gian & cursor
        Time.timeScale   = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        // Phục hồi âm lượng nhạc nền
        if (gameBGM != null) gameBGM.volume = originalVolume;
    }

    /// <summary>Gán cho nút LƯU GAME</summary>
    public void SaveGame()
    {
        string msg = SaveSystem.Save(out string reason) ? "Đã lưu game" : "Không lưu được: " + reason;
        if (QuestUI.Instance != null) QuestUI.Instance.SetNotificationText(msg);
        if (saveStatusText != null) saveStatusText.text = msg;
    }

    /// <summary>Gán cho nút MAIN MENU</summary>
    public void GoToMainMenu()
    {
        Time.timeScale = 1f; // quan trọng: phải reset trước khi load scene
        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>Gán cho nút QUIT</summary>
    public void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ──────────────────────────────────────────────
    //  Internal
    // ──────────────────────────────────────────────

    private void PauseGame()
    {
        isPaused = true;
        if (saveStatusText != null) saveStatusText.text = "";

        // Hiện panel
        pausePanel?.SetActive(true);
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeIn());

        // Dừng thời gian & hiện cursor
        Time.timeScale   = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        // Giảm nhạc nền
        if (gameBGM != null) gameBGM.volume = originalVolume * 0.2f;
    }

    private IEnumerator FadeIn()
    {
        if (pauseCanvasGroup == null) yield break;
        pauseCanvasGroup.alpha = 0f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            pauseCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }
        pauseCanvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOutAndHide()
    {
        if (pauseCanvasGroup == null)
        {
            pausePanel?.SetActive(false);
            yield break;
        }

        float startAlpha = pauseCanvasGroup.alpha;
        float elapsed    = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            pauseCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDuration);
            yield return null;
        }
        pauseCanvasGroup.alpha = 0f;
        pausePanel?.SetActive(false);
    }

    // ──────────────────────────────────────────────
    //  Gọi từ bên ngoài (VD: DialogueManager muốn biết đang pause không)
    // ──────────────────────────────────────────────
    public bool IsPaused => isPaused;
}
