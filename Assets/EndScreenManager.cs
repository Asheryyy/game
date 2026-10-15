using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Quản lý màn hình kết thúc (Bad End / Good End).
///
/// SETUP trong SampleScene:
///   1. Tạo Canvas tên "EndCanvas", Sort Order = 20 (cao hơn PauseCanvas).
///   2. Thêm CanvasGroup vào EndCanvas.
///   3. Bên trong EndCanvas tạo:
///        - Panel đen tên "EndPanel" (Image màu đen, alpha = 255)
///            ├── Text (TMP) tên "EndTitleText"   → tiêu đề lớn (VD: "BAD END")
///            ├── Text (TMP) tên "EndBodyText"    → đoạn văn kết thúc
///            └── Button tên "RestartButton"      → text "CHƠI LẠI"
///                 Button tên "MainMenuButton"    → text "MENU CHÍNH"
///   4. Tạo GameObject rỗng "EndScreenManager", gán script này.
///   5. Kéo các field vào Inspector.
///   6. Gán OnClick():
///        RestartButton  → EndScreenManager.OnRestartButton
///        MainMenuButton → EndScreenManager.OnMainMenuButton
///
/// GỌI TỪ CODE KHÁC:
///   EndScreenManager.Instance.ShowBadEnd("Nhân vật chính bị bắt...");
///   EndScreenManager.Instance.ShowGoodEnd("Sự thật đã được phơi bày...");
/// </summary>
public class EndScreenManager : MonoBehaviour
{
    public static EndScreenManager Instance { get; private set; }

    [Header("=== UI ===")]
    [SerializeField] private CanvasGroup endCanvasGroup;
    [SerializeField] private GameObject  endPanel;
    [SerializeField] private TMP_Text    endTitleText;
    [SerializeField] private TMP_Text    endBodyText;

    [Header("=== NỘI DUNG MẶC ĐỊNH ===")]
    [TextArea(3, 6)]
    [SerializeField] private string defaultBadEndTitle  = "BAD END";
    [TextArea(3, 6)]
    [SerializeField] private string defaultBadEndBody   =
        "Nhân vật chính đã bị phát hiện và bắt đi.\n\nBí mật của chung cư vẫn còn chìm trong bóng tối...";

    [TextArea(3, 6)]
    [SerializeField] private string defaultGoodEndTitle = "GOOD END";
    [TextArea(3, 6)]
    [SerializeField] private string defaultGoodEndBody  =
        "Đường dây buôn người đã bị triệt phá.\nLinh hồn con ma cuối cùng cũng được siêu thoát.\n\nSự thật đã được phơi bày.";

    [Header("=== MÀU SẮC ===")]
    [SerializeField] private Color badEndTitleColor  = new Color(0.85f, 0.15f, 0.15f); // đỏ tối
    [SerializeField] private Color goodEndTitleColor = new Color(0.9f,  0.85f, 0.6f);  // vàng nhạt

    [Header("=== HIỆU ỨNG ===")]
    [Tooltip("Thời gian fade in màn đen (giây)")]
    [SerializeField] private float fadeInDuration   = 1.5f;
    [Tooltip("Giây chờ sau khi fade xong trước khi hiện text")]
    [SerializeField] private float textDelay        = 0.5f;
    [Tooltip("Thời gian fade in text (giây)")]
    [SerializeField] private float textFadeDuration = 1.0f;

    [Header("=== SCENE ===")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string gameSceneName     = "SampleScene";

    [Header("=== AUDIO ===")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioClip   badEndMusic;
    [SerializeField] private AudioClip   goodEndMusic;

    // ──────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Ẩn màn hình kết thúc khi bắt đầu
        if (endCanvasGroup != null) endCanvasGroup.alpha = 0f;
        endPanel?.SetActive(false);
    }

    // ──────────────────────────────────────────────
    //  API công khai — gọi từ bất kỳ script nào
    // ──────────────────────────────────────────────

    /// <summary>
    /// Hiện Bad End. Truyền chuỗi rỗng để dùng nội dung mặc định.
    /// Ví dụ: EndScreenManager.Instance.ShowBadEnd("Bạn đã bị bắt...");
    /// </summary>
    public void ShowBadEnd(string bodyText = "")
    {
        string title = defaultBadEndTitle;
        string body  = string.IsNullOrEmpty(bodyText) ? defaultBadEndBody : bodyText;
        StartCoroutine(ShowEndScreen(title, body, badEndTitleColor, badEndMusic));
    }

    /// <summary>
    /// Hiện Good End. Truyền chuỗi rỗng để dùng nội dung mặc định.
    /// Ví dụ: EndScreenManager.Instance.ShowGoodEnd();
    /// </summary>
    public void ShowGoodEnd(string bodyText = "")
    {
        string title = defaultGoodEndTitle;
        string body  = string.IsNullOrEmpty(bodyText) ? defaultGoodEndBody : bodyText;
        StartCoroutine(ShowEndScreen(title, body, goodEndTitleColor, goodEndMusic));
    }

    // ──────────────────────────────────────────────
    //  Nút bấm
    // ──────────────────────────────────────────────

    public void OnRestartButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnMainMenuButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // ──────────────────────────────────────────────
    //  Coroutine chính
    // ──────────────────────────────────────────────

    private IEnumerator ShowEndScreen(string title, string body, Color titleColor, AudioClip music)
    {
        // dọn UI trong game (ô nhiệm vụ, chữ "BẤM LIÊN TỤC [E]...") để không đè lên màn hình kết thúc
        if (QuestUI.Instance != null) QuestUI.Instance.HideAll();
        SaveSystem.DeleteSave(); // chơi xong → xóa save, lần sau "Tiếp tục" không vào lại đoạn cuối
        // Khoá input người chơi
        Time.timeScale   = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        // Hiện panel, ẩn text trước
        endPanel?.SetActive(true);
        if (endTitleText != null) { endTitleText.text  = title; SetTextAlpha(endTitleText, 0f); }
        if (endBodyText  != null) { endBodyText.text   = body;  SetTextAlpha(endBodyText,  0f); }
        if (endTitleText != null)   endTitleText.color = new Color(titleColor.r, titleColor.g, titleColor.b, 0f);

        // Fade nhạc nền cũ
        if (bgmSource != null && bgmSource.isPlaying)
            yield return StartCoroutine(FadeBGM(bgmSource, 0.8f));

        // Fade in màn đen
        yield return StartCoroutine(FadeCanvas(endCanvasGroup, 0f, 1f, fadeInDuration));

        yield return new WaitForSecondsRealtime(textDelay);

        // Phát nhạc kết thúc
        if (bgmSource != null && music != null)
        {
            bgmSource.clip   = music;
            bgmSource.volume = 1f;
            bgmSource.Play();
        }

        // Fade in tiêu đề
        yield return StartCoroutine(FadeTextIn(endTitleText, titleColor, textFadeDuration));

        yield return new WaitForSecondsRealtime(0.4f);

        // Fade in nội dung
        yield return StartCoroutine(FadeTextIn(endBodyText, Color.white, textFadeDuration * 1.2f));
    }

    // ──────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────

    private static IEnumerator FadeCanvas(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) yield break;
        float elapsed = 0f;
        cg.alpha = from;
        while (elapsed < duration)
        {
            elapsed  += Time.unscaledDeltaTime;
            cg.alpha  = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        cg.alpha = to;
    }

    private static IEnumerator FadeTextIn(TMP_Text txt, Color targetColor, float duration)
    {
        if (txt == null) yield break;
        float elapsed = 0f;
        Color start   = new Color(targetColor.r, targetColor.g, targetColor.b, 0f);
        txt.color     = start;
        while (elapsed < duration)
        {
            elapsed    += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            txt.color   = new Color(targetColor.r, targetColor.g, targetColor.b, alpha);
            yield return null;
        }
        txt.color = targetColor;
    }

    private static void SetTextAlpha(TMP_Text txt, float alpha)
    {
        if (txt == null) return;
        Color c = txt.color;
        txt.color = new Color(c.r, c.g, c.b, alpha);
    }

    private static IEnumerator FadeBGM(AudioSource src, float duration)
    {
        float startVol = src.volume;
        float elapsed  = 0f;
        while (elapsed < duration)
        {
            elapsed    += Time.unscaledDeltaTime;
            src.volume  = Mathf.Lerp(startVol, 0f, elapsed / duration);
            yield return null;
        }
        src.Stop();
    }
}
