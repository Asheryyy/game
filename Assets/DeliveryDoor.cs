using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// Cửa căn hộ nhận hàng: bấm E (đã lấy hàng ở kho) → NPC nói chuyện → đánh dấu đã giao.
/// Gắn vào object RỖNG có Collider (Is Trigger) trước cửa — đừng gắn thẳng vào cửa
/// (cửa cần collider thường để chặn người).
/// clueId: ghi nhận thông tin NPC tiết lộ (vd: "doi_2_chu") — dùng làm điều kiện cho
/// ClueItem / ChoiceEvent về sau giống manh mối nhặt được.
/// </summary>
public class DeliveryDoor : MonoBehaviour
{
    [SerializeField] private DeliveryDay day;
    [Tooltip("Số thứ tự đơn trong DeliveryDay (0 = đơn đầu tiên)")]
    [SerializeField] private int orderIndex;
    [SerializeField] private string hintText = "Bấm [E] để giao hàng";
    [SerializeField] private string emptyHandText = "Chưa có hàng của nhà này — ra kho lấy";
    [SerializeField] private string wrongBoxText = "Không phải hàng của nhà này";

    [Header("=== NPC ===")]
    [Tooltip("NPC đứng trước cửa (TẮT sẵn, KHÔNG để điểm giao làm con của NPC): hiện khi nhặt đúng thùng của nhà này, ẩn sau khi giao xong")]
    [SerializeField] private GameObject npc;
    [Tooltip("NPC nói khi nhận hàng (chi tiết bất thường nằm ở đây)")]
    [SerializeField] private DialogueLine[] deliverLines;
    [Tooltip("Ghi nhận thông tin này như 1 manh mối (để trống = không)")]
    [SerializeField] private string clueId;

    [Header("=== CACH XUAT HIEN (để trống hết = NPC đứng chờ sẵn từ lúc nhặt thùng) ===")]
    [Tooltip("Bản lề cửa nhà (cha của cánh cửa, giống DoorOpen): cầm hàng tới gần → cửa mở lộ NPC đứng BÊN TRONG cửa, giao xong cửa đóng")]
    [SerializeField] private Transform doorHinge;
    [Tooltip("Góc mở (độ). Âm = mở chiều ngược lại")]
    [SerializeField] private float doorAngle = 90f;
    [Tooltip("Hù: cầm hàng tới gần → NPC hiện ra SAU LƯNG người chơi, cách chừng này mét (0 = tắt)")]
    [SerializeField] private float appearBehind = 0f;
    [Tooltip("Tiếng cửa kẽo kẹt / tiếng hù lúc NPC xuất hiện (để trống = không)")]
    [SerializeField] private AudioSource appearSound;
    [Tooltip("NPC ĐI BỘ từ điểm này (trong nhà sau cánh cửa / cuối hành lang) tới chỗ đứng đã đặt sẵn trong scene; giao xong đi ngược về rồi ẩn. " +
             "Animator cần Bool \"Walking\" (Tools → Tạo Animator NPC (Đứng ⇄ Đi)). Có điểm này thì bỏ qua Appear Behind")]
    [SerializeField] private Transform walkFrom;
    [Tooltip("m/giây")]
    [SerializeField] private float walkSpeed = 1.1f;

    [Header("=== SAU KHI GIAO ===")]
    [Tooltip("Bật các object này (vd: NPC đứng ở cửa → ẩn, cửa đóng lại...)")]
    [SerializeField] private GameObject[] activateOnDelivered;
    [SerializeField] private UnityEvent onDelivered;

    bool playerNearby, busy, npcShown, moving;
    Quaternion doorClosed;
    Coroutine doorMove;
    Vector3 standPos;
    Quaternion standRot;
    Animator npcAnim;

    // vùng bấm E tối thiểu ~2.5m (mét thật, đã tính scale) — nhỏ quá phải dí sát mặt NPC mới bấm được
    void Awake()
    {
        if (doorHinge != null) doorClosed = doorHinge.localRotation;
        if (npc != null)
        {
            standPos = npc.transform.position;
            standRot = npc.transform.rotation;
            npcAnim = npc.GetComponentInChildren<Animator>(true);
        }
        if (!TryGetComponent(out BoxCollider box)) return;
        Vector3 s = transform.lossyScale;
        box.size = new Vector3(Mathf.Max(box.size.x, 2.5f / Mathf.Abs(s.x)),
                               Mathf.Max(box.size.y, 2f / Mathf.Abs(s.y)),
                               Mathf.Max(box.size.z, 2.5f / Mathf.Abs(s.z)));
    }

    bool Waiting() => day != null && day.IsRunning && !day.IsDelivered(orderIndex);
    bool HoldingMine() => day.Carrying == orderIndex;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || !Waiting()) return;
        playerNearby = true;
        if (QuestUI.Instance == null) return;
        if (HoldingMine()) QuestUI.Instance.ShowNotification(hintText);
        else if (day.Carrying < 0) QuestUI.Instance.ShowNotification(emptyHandText);
        else QuestUI.Instance.ShowNotification($"{wrongBoxText} (đang cầm hàng {day.OrderApartment(day.Carrying)})");
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) playerNearby = false;
    }

    void Update()
    {
        if (!playerNearby || busy || moving || !Waiting() || !HoldingMine()) return;
        if (DialogueManager.Instance.IsActive) return;
        if (Keyboard.current.eKey.wasPressedThisFrame && Facing()) StartCoroutine(DeliverRoutine());
    }

    // vùng bấm rộng chồng lên thang máy/cửa khác → phải nhìn về phía NPC mới giao (kẻo bấm E đi thang máy cũng giao luôn)
    bool Facing()
    {
        Camera cam = Camera.main;
        if (cam == null) return true;
        Vector3 to = (npc != null ? npc.transform.position : transform.position) - cam.transform.position;
        Vector3 fwd = cam.transform.forward;
        to.y = fwd.y = 0f;
        return to.sqrMagnitude < 0.01f || Vector3.Angle(fwd, to) < 60f;
    }

    // tự bật/tắt theo trạng thái → đúng cả sau khi tải save
    void LateUpdate()
    {
        if (npc == null || day == null) return;
        // mở cửa / hù sau lưng: chờ người chơi tới gần mới xuất hiện (đã hiện rồi thì ở lại tới khi giao xong)
        bool whenNear = doorHinge != null || appearBehind > 0f || walkFrom != null;
        bool show = busy || (Waiting() && HoldingMine() && (!whenNear || playerNearby || npcShown));
        // chỉ bật/tắt lúc đổi trạng thái → ngày sau dùng lại NPC này không bị ngày trước giành tắt
        if (show != npcShown) SetNpc(show);
    }

    void SetNpc(bool show)
    {
        npcShown = show;
        if (npc == null) return;
        if (show && appearBehind > 0f && walkFrom == null) PlaceBehindPlayer();
        if (show && appearSound != null) appearSound.Play();
        if (doorHinge == null && walkFrom == null) { npc.SetActive(show); return; }
        if (doorMove != null) StopCoroutine(doorMove);
        doorMove = StartCoroutine(show ? Appear() : Leave());
    }

    // hiện ở điểm xuất phát → mở cửa → đi tới chỗ đứng → quay mặt về người chơi
    IEnumerator Appear()
    {
        moving = walkFrom != null;
        npc.SetActive(true);
        if (walkFrom != null)
        {
            Vector3 start = walkFrom.position;
            start.y = standPos.y;   // giữ độ cao sàn của chỗ đứng (điểm xuất phát đặt cao thấp không sao)
            npc.transform.SetPositionAndRotation(start, FlatLook(standPos - start, npc.transform.rotation));
        }
        if (doorHinge != null) yield return SwingDoor(true);
        if (walkFrom != null)
        {
            yield return WalkTo(standPos);
            Camera cam = Camera.main;
            Quaternion from = npc.transform.rotation;
            Quaternion to = cam != null ? FlatLook(cam.transform.position - standPos, standRot) : standRot;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.4f)
            {
                npc.transform.rotation = Quaternion.Slerp(from, to, t);
                yield return null;
            }
            npc.transform.rotation = to;
        }
        moving = false;
    }

    // đi ngược về điểm xuất phát → đóng cửa → ẩn
    IEnumerator Leave()
    {
        if (walkFrom != null && npc.activeSelf) { moving = true; yield return WalkTo(walkFrom.position); }
        if (doorHinge != null) yield return SwingDoor(false);
        npc.SetActive(false);
        if (walkFrom != null) npc.transform.SetPositionAndRotation(standPos, standRot);   // trả chỗ cũ cho lần sau / save
        moving = false;
    }

    // shortcut: đi thẳng, không NavMesh — giữa điểm xuất phát và chỗ đứng phải trống, cần né vật cản thì đổi sang NavMeshAgent
    IEnumerator WalkTo(Vector3 target)
    {
        SetWalking(true);
        Transform t = npc.transform;
        target.y = t.position.y;
        while ((target - t.position).sqrMagnitude > 0.0025f)
        {
            t.rotation = Quaternion.Slerp(t.rotation, FlatLook(target - t.position, t.rotation), Time.deltaTime * 8f);
            t.position = Vector3.MoveTowards(t.position, target, walkSpeed * Time.deltaTime);
            yield return null;
        }
        t.position = target;
        SetWalking(false);
    }

    void SetWalking(bool on)
    {
        if (npcAnim == null || npcAnim.runtimeAnimatorController == null) return;
        foreach (AnimatorControllerParameter p in npcAnim.parameters)
            if (p.name == "Walking" && p.type == AnimatorControllerParameterType.Bool) { npcAnim.SetBool("Walking", on); return; }
    }

    static Quaternion FlatLook(Vector3 dir, Quaternion fallback)
    {
        dir.y = 0f;
        return dir.sqrMagnitude < 1e-4f ? fallback : Quaternion.LookRotation(dir);
    }

    void PlaceBehindPlayer()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        Vector3 back = -cam.transform.forward;
        back.y = 0f;
        if (back.sqrMagnitude < 0.01f) return;
        back.Normalize();
        // sau lưng là tường → đứng sát tường chứ không chui vào trong tường
        float d = appearBehind;
        if (Physics.Raycast(cam.transform.position, back, out RaycastHit hit, d, ~0, QueryTriggerInteraction.Ignore))
            d = Mathf.Max(0.5f, hit.distance - 0.4f);
        Vector3 pos = cam.transform.position + back * d;
        pos.y = npc.transform.position.y; // giữ độ cao sàn đã đặt sẵn trong scene
        npc.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(-back));
    }

    // mở: NPC (đứng sẵn sau cánh cửa) hiện trước rồi cửa mở ra; đóng: cửa khép xong NPC mới ẩn (Appear/Leave lo bật/tắt NPC)
    IEnumerator SwingDoor(bool open)
    {
        Quaternion from = doorHinge.localRotation;
        Quaternion to = open ? doorClosed * Quaternion.Euler(0f, doorAngle, 0f) : doorClosed;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.8f)
        {
            doorHinge.localRotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        doorHinge.localRotation = to;
    }

    IEnumerator DeliverRoutine()
    {
        busy = true;
        playerNearby = false;
        if (QuestUI.Instance != null) QuestUI.Instance.ShowNotification("Đã giao " + day.OrderLabel(orderIndex));

        // đưa hàng → thùng trên tay biến mất ngay (đơn cuối: DeliveryDay tự chờ thoại xong mới chốt ngày)
        day.Deliver(orderIndex);
        if (!string.IsNullOrEmpty(clueId)) ClueItem.MarkFound(clueId);
        foreach (GameObject go in activateOnDelivered)
            if (go != null) go.SetActive(true);

        if (deliverLines != null && deliverLines.Length > 0)
        {
            DialogueManager.Instance.StartDialogue(deliverLines);
            yield return null;
            while (DialogueManager.Instance.IsActive) yield return null;
        }

        onDelivered?.Invoke();
        busy = false;
        // giao xong → NPC vào nhà luôn (không chờ LateUpdate)
        SetNpc(false);
    }

    // chơi lại từ đầu → reset (giống ClueItem)
    void OnDestroy()
    {
        if (!string.IsNullOrEmpty(clueId)) ClueItem.ForgetFound(clueId);
    }
}
