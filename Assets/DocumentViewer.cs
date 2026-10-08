using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

/// <summary>
/// Màn hình đọc giấy tờ (hóa đơn, thư, giấy note...): tờ giấy phóng to giữa màn hình.
/// Gắn vào 1 object trong Canvas (luôn bật). Panel con tắt sẵn, script tự bật khi đọc.
/// ClueItem có "Document Text" sẽ tự gọi màn hình này trước khi hiện suy nghĩ.
/// </summary>
public class DocumentViewer : MonoBehaviour
{
    public static DocumentViewer Instance { get; private set; }

    [SerializeField] private GameObject panel;          // nền tối + tờ giấy
    [SerializeField] private TextMeshProUGUI titleText;  // tiêu đề trên tờ giấy
    [SerializeField] private TextMeshProUGUI bodyText;   // nội dung
    [SerializeField] private TextMeshProUGUI hintText;   // "Bấm [E] để đóng"
    [SerializeField] private string closeHint = "Bấm [E] để đóng";
    [Tooltip("Tiếng lật giấy khi mở (Output = SFX)")]
    [SerializeField] private AudioSource paperSound;

    public bool IsOpen { get; private set; }

    void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    /// Mở tờ giấy, chờ người chơi đọc xong bấm E (hoặc Esc) mới trả về
    public IEnumerator Read(string title, string body)
    {
        if (panel == null) yield break;

        if (titleText != null) { titleText.text = title; titleText.gameObject.SetActive(!string.IsNullOrEmpty(title)); }
        if (bodyText != null) bodyText.text = body;
        if (hintText != null) hintText.text = closeHint;
        if (paperSound != null) paperSound.Play();

        // khóa di chuyển trong lúc đọc
        GameObject player = GameObject.FindWithTag("Player");
        CharacterController cc = player != null ? player.GetComponent<CharacterController>() : null;
        if (cc != null) cc.enabled = false;

        panel.SetActive(true);
        IsOpen = true;

        yield return null;                       // bỏ qua cú bấm E vừa dùng để nhặt
        yield return new WaitForSeconds(0.3f);   // tránh lỡ tay đóng ngay
        while (!Keyboard.current.eKey.wasPressedThisFrame && !Keyboard.current.escapeKey.wasPressedThisFrame)
            yield return null;

        panel.SetActive(false);
        IsOpen = false;
        if (cc != null) cc.enabled = true;
        yield return null;                       // cú bấm E để đóng không bị tính là nhặt món khác
    }
}
