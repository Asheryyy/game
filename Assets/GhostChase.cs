using UnityEngine;
using UnityEngine.AI;

public class GhostChase : MonoBehaviour
{
    [Header("=== PATROL / SCARE ===")]
    [Tooltip("Kéo Player vào đây (hoặc để trống, script tự tìm theo Tag 'Player')")]
    public Transform player;

    [Tooltip("Player vào trong khoảng này → con ma kích hoạt")]
    public float patrolTriggerRange = 8f;

    [Tooltip("Điểm A — vị trí bắt đầu lướt")]
    public Transform waypointA;

    [Tooltip("Điểm B — vị trí kết thúc lướt")]
    public Transform waypointB;

    [Header("Chọn chế độ")]
    [Tooltip("TRUE = lướt qua lại liên tục (dùng NavMesh) | FALSE = hù 1 lần rồi đứng yên (di chuyển thẳng, không cần NavMesh)")]
    public bool loopPatrol = false;

    [Tooltip("Tốc độ lướt khi patrol liên tục")]
    public float patrolSpeed = 2.5f;

    [Tooltip("Tốc độ lao khi hù (1 lần)")]
    public float dashSpeed = 10f;

    [Header("=== ÂM THANH ===")]
    [Tooltip("AudioSource chứa nhạc căng thẳng")]
    public AudioSource tenseMusicSource;
    [Tooltip("Tốc độ fade in/out âm lượng")]
    public float fadeSpeed = 1.5f;
    [Tooltip("Volume tối đa khi Player trong vùng")]
    public float maxVolume = 0.8f;

    [Header("=== PHAN UNG CUA NHAN VAT ===")]
    [Tooltip("Nhân vật tự nói sau khi ma lao tới B (chỉ 1 lần)")]
    public DialogueLine[] reactionLines;
    [Tooltip("Chờ bao lâu sau khi ma dừng mới nói (giây)")]
    public float reactionDelay = 0.5f;

    // ──────────────────────────────────────────────
    NavMeshAgent agent;
    bool hasReacted    = false;
    bool patrolActive  = false;
    bool hasDashed     = false;
    bool isDashing     = false;
    int  currentWaypoint = 0;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
            else Debug.LogError("[GhostChase] Không tìm thấy Tag 'Player'!", this);
        }

        // Tắt agent lúc đầu — chế độ hù dùng MoveTowards, không cần agent
        if (agent != null)
        {
            agent.isStopped = agent.isOnNavMesh;
            if (!loopPatrol) agent.enabled = false; // hù 1 lần: tắt hẳn NavMesh
        }
    }

    void Update()
    {
        if (player == null || waypointA == null || waypointB == null) return;

        float dist       = Vector3.Distance(transform.position, player.position);
        bool  playerNear = dist <= patrolTriggerRange;

        // ── Fade nhạc theo vùng ──
        HandleMusic(playerNear);

        // ══ CHẾ ĐỘ HÙ 1 LẦN (không cần NavMesh) ══
        if (!loopPatrol)
        {
        if (playerNear && !hasDashed)
            {
                hasDashed  = true;
                isDashing  = true;
                // KHÔNG warp — con ma lướt từ chỗ đang đứng (WaypointA) đến B
                // → Nhớ đặt con ma tại WaypointA trong Editor!
                Debug.Log("[Ghost] HÙ! Lao từ A → B!");
            }

            if (isDashing)
            {
                // Di chuyển thẳng từ A → B, không cần NavMesh
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    waypointB.position,
                    dashSpeed * Time.deltaTime
                );

                // Quay mặt về hướng đang chạy
                Vector3 dir = (waypointB.position - transform.position);
                if (dir.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.LookRotation(dir);

                // Tới B rồi → dừng
                if (Vector3.Distance(transform.position, waypointB.position) < 0.2f)
                {
                    isDashing = false;
                    Debug.Log("[Ghost] Đã tới B, dừng lại.");
                    if (!hasReacted && reactionLines != null && reactionLines.Length > 0)
                    {
                        hasReacted = true;
                        Invoke(nameof(PlayReaction), reactionDelay);
                    }
                }
            }

            // Reset khi player rời đi (để lần sau vào lại vẫn bị hù)
            if (!playerNear && hasDashed && !isDashing)
            {
                hasDashed = false;
                Debug.Log("[Ghost] Player rời đi → reset.");
            }

            return;
        }

        // ══ CHẾ ĐỘ PATROL LIÊN TỤC (dùng NavMesh) ══
        if (agent == null || !agent.isOnNavMesh) return;

        if (!playerNear)
        {
            if (patrolActive)
            {
                patrolActive    = false;
                agent.isStopped = true;
                agent.ResetPath();
                Debug.Log("[Ghost] Player rời đi → dừng patrol.");
            }
            return;
        }

        if (!patrolActive)
        {
            patrolActive    = true;
            currentWaypoint = 0;
            agent.speed     = patrolSpeed;
            agent.isStopped = false;
            agent.SetDestination(waypointA.position);
            Debug.Log("[Ghost] Bắt đầu lướt qua lại!");
        }

        if (!agent.pathPending && agent.remainingDistance <= 0.5f)
        {
            currentWaypoint = (currentWaypoint == 0) ? 1 : 0;
            Transform next  = (currentWaypoint == 0) ? waypointA : waypointB;
            agent.SetDestination(next.position);
        }
    }

    void PlayReaction()
    {
        if (DialogueManager.Instance != null && !DialogueManager.Instance.IsActive)
            DialogueManager.Instance.StartDialogue(reactionLines);
    }

    // Fade in khi Player vào, fade out khi ra
    void HandleMusic(bool playerNear)
    {
        if (tenseMusicSource == null) return;

        if (playerNear)
        {
            if (!tenseMusicSource.isPlaying) tenseMusicSource.Play();
            tenseMusicSource.volume = Mathf.MoveTowards(
                tenseMusicSource.volume, maxVolume, fadeSpeed * Time.deltaTime);
        }
        else
        {
            tenseMusicSource.volume = Mathf.MoveTowards(
                tenseMusicSource.volume, 0f, fadeSpeed * Time.deltaTime);
            if (tenseMusicSource.volume <= 0f && tenseMusicSource.isPlaying)
                tenseMusicSource.Stop();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, patrolTriggerRange);

        if (waypointA != null && waypointB != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(waypointA.position, waypointB.position);
            Gizmos.DrawSphere(waypointA.position, 0.3f);
            Gizmos.DrawSphere(waypointB.position, 0.3f);
        }
    }
}


