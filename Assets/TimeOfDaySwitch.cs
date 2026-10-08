using UnityEngine;

/// <summary>
/// Chuyển cảnh Chiều ↔ Đêm: đổi mặt trời, bầu trời, ánh sáng môi trường, bật/tắt mưa.
/// Gắn vào 1 GameObject trống. Gọi SetNight() / SetAfternoon() từ event
/// (vd: TeleportZone → On Teleported).
/// </summary>
public class TimeOfDaySwitch : MonoBehaviour
{
    [SerializeField] private Light sun; // Directional Light
    [SerializeField] private bool startAtNight = false;

    [Header("=== XE CHIEU ===")]
    [SerializeField] private Vector3 afternoonSunRotation = new Vector3(15f, -60f, 0f); // mặt trời thấp
    [SerializeField] private Color afternoonSunColor = new Color(1f, 0.68f, 0.42f);       // cam vàng
    [SerializeField] private float afternoonSunIntensity = 1.2f;
    [SerializeField] private Color afternoonAmbient = new Color(0.55f, 0.45f, 0.38f);
    [SerializeField] private Material afternoonSkybox; // để trống = giữ skybox hiện tại
    [Tooltip("Các object chỉ có ban chiều")]
    [SerializeField] private GameObject[] afternoonObjects;

    [Header("=== BUOI SANG ===")]
    [SerializeField] private Vector3 morningSunRotation = new Vector3(35f, 120f, 0f);
    [SerializeField] private Color morningSunColor = new Color(1f, 0.95f, 0.85f);        // trắng ấm
    [SerializeField] private float morningSunIntensity = 1f;
    [SerializeField] private Color morningAmbient = new Color(0.5f, 0.52f, 0.55f);
    [SerializeField] private Material morningSkybox;
    [Tooltip("Các object chỉ có buổi sáng")]
    [SerializeField] private GameObject[] morningObjects;

    [Header("=== BAN DEM ===")]
    [SerializeField] private Vector3 nightSunRotation = new Vector3(50f, 30f, 0f);
    [SerializeField] private Color nightSunColor = new Color(0.45f, 0.55f, 0.8f);         // xanh trăng
    [SerializeField] private float nightSunIntensity = 0.08f;
    [SerializeField] private Color nightAmbient = new Color(0.04f, 0.05f, 0.08f);
    [SerializeField] private Material nightSkybox;
    [Tooltip("Các object chỉ có ban đêm (kéo Rain vào đây)")]
    [SerializeField] private GameObject[] nightObjects;

    public static TimeOfDaySwitch Instance { get; private set; }
    public bool IsNight { get; private set; }

    void Awake()
    {
        Instance = this;
        if (startAtNight) SetNight(); else SetAfternoon();
    }

    public void SetAfternoon() =>
        Apply(false, afternoonSunRotation, afternoonSunColor, afternoonSunIntensity, afternoonAmbient, afternoonSkybox);

    public void SetNight() =>
        Apply(true, nightSunRotation, nightSunColor, nightSunIntensity, nightAmbient, nightSkybox);

    public void SetMorning()
    {
        Apply(false, morningSunRotation, morningSunColor, morningSunIntensity, morningAmbient, morningSkybox);
        foreach (GameObject go in afternoonObjects) if (go != null) go.SetActive(false);
        foreach (GameObject go in morningObjects)   if (go != null) go.SetActive(true);
    }

    void Apply(bool night, Vector3 rot, Color color, float intensity, Color ambient, Material sky)
    {
        IsNight = night;
        foreach (GameObject go in morningObjects) if (go != null) go.SetActive(false);
        if (sun != null)
        {
            sun.transform.rotation = Quaternion.Euler(rot);
            sun.color = color;
            sun.intensity = intensity;
        }
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambient;
        if (sky != null) RenderSettings.skybox = sky;
        DynamicGI.UpdateEnvironment();

        foreach (GameObject go in afternoonObjects) if (go != null) go.SetActive(!night);
        foreach (GameObject go in nightObjects)     if (go != null) go.SetActive(night);
    }
}
