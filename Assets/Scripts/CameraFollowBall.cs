using UnityEngine;

/// <summary>
/// 第一人称视角自动追踪羽毛球
/// - 水平旋转（Y）: 施加在 Player body 上
/// - 垂直旋转（X）: 施加在 Camera pivot 上
/// - 鼠标不再控制视角，鼠标只控制球拍
/// </summary>
public class CameraFollowBall : MonoBehaviour
{
    [Header("追踪目标")]
    public Transform ball;                  // 羽毛球

    [Header("旋转轴")]
    public Transform playerBody;            // 水平旋转（Player）
    public Transform cameraPivot;           // 垂直旋转（Main Camera）

    [Header("参数")]
    public float smoothSpeed = 8f;          // 旋转平滑度
    public float minVerticalAngle = -70f;   // 垂直向下限制
    public float maxVerticalAngle = 70f;    // 垂直向上限制
    public float minDistanceToTrack = 0.3f; // 球太近时不追踪（避免抖动）

    void LateUpdate()
    {
        if (ball == null || playerBody == null || cameraPivot == null)
            return;

        Vector3 direction = ball.position - cameraPivot.position;

        // 球太近时跳过
        if (direction.sqrMagnitude < minDistanceToTrack * minDistanceToTrack)
            return;

        // === 水平旋转 (Y) → Player body ===
        Vector3 flatDir = direction;
        flatDir.y = 0f;
        if (flatDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetYRot = Quaternion.LookRotation(flatDir);
            playerBody.rotation = Quaternion.Slerp(
                playerBody.rotation,
                targetYRot,
                smoothSpeed * Time.deltaTime
            );
        }

        // === 垂直旋转 (X) → Camera pivot ===
        float verticalAngle = Mathf.Asin(-direction.normalized.y) * Mathf.Rad2Deg;
        verticalAngle = Mathf.Clamp(verticalAngle, minVerticalAngle, maxVerticalAngle);
        Quaternion targetXRot = Quaternion.Euler(verticalAngle, 0f, 0f);
        cameraPivot.localRotation = Quaternion.Slerp(
            cameraPivot.localRotation,
            targetXRot,
            smoothSpeed * Time.deltaTime
        );
    }
}
