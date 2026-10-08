using UnityEngine;
using System.Collections;

/// <summary>
/// Gắn vào 1 Box Collider (Is Trigger) phủ hành lang.
/// Player vào → sương mù đen dày + trời đen: chỉ thấy vài mét quanh mình.
/// Player ra → trả lại như cũ.
/// </summary>
[RequireComponent(typeof(Collider))]
public class DarkZone : MonoBehaviour
{
    [Header("=== SUONG MU ===")]
    [SerializeField] private Color fogColor = Color.black;
    [Tooltip("Bắt đầu tối từ khoảng cách này (m)")]
    [SerializeField] private float fogStart = 2f;
    [Tooltip("Tới khoảng cách này là đen hẳn (m)")]
    [SerializeField] private float fogEnd = 12f;

    [Header("=== ANH SANG ===")]
    [Tooltip("Ánh sáng môi trường khi ở trong (càng tối càng đáng sợ)")]
    [SerializeField] private Color ambientColor = new Color(0.05f, 0.05f, 0.07f);
    [SerializeField] private float fadeDuration = 1f;
    [Tooltip("TRUE = chỉ tối khi đang là ban đêm (TimeOfDaySwitch). Ban ngày đi qua bình thường")]
    [SerializeField] private bool onlyAtNight = true;

    // lưu cài đặt gốc để trả lại khi ra
    bool savedFog; Color savedFogColor; FogMode savedFogMode;
    float savedStart, savedEnd; Color savedAmbient;
    CameraClearFlags savedClear; Color savedBg;
    Camera cam;
    Coroutine fading;
    bool inside;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void Start() => cam = Camera.main;

    void SaveSettings()
    {
        savedFog      = RenderSettings.fog;
        savedFogColor = RenderSettings.fogColor;
        savedFogMode  = RenderSettings.fogMode;
        savedStart    = RenderSettings.fogStartDistance;
        savedEnd      = RenderSettings.fogEndDistance;
        savedAmbient  = RenderSettings.ambientLight;
        if (cam != null) { savedClear = cam.clearFlags; savedBg = cam.backgroundColor; }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || inside) return;
        if (onlyAtNight && TimeOfDaySwitch.Instance != null && !TimeOfDaySwitch.Instance.IsNight) return;
        // lưu lúc bước vào (không phải lúc Start) vì trời có thể đã đổi chiều → đêm
        if (fading == null) SaveSettings();
        inside = true;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        // trời (skybox) không bị sương che → đổi nền camera sang đen
        if (cam != null) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = fogColor; }
        StartFade(fogColor, fogStart, fogEnd, ambientColor, null);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player") || !inside) return;
        inside = false;
        StartFade(savedFogColor, savedStart, Mathf.Max(savedEnd, 300f), savedAmbient, () =>
        {
            RenderSettings.fog = savedFog;
            RenderSettings.fogMode = savedFogMode;
            RenderSettings.fogStartDistance = savedStart;
            RenderSettings.fogEndDistance = savedEnd;
            if (cam != null) { cam.clearFlags = savedClear; cam.backgroundColor = savedBg; }
        });
    }

    void StartFade(Color color, float start, float end, Color ambient, System.Action onDone)
    {
        if (fading != null) StopCoroutine(fading);
        fading = StartCoroutine(Fade(color, start, end, ambient, onDone));
    }

    IEnumerator Fade(Color color, float start, float end, Color ambient, System.Action onDone)
    {
        Color c0 = RenderSettings.fogColor, a0 = RenderSettings.ambientLight;
        // lúc mới vào: kéo sương từ xa lại gần cho mượt
        float s0 = inside ? 50f : RenderSettings.fogStartDistance;
        float e0 = inside ? 300f : RenderSettings.fogEndDistance;
        for (float t = 0; t < 1f; t += Time.deltaTime / fadeDuration)
        {
            RenderSettings.fogColor         = Color.Lerp(c0, color, t);
            RenderSettings.fogStartDistance = Mathf.Lerp(s0, start, t);
            RenderSettings.fogEndDistance   = Mathf.Lerp(e0, end, t);
            RenderSettings.ambientLight     = Color.Lerp(a0, ambient, t);
            yield return null;
        }
        RenderSettings.fogColor = color;
        RenderSettings.fogStartDistance = start;
        RenderSettings.fogEndDistance = end;
        RenderSettings.ambientLight = ambient;
        onDone?.Invoke();
        fading = null;
    }

    void OnDestroy()
    {
        // thoát Play / đổi scene giữa chừng → trả lại cài đặt gốc
        if (!inside) return;
        RenderSettings.fog = savedFog; RenderSettings.fogColor = savedFogColor;
        RenderSettings.fogMode = savedFogMode; RenderSettings.ambientLight = savedAmbient;
        RenderSettings.fogStartDistance = savedStart; RenderSettings.fogEndDistance = savedEnd;
    }
}
