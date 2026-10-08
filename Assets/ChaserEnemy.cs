using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// Kẻ địch đuổi theo Player (vd: giang hồ quay về 302).
/// Bật object lên → (hét 1 câu) → chờ vài giây cho Player kịp chạy → đuổi.
/// Bắt được (lại quá gần) → On Caught (vd: EndScreenManager.ShowBadEnd).
/// Có NavMeshAgent + đã Bake NavMesh → đi vòng tránh tường. Không có → đi thẳng tới Player.
/// </summary>
public class ChaserEnemy : MonoBehaviour
{
    [SerializeField] private float speed = 3.2f;
    [Tooltip("Chờ bao lâu rồi mới bắt đầu đuổi (giây) — cho Player kịp phản ứng")]
    [SerializeField] private float startDelay = 1.5f;
    [Tooltip("Lại gần hơn khoảng này = bị bắt")]
    [SerializeField] private float catchDistance = 1.2f;
    [Tooltip("Player ở xa hơn khoảng này (vd: đã trèo ra ban công) = bỏ cuộc, đứng yên")]
    [SerializeField] private float giveUpDistance = 40f;

    [Header("=== KHI XUAT HIEN ===")]
    [Tooltip("Câu hét khi phát hiện Player (hiện ở ô thông báo, không dừng game)")]
    [SerializeField] private string shout = "GIANG HỒ: Đứa nào trong đó?!";
    [SerializeField] private AudioSource shoutSound;
    [Tooltip("Tên tham số tốc độ trong Animator (để trống = không dùng)")]
    [SerializeField] private string animSpeedParam = "Speed";

    [Header("=== DAP CUA XONG VAO ===")]
    [Tooltip("Bản lề cánh cửa chính (cha của cánh cửa, giống DoorOpen). Để trống = không đạp cửa")]
    [SerializeField] private Transform kickDoorHinge;
    [Tooltip("Cửa 2 cánh: bản lề cánh thứ 2 (bật ngược chiều cánh 1). Để trống = cửa 1 cánh")]
    [SerializeField] private Transform kickDoorHinge2;
    [Tooltip("Góc cửa bật ra (độ). Âm = chiều ngược lại")]
    [SerializeField] private float kickAngle = 100f;
    [Tooltip("Cửa bật mở trong bao lâu (giây) — càng nhỏ càng mạnh")]
    [SerializeField] private float kickTime = 0.15f;
    [Tooltip("Tiếng RẦM (Output = SFX)")]
    [SerializeField] private AudioSource kickSound;
    [Tooltip("Chờ bao lâu từ lúc xuất hiện tới lúc đạp (giây) — cho kịp nghe tiếng bước chân ngoài cửa")]
    [SerializeField] private float kickDelay = 1f;
    [Tooltip("Điểm ngay trong cửa: hắn chạy tới đây trước rồi mới đuổi (để không đi xuyên tường)")]
    [SerializeField] private Transform entryPoint;
    [SerializeField] private float camShake = 0.12f;

    [Header("=== BI BAT ===")]
    [SerializeField] private UnityEvent onCaught;

    [Header("=== CHONG TRA (cần có dao) ===")]
    [Tooltip("Có vật này (Clue Id, vd: con_dao) thì khi hắn lại gần sẽ được chống trả thay vì bị bắt ngay")]
    [SerializeField] private string fightItemId = "con_dao";
    [Tooltip("Hắn lại gần trong khoảng này → bắt đầu vật lộn")]
    [SerializeField] private float fightRange = 2.2f;
    [Tooltip("Phải bấm E bao nhiêu lần...")]
    [SerializeField] private int pressesNeeded = 15;
    [Tooltip("...trong bao nhiêu giây")]
    [SerializeField] private float fightTime = 4f;
    [SerializeField] private AudioSource fightSound;
    [Tooltip("Thắng: hắn gục (vd: tắt hắn đi, EndScreenManager.ShowGoodEnd)")]
    [SerializeField] private UnityEvent onWinFight;

    Transform player;
    NavMeshAgent agent;
    Animator anim;
    bool chasing, caught, fighting;

    void OnEnable()
    {
        GameObject p = GameObject.FindWithTag("Player");
        player = p != null ? p.transform : null;
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        if (agent != null) { agent.speed = speed; agent.isStopped = true; }
        StartCoroutine(Begin());
    }

    IEnumerator Begin()
    {
        // chờ thoại (vd: cô gái "Chúng nó sắp về rồi!") xong mới tới cảnh đạp cửa
        yield return null;
        while (DialogueManager.Instance != null && DialogueManager.Instance.IsActive) yield return null;

        // 1. Đạp cửa
        if (kickDoorHinge != null)
        {
            yield return new WaitForSeconds(kickDelay);
            if (kickSound != null) kickSound.Play();
            StartCoroutine(ShakeCamera(0.25f));
            Quaternion from = kickDoorHinge.localRotation;
            Quaternion to = from * Quaternion.Euler(0f, kickAngle, 0f);
            Quaternion from2 = kickDoorHinge2 != null ? kickDoorHinge2.localRotation : Quaternion.identity;
            Quaternion to2 = from2 * Quaternion.Euler(0f, -kickAngle, 0f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, kickTime))
            {
                kickDoorHinge.localRotation = Quaternion.Slerp(from, to, t);
                if (kickDoorHinge2 != null) kickDoorHinge2.localRotation = Quaternion.Slerp(from2, to2, t);
                yield return null;
            }
            kickDoorHinge.localRotation = to;
            if (kickDoorHinge2 != null) kickDoorHinge2.localRotation = to2;
        }

        if (!string.IsNullOrEmpty(shout) && QuestUI.Instance != null) QuestUI.Instance.ShowNotification(shout);
        if (shoutSound != null) shoutSound.Play();
        // 2. Chạy qua cửa vào trong phòng
        if (entryPoint != null)
        {
            if (agent != null && agent.isOnNavMesh) { agent.isStopped = false; agent.SetDestination(entryPoint.position); }
            while (Vector3.Distance(Flat(transform.position), Flat(entryPoint.position)) > 0.3f)
            {
                if (agent == null || !agent.isOnNavMesh)
                    transform.position = Vector3.MoveTowards(transform.position,
                        new Vector3(entryPoint.position.x, transform.position.y, entryPoint.position.z), speed * 1.3f * Time.deltaTime);
                Vector3 d = entryPoint.position - transform.position; d.y = 0f;
                if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(d);
                SetAnim(speed);
                yield return null;
            }
        }

        yield return new WaitForSeconds(startDelay);
        chasing = true;
    }

    void Update()
    {
        if (!chasing || caught || fighting || player == null) { SetAnim(0f); return; }
        // đang đọc thoại (player không đi được) → đứng chờ, không bắt "ăn gian"
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive) { Stop(); return; }

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > giveUpDistance) { Stop(); return; }

        bool hasWeapon = !string.IsNullOrEmpty(fightItemId) && ClueItem.HasFound(fightItemId);
        if (hasWeapon && dist <= fightRange)
        {
            StartCoroutine(Fight());
            return;
        }

        if (dist <= catchDistance)
        {
            caught = true;
            Stop();
            onCaught?.Invoke();
            return;
        }

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
        else
        {
            // không có NavMesh → đi thẳng (có thể xuyên đồ đạc, đủ dùng cho prototype)
            Vector3 target = new Vector3(player.position.x, transform.position.y, player.position.z);
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        }

        Vector3 dir = player.position - transform.position; dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
        SetAnim(speed);
    }

    // Vật lộn: bấm E liên tục cho đủ số lần trong thời gian giới hạn
    IEnumerator Fight()
    {
        fighting = true;
        Stop();
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false; // đứng yên vật lộn
        if (fightSound != null) fightSound.Play();

        int presses = 0;
        float timeLeft = fightTime;
        while (timeLeft > 0f && presses < pressesNeeded)
        {
            if (Keyboard.current.eKey.wasPressedThisFrame) presses++;
            timeLeft -= Time.deltaTime;
            if (QuestUI.Instance != null)
                QuestUI.Instance.SetNotificationText($"BẤM LIÊN TỤC [E] ĐỂ CHỐNG TRẢ!  {presses}/{pressesNeeded}");
            yield return null;
        }

        if (cc != null) cc.enabled = true;
        caught = true; // dù thắng hay thua cũng không đuổi nữa

        if (presses >= pressesNeeded) onWinFight?.Invoke();
        else onCaught?.Invoke();
    }

    static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

    IEnumerator ShakeCamera(float duration)
    {
        Camera cam = Camera.main;
        if (cam == null || camShake <= 0f) yield break;
        Transform t = cam.transform;
        Vector3 basePos = t.localPosition;
        for (float k = 0f; k < duration; k += Time.deltaTime)
        {
            t.localPosition = basePos + Random.insideUnitSphere * camShake;
            yield return null;
        }
        t.localPosition = basePos;
    }

    void Stop()
    {
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
        SetAnim(0f);
    }

    void SetAnim(float v)
    {
        if (anim == null || string.IsNullOrEmpty(animSpeedParam)) return;
        foreach (AnimatorControllerParameter p in anim.parameters)
            if (p.name == animSpeedParam && p.type == AnimatorControllerParameterType.Float) { anim.SetFloat(animSpeedParam, v); return; }
    }
}
