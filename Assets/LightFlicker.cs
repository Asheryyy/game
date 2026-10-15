using UnityEngine;

/// <summary>
/// Đèn chập chờn: độ sáng dao động nhẹ liên tục, thỉnh thoảng tắt phụt vài lần rồi sáng lại.
/// Gắn vào object có Light (vd: đèn sau ô cửa sổ phòng 302 ở menu, đèn hành lang).
/// </summary>
[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour
{
    [SerializeField] private float baseIntensity = 2f;
    [Tooltip("Dao động nhẹ quanh mức sáng gốc (0 = đứng yên)")]
    [SerializeField] private float jitter = 0.25f;
    [Tooltip("Khoảng thời gian ngẫu nhiên giữa 2 lần chập chờn mạnh (giây)")]
    [SerializeField] private Vector2 burstEvery = new Vector2(3f, 8f);
    [Tooltip("Số lần tắt-bật trong 1 lần chập chờn")]
    [SerializeField] private Vector2Int blinks = new Vector2Int(2, 5);

    Light lamp;
    float nextBurst, burstEnd;

    void Awake()
    {
        lamp = GetComponent<Light>();
        nextBurst = Time.unscaledTime + Random.Range(burstEvery.x, burstEvery.y);
    }

    void Update()
    {
        float t = Time.unscaledTime;

        if (t >= nextBurst)
        {
            // mỗi lần nháy ~0.08s
            burstEnd = t + Random.Range(blinks.x, blinks.y + 1) * 0.08f;
            nextBurst = burstEnd + Random.Range(burstEvery.x, burstEvery.y);
        }

        if (t < burstEnd)
            lamp.intensity = Random.value < 0.5f ? 0f : baseIntensity;   // tắt-bật loạn
        else
            lamp.intensity = baseIntensity + (Mathf.PerlinNoise(t * 3f, 0f) - 0.5f) * 2f * jitter; // dao động êm
    }
}
