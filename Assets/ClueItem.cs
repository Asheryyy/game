using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.Events;

/// <summary>
/// Manh mối: Player đứng gần + bấm E → hiện suy nghĩ (dialogue), đánh dấu đã tìm thấy.
/// Gắn vào object có Collider (Is Trigger) bao quanh vật.
/// Có thể yêu cầu tìm manh mối khác trước (requiredClueId),
/// nhận quest qua DialogueLine.questToGive, hoặc hoàn thành quest (questToComplete).
/// </summary>
public class ClueItem : MonoBehaviour
{
    [Header("=== MANH MOI ===")]
    [SerializeField] private string clueId = "hoa_don_nuoc";
    [Tooltip("Phải tìm manh mối này trước (để trống = không cần)")]
    [SerializeField] private string requiredClueId;
    [SerializeField] private string hintText = "Bấm [E] để xem";
    [Tooltip("TRUE = bước vào là tự chạy, không cần bấm E (dùng cho suy nghĩ/sự kiện)")]
    [SerializeField] private bool autoTrigger = false;
    [Tooltip("Thuộc nhóm manh mối nào (tìm đủ cả nhóm mới mở bước tiếp). Để trống = không")]
    [SerializeField] private ClueGroup group;

    [Header("=== GIAY TO (de trong = khong co) ===")]
    [Tooltip("Tiêu đề trên tờ giấy (vd: HÓA ĐƠN TIỀN NƯỚC)")]
    [SerializeField] private string documentTitle;
    [Tooltip("Nội dung tờ giấy — có chữ thì bấm E sẽ hiện tờ giấy phóng to trước, đọc xong mới hiện suy nghĩ")]
    [TextArea(4, 12)] [SerializeField] private string documentText;
    [Tooltip("Nhân vật nói TRƯỚC khi mở tờ giấy (vd: 'Tờ giấy kẹp sau ngăn kéo...'). Thoughts bên dưới = nói SAU khi đọc")]
    [SerializeField] private DialogueLine[] beforeReadLines;
    [Tooltip("Font riêng cho tờ giấy này (vd: font viết tay). Để trống = font mặc định")]
    [SerializeField] private TMPro.TMP_FontAsset documentFont;
    [Tooltip("Màu chữ riêng (vd: xanh mực, đỏ). Alpha = 0 → dùng màu mặc định")]
    [SerializeField] private Color documentColor = new Color(0, 0, 0, 0);

    [Header("=== SUY NGHI KHI TIM THAY ===")]
    [SerializeField] private DialogueLine[] thoughts;

    [Header("=== QUEST ===")]
    [Tooltip("Hoàn thành quest này khi tìm thấy (để trống = không)")]
    [SerializeField] private string questToComplete;

    [Header("=== SAU KHI TIM THAY ===")]
    [SerializeField] private bool hideAfterFound = false;
    [Tooltip("Bật các object này lên (vd: manh mối tiếp theo)")]
    [SerializeField] private GameObject[] activateOnFound;
    [Tooltip("Gọi ngay khi tìm thấy (vd: KnockoutEvent.Play — sự kiện sẽ tự chờ thoại xong)")]
    [SerializeField] private UnityEvent onFound;

    static readonly HashSet<string> foundClues = new HashSet<string>();
    public static bool HasFound(string id) => foundClues.Contains(id);
    public static IEnumerable<string> AllFound => foundClues;
    public static void RestoreFound(IEnumerable<string> ids)
    {
        foundClues.Clear();
        if (ids != null) foreach (string id in ids) foundClues.Add(id);
    }

    bool playerNearby = false;

    void Awake()
    {
        if (group != null) group.Register(this);

        // Đã có trigger sẵn → dùng luôn
        foreach (Collider c in GetComponents<Collider>())
            if (c.isTrigger) return;

        // Chưa có → tự thêm BoxCollider trigger bao quanh model (kể cả model nằm ở object con).
        // Collider cũ (MeshCollider...) giữ nguyên để vẫn va chạm.
        BoxCollider box = gameObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        Renderer[] rends = GetComponentsInChildren<Renderer>();
        if (rends.Length > 0)
        {
            Bounds b = rends[0].bounds;
            foreach (Renderer r in rends) b.Encapsulate(r.bounds);
            // đổi bounds thế giới → local của object này
            box.center = transform.InverseTransformPoint(b.center);
            Vector3 s = transform.lossyScale;
            box.size = new Vector3(b.size.x / Mathf.Abs(s.x), b.size.y / Mathf.Abs(s.y), b.size.z / Mathf.Abs(s.z));
        }
        // nới rộng tối thiểu ~1.5m quanh vật để dễ bấm E
        Vector3 ls = transform.lossyScale;
        Vector3 minSize = new Vector3(1.5f / Mathf.Abs(ls.x), 1.5f / Mathf.Abs(ls.y), 1.5f / Mathf.Abs(ls.z));
        box.size = Vector3.Max(box.size * 1.5f, minSize);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (HasFound(clueId)) return;
        if (!string.IsNullOrEmpty(requiredClueId) && !HasFound(requiredClueId)) return;
        // Manh mối dùng để hoàn thành quest → chỉ mở khi quest đó ĐANG làm
        // (tránh bấm trước khi nhận quest → quest nhận sau không bao giờ xong được)
        if (!string.IsNullOrEmpty(questToComplete) && !QuestIsActive(questToComplete)) return;
        playerNearby = true;
        if (autoTrigger) return; // Update sẽ tự chạy, không cần hiện gợi ý
        if (QuestUI.Instance != null) QuestUI.Instance.ShowNotification(hintText);
        else Debug.LogWarning("[Clue] Scene chưa có QuestUI → không hiện được chữ gợi ý");
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) playerNearby = false;
    }

    void Update()
    {
        if (!playerNearby || DialogueManager.Instance.IsActive) return;
        if (!autoTrigger && !Keyboard.current.eKey.wasPressedThisFrame) return;

        playerNearby = false;
        foundClues.Add(clueId);

        bool hasDoc = !string.IsNullOrEmpty(documentText) && DocumentViewer.Instance != null;
        if (hasDoc)
            StartCoroutine(ReadThenFound());
        else
            OnFoundNow();
    }

    // Đọc tờ giấy xong rồi mới hiện suy nghĩ, giao/xong quest...
    System.Collections.IEnumerator ReadThenFound()
    {
        if (beforeReadLines != null && beforeReadLines.Length > 0)
        {
            DialogueManager.Instance.StartDialogue(beforeReadLines);
            yield return null;
            while (DialogueManager.Instance.IsActive) yield return null;
        }
        yield return DocumentViewer.Instance.Read(documentTitle, documentText, documentFont,
            documentColor.a > 0f ? documentColor : (Color?)null);
        OnFoundNow();
    }

    void OnFoundNow()
    {
        if (thoughts != null && thoughts.Length > 0)
            DialogueManager.Instance.StartDialogue(thoughts);

        if (!string.IsNullOrEmpty(questToComplete) && QuestManager.Instance != null)
            QuestManager.Instance.CompleteQuest(questToComplete);

        foreach (GameObject go in activateOnFound)
            if (go != null) go.SetActive(true);

        onFound?.Invoke();
        if (group != null) group.OnClueFound(this);

        if (hideAfterFound) gameObject.SetActive(false);
    }

    static bool QuestIsActive(string id)
    {
        if (QuestManager.Instance == null) return true;
        Quest q = QuestManager.Instance.GetQuest(id);
        return q != null && q.isActive && !q.isCompleted;
    }

    void OnDestroy() => foundClues.Remove(clueId); // chơi lại từ đầu → reset
}
