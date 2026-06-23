using UnityEngine;

public class MoonOrbit : MonoBehaviour
{
    public Transform orbitCenter;
    public Transform orbitFrame;
    public float orbitRadius = 0.42f;
    public float orbitSpeed = 35f;
    public float selfRotationSpeed = 45f;
    public float orbitTiltDegrees = 12f;

    float orbitAngle;

    void OnEnable()
    {
        if (orbitCenter != null)
        {
            var frame = ResolveOrbitFrame();
            var centerLocalPosition = GetCenterLocalPosition(frame);
            var localOffset = frame.InverseTransformPoint(transform.position) - centerLocalPosition;
            if (localOffset.sqrMagnitude > 0.0001f)
            {
                orbitAngle = CalculateOrbitAngle(localOffset);
            }
        }
    }

    void Update()
    {
        if (orbitCenter == null)
        {
            return;
        }

        orbitAngle += orbitSpeed * Time.deltaTime;

        var frame = ResolveOrbitFrame();
        var localPosition = GetCenterLocalPosition(frame)
            + CalculateOrbitOffset(orbitAngle, orbitRadius, orbitTiltDegrees);
        transform.position = frame.TransformPoint(localPosition);
        transform.Rotate(Vector3.up, selfRotationSpeed * Time.deltaTime, Space.Self);
    }

    public static float CalculateOrbitAngle(Vector3 localOffset)
    {
        return Mathf.Atan2(localOffset.z, localOffset.x) * Mathf.Rad2Deg;
    }

    public static Vector3 CalculateOrbitOffset(float angleDegrees, float radius, float tiltDegrees)
    {
        var orbitRotation = Quaternion.Euler(tiltDegrees, 0f, 0f);
        var flatOffset = new Vector3(
            Mathf.Cos(angleDegrees * Mathf.Deg2Rad) * radius,
            0f,
            Mathf.Sin(angleDegrees * Mathf.Deg2Rad) * radius);
        return orbitRotation * flatOffset;
    }

    Transform ResolveOrbitFrame()
    {
        if (orbitFrame != null)
        {
            return orbitFrame;
        }

        return orbitCenter.parent != null ? orbitCenter.parent : orbitCenter;
    }

    Vector3 GetCenterLocalPosition(Transform frame)
    {
        return frame == orbitCenter ? Vector3.zero : frame.InverseTransformPoint(orbitCenter.position);
    }
}
