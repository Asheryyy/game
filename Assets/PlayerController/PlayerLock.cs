using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLock : MonoBehaviour
{
    [SerializeField] private float sens = 0.1f;
    private float xRotation;
    private PlayerControl m_Actions;
    private PlayerControl.PlayerActions m_Player;
    void Awake()
    {
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
        xRotation += val.y*sens;
        xRotation = Mathf.Clamp(xRotation,-90,90);
        transform.localRotation = Quaternion.Euler(-xRotation,0,0);
    }
}
