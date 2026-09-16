using UnityEngine;
using UnityEngine.InputSystem;

public class DoorTrigger : MonoBehaviour
{
    [SerializeField] private Transform destination;   // Điểm đến (trong/ngoài nhà)
    [SerializeField] private string hintText = "Bấm E để vào";

    private bool playerNearby = false;
    private CharacterController playerCC;
    private float cooldownTimer = 0f;
    private const float COOLDOWN = 1.5f; // Thời gian chờ sau khi teleport (giây)

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (cooldownTimer > 0) return; // Đang trong thời gian cooldown → bỏ qua
        playerNearby = true;
        playerCC = other.GetComponent<CharacterController>();
        QuestUI.Instance.ShowNotification(hintText);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerNearby = false;
        playerCC = null;
    }

    void Update()
    {
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
            return;
        }
        if (!playerNearby) return;
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            TeleportPlayer();
        }
    }

    void TeleportPlayer()
    {
        if (destination == null) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        // Tắt CharacterController trước khi teleport (tránh lỗi)
        if (playerCC != null) playerCC.enabled = false;
        player.transform.position = destination.position;
        player.transform.rotation = destination.rotation;
        if (playerCC != null) playerCC.enabled = true;

        // Reset cooldown để tránh trigger lại ngay lập tức
        playerNearby = false;
        cooldownTimer = COOLDOWN;
    }
}
