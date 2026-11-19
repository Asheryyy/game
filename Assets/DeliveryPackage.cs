using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// 1 thùng hàng trước kho = 1 đơn. Bấm E → cầm lên tay (DeliveryDay bật Held Box), thùng này ẩn đi.
/// Gắn vào thùng (tự thêm trigger quanh thùng nếu chưa có). Mỗi lần chỉ cầm được 1 thùng.
/// </summary>
public class DeliveryPackage : MonoBehaviour
{
    [SerializeField] private DeliveryDay day;
    [Tooltip("Số thứ tự đơn trong DeliveryDay (0 = đơn đầu tiên)")]
    [SerializeField] private int orderIndex;
    [Tooltip("Chữ dán trên thùng — tự ghi số phòng (để trống = không)")]
    [SerializeField] private TMP_Text label;

    bool playerNearby;

    public int OrderIndex => orderIndex;

    void Awake()
    {
        foreach (Collider c in GetComponents<Collider>())
            if (c.isTrigger) return;
        // chưa có trigger → thêm 1 cái rộng ~1.5m quanh thùng (collider cũ giữ nguyên để va chạm)
        SphereCollider s = gameObject.AddComponent<SphereCollider>();
        s.isTrigger = true;
        s.radius = 1.5f / Mathf.Max(0.01f, transform.lossyScale.x);
    }

    void Start()
    {
        if (label != null && day != null) label.text = day.OrderApartment(orderIndex);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || day == null || !day.IsRunning || day.IsPickedUp(orderIndex)) return;
        playerNearby = true;
        if (QuestUI.Instance != null)
            QuestUI.Instance.ShowNotification($"Bấm [E] để lấy {day.OrderLabel(orderIndex)} ({day.OrderApartment(orderIndex)})");
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) playerNearby = false;
    }

    void Update()
    {
        if (!playerNearby || DialogueManager.Instance.IsActive) return;
        if (!Keyboard.current.eKey.wasPressedThisFrame) return;
        if (day.PickUp(orderIndex))
        {
            playerNearby = false;
            gameObject.SetActive(false);
        }
    }
}
