using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAnimation : MonoBehaviour
{
    private Animator anim;
    private CharacterController controller;
    private bool isMoving;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        controller = GetComponent<CharacterController>();
    }

    void OnMove(InputValue value)
    {
        isMoving = value.Get<Vector2>().magnitude > 0.1f;
    }

    void Update()
    {
        if (anim == null) return;

        bool grounded = controller.isGrounded;
        anim.SetBool("IsGrounded", grounded);

        if (grounded)
            anim.SetBool("IsRunning", isMoving);
        else
            anim.SetBool("IsRunning", false);
    }

    public void TriggerJump()
    {
        anim?.SetTrigger("Jump");
        anim?.SetBool("IsGrounded", false);
    }
}
