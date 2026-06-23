using UnityEngine;
using Vuforia;

[DefaultExecutionOrder(10000)]
public class VuforiaTargetPoseStabilizer : MonoBehaviour
{
    public float positionSharpness = 14f;
    public float rotationSharpness = 16f;
    public float snapDistance = 0.35f;
    public float snapAngle = 45f;

    ObserverBehaviour observerBehaviour;
    Vector3 smoothedPosition;
    Quaternion smoothedRotation;
    bool hasSmoothedPose;

    void LateUpdate()
    {
        if (observerBehaviour == null)
        {
            observerBehaviour = GetComponent<ObserverBehaviour>();
        }

        if (observerBehaviour == null || !HasUsablePose(observerBehaviour.TargetStatus))
        {
            hasSmoothedPose = false;
            return;
        }

        var rawPosition = transform.position;
        var rawRotation = transform.rotation;
        if (!hasSmoothedPose || ShouldSnap(
                Vector3.Distance(smoothedPosition, rawPosition),
                Quaternion.Angle(smoothedRotation, rawRotation),
                snapDistance,
                snapAngle))
        {
            smoothedPosition = rawPosition;
            smoothedRotation = rawRotation;
            hasSmoothedPose = true;
            return;
        }

        smoothedPosition = Vector3.Lerp(
            smoothedPosition,
            rawPosition,
            CalculateBlend(positionSharpness, Time.unscaledDeltaTime));
        smoothedRotation = Quaternion.Slerp(
            smoothedRotation,
            rawRotation,
            CalculateBlend(rotationSharpness, Time.unscaledDeltaTime));
        transform.SetPositionAndRotation(smoothedPosition, smoothedRotation);
    }

    bool HasUsablePose(TargetStatus targetStatus)
    {
        return targetStatus.Status == Status.TRACKED
            || targetStatus.Status == Status.EXTENDED_TRACKED
            || (targetStatus.Status == Status.LIMITED && targetStatus.StatusInfo == StatusInfo.NORMAL);
    }

    public static float CalculateBlend(float sharpness, float deltaTime)
    {
        return 1f - Mathf.Exp(-Mathf.Max(0f, sharpness) * Mathf.Max(0f, deltaTime));
    }

    public static bool ShouldSnap(float positionDistance, float rotationAngle, float maxPositionDistance, float maxRotationAngle)
    {
        return positionDistance > Mathf.Max(0f, maxPositionDistance)
            || rotationAngle > Mathf.Max(0f, maxRotationAngle);
    }
}
