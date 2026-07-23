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
    public Transform headTransform;
    public Transform bottomTransform;
    public float headRotateSpeed = 10f;

    [Header("拖尾颜色（按速度变化）")]
    public TrailRenderer trailRenderer;
    public float colorMinSpeed = 5f;        // 低于此速度 = minColor
    public float colorMaxSpeed = 25f;       // 高于此速度 = maxColor
    public Color lowSpeedColor = new Color(0.2f, 0.5f, 1f);   // 慢速：蓝
    public Color midSpeedColor = new Color(0.2f, 1f, 0.3f);   // 中速：绿
    public Color highSpeedColor = new Color(1f, 0.3f, 0f);    // 高速：红橙

    [Header("发球")]
    public Transform cameraTransform;       // 玩家摄像机
    public CameraFollowBall cameraFollow;   // 相机追踪脚本
    public float serveDistance = 1.5f;      // 发球时球离摄像机距离
    public bool isServing = true;

    private Rigidbody rb;
    private Vector3 startPosition;
    private Quaternion startRotation;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        startPosition = transform.position;
        startRotation = transform.rotation;

        // 发球状态不显示拖尾
        if (trailRenderer != null)
            trailRenderer.enabled = false;
    }

    void Update()
    {
        // 发球状态：球跟随摄像机视角悬浮（瞄准时不跟，让相机转过来看球）
        if (isServing && cameraTransform != null)
        {
            bool isAiming = cameraFollow != null && cameraFollow.enabled;
            if (!isAiming)
            {
                // 球始终在摄像机画面正中心
                Vector3 targetPos = cameraTransform.position
                    + cameraTransform.forward * serveDistance;
                transform.position = Vector3.Lerp(transform.position, targetPos, 15f * Time.deltaTime);
            }
            transform.rotation = Quaternion.identity;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    void FixedUpdate()
    {
        // 发球状态不参与物理
        if (isServing) return;

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

        // 根据速度改拖尾颜色
        UpdateTrailColor();

        // Reset if fallen below level
        if (transform.position.y < minY)
            ResetPosition();
    }

    void UpdateTrailColor()
    {
        if (trailRenderer == null) return;

        float speed = rb.linearVelocity.magnitude;
        float t = Mathf.InverseLerp(colorMinSpeed, colorMaxSpeed, speed);

        Color trailColor;
        if (t < 0.5f)
            trailColor = Color.Lerp(lowSpeedColor, midSpeedColor, t * 2f);
        else
            trailColor = Color.Lerp(midSpeedColor, highSpeedColor, (t - 0.5f) * 2f);

        trailRenderer.startColor = trailColor;
        trailRenderer.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
    }

    /// <summary>Apply a hit force to the shuttlecock.</summary>
    public void Hit(Vector3 direction, float power)
    {
        isServing = false;
        if (trailRenderer != null) trailRenderer.enabled = true; // 击球后显示拖尾
        rb.linearVelocity = direction.normalized * power;
        rb.angularVelocity = Vector3.zero;
    }

    /// <summary>Reset shuttlecock to starting position.</summary>
    public void ResetPosition()
    {
        isServing = true;
        if (trailRenderer != null) trailRenderer.enabled = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    void OnCollisionEnter(Collision collision)
    {
        // Optional: play bounce sound, add score logic, etc.
    }
}
