using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using TMPro;
using System.Collections;

/// <summary>
/// Giường: bấm E (khi đang có quest đi ngủ) → màn hình đen → hiện chữ "Sáng hôm sau..."
/// → đổi sang buổi sáng (On Wake Up: TimeOfDaySwitch.SetMorning) → sáng màn hình → nhân vật tự nói.
/// Gắn vào object có Collider (Is Trigger) quanh giường.
/// </summary>
public class SleepBed : MonoBehaviour
{
    [Header("=== DIEU KIEN ===")]
    [Tooltip("Chỉ ngủ được khi đang có quest này (vd: di_ngu). Để trống = lúc nào cũng ngủ được")]
    [SerializeField] private string requiredQuestId = "di_ngu";
    [SerializeField] private string hintText = "Bấm [E] để đi ngủ";
    [SerializeField] private string notSleepyText = "Chưa buồn ngủ...";

    [Header("=== CHUYEN CANH ===")]
    [Tooltip("Image đen phủ màn hình (dùng chung với TeleportZone)")]
    [SerializeField] private Image fadeImage;
    [Tooltip("Chữ hiện giữa màn hình đen (TextMeshPro UI, nằm TRÊN fadeImage)")]
    [SerializeField] private TextMeshProUGUI captionText;
    [SerializeField] private string caption = "Sáng hôm sau...";
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float captionDuration = 2.5f;

    [Header("=== SAU KHI NGU DAY ===")]
    [Tooltip("Điểm nhân vật đứng dậy (cạnh giường). Để trống = giữ nguyên chỗ")]
    [SerializeField] private Transform wakeUpPoint;
    [Tooltip("Gọi lúc màn hình đen: TimeOfDaySwitch.SetMorning, bật hóa đơn trước cửa...")]
    [SerializeField] private UnityEvent onWakeUp;
    [Tooltip("Nhân vật nói sau khi thức dậy (dòng cuối có thể giao quest mới)")]
    [SerializeField] private DialogueLine[] wakeUpLines;

    bool playerNearby, sleeping, slept;
    GameObject player;

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
        if (captionText != null) captionText.gameObject.SetActive(false);
    }

    bool CanSleep() =>
        string.IsNullOrEmpty(requiredQuestId) ||
        (QuestManager.Instance != null && QuestManager.Instance.GetQuest(requiredQuestId) is Quest q
         && q.isActive && !q.isCompleted);

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || slept) return;
        playerNearby = true;
        player = other.gameObject;
        Show(CanSleep() ? hintText : notSleepyText);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) playerNearby = false;
    }

    void Update()
    {
        if (!playerNearby || sleeping || slept || !CanSleep()) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive) return;
        if (Keyboard.current.eKey.wasPressedThisFrame) StartCoroutine(Sleep());
    }

    IEnumerator Sleep()
    {
        sleeping = true;
        yield return Fade(0f, 1f);

        if (captionText != null)
        {
            captionText.text = caption;
            captionText.gameObject.SetActive(true);
        }

        if (!string.IsNullOrEmpty(requiredQuestId) && QuestManager.Instance != null)
            QuestManager.Instance.CompleteQuest(requiredQuestId);

        try { onWakeUp?.Invoke(); }
        catch (System.Exception e) { Debug.LogException(e, this); }

        if (wakeUpPoint != null && player != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.transform.SetPositionAndRotation(wakeUpPoint.position,
                Quaternion.Euler(0f, wakeUpPoint.eulerAngles.y, 0f));
            Physics.SyncTransforms();
            if (cc != null) cc.enabled = true;
        }

        yield return new WaitForSeconds(captionDuration);
        if (captionText != null) captionText.gameObject.SetActive(false);
        yield return Fade(1f, 0f);

        slept = true;
        sleeping = false;
        if (wakeUpLines != null && wakeUpLines.Length > 0 && DialogueManager.Instance != null)
            DialogueManager.Instance.StartDialogue(wakeUpLines);
    }

    IEnumerator Fade(float from, float to)
    {
        if (fadeImage == null) yield break;
        for (float t = 0f; t < 1f; t += Time.deltaTime / fadeDuration)
        {
            SetAlpha(Mathf.Lerp(from, to, t));
            yield return null;
        }
        SetAlpha(to);
    }

    void SetAlpha(float a)
    {
        Color c = fadeImage.color;
        fadeImage.color = new Color(c.r, c.g, c.b, a);
    }

    static void Show(string msg)
    {
        // để trống = không hiện gì (vd: giường thứ 2 chồng lên giường 1 → khỏi đè chữ)
        if (string.IsNullOrEmpty(msg)) return;
        if (QuestUI.Instance != null) QuestUI.Instance.ShowNotification(msg);
    }
}
