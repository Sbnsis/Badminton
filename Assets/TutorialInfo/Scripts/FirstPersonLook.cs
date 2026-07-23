using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonLook : MonoBehaviour
{
    [Header("灵敏度")]
    public float mouseSensitivity = 2f;

    [Header("垂直角度限制")]
    public float minLookAngle = -90f;
    public float maxLookAngle = 90f;

    public Transform cameraHolder;  // ★ 拖 Main Camera 进来

    private float verticalRotation;
    private Vector2 lookInput;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    void Update()
    {
        // 水平：旋转 Player 自身
        transform.Rotate(Vector3.up * lookInput.x * mouseSensitivity);

        // 垂直：只旋转 Camera
        verticalRotation -= lookInput.y * mouseSensitivity;
        verticalRotation = Mathf.Clamp(verticalRotation, minLookAngle, maxLookAngle);
        cameraHolder.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }
}