using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// 1 việc dọn dẹp (quét bụi, lau vết bẩn, nhặt rác, xếp đồ...).
/// Player đứng gần → GIỮ E trong vài giây → vết bẩn mờ dần rồi biến mất.
/// Gắn vào object có Collider (Is Trigger) quanh chỗ bẩn. Thuộc 1 ChoreGroup.
/// </summary>
public class ChoreItem : MonoBehaviour
{
    [SerializeField] private ChoreGroup group;
    [SerializeField] private string choreName = "Quét bụi";
    [Tooltip("Phải giữ E bao lâu (giây)")]
    [SerializeField] private float holdTime = 1.5f;
    [Tooltip("Phải nhặt vật này trước (Clue Id, vd: con_dao). Để trống = không cần")]
    [SerializeField] private string requiredItemId;
    [SerializeField] private string missingItemText = "Cần thứ gì đó để làm việc này...";

    [Header("=== HINH ANH ===")]
    [Tooltip("Các object biến mất khi dọn xong (vết bẩn, rác, đống đồ bừa bộn)")]
    [SerializeField] private GameObject[] hideWhenDone;
    [Tooltip("Các object hiện ra khi dọn xong (đồ đã xếp gọn)")]
    [SerializeField] private GameObject[] showWhenDone;

    [Header("=== AM THANH ===")]
    [Tooltip("AudioSource phát tiếng quét/lau khi đang giữ E (Output = SFX, Loop)")]
    [SerializeField] private AudioSource workSound;

    public bool IsDone { get; private set; }

    bool playerNearby;
    float progress;

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
        else
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(2f, 2f, 2f);
        }
        if (group != null) group.Register(this);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || IsDone) return;
        playerNearby = true;
        Show(HasItem() ? $"Giữ [E] để {choreName.ToLower()}" : missingItemText);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerNearby = false;
        StopWork();
    }

    void Update()
    {
        if (!playerNearby || IsDone) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive) return;
        if (!HasItem())
        {
            if (Keyboard.current.eKey.wasPressedThisFrame) Show(missingItemText);
            return;
        }

        if (Keyboard.current.eKey.isPressed)
        {
            if (progress == 0f && workSound != null) workSound.Play();
            progress += Time.deltaTime;
            int pct = Mathf.Clamp(Mathf.RoundToInt(progress / holdTime * 100f), 0, 100);
            if (QuestUI.Instance != null) QuestUI.Instance.SetNotificationText($"Đang {choreName.ToLower()}... {pct}%");
            if (progress >= holdTime) Finish();
        }
        else if (progress > 0f)
        {
            StopWork(); // buông E giữa chừng → làm lại
            Show($"Giữ [E] để {choreName.ToLower()}");
        }
    }

    bool HasItem() => string.IsNullOrEmpty(requiredItemId) || ClueItem.HasFound(requiredItemId);

    void StopWork()
    {
        progress = 0f;
        if (workSound != null) workSound.Stop();
    }

    void Finish()
    {
        IsDone = true;
        playerNearby = false;
        if (workSound != null) workSound.Stop();
        StartCoroutine(FadeOutThen());
    }

    IEnumerator FadeOutThen()
    {
        // thu nhỏ dần vết bẩn cho có cảm giác "biến mất"
        float t = 0f;
        Vector3[] scales = new Vector3[hideWhenDone.Length];
        for (int i = 0; i < hideWhenDone.Length; i++)
            if (hideWhenDone[i] != null) scales[i] = hideWhenDone[i].transform.localScale;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            for (int i = 0; i < hideWhenDone.Length; i++)
                if (hideWhenDone[i] != null)
                    hideWhenDone[i].transform.localScale = Vector3.Lerp(scales[i], Vector3.zero, t / 0.4f);
            yield return null;
        }
        // báo nhóm TRƯỚC khi ẩn (lỡ hideWhenDone chứa chính object này thì coroutine dừng)
        if (group != null) group.OnChoreDone(this);
        else Show($"Xong: {choreName}");

        foreach (GameObject go in showWhenDone) if (go != null) go.SetActive(true);
        foreach (GameObject go in hideWhenDone) if (go != null) go.SetActive(false);
    }

    static void Show(string msg)
    {
        if (QuestUI.Instance != null) QuestUI.Instance.ShowNotification(msg);
    }
}
