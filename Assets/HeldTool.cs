using UnityEngine;

/// <summary>
/// Đồ cầm tay góc nhìn thứ nhất (vd: cây chổi): đặt làm CON của camera, góc dưới phải màn hình.
/// - Đứng yên: thở nhẹ.  - Đi lại: lắc lư theo bước chân.
/// - Đang giữ E dọn dẹp (ChoreItem): quét qua quét lại.
/// - Đang hội thoại / đọc giấy: hạ xuống khuất màn hình.
/// Tắt sẵn object này; nhặt chổi thì bật lên (ClueItem → Activate On Found),
/// dọn xong thì tắt (ChoreGroup → On All Done → SetActive false).
/// </summary>
public class HeldTool : MonoBehaviour
{
    [Header("=== QUET (khi giữ E) ===")]
    [Tooltip("Góc quét qua lại (độ)")]
    [SerializeField] private float sweepAngle = 35f;
    [Tooltip("Số lần quét mỗi giây")]
    [SerializeField] private float sweepSpeed = 2.2f;
    [Tooltip("Chổi chúi xuống bao nhiêu độ khi quét")]
    [SerializeField] private float sweepTilt = 20f;

    [Header("=== LAC LU ===")]
    [SerializeField] private float bobAmount = 0.015f;
    [SerializeField] private float bobSpeed = 9f;
    [Tooltip("Hạ xuống bao nhiêu khi đang thoại/đọc giấy (m)")]
    [SerializeField] private float hideDrop = 0.6f;
    [SerializeField] private float smooth = 10f;

    Vector3 restPos;
    Quaternion restRot;
    CharacterController cc;
    float sweepT, bobT, sweepWeight, hideWeight;

    void Awake()
    {
        restPos = transform.localPosition;
        restRot = transform.localRotation;
    }

    void OnEnable()
    {
        GameObject p = GameObject.FindWithTag("Player");
        cc = p != null ? p.GetComponent<CharacterController>() : null;
        sweepWeight = 0f;
        hideWeight = 1f; // mới nhặt: đưa từ dưới lên
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;

        bool busy = (DialogueManager.Instance != null && DialogueManager.Instance.IsActive) ||
                    (DocumentViewer.Instance != null && DocumentViewer.Instance.IsOpen);
        bool sweeping = ChoreItem.AnyWorking && !busy;

        sweepWeight = Mathf.MoveTowards(sweepWeight, sweeping ? 1f : 0f, dt * 4f);
        hideWeight  = Mathf.MoveTowards(hideWeight, busy ? 1f : 0f, dt * 3f);

        // quét: lắc ngang theo hình sin
        if (sweeping) sweepT += dt * sweepSpeed * Mathf.PI * 2f;
        float swing = Mathf.Sin(sweepT) * sweepAngle * sweepWeight;

        // lắc lư theo bước chân
        float speed = cc != null && cc.enabled ? new Vector3(cc.velocity.x, 0f, cc.velocity.z).magnitude : 0f;
        bobT += dt * (speed > 0.1f ? bobSpeed : 1.5f);
        float bobScale = speed > 0.1f ? 1f : 0.3f;
        Vector3 bob = new Vector3(Mathf.Cos(bobT * 0.5f) * bobAmount, Mathf.Sin(bobT) * bobAmount, 0f) * bobScale;

        Vector3 pos = restPos + bob
                    + new Vector3(Mathf.Sin(sweepT) * 0.12f * sweepWeight, -0.05f * sweepWeight, 0.05f * sweepWeight)
                    + Vector3.down * hideDrop * hideWeight;
        Quaternion rot = restRot * Quaternion.Euler(sweepTilt * sweepWeight, 0f, swing);

        transform.localPosition = Vector3.Lerp(transform.localPosition, pos, dt * smooth);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, rot, dt * smooth);
    }
}
