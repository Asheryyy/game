using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gắn vào 1 Box Collider (Is Trigger) phủ bên trong nhà.
/// Player vào nhà → tắt hạt mưa, tiếng mưa nhỏ lại. Ra ngoài → bật lại.
/// </summary>
[RequireComponent(typeof(Collider))]
public class IndoorZone : MonoBehaviour
{
    [SerializeField] private ParticleSystem rain;
    [SerializeField] private AudioSource rainSound;
    [Tooltip("Tỉ lệ tiếng mưa khi ở trong nhà (0 = tắt hẳn)")]
    [SerializeField] [Range(0f, 1f)] private float indoorVolumeScale = 0.3f;
    [SerializeField] private float fadeSpeed = 1f;
    [Tooltip("TRUE = chỉ chạy LẦN ĐẦU. Ra khỏi vùng là tự tắt luôn, lần sau đi vào không còn tác dụng")]
    [SerializeField] private bool onlyFirstTime = false;

    Collider zone;
    Transform player;

    // các zone Player đang đứng trong — nhiều zone chồng nhau vẫn đúng,
    // Enter bị gọi lặp cũng không đếm trùng
    static readonly HashSet<IndoorZone> activeZones = new HashSet<IndoorZone>();
    static float outdoorVolume = -1f;

    void Awake()
    {
        zone = GetComponent<Collider>();
        zone.isTrigger = true;
        if (rainSound != null && outdoorVolume < 0f) outdoorVolume = rainSound.volume;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        activeZones.Add(this);
        player = other.transform;
        if (rain != null) rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) Leave();
    }

    void Leave()
    {
        if (!activeZones.Remove(this)) return;
        if (activeZones.Count == 0 && rain != null) rain.Play();
        if (onlyFirstTime) gameObject.SetActive(false); // SaveSystem lưu trạng thái tắt
    }

    void Update()
    {
        // Teleport tắt CharacterController → Unity KHÔNG gọi OnTriggerExit → tự kiểm tra đã ra khỏi vùng chưa
        if (player != null && activeZones.Contains(this) &&
            Vector3.Distance(zone.ClosestPoint(player.position), player.position) > 1f)
            Leave();

        // Mưa vừa được bật lại (vd: ngủ dậy → chuyển đêm) trong khi Player đang ở trong nhà → tắt hạt mưa
        if (activeZones.Count > 0 && rain != null && rain.isEmitting)
            rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (rainSound == null || outdoorVolume < 0f) return;
        float target = activeZones.Count > 0 ? outdoorVolume * indoorVolumeScale : outdoorVolume;
        rainSound.volume = Mathf.MoveTowards(rainSound.volume, target, fadeSpeed * Time.deltaTime);
    }

    void OnDestroy() { activeZones.Remove(this); outdoorVolume = -1f; } // reset khi đổi scene
}
