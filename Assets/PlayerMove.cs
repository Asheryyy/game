using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] private float sens = 0.1f;
    [SerializeField] private float speed = 5f;
    private CharacterController controller;
    private PlayerControl m_Actions;
    private PlayerControl.PlayerActions m_Player;
    private float velocityY = 0f;
    private float gravity = -9.81f;
    [SerializeField] private float slowSpeed  = 2f;
    [SerializeField] private float fastSpeed  = 10f;
    [SerializeField] private float normalSpeed = 5f;
    [SerializeField] private Animator animator;
    [SerializeField] private float jumpForce = 5f;

    [Header("=== BƯỚC CHÂN ===")]
    [SerializeField] private AudioSource footstepSource;
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private float stepIntervalWalk   = 0.5f;
    [SerializeField] private float stepIntervalRun    = 0.3f;
    [SerializeField] private float stepIntervalCrouch = 0.7f;

    [Header("=== STAMINA ===")]
    [SerializeField] private float maxStamina     = 100f;
    [SerializeField] private float staminaDrain   = 20f;  // hao/giây khi chạy
    [SerializeField] private float staminaRegen   = 10f;  // hồi/giây khi nghỉ
    [SerializeField] private float regenDelay     = 1.5f; // giây chờ trước khi hồi
    [SerializeField] private UnityEngine.UI.Slider staminaBar; // kéo UI Slider vào

    float currentStamina;
    float regenTimer = 0f;
    bool  canRun     = true;

    float stepTimer  = 0f;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        m_Actions = new PlayerControl();
        m_Player = m_Actions.Player;
        m_Player.Enable();
    }

    void Start()
    {
        currentStamina = maxStamina;
        if (staminaBar != null)
        {
            staminaBar.maxValue = maxStamina;
            staminaBar.value    = maxStamina;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(DialogueManager.Instance.IsActive) return;
        // Đang bị dịch chuyển (CharacterController tạm tắt) → đứng yên
        if(!controller.enabled) return;
        // Block input khi đang pause
        if(PauseMenuManager_IsActive()) return;
        Vector2 val = m_Player.Look.ReadValue<Vector2>();
        float mouseX = val.x*sens;

        float animSpeed = 0f;
        transform.Rotate(0f,mouseX,0f);
        Vector2 movement = m_Player.Move.ReadValue<Vector2>();
        Vector3 move = (transform.forward*movement.y + transform.right*movement.x);
        if(controller.isGrounded == true)
        {
            velocityY = -2f;
        }
        else
        {
            velocityY += gravity*Time.deltaTime;
        }
        if(controller.isGrounded)
        {
            if(m_Player.Run.IsPressed() && canRun && movement.magnitude > 0.1f)
            {
                // ── Chạy: hao stamina ──
                currentStamina -= staminaDrain * Time.deltaTime;
                regenTimer      = regenDelay;
                if (currentStamina <= 0f)
                {
                    currentStamina = 0f;
                    canRun         = false; // khóa chạy cho đến khi hồi đủ
                }
                speed     = fastSpeed;
                animSpeed = 1f;
            }
            else if(m_Player.Crouch.IsPressed())
            {
                speed = slowSpeed;
                animSpeed = movement.magnitude > 0.1f ? 0.3f : 0f;
            }
            else
            {
                speed     = normalSpeed;
                animSpeed = movement.magnitude > 0.1f ? 0.5f : 0f;
            }

            // ── Hồi stamina khi không chạy ──
            bool isRunning = m_Player.Run.IsPressed() && canRun && movement.magnitude > 0.1f;
            if (!isRunning)
            {
                regenTimer -= Time.deltaTime;
                if (regenTimer <= 0f)
                {
                    currentStamina = Mathf.Min(currentStamina + staminaRegen * Time.deltaTime, maxStamina);
                    if (currentStamina >= maxStamina * 0.3f) canRun = true; // mở khóa khi hồi 30%
                }
            }

            // Cập nhật UI
            if (staminaBar != null) staminaBar.value = currentStamina;

            // ── Tiếng bước chân ──
            bool isMoving = movement.magnitude > 0.1f;
            if (isMoving)
            {
                float interval = m_Player.Run.IsPressed()    ? stepIntervalRun
                               : m_Player.Crouch.IsPressed() ? stepIntervalCrouch
                               : stepIntervalWalk;
                stepTimer -= Time.deltaTime;
                if (stepTimer <= 0f)
                {
                    PlayFootstep();
                    stepTimer = interval;
                }
            }
            else
            {
                stepTimer = 0f; // reset để bước đầu tiên phát ngay khi đi lại
            }
        }
        else
        {
            animSpeed = 0f;
        }
        if(controller.isGrounded == true && m_Player.Jump.WasPressedThisFrame())
        {
            velocityY = jumpForce;
            animator.SetTrigger("Jump");
        }
        Vector3 finalMove = move*speed + Vector3.up*velocityY;
        controller.Move(finalMove*Time.deltaTime);
        animator.SetFloat("Speed", animSpeed);
    }

    void PlayFootstep()
    {
        if (footstepSource == null || footstepClips == null || footstepClips.Length == 0) return;
        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
        footstepSource.PlayOneShot(clip);
    }

    // Kiểm tra pause một cách an toàn — không bị lỗi nếu chưa có PauseMenuManager trong scene
    private bool PauseMenuManager_IsActive()
    {
        var pm = FindFirstObjectByType<PauseMenuManager>();
        return pm != null && pm.IsPaused;
    }

    void OnDestroy()
    {
        m_Player.Disable();
        m_Actions?.Dispose();
    }
}
