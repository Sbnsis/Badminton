using UnityEngine;

/// <summary>
/// Makes the camera always face toward the shuttlecock.
/// Horizontal rotation on player body, vertical rotation on camera pivot.
/// </summary>
public class CameraFollowShuttlecock : MonoBehaviour
{
    [Header("Target")]
    public Transform shuttlecock;

    [Header("Rotation Pivots")]
    public Transform playerBody;
    public Transform cameraPivot;

    [Header("Settings")]
    public float smoothSpeed = 8f;
    public float minVerticalAngle = -60f;
    public float maxVerticalAngle = 60f;

    void LateUpdate()
    {
        if (shuttlecock == null || playerBody == null || cameraPivot == null)
            return;

        Vector3 direction = shuttlecock.position - cameraPivot.position;

        if (direction.sqrMagnitude < 0.001f)
            return;

        // Horizontal rotation (Y) on player body
        Vector3 flatDir = direction;
        flatDir.y = 0f;
        if (flatDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetYRot = Quaternion.LookRotation(flatDir);
            playerBody.rotation = Quaternion.Slerp(playerBody.rotation, targetYRot,
                smoothSpeed * Time.deltaTime);
        }

        // Vertical rotation (X) on camera pivot
        float verticalAngle = Mathf.Asin(-direction.normalized.y) * Mathf.Rad2Deg;
        verticalAngle = Mathf.Clamp(verticalAngle, minVerticalAngle, maxVerticalAngle);
        Quaternion targetXRot = Quaternion.Euler(verticalAngle, 0f, 0f);
        cameraPivot.localRotation = Quaternion.Slerp(cameraPivot.localRotation, targetXRot,
            smoothSpeed * Time.deltaTime);
    }
}
