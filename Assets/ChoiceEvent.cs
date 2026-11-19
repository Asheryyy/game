using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class ChoiceOption
{
    [Tooltip("Chữ trên nút (vd: Chạy xuống phòng quản lý)")]
    public string label;
    [Tooltip("Chỉ hiện lựa chọn này khi đã có manh mối này (vd: hen_giai_cuu). Để trống = luôn hiện")]
    public string requiredClueId;
    [Tooltip("Thoại sau khi chọn (để trống = không)")]
    public DialogueLine[] lines;
    [Tooltip("Gọi sau khi thoại xong (vd: EndScreenManager.ShowBadEnd, bật cutscene...)")]
    public UnityEvent onChosen;
}

/// <summary>
/// Bảng lựa chọn (tối đa 3-4 nút) — dùng cho đoạn gặp giang hồ ở ngày 4.
/// Gọi Play() (từ UnityEvent: ClueItem On Found, KnockWall...) hoặc bật Play On Enter
/// rồi gắn vào object có Collider (Is Trigger).
/// UI tự dựng, không cần làm Canvas. Bấm chuột hoặc phím 1/2/3.
/// Lựa chọn có Required Clue Id bị ẩn nếu chưa có manh mối đó.
/// </summary>
public class ChoiceEvent : MonoBehaviour, ISaveable
{
    /// Đang hiện bảng lựa chọn (PlayerMove dùng để khóa di chuyển)
    public static bool IsOpen { get; private set; }

    [TextArea(2, 4)]
    [SerializeField] private string prompt = "Phải làm gì bây giờ?";
    [SerializeField] private ChoiceOption[] options;
    [Tooltip("TRUE = Player bước vào trigger là hiện bảng")]
    [SerializeField] private bool playOnEnter = false;
    [Tooltip("Dừng thời gian lúc đang chọn (giang hồ đứng yên)")]
    [SerializeField] private bool freezeTime = true;
    [Tooltip("Font có dấu tiếng Việt. Để trống = mượn font của UI có sẵn trong scene")]
    [SerializeField] private TMP_FontAsset font;

    bool played;

    public string CaptureState() => played ? "1" : "0";
    public void RestoreState(string s) => played = s == "1";

    void OnTriggerEnter(Collider other)
    {
        if (playOnEnter && other.CompareTag("Player")) Play();
    }

    public void Play()
    {
        if (played) return;
        played = true;
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        // chờ thoại / tờ giấy đang mở đóng lại
        yield return null;
        while (DialogueManager.Instance.IsActive || (DocumentViewer.Instance != null && DocumentViewer.Instance.IsOpen))
            yield return null;

        var visible = new List<ChoiceOption>();
        foreach (ChoiceOption o in options)
            if (string.IsNullOrEmpty(o.requiredClueId) || ClueItem.HasFound(o.requiredClueId)) visible.Add(o);
        if (visible.Count == 0) yield break;

        int chosen = -1;
        GameObject ui = BuildUI(visible, i => chosen = i);
        IsOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        float oldScale = Time.timeScale;
        if (freezeTime) Time.timeScale = 0f;

        while (chosen < 0)
        {
            chosen = DigitPressed(visible.Count);
            yield return null;
        }

        Destroy(ui);
        IsOpen = false;
        if (freezeTime) Time.timeScale = oldScale;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        ChoiceOption opt = visible[chosen];
        if (opt.lines != null && opt.lines.Length > 0)
        {
            DialogueManager.Instance.StartDialogue(opt.lines);
            yield return null;
            while (DialogueManager.Instance.IsActive) yield return null;
        }

        try { opt.onChosen?.Invoke(); }
        catch (Exception e) { Debug.LogException(e, this); }
    }

    static int DigitPressed(int count)
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return -1;
        Key[] keys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4 };
        for (int i = 0; i < count && i < keys.Length; i++)
            if (kb[keys[i]].wasPressedThisFrame) return i;
        return -1;
    }

    // ──────────────────────────────────────────────
    //  Dựng UI
    // ──────────────────────────────────────────────

    GameObject BuildUI(List<ChoiceOption> visible, Action<int> onPick)
    {
        EnsureEventSystem();
        TMP_FontAsset f = font;
        if (f == null)
        {
            TextMeshProUGUI any = FindFirstObjectByType<TextMeshProUGUI>(FindObjectsInactive.Include);
            f = any != null ? any.font : TMP_Settings.defaultFontAsset;
        }

        // Canvas dưới EndCanvas (20) để màn kết thúc vẫn đè lên được
        var root = new GameObject("[ChoiceCanvas]", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 15;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        Image dim = NewChild<Image>(root.transform, "Dim");
        Stretch(dim.rectTransform);
        dim.color = new Color(0f, 0f, 0f, 0.65f);

        float buttonH = 90f, gap = 24f;
        float listH = visible.Count * buttonH + (visible.Count - 1) * gap;

        TextMeshProUGUI title = NewText(root.transform, "Prompt", prompt, f, 46);
        title.fontStyle = FontStyles.Bold;
        Place(title.rectTransform, new Vector2(0f, listH / 2f + 90f), new Vector2(1400f, 120f));

        for (int i = 0; i < visible.Count; i++)
        {
            int idx = i;
            Image bg = NewChild<Image>(root.transform, "Option" + (i + 1));
            Place(bg.rectTransform, new Vector2(0f, listH / 2f - buttonH / 2f - i * (buttonH + gap)), new Vector2(900f, buttonH));

            // nền trắng, màu thật lấy từ ColorBlock (đen → đỏ tối khi rê chuột)
            Button btn = bg.gameObject.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = cb.selectedColor = new Color(0.1f, 0.1f, 0.1f, 0.92f);
            cb.highlightedColor = new Color(0.45f, 0.08f, 0.08f, 0.95f);
            cb.pressedColor = new Color(0.6f, 0.1f, 0.1f, 1f);
            btn.colors = cb;
            btn.onClick.AddListener(() => onPick(idx));

            TextMeshProUGUI label = NewText(bg.transform, "Label", $"{i + 1}.  {visible[i].label}", f, 34);
            Stretch(label.rectTransform);
        }
        return root;
    }

    static void EnsureEventSystem()
    {
        if (EventSystem.current != null || FindFirstObjectByType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    static T NewChild<T>(Transform parent, string name) where T : Component
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.AddComponent<T>();
    }

    static TextMeshProUGUI NewText(Transform parent, string name, string text, TMP_FontAsset f, float size)
    {
        TextMeshProUGUI t = NewChild<TextMeshProUGUI>(parent, name);
        if (f != null) t.font = f;
        t.text = text;
        t.fontSize = size;
        t.color = Color.white;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return t;
    }

    static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    static void Place(RectTransform r, Vector2 pos, Vector2 size)
    {
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = size;
    }
}
