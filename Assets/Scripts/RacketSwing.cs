using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Tracks mouse movement direction and distance to determine racket swing.
/// Left click triggers the actual hit on the shuttlecock.
/// </summary>
public class RacketSwing : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform;
    public Shuttlecock shuttlecock;
    public Animator racketAnimator;
    public Transform racketTransform;

    [Header("Swing Settings")]
    public float powerMultiplier = 0.08f;
    public float minSwingPower = 5f;
    public float maxSwingPower = 30f;
    public float powerDecayRate = 40f;
    public float directionSmoothSpeed = 12f;

    [Header("Hit Range")]
    public float maxHitDistance = 3f;

    [Header("Racket Position")]
    public float maxRacketOffset = 0.3f;
    public float maxRacketPullback = 0.2f;
    public float positionSmoothSpeed = 10f;

    [Header("Racket Rotation Visual")]
    public float visualSwingAngle = 30f;
    public float visualSwingDuration = 0.15f;

    private Vector2 smoothedMouseDelta;
    private float accumulatedPower;
    private float visualSwingTimer;
    private Vector3 racketBasePosition;
    private Quaternion racketBaseRotation;
    private Vector3 targetRacketPos;

    void Start()
    {
        if (racketTransform != null)
        {
            racketBasePosition = racketTransform.localPosition;
            racketBaseRotation = racketTransform.localRotation;
            targetRacketPos = racketBasePosition;
        }
    }

    void Update()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        smoothedMouseDelta = Vector2.Lerp(smoothedMouseDelta, mouseDelta,
            directionSmoothSpeed * Time.deltaTime);

        accumulatedPower += mouseDelta.magnitude * powerMultiplier;
        accumulatedPower = Mathf.Min(accumulatedPower, maxSwingPower);
        accumulatedPower -= powerDecayRate * Time.deltaTime;
        accumulatedPower = Mathf.Max(0f, accumulatedPower);

        // Update racket position based on mouse direction and power
        UpdateRacketPosition();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Swing();
        }

        // Visual racket swing animation
        if (visualSwingTimer > 0f)
        {
            visualSwingTimer -= Time.deltaTime;
            float t = 1f - (visualSwingTimer / visualSwingDuration);
            if (racketTransform != null)
            {
                float angle = Mathf.Sin(t * Mathf.PI) * visualSwingAngle;
                racketTransform.localRotation = racketBaseRotation * Quaternion.Euler(-angle, 0f, 0f);
            }
            if (visualSwingTimer <= 0f && racketTransform != null)
            {
                racketTransform.localRotation = racketBaseRotation;
            }
        }
    }

    void UpdateRacketPosition()
    {
        if (racketTransform == null) return;

        float powerPercent = Mathf.Clamp01(accumulatedPower / maxSwingPower);

        // Direction: offset racket in mouse direction
        Vector2 dir = smoothedMouseDelta;
        if (dir.magnitude > 1f)
            dir = dir.normalized;
        else
            dir = dir / 1f;

        // XY offset for aim direction, Z pullback for power
        targetRacketPos = racketBasePosition
            + new Vector3(dir.x * maxRacketOffset, dir.y * maxRacketOffset, 0f)
            + new Vector3(0f, 0f, -powerPercent * maxRacketPullback);

        racketTransform.localPosition = Vector3.Lerp(
            racketTransform.localPosition, targetRacketPos,
            positionSmoothSpeed * Time.deltaTime);
    }

    void Swing()
    {
        if (shuttlecock == null)
        {
            Debug.LogWarning("RacketSwing: No Shuttlecock assigned!");
            return;
        }

        // Distance check
        float distance = Vector3.Distance(transform.position, shuttlecock.transform.position);
        if (distance > maxHitDistance)
        {
            Debug.Log(string.Format("Too far! Distance: {0:F1}, Max: {1:F1}", distance, maxHitDistance));
            return;
        }

        Vector3 swingDir;
        if (smoothedMouseDelta.magnitude > 1f)
        {
            swingDir = cameraTransform.right * smoothedMouseDelta.x
                     + cameraTransform.up * smoothedMouseDelta.y;
        }
        else
        {
            swingDir = cameraTransform.forward;
        }
        swingDir.Normalize();

        float power = Mathf.Clamp(accumulatedPower, minSwingPower, maxSwingPower);

        shuttlecock.Hit(swingDir, power);

        Debug.Log(string.Format("Swing! Dir: {0}, Power: {1:F1}", swingDir, power));

        visualSwingTimer = visualSwingDuration;
        racketAnimator?.SetTrigger("Swing");

        accumulatedPower = 0f;
        smoothedMouseDelta = Vector2.zero;
    }

    public Vector2 GetSwingDirection2D() { return smoothedMouseDelta; }
    public float GetAccumulatedPower() { return accumulatedPower; }
    public float GetPowerPercent() { return Mathf.Clamp01(accumulatedPower / maxSwingPower); }
}
