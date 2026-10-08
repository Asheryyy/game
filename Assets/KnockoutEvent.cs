using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using System.Collections;

/// <summary>
/// Sự kiện bị đánh ngất: nhân vật ngồi xuống xem → (thoại xong) → tiếng bước chân sau lưng
/// → BỤP! màn hình đen ngay → chữ "Vài giờ sau..." → tỉnh dậy (đổi giờ, đồ vật biến mất...).
/// Gắn vào 1 GameObject trống RIÊNG (luôn bật). Gọi Play() từ ClueItem → On Found.
/// </summary>
public class KnockoutEvent : MonoBehaviour
{
    [Header("=== NGOI XUONG ===")]
    [Tooltip("Hạ camera xuống bao nhiêu mét khi ngồi xem (0 = không ngồi)")]
    [SerializeField] private float crouchAmount = 0.7f;
    [SerializeField] private float crouchSpeed = 2f;

    [Header("=== BI DANH ===")]
    [Tooltip("Chờ bao lâu sau khi thoại xong mới bị đánh (giây)")]
    [SerializeField] private float delayBeforeHit = 1.2f;
    [Tooltip("Tiếng bước chân/sàn kêu sau lưng, phát ngay khi thoại xong (để người chơi giật mình)")]
    [SerializeField] private AudioSource footstepBehind;
    [Tooltip("Tiếng đánh 'bụp' (Output = SFX)")]
    [SerializeField] private AudioSource hitSound;
    [SerializeField] private float shakeStrength = 0.15f;

    [Header("=== MAN HINH ===")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private TextMeshProUGUI captionText;
    [SerializeField] private string caption = "Vài giờ sau...";
    [Tooltip("Màn đen giữ bao lâu (giây)")]
    [SerializeField] private float blackDuration = 3f;
    [Tooltip("Tỉnh dậy: màn hình sáng dần chậm, như mở mắt")]
    [SerializeField] private float wakeFadeDuration = 3f;

    [Header("=== TINH DAY ===")]
    [Tooltip("Chỗ nhân vật nằm tỉnh dậy (để trống = ngay chỗ bị đánh)")]
    [SerializeField] private Transform wakeUpPoint;
    [Tooltip("Gọi lúc màn hình đen: TimeOfDaySwitch.SetAfternoon, tắt cái gương...")]
    [SerializeField] private UnityEvent onBlackout;
    [Tooltip("Nhân vật nói khi tỉnh (dòng cuối có thể giao quest mới)")]
    [SerializeField] private DialogueLine[] wakeUpLines;

    bool played;

    public void Play()
    {
        if (played) return;
        played = true;
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        Camera cam = Camera.main;
        Transform camT = cam != null ? cam.transform : null;
        Vector3 camStart = camT != null ? camT.localPosition : Vector3.zero;
        GameObject player = GameObject.FindWithTag("Player");
        CharacterController cc = player != null ? player.GetComponent<CharacterController>() : null;

        // khóa di chuyển (PlayerMove bỏ qua khi CC tắt)
        if (cc != null) cc.enabled = false;

        // 1. Ngồi xuống (trong lúc đang đọc thoại)
        if (camT != null && crouchAmount > 0f)
            yield return MoveCam(camT, camStart, camStart + Vector3.down * crouchAmount, crouchSpeed);

        // 2. Chờ thoại xong
        while (DialogueManager.Instance != null && DialogueManager.Instance.IsActive) yield return null;

        // 3. Tiếng động sau lưng... rồi BỤP
        if (footstepBehind != null) footstepBehind.Play();
        yield return new WaitForSeconds(delayBeforeHit);
        if (hitSound != null) hitSound.Play();
        if (camT != null) yield return Shake(camT, 0.15f);
        SetAlpha(1f); // đen NGAY, không fade

        // 4. Màn đen + chữ
        yield return new WaitForSeconds(0.8f);
        if (captionText != null) { captionText.text = caption; captionText.gameObject.SetActive(true); }

        try { onBlackout?.Invoke(); }
        catch (System.Exception e) { Debug.LogException(e, this); }

        if (player != null && wakeUpPoint != null)
        {
            player.transform.SetPositionAndRotation(wakeUpPoint.position,
                Quaternion.Euler(0f, wakeUpPoint.eulerAngles.y, 0f));
            Physics.SyncTransforms();
        }
        if (camT != null) camT.localPosition = camStart + Vector3.down * crouchAmount; // vẫn đang nằm/ngồi

        yield return new WaitForSeconds(blackDuration);
        if (captionText != null) captionText.gameObject.SetActive(false);

        // 5. Tỉnh dậy: mở mắt chậm + đứng dậy
        StartCoroutine(FadeAlpha(1f, 0f, wakeFadeDuration));
        if (camT != null)
            yield return MoveCam(camT, camT.localPosition, camStart, 1f / Mathf.Max(0.1f, wakeFadeDuration));
        else yield return new WaitForSeconds(wakeFadeDuration);

        if (cc != null) cc.enabled = true;

        if (wakeUpLines != null && wakeUpLines.Length > 0 && DialogueManager.Instance != null)
            DialogueManager.Instance.StartDialogue(wakeUpLines);
    }

    IEnumerator MoveCam(Transform t, Vector3 from, Vector3 to, float speed)
    {
        for (float k = 0f; k < 1f; k += Time.deltaTime * speed)
        {
            t.localPosition = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, k));
            yield return null;
        }
        t.localPosition = to;
    }

    IEnumerator Shake(Transform t, float duration)
    {
        Vector3 basePos = t.localPosition;
        for (float k = 0f; k < duration; k += Time.deltaTime)
        {
            t.localPosition = basePos + Random.insideUnitSphere * shakeStrength;
            yield return null;
        }
        t.localPosition = basePos;
    }

    IEnumerator FadeAlpha(float from, float to, float duration)
    {
        for (float k = 0f; k < 1f; k += Time.deltaTime / duration)
        {
            SetAlpha(Mathf.Lerp(from, to, k));
            yield return null;
        }
        SetAlpha(to);
    }

    void SetAlpha(float a)
    {
        if (fadeImage == null) return;
        Color c = fadeImage.color;
        fadeImage.color = new Color(c.r, c.g, c.b, a);
    }
}
