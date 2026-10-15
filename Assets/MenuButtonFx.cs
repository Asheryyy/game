using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Hiệu ứng nút menu kiểu tối giản: rê chuột / chọn bằng bàn phím → chữ đổi sang màu nhấn,
/// 2 icon hoa thị hiện ra 2 bên (mờ dần vào), chữ hơi phóng to. Rời đi → trở lại như cũ.
/// Gắn vào chính object Button.
/// </summary>
public class MenuButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private TMP_Text label;
    [Tooltip("2 Image hoa thị 2 bên chữ (để trống = không dùng)")]
    [SerializeField] private Graphic leftIcon;
    [SerializeField] private Graphic rightIcon;

    [Header("=== MAU (theo bảng màu game) ===")]
    [SerializeField] private Color normalColor = new Color32(0xE6, 0xE1, 0xD6, 0xFF);   // chữ chính
    [SerializeField] private Color hoverColor  = new Color32(0xB8, 0x86, 0x2B, 0xFF);   // điểm nhấn
    [SerializeField] private Color disabledColor = new Color32(0x8C, 0x88, 0x80, 0x80); // chữ phụ, mờ
    [SerializeField] private float hoverScale = 1.06f;
    [SerializeField] private float speed = 10f;

    [Tooltip("Tiếng 'tạch' khi rê chuột (Output = SFX, không bắt buộc)")]
    [SerializeField] private AudioSource hoverSound;

    Button button;
    bool highlighted;
    float t; // 0 = bình thường, 1 = đang chọn

    void Awake()
    {
        button = GetComponent<Button>();
        if (label == null) label = GetComponentInChildren<TMP_Text>();
        Apply(0f);
    }

    void OnDisable() { highlighted = false; t = 0f; Apply(0f); }

    public void OnPointerEnter(PointerEventData e) => SetHighlight(true);
    public void OnPointerExit(PointerEventData e)  => SetHighlight(false);
    public void OnSelect(BaseEventData e)          => SetHighlight(true);
    public void OnDeselect(BaseEventData e)        => SetHighlight(false);

    void SetHighlight(bool on)
    {
        if (on && !Interactable()) return;
        if (on && !highlighted && hoverSound != null) hoverSound.Play();
        highlighted = on;
    }

    bool Interactable() => button == null || button.interactable;

    void Update()
    {
        // unscaled: menu Pause vẫn chạy khi timeScale = 0
        t = Mathf.MoveTowards(t, highlighted ? 1f : 0f, speed * Time.unscaledDeltaTime);
        Apply(t);
    }

    void Apply(float k)
    {
        if (label != null)
            label.color = Interactable() ? Color.Lerp(normalColor, hoverColor, k) : disabledColor;
        transform.localScale = Vector3.one * Mathf.Lerp(1f, hoverScale, k);
        SetAlpha(leftIcon, k);
        SetAlpha(rightIcon, k);
    }

    static void SetAlpha(Graphic g, float a)
    {
        if (g == null) return;
        Color c = g.color; c.a = a; g.color = c;
    }
}
