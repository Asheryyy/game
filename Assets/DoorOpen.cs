using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// Cửa mở bằng E: cánh cửa xoay quanh bản lề rồi Player đi vào được.
/// Gắn vào 1 object có Box Collider (Is Trigger) đặt TRƯỚC cửa.
/// Cánh cửa (vd: small_door) phải là CON của 1 object "BanLe" đặt đúng mép bản lề → script xoay BanLe.
/// </summary>
public class DoorOpen : MonoBehaviour, ISaveable
{
    [Tooltip("Object bản lề (cha của cánh cửa), đặt ở mép cửa phía có bản lề")]
    [SerializeField] private Transform hinge;
    [Tooltip("Cửa 2 cánh: bản lề của cánh thứ 2 (xoay ngược chiều cánh 1). Để trống = cửa 1 cánh")]
    [SerializeField] private Transform secondHinge;
    [Tooltip("Góc mở (độ). Âm = mở chiều ngược lại")]
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float openTime = 1.2f;

    [Header("=== DIEU KIEN ===")]
    [Tooltip("Chỉ mở được khi đang có quest này (để trống = luôn mở được)")]
    [SerializeField] private string requiredQuestId;
    [Tooltip("Phải nhặt vật này trước (Clue Id của ClueItem, vd: chia_khoa). Để trống = không cần")]
    [SerializeField] private string requiredItemId;
    [SerializeField] private string hintText = "Bấm [E] để mở cửa";
    [Tooltip("Hiện khi chưa mở được (để trống = không hiện gì)")]
    [SerializeField] private string lockedText = "Cửa khóa rồi...";

    [Header("=== KHI MO ===")]
    [Tooltip("Tiếng cửa kẽo kẹt (Output = SFX)")]
    [SerializeField] private AudioSource openSound;
    [Tooltip("Hoàn thành quest này khi mở cửa (để trống = không)")]
    [SerializeField] private string questToComplete;
    [SerializeField] private UnityEvent onOpened;

    bool playerNearby, opened, opening;

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    public string CaptureState() =>
        (opened ? "1" : "0") + ";" + (hinge != null ? SaveSystem.Rot(hinge.localRotation) : "") +
        ";" + (secondHinge != null ? SaveSystem.Rot(secondHinge.localRotation) : "");
    public void RestoreState(string s)
    {
        string[] p = s.Split(';');
        opened = p[0] == "1";
        if (hinge != null && p.Length > 1 && p[1].Length > 0) hinge.localRotation = SaveSystem.ParseRot(p[1]);
        if (secondHinge != null && p.Length > 2 && p[2].Length > 0) secondHinge.localRotation = SaveSystem.ParseRot(p[2]);
        if (opened) DisableLeafColliders();
    }

    // Cửa đã mở → tắt collider cánh cửa để không chắn lối đi (lúc đóng vẫn chặn bình thường)
    void DisableLeafColliders()
    {
        foreach (Transform h in new[] { hinge, secondHinge })
        {
            if (h == null) continue;
            foreach (Collider c in h.GetComponentsInChildren<Collider>())
                if (!c.isTrigger) c.enabled = false;
        }
    }

    bool CanOpen()
    {
        if (!string.IsNullOrEmpty(requiredItemId) && !ClueItem.HasFound(requiredItemId)) return false;
        if (string.IsNullOrEmpty(requiredQuestId) || QuestManager.Instance == null) return true;
        Quest q = QuestManager.Instance.GetQuest(requiredQuestId);
        return q != null && q.isActive && !q.isCompleted;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || opened) return;
        playerNearby = true;
        Show(CanOpen() ? hintText : lockedText);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) playerNearby = false;
    }

    void Update()
    {
        if (!playerNearby || opened || opening) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive) return;
        if (!Keyboard.current.eKey.wasPressedThisFrame) return;

        if (CanOpen()) StartCoroutine(Open());
        else Show(lockedText);
    }

    IEnumerator Open()
    {
        opening = true;
        if (openSound != null) openSound.Play();

        Quaternion from1 = hinge != null ? hinge.localRotation : Quaternion.identity;
        Quaternion to1   = from1 * Quaternion.Euler(0f, openAngle, 0f);
        Quaternion from2 = secondHinge != null ? secondHinge.localRotation : Quaternion.identity;
        Quaternion to2   = from2 * Quaternion.Euler(0f, -openAngle, 0f); // cánh 2 mở ngược chiều
        for (float t = 0f; t < 1f; t += Time.deltaTime / openTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t);
            if (hinge != null)       hinge.localRotation       = Quaternion.Slerp(from1, to1, k);
            if (secondHinge != null) secondHinge.localRotation = Quaternion.Slerp(from2, to2, k);
            yield return null;
        }
        if (hinge != null)       hinge.localRotation = to1;
        if (secondHinge != null) secondHinge.localRotation = to2;

        opened = true;
        opening = false;
        DisableLeafColliders();
        if (!string.IsNullOrEmpty(questToComplete) && QuestManager.Instance != null)
            QuestManager.Instance.CompleteQuest(questToComplete);
        onOpened?.Invoke();
    }

    static void Show(string msg)
    {
        if (!string.IsNullOrEmpty(msg) && QuestUI.Instance != null) QuestUI.Instance.ShowNotification(msg);
    }
}
