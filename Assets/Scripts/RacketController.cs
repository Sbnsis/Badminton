using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 球拍 + 摄像机控制
///
/// 普通状态：鼠标 = 自由视角（FPS 旋转），球拍闲置在中心
/// 按住左键：摄像机锁定羽毛球，鼠标 = 拉开球拍（引拍瞄准）
/// 松开左键：球拍回中挥拍 + 击球判定，摄像机恢复自由视角
/// </summary>
public class RacketController : MonoBehaviour
{
    [Header("引用")]
    public Transform racketPivot;
    public Transform cameraTransform;       // 第一人称摄像机（垂直旋转 pivot）
    public CameraFollowBall cameraFollow;  // 球追踪脚本（瞄准时启用）
    public Shuttlecock shuttlecock;

    [Header("中心点（相对摄像机）")]
    public Vector3 centerLocalPos = new Vector3(0.3f, -0.2f, 1.5f);

    [Header("球拍可拉开的范围")]
    public float pullRangeH = 1f;
    public float pullRangeV = 0.75f;

    [Header("自由视角")]
    public float lookSensitivity = 2f;
    public float minLookY = -85f;
    public float maxLookY = 85f;

    [Header("手感")]
    public float aimSensitivity = 0.005f; // 瞄准时鼠标 delta → 球拍偏移的灵敏度
    public float followSpeed = 20f;
    public float swingSpeed = 25f;

    [Header("旋转")]
    public float pullTiltAngle = 15f;
    public float swingRotateAngle = 40f;
    public float rotateSpeed = 15f;

    [Header("击球方向")]
    public Vector3 courtForward = new Vector3(0f, 0f, 1f);  // 对方半场方向（垂直球网）
    public float racketInfluence = 0.5f;    // 球拍偏移对方向的影响强度 (0~1)

    [Header("击球")]
    public float maxHitDistance = 5f;
    public float minPower = 5f;
    public float maxPower = 35f;

    // 内部
    private Vector3 currentLocalPos;
    private Quaternion currentLocalRot;
    private Vector2 mouseOffset;
    private bool isSwinging;
    private Vector3 swingStartPos;
    private float verticalLookAngle;
    private bool wasLeftHeld;
    private bool wasRightHeld;

    void Start()
    {
        if (racketPivot != null)
        {
            currentLocalPos = centerLocalPos;
            racketPivot.localPosition = centerLocalPos;
            currentLocalRot = racketPivot.localRotation;
        }
        if (cameraTransform == null)
            cameraTransform = GetComponentInChildren<Camera>()?.transform;
        if (cameraFollow != null)
            cameraFollow.enabled = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (racketPivot == null || cameraTransform == null)
            return;

        bool leftHeld = Mouse.current.leftButton.isPressed;
        bool rightHeld = Mouse.current.rightButton.isPressed;
        bool anyAim = leftHeld || rightHeld;
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        if (!anyAim)
        {
            // ========== 自由视角模式 ==========

            // 关闭球追踪
            if (cameraFollow != null) cameraFollow.enabled = false;

            // 鼠标旋转视角
            if (wasLeftHeld || wasRightHeld)
            {
                // 过渡帧：只同步角度，不旋转（避免跳动）
                Vector3 euler = cameraTransform.localEulerAngles;
                verticalLookAngle = euler.x;
                if (verticalLookAngle > 180f) verticalLookAngle -= 360f;
            }
            else
            {
                // 水平旋转 Player body
                transform.Rotate(Vector3.up * mouseDelta.x * lookSensitivity * 0.1f);

                // 垂直旋转 Camera pivot
                verticalLookAngle -= mouseDelta.y * lookSensitivity * 0.1f;
                verticalLookAngle = Mathf.Clamp(verticalLookAngle, minLookY, maxLookY);
                cameraTransform.localRotation = Quaternion.Euler(verticalLookAngle, 0f, 0f);
            }

            // 球拍归位
            if (!isSwinging)
            {
                currentLocalPos = Vector3.Lerp(currentLocalPos, centerLocalPos, followSpeed * Time.deltaTime);
                racketPivot.localPosition = currentLocalPos;
                currentLocalRot = Quaternion.Slerp(currentLocalRot, Quaternion.identity, rotateSpeed * Time.deltaTime);
                racketPivot.localRotation = currentLocalRot;
            }
            else
            {
                // 如果之前挥拍中，继续完成动画
                UpdateSwingReturn();
            }

            // 刚松开的瞬间 → 触发挥拍
            if (wasLeftHeld || wasRightHeld)
            {
                Swing();
            }
        }
        else
        {
            // ========== 瞄准模式（按住左键或右键）==========

            // 刚按下 → 重置球拍到中心
            if (!wasLeftHeld && !wasRightHeld)
            {
                mouseOffset = Vector2.zero;
            }

            // 左键=追踪球，右键=摄像机不动
            if (cameraFollow != null) cameraFollow.enabled = leftHeld;

            if (!isSwinging)
            {
                // 用鼠标 delta 累积偏移（因为光标被锁定了，不能用绝对坐标）
                mouseOffset += mouseDelta * aimSensitivity;
                mouseOffset = Vector2.ClampMagnitude(mouseOffset, 1f);

                Vector3 pullOffset = new Vector3(
                    mouseOffset.x * pullRangeH,
                    mouseOffset.y * pullRangeV,
                    0f
                );
                Vector3 targetLocalPos = centerLocalPos + pullOffset;

                // 球拍跟随鼠标（拉开）
                currentLocalPos = Vector3.Lerp(currentLocalPos, targetLocalPos, followSpeed * Time.deltaTime);
                racketPivot.localPosition = currentLocalPos;

                // 旋转：向拉开方向倾斜
                Vector3 pullDir = (currentLocalPos - centerLocalPos).normalized;
                Quaternion targetRot;
                if (pullDir.magnitude > 0.01f)
                {
                    Vector3 tiltAxis = new Vector3(-pullDir.y, pullDir.x, 0f);
                    float tiltAmount = (currentLocalPos - centerLocalPos).magnitude
                        / Mathf.Sqrt(pullRangeH * pullRangeH + pullRangeV * pullRangeV);
                    targetRot = Quaternion.AngleAxis(tiltAmount * pullTiltAngle, tiltAxis);
                }
                else
                {
                    targetRot = Quaternion.identity;
                }
                currentLocalRot = Quaternion.Slerp(currentLocalRot, targetRot, rotateSpeed * Time.deltaTime);
                racketPivot.localRotation = currentLocalRot;
            }
            else
            {
                UpdateSwingReturn();
            }
        }

        wasLeftHeld = leftHeld;
        wasRightHeld = rightHeld;
    }

    /// <summary>
    /// 挥拍回中动画（共用）
    /// </summary>
    void UpdateSwingReturn()
    {
        currentLocalPos = Vector3.Lerp(currentLocalPos, centerLocalPos, swingSpeed * Time.deltaTime);
        racketPivot.localPosition = currentLocalPos;

        Vector3 swingDir = (centerLocalPos - swingStartPos).normalized;
        float totalDist = Vector3.Distance(centerLocalPos, swingStartPos);
        float remainingDist = Vector3.Distance(currentLocalPos, centerLocalPos);
        float progress = totalDist > 0.01f ? 1f - (remainingDist / totalDist) : 1f;

        float swingAngle = Mathf.Sin(progress * Mathf.PI) * swingRotateAngle;
        Vector3 rotateAxis = new Vector3(-swingDir.y, swingDir.x, 0f).normalized;
        Quaternion targetRot = Quaternion.AngleAxis(-swingAngle, rotateAxis);
        currentLocalRot = Quaternion.Slerp(currentLocalRot, targetRot, swingSpeed * 2f * Time.deltaTime);
        racketPivot.localRotation = currentLocalRot;

        if (remainingDist < 0.03f)
        {
            currentLocalPos = centerLocalPos;
            racketPivot.localPosition = centerLocalPos;
            currentLocalRot = Quaternion.identity;
            racketPivot.localRotation = Quaternion.identity;
            isSwinging = false;
        }
    }

    void Swing()
    {
        float pullDist = Vector3.Distance(currentLocalPos, centerLocalPos);

        if (pullDist < 0.05f)
        {
            Debug.Log("球拍在中心，没有拉开，无法挥拍");
            return;
        }

        isSwinging = true;
        swingStartPos = currentLocalPos;

        if (shuttlecock == null) return;

        float distToBall = Vector3.Distance(transform.position, shuttlecock.transform.position);
        if (distToBall > maxHitDistance)
        {
            Debug.Log($"挥空！球距玩家: {distToBall:F2}m > {maxHitDistance}m");
            return;
        }

        // 基础方向 = 对方半场，球拍偏移反向（弹弓：往后拉→往前打）
        Vector3 racketOffset = racketPivot.localPosition - centerLocalPos; // 球拍相对中心
        Vector3 racketWorldDir = cameraTransform.TransformDirection((-racketOffset).normalized); // 反向=弹弓
        Vector3 hitDir = (courtForward.normalized + racketWorldDir * racketInfluence).normalized;

        float maxPull = Mathf.Sqrt(pullRangeH * pullRangeH + pullRangeV * pullRangeV);
        float power = Mathf.Lerp(minPower, maxPower, Mathf.Clamp01(pullDist / maxPull));

        shuttlecock.Hit(hitDir, power);
        Debug.Log($"击中！方向:{hitDir}, 力度:{power:F1}");
    }

    public Vector2 GetMouseOffset() => mouseOffset;
    public bool IsSwinging() => isSwinging;
    public float GetPullPercent()
    {
        float maxPull = Mathf.Sqrt(pullRangeH * pullRangeH + pullRangeV * pullRangeV);
        return Mathf.Clamp01(Vector3.Distance(currentLocalPos, centerLocalPos) / maxPull);
    }
}
