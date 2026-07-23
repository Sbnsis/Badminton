using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonMove : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;

    [Header("Jump")]
    public float jumpHeight = 1.5f;

    [Header("Gravity")]
    public float gravity = -9.81f;

    private CharacterController controller;
    private Vector2 moveInput;
    private float verticalVelocity;
    private bool spaceWasHeld;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    void Update()
    {
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        move *= walkSpeed;

        bool spaceHeld = Keyboard.current.spaceKey.isPressed;
        bool justPressed = spaceHeld && !spaceWasHeld;  // 刚按下的那一帧

        if (controller.isGrounded)
        {
            verticalVelocity = -1f;

            if (justPressed)  // 只在一瞬间触发
            {
                verticalVelocity = Mathf.Sqrt(-2f * gravity * jumpHeight);
                GetComponent<PlayerAnimation>()?.TriggerJump();
            }
        }
        else
        {
            if (!spaceHeld && verticalVelocity > 0)
                verticalVelocity = 0;

            verticalVelocity += gravity * Time.deltaTime;
        }

        move.y = verticalVelocity;
        controller.Move(move * Time.deltaTime);

        spaceWasHeld = spaceHeld;  // 记住这帧状态，下帧比较
    }
}
