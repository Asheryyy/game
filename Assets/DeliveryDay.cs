using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using TMPro;
using System;
using System.Collections;
using System.Text;

[Serializable]
public class DeliveryOrder
{
    [Tooltip("Tên món hàng (vd: Trà sữa)")]
    public string itemName;
    [Tooltip("Căn hộ nhận (vd: P.204)")]
    public string apartment;
}

/// <summary>
/// 1 ngày giao hàng: nhận phiếu → ra kho nhặt TỪNG thùng THEO THỨ TỰ (giao xong đơn 0 mới hiện thùng đơn 1) (DeliveryPackage) → cầm trên tay
/// → đưa đúng nhà (DeliveryDoor) thì thùng biến mất → quay lại kho lấy thùng tiếp.
/// Giao hết thì hoàn thành quest, nhân vật nói doneLines (dòng cuối có thể giao quest "ve_phong"...).
/// Cả ngày gom vào 1 quest, mô tả quest tự cập nhật tiến độ.
///
/// SETUP:
///   1. Tạo GameObject rỗng "Ngay2_GiaoHang", gắn script này, điền Day Quest + danh sách Orders.
///   2. Bắt đầu ngày: gọi Begin() — vd từ ClueItem tờ phiếu nhận việc (On Found → DeliveryDay.Begin).
///   3. Thùng trước kho: mỗi thùng gắn DeliveryPackage (số thứ tự đơn), gom vào 1 object cha
///      đang TẮT rồi kéo vào ô Packages.
///   4. Held Box: 1 thùng làm CON của camera, gắn HeldTool, TẮT sẵn — dùng chung cho mọi đơn.
///   5. Mỗi cửa căn hộ: DeliveryDoor + số thứ tự đơn.
/// </summary>
public class DeliveryDay : MonoBehaviour, ISaveable
{
    [Header("=== QUEST CUA NGAY ===")]
    [SerializeField] private Quest dayQuest = new Quest { questId = "giao_hang_ngay2", questName = "Giao hàng" };
    [SerializeField] private string pickupLine = "Ra kho lấy hàng";
    [SerializeField] private DeliveryOrder[] orders;

    [Header("=== THUNG HANG ===")]
    [Tooltip("Object cha chứa các thùng trước kho (TẮT sẵn): nhận phiếu → hiện ra")]
    [SerializeField] private GameObject[] packages;
    [Tooltip("Thùng cầm trên tay (con của camera, có HeldTool, TẮT sẵn)")]
    [SerializeField] private GameObject heldBox;
    [Tooltip("Chữ dán trên thùng cầm tay — tự ghi số phòng (để trống = không)")]
    [SerializeField] private TMP_Text heldLabel;

    [Header("=== THOAI ===")]
    [Tooltip("Nhân vật nói khi nhặt thùng ĐẦU TIÊN (để trống = không)")]
    [FormerlySerializedAs("pickedUpLines")]
    [SerializeField] private DialogueLine[] firstPickupLines;
    [Tooltip("Nhân vật nói khi giao xong hết (dòng cuối có thể giao quest mới, vd: ve_phong)")]
    [SerializeField] private DialogueLine[] doneLines;

    [Header("=== SAU KHI GIAO HET ===")]
    [Tooltip("Gọi khi giao xong đơn cuối (vd: bật tờ cảnh báo trước cửa, bật sự kiện cái gương)")]
    [SerializeField] private UnityEvent onAllDelivered;

    int carrying = -1;           // đơn đang cầm trên tay (-1 = tay không)
    bool[] pickedUp, delivered;
    DeliveryPackage[] boxes;

    public int OrderCount => orders != null ? orders.Length : 0;
    public int Carrying => carrying;
    public bool IsPickedUp(int i) => InRange(i) && pickedUp[i];
    public bool IsDelivered(int i) => InRange(i) && delivered[i];
    public string OrderLabel(int i) => InRange(i) ? orders[i].itemName : "hàng";
    public string OrderApartment(int i) => InRange(i) ? orders[i].apartment : "";
    bool InRange(int i) => delivered != null && i >= 0 && i < delivered.Length;

    /// Đang trong ngày giao hàng (đã nhận phiếu, chưa giao hết)
    public bool IsRunning
    {
        get
        {
            Quest q = CurrentQuest();
            return q != null && q.isActive && !q.isCompleted;
        }
    }

    void Awake()
    {
        pickedUp = new bool[OrderCount];
        delivered = new bool[OrderCount];
        var list = new System.Collections.Generic.List<DeliveryPackage>();
        if (packages != null)
            foreach (GameObject go in packages)
                if (go != null) list.AddRange(go.GetComponentsInChildren<DeliveryPackage>(true));
        boxes = list.ToArray();
        // thùng đang bật sẵn trong scene → ẩn tới khi ngày này bắt đầu (kẻo thùng ngày 3 hiện từ ngày 2)
        foreach (DeliveryPackage b in boxes) b.gameObject.SetActive(false);
    }

    // quest ngày có thể được giao từ thoại (vd: thức dậy ở giường) thay vì Begin() → tự bật thùng khi quest bắt đầu
    bool wasRunning;
    void Update()
    {
        bool running = IsRunning;
        if (running == wasRunning) return;
        wasRunning = running;
        if (running && packages != null)
            foreach (GameObject go in packages)
                if (go != null) go.SetActive(true);
        UpdatePackages();
        RefreshQuest();
    }

    // giao theo thứ tự: chỉ hiện thùng kế tiếp khi thùng trước đã giao xong
    void UpdatePackages()
    {
        foreach (DeliveryPackage b in boxes)
        {
            int i = b.OrderIndex;
            if (InRange(i)) b.gameObject.SetActive(IsRunning && !pickedUp[i] && (i == 0 || delivered[i - 1]));
        }
    }

    // "carrying|picked|delivered", vd: "1|110|100"
    public string CaptureState() => $"{carrying}|{Flags(pickedUp)}|{Flags(delivered)}";

    public void RestoreState(string s)
    {
        string[] p = s.Split('|');
        if (p.Length < 3) return;
        int.TryParse(p[0], out carrying);
        ReadFlags(p[1], pickedUp);
        ReadFlags(p[2], delivered);
        UpdateHeldBox();
        UpdatePackages();
    }

    static string Flags(bool[] a)
    {
        var sb = new StringBuilder();
        foreach (bool b in a) sb.Append(b ? '1' : '0');
        return sb.ToString();
    }

    static void ReadFlags(string s, bool[] a)
    {
        for (int i = 0; i < a.Length && i < s.Length; i++) a[i] = s[i] == '1';
    }

    // Sau khi tải save, QuestManager giữ bản Quest mới → luôn lấy qua id
    Quest CurrentQuest() =>
        QuestManager.Instance != null ? QuestManager.Instance.GetQuest(dayQuest.questId) : null;

    /// Bắt đầu ngày giao hàng (gọi từ tờ phiếu nhận việc / SleepBed On Wake Up)
    public void Begin()
    {
        if (QuestManager.Instance == null || CurrentQuest() != null) return;
        QuestManager.Instance.StartQuest(dayQuest);
        if (packages != null)
            foreach (GameObject go in packages)
                if (go != null) go.SetActive(true);
        UpdatePackages();
        RefreshQuest();
    }

    /// Thùng trước kho gọi khi nhặt. Trả về false nếu không nhặt được (tay đang bận...)
    public bool PickUp(int i)
    {
        if (!IsRunning || !InRange(i) || pickedUp[i]) return false;
        if (carrying >= 0)
        {
            Notify("Tay đang bận — đi giao " + OrderLabel(carrying) + " trước đã");
            return false;
        }

        bool first = Array.IndexOf(pickedUp, true) < 0;
        pickedUp[i] = true;
        carrying = i;
        UpdateHeldBox();
        RefreshQuest();
        Notify($"Đã lấy {orders[i].itemName} → {orders[i].apartment}");

        if (first && firstPickupLines != null && firstPickupLines.Length > 0)
            DialogueManager.Instance.StartDialogue(firstPickupLines);
        return true;
    }

    /// Cửa căn hộ gọi khi đưa hàng xong
    public void Deliver(int i)
    {
        if (!IsRunning || carrying != i || delivered[i]) return;
        delivered[i] = true;
        carrying = -1;
        UpdateHeldBox();
        UpdatePackages();
        RefreshQuest();
        SaveSystem.RequestAutoSave();

        foreach (bool d in delivered) if (!d) return;
        StartCoroutine(Finish());
    }

    IEnumerator Finish()
    {
        // chờ NPC nói xong câu cuối rồi mới chốt ngày
        yield return null;
        while (DialogueManager.Instance.IsActive) yield return null;

        QuestManager.Instance.CompleteQuest(dayQuest.questId);
        if (doneLines != null && doneLines.Length > 0)
            DialogueManager.Instance.StartDialogue(doneLines);

        try { onAllDelivered?.Invoke(); }
        catch (Exception e) { Debug.LogException(e, this); }
    }

    void UpdateHeldBox()
    {
        if (heldBox != null) heldBox.SetActive(carrying >= 0);
        if (heldLabel != null) heldLabel.text = OrderApartment(carrying);
    }

    void RefreshQuest()
    {
        Quest q = CurrentQuest();
        if (q == null) return;

        var sb = new StringBuilder();
        bool allPicked = Array.IndexOf(pickedUp, false) < 0;
        if (carrying >= 0) sb.AppendLine($"Đang cầm: {orders[carrying].itemName} → {orders[carrying].apartment}");
        else if (!allPicked) sb.AppendLine(pickupLine);
        for (int i = 0; i < OrderCount; i++)
            sb.AppendLine($"{(delivered[i] ? "[x]" : "[  ]")} {orders[i].itemName} → {orders[i].apartment}");
        q.description = sb.ToString().TrimEnd();

        if (QuestUI.Instance != null) QuestUI.Instance.UpdateUI();
    }

    static void Notify(string msg)
    {
        if (QuestUI.Instance != null) QuestUI.Instance.ShowNotification(msg);
    }
}
