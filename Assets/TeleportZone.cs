using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// Đặt vào cửa vào chung cư → player đứng ở cửa bấm E (hoặc bước vào) sẽ bị tele vào hành lang.
/// Có thể khóa tới khi nhận quest (requiredQuestId).
///
/// SETUP:
///   1. Tạo GameObject rỗng tên "TeleportZone_CuaVao"
///   2. Add Component → Box Collider → tick "Is Trigger"
///      Kéo rộng Collider vừa đủ cái cửa
///   3. Gán script TeleportZone này vào
///   4. Tạo GameObject rỗng tên "SpawnPoint_HanhLang"
///      Đặt nó ĐÚNG VỊ TRÍ muốn player xuất hiện trong hành lang
///   5. Kéo SpawnPoint_HanhLang vào field "Destination" trong Inspector
///   6. (Tuỳ chọn) Tạo Canvas + Image đen cho hiệu ứng fade
/// </summary>
public class TeleportZone : MonoBehaviour
{
    [Header("=== TELEPORT ===")]
    [Tooltip("Kéo GameObject SpawnPoint vào đây — player sẽ tele đến chỗ này")]
    [SerializeField] private Transform destination;

    [Tooltip("Xoay hướng nhìn của player sau khi tele (Y rotation). -1 = giữ nguyên hướng cũ")]
    [SerializeField] private float destinationYRotation = -1f;

    [Header("=== HIỆU ỨNG FADE ===")]
    [Tooltip("Có fade đen khi tele không? Trông mượt hơn nhiều")]
    [SerializeField] private bool useFade = true;

    [Tooltip("Image đen phủ toàn màn hình (tạo UI Image màu đen, để trong Canvas Sort Order cao)")]
    [SerializeField] private Image fadeImage;

    [Tooltip("Thời gian fade ra (giây)")]
    [SerializeField] private float fadeDuration = 0.4f;

    [Header("=== DIEU KIEN ===")]
    [Tooltip("TRUE = phải bấm E mới vào. FALSE = bước vào là tele luôn")]
    [SerializeField] private bool requirePressE = true;
    [SerializeField] private string hintText = "Bấm [E] để vào chung cư";
    [Tooltip("Phải nhận quest này rồi mới vào được (để trống = luôn mở)")]
    [SerializeField] private string requiredQuestId;
    [Tooltip("KHÓA cửa trong lúc đang làm quest này (vd: ve_phong — phải xem cái gương trước). Xong quest thì mở lại")]
    [SerializeField] private string lockedWhileQuestActive;
    [Tooltip("Hiện khi bấm E lúc cửa đang khóa")]
    [SerializeField] private string lockedText = "Cửa khóa rồi...";
    [Tooltip("Các object chỉ hiện khi đã mở khóa (mũi tên, đèn, cánh cửa sáng...)")]
    [SerializeField] private GameObject[] showWhenUnlocked;

    [Header("=== SU KIEN ===")]
    [Tooltip("Gọi lúc màn hình đang đen, ngay sau khi dịch chuyển (vd: TimeOfDaySwitch.SetNight)")]
    [SerializeField] private UnityEvent onTeleported;

    // Chạy hiệu ứng trên 1 object riêng luôn bật → dù On Teleported có tắt
    // chính cửa này (vd: cửa nằm trong phòng bị SetActive false) thì fade vẫn chạy hết
    static MonoBehaviour runner;
    static MonoBehaviour Runner
    {
        get
        {
            if (runner == null)
                runner = new GameObject("[TeleportRunner]").AddComponent<TeleportRunner>();
            return runner;
        }
    }

    // ──────────────────────────────────────────────
    private bool isTeleporting = false; // tránh tele 2 lần
    private GameObject playerInside;
    private bool unlocked;

    void Start()
    {
        // Ẩn fade image lúc đầu
        if (fadeImage != null)
            SetFadeAlpha(0f);
        SetUnlocked(IsUnlocked());
    }

    bool IsUnlocked()
    {
        if (QuestManager.Instance == null) return string.IsNullOrEmpty(requiredQuestId);
        if (!string.IsNullOrEmpty(lockedWhileQuestActive))
        {
            Quest q = QuestManager.Instance.GetQuest(lockedWhileQuestActive);
            if (q != null && q.isActive && !q.isCompleted) return false; // đang làm quest đó → khóa
        }
        return string.IsNullOrEmpty(requiredQuestId) || QuestManager.Instance.GetQuest(requiredQuestId) != null;
    }

    // Cửa bị khóa tạm (đã từng mở được) → bấm E hiện lý do
    bool TemporarilyLocked() =>
        !string.IsNullOrEmpty(lockedWhileQuestActive) && QuestManager.Instance != null &&
        QuestManager.Instance.GetQuest(lockedWhileQuestActive) is Quest q && q.isActive && !q.isCompleted;

    void SetUnlocked(bool value)
    {
        unlocked = value;
        foreach (GameObject go in showWhenUnlocked)
            if (go != null) go.SetActive(value);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = other.gameObject;
        if (TemporarilyLocked())
        {
            if (QuestUI.Instance != null && !string.IsNullOrEmpty(lockedText)) QuestUI.Instance.ShowNotification(lockedText);
            return;
        }
        if (!unlocked || isTeleporting) return;
        if (destination == null)
        {
            Debug.LogWarning("[TeleportZone] Chưa gán Destination!", this);
            return;
        }

        if (requirePressE)
        {
            if (QuestUI.Instance != null) QuestUI.Instance.ShowNotification(hintText);
        }
        else Runner.StartCoroutine(DoTeleport(other.gameObject));
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) playerInside = null;
    }

    void Update()
    {
        // Trạng thái khóa có thể đổi bất cứ lúc nào (nhận quest → mở, đang làm quest khóa → khóa)
        bool now = IsUnlocked();
        if (now != unlocked)
        {
            SetUnlocked(now);
            if (now && playerInside != null && requirePressE && QuestUI.Instance != null)
                QuestUI.Instance.ShowNotification(hintText);
        }

        if (!unlocked || !requirePressE || playerInside == null || isTeleporting) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive) return;
        if (destination != null && Keyboard.current.eKey.wasPressedThisFrame)
            Runner.StartCoroutine(DoTeleport(playerInside));
    }

    // ──────────────────────────────────────────────

    private IEnumerator DoTeleport(GameObject player)
    {
        isTeleporting = true;
        playerInside = null; // đã rời cửa

        if (useFade && fadeImage != null)
        {
            // Fade ra đen
            yield return Fade(0f, 1f, fadeDuration);
        }

        // Màn hình đang đen → chạy event TRƯỚC khi dịch chuyển
        // (bật phòng, đổi trời...) để sàn nơi đến có sẵn collider, player không rơi xuyên.
        // Bọc try/catch: event lỗi cũng không được làm kẹt màn hình đen
        try { onTeleported?.Invoke(); }
        catch (System.Exception e) { Debug.LogException(e, this); }
        Physics.SyncTransforms();

        // ── Dịch chuyển player ──
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false; // tắt CC trước khi dịch chuyển

        player.transform.position = destination.position;

        // Xoay hướng nhìn nếu có chỉ định
        if (destinationYRotation >= 0f)
            player.transform.rotation = Quaternion.Euler(0f, destinationYRotation, 0f);
        Physics.SyncTransforms();

        // Kiểm tra có sàn dưới điểm đến không (in ra Console để dễ tìm lỗi rơi xuyên)
        if (Physics.Raycast(destination.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 5f,
                            ~0, QueryTriggerInteraction.Ignore))
            Debug.Log($"[TeleportZone] Sàn dưới {destination.name}: {hit.collider.name} (cách {hit.distance - 0.5f:0.00}m)", hit.collider);
        else
            Debug.LogWarning($"[TeleportZone] KHÔNG có sàn (collider) nào dưới {destination.name} trong 5m → player sẽ rơi!", destination);

        // Giữ CC tắt thêm vài frame physics để sàn (collider) của phòng vừa bật kịp có mặt
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        if (cc != null) cc.enabled = true;

        if (useFade && fadeImage != null)
        {
            // Fade trở lại sáng
            yield return Fade(1f, 0f, fadeDuration);
        }

        isTeleporting = false;
    }

    // ──────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────

    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        SetFadeAlpha(from);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetFadeAlpha(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }
        SetFadeAlpha(to);
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeImage == null) return;
        Color c = fadeImage.color;
        fadeImage.color = new Color(c.r, c.g, c.b, alpha);
    }

    // Hiện trigger trong Scene view để dễ đặt
    void OnDrawGizmos()
    {
        if (destination == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, destination.position);
        Gizmos.DrawWireSphere(destination.position, 0.4f);
        Gizmos.DrawIcon(destination.position, "sv_icon_dot6_sml", true);
    }
}

/// Object rỗng chỉ để chạy coroutine của TeleportZone.
public class TeleportRunner : MonoBehaviour { }
