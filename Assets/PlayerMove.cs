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
    [SerializeField]private float slowSpeed =2f;
    [SerializeField] private float fastSpeed = 10f;
    [SerializeField] private float normalSpeed = 5f;
    [SerializeField] private Animator animator;
    [SerializeField] private float jumpForce = 5f;
    void Awake()
    {
        controller = GetComponent<CharacterController>();
        m_Actions = new PlayerControl();
        m_Player = m_Actions.Player;
        m_Player.Enable();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(DialogueManager.Instance.IsActive) return;
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
            if(m_Player.Run.IsPressed())
            {
                speed = fastSpeed;
                animSpeed = movement.magnitude > 0.1f ? 1f : 0f;
            }
            else if(m_Player.Crouch.IsPressed())
            {
                speed = slowSpeed;
                animSpeed = movement.magnitude > 0.1f ? 0.3f : 0f;
            }
            else
            {
                speed = normalSpeed;
                animSpeed = movement.magnitude > 0.1f ? 0.5f : 0f;
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
}
