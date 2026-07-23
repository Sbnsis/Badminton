using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Shuttlecock : MonoBehaviour
{
    [Header("Physics")]
    [Tooltip("Air drag multiplier applied each physics frame. Lower = more drag.")]
    public float airDrag = 0.98f;
    [Tooltip("Gravity multiplier. 0.3 = 30% of normal gravity (shuttlecock floats).")]
    public float gravityScale = 0.3f;

    [Header("Limits")]
    public float maxSpeed = 40f;
    public float minY = 0.5f;

    [Header("朝向")]
    public Transform headTransform;         // 球头节点
    public Transform bottomTransform;       // 球底节点
    public float headRotateSpeed = 10f;     // 旋转速度

    private Rigidbody rb;
    private Vector3 startPosition;
    private Quaternion startRotation;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    void FixedUpdate()
    {
        // Custom gravity
        rb.linearVelocity += Physics.gravity * gravityScale * Time.fixedDeltaTime;

        // Air drag (shuttlecock has high drag)
        rb.linearVelocity *= airDrag;

        // Clamp max speed
        if (rb.linearVelocity.magnitude > maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;

        // head→bottom 连线对齐速度方向，head 在前
        if (headTransform != null && bottomTransform != null && rb.linearVelocity.sqrMagnitude > 0.25f)
        {
            // bottom → head 的局部方向
            Vector3 localHeadDir = (headTransform.localPosition - bottomTransform.localPosition).normalized;
            // 当前这个局部方向在世界空间指向哪里
            Vector3 currentWorldDir = transform.TransformDirection(localHeadDir);
            // 需要旋转多少才能对齐到速度方向
            Vector3 velocityDir = rb.linearVelocity.normalized;
            Quaternion delta = Quaternion.FromToRotation(currentWorldDir, velocityDir);
            // 应用旋转
            Quaternion targetRot = delta * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, headRotateSpeed * Time.fixedDeltaTime);
        }

        // Reset if fallen below level
        if (transform.position.y < minY)
            ResetPosition();
    }

    /// <summary>Apply a hit force to the shuttlecock.</summary>
    public void Hit(Vector3 direction, float power)
    {
        rb.linearVelocity = direction.normalized * power;
        rb.angularVelocity = Vector3.zero;
    }

    /// <summary>Reset shuttlecock to starting position.</summary>
    public void ResetPosition()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = startPosition;
        transform.rotation = startRotation;
    }

    void OnCollisionEnter(Collision collision)
    {
        // Optional: play bounce sound, add score logic, etc.
    }
}
