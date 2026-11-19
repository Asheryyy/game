using UnityEngine;

/// <summary>
/// NPC đang làm việc riêng (bảo vệ tập võ, chị bán phở bưng phở đi lại, đứa nhỏ chạy chơi, ông già đi bộ...):
/// người chơi lại gần → dừng, quay mặt về phía người chơi (để giao/nhận hàng); đi xa → làm tiếp.
/// Gắn vào object gốc của NPC (tự thêm trigger tròn nếu chưa có).
/// Animator có tham số Bool "Training" (Tools → Tạo Animator ...) thì chuyển Hoạt động ⇄ Đứng;
/// không có (vd: OngGia_Animator chỉ có 1 clip đi bộ) thì đứng hình clip lúc dừng.
/// Muốn NPC hiện ra lúc nhặt thùng: TẮT sẵn, kéo vào ô Npc của DeliveryDoor — hiện ra là hoạt động ngay.
/// </summary>
public class GuardTraining : MonoBehaviour
{
    [Tooltip("Người chơi lại gần trong bán kính này (m) thì dừng")]
    [SerializeField] private float stopRadius = 3f;
    [Tooltip("Tốc độ quay mặt")]
    [SerializeField] private float turnSpeed = 5f;

    [Header("=== ĐI LẠI (để trống = đứng tại chỗ) ===")]
    [Tooltip("Các điểm đi vòng lần lượt rồi lặp lại. KHÔNG để làm con của NPC này (sẽ chạy theo NPC)")]
    [SerializeField] private Transform[] waypoints;
    [Tooltip("m/giây — đi bộ ~1.2, chạy/đạp xe ~3.5")]
    [SerializeField] private float moveSpeed = 1.2f;

    Animator anim;
    Transform player;
    bool hasParam;
    int wp;

    void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        if (anim != null) anim.applyRootMotion = false; // script tự di chuyển, không để clip kéo đi
        SphereCollider s = gameObject.AddComponent<SphereCollider>();
        s.isTrigger = true;
        s.radius = stopRadius / Mathf.Max(0.01f, transform.lossyScale.x);
    }

    void Start()
    {
        if (anim == null || anim.runtimeAnimatorController == null) return;
        foreach (AnimatorControllerParameter p in anim.parameters)
            if (p.name == "Training") hasParam = true;
    }

    void SetActivity(bool on)
    {
        if (anim == null) return;
        if (hasParam) anim.SetBool("Training", on);
        else anim.speed = on ? 1f : 0f;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        player = other.transform;
        SetActivity(false);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        player = null;
        SetActivity(true);
    }

    // bị DeliveryDoor tắt lúc người chơi còn đứng gần → lần hiện sau không bị kẹt ở trạng thái "đang dừng"
    void OnDisable()
    {
        player = null;
        if (anim != null) anim.speed = 1f;
    }

    void Update()
    {
        if (player != null) { Face(player.position - transform.position); return; }
        if (waypoints == null || waypoints.Length == 0 || waypoints[wp] == null) return;

        // shortcut: đi thẳng giữa các điểm trên mặt phẳng (không NavMesh, không lên dốc/cầu thang), cần né vật cản thì đổi sang NavMeshAgent
        Vector3 target = waypoints[wp].position;
        target.y = transform.position.y;
        if ((target - transform.position).sqrMagnitude < 0.04f) { wp = (wp + 1) % waypoints.Length; return; }
        Face(target - transform.position);
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
    }

    void Face(Vector3 dir)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * turnSpeed);
    }
}
