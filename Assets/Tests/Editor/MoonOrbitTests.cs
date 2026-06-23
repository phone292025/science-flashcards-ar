using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class MoonOrbitTests
{
    Type moonOrbitType;
    MethodInfo calculateOrbitAngle;
    MethodInfo calculateOrbitOffset;

    [SetUp]
    public void SetUp()
    {
        moonOrbitType = Type.GetType("MoonOrbit, Assembly-CSharp");
        Assert.That(moonOrbitType, Is.Not.Null);

        calculateOrbitAngle = moonOrbitType.GetMethod("CalculateOrbitAngle", BindingFlags.Public | BindingFlags.Static);
        calculateOrbitOffset = moonOrbitType.GetMethod("CalculateOrbitOffset", BindingFlags.Public | BindingFlags.Static);
    }

    [Test]
    public void CalculateOrbitOffsetPlacesZeroDegreesOnPositiveX()
    {
        var offset = InvokeCalculateOrbitOffset(0f, 0.42f, 12f);

        Assert.That(offset.x, Is.EqualTo(0.42f).Within(0.0001f));
    }

    [Test]
    public void CalculateOrbitOffsetKeepsRadiusAfterTilt()
    {
        var offset = InvokeCalculateOrbitOffset(90f, 0.42f, 12f);

        Assert.That(offset.magnitude, Is.EqualTo(0.42f).Within(0.0001f));
    }

    [Test]
    public void CalculateOrbitOffsetTiltsVerticalPosition()
    {
        var offset = InvokeCalculateOrbitOffset(90f, 0.42f, 12f);

        Assert.That(offset.y, Is.LessThan(0f));
    }

    [Test]
    public void CalculateOrbitAngleReturnsNinetyDegreesForForwardOffset()
    {
        var angle = InvokeCalculateOrbitAngle(Vector3.forward);

        Assert.That(angle, Is.EqualTo(90f).Within(0.0001f));
    }

    [Test]
    public void UpdateUsesOrbitFrameSpace()
    {
        var frame = new GameObject("Frame").transform;
        var center = new GameObject("Center").transform;
        var moon = new GameObject("Moon").transform;

        try
        {
            frame.rotation = Quaternion.Euler(0f, 45f, 0f);
            center.SetParent(frame, false);
            moon.SetParent(frame, false);

            center.localPosition = new Vector3(0f, 0.2f, 0f);
            moon.localPosition = center.localPosition + Vector3.right * 0.42f;

            var orbit = moon.gameObject.AddComponent(moonOrbitType);
            moonOrbitType.GetField("orbitCenter").SetValue(orbit, center);
            moonOrbitType.GetField("orbitFrame").SetValue(orbit, frame);
            moonOrbitType.GetField("orbitRadius").SetValue(orbit, 0.42f);
            moonOrbitType.GetField("orbitSpeed").SetValue(orbit, 0f);
            moonOrbitType.GetField("selfRotationSpeed").SetValue(orbit, 0f);
            moonOrbitType.GetField("orbitTiltDegrees").SetValue(orbit, 12f);

            orbit.SendMessage("Update");

            Assert.That(moon.localPosition.x, Is.EqualTo(center.localPosition.x + 0.42f).Within(0.0001f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(moon.gameObject);
            UnityEngine.Object.DestroyImmediate(center.gameObject);
            UnityEngine.Object.DestroyImmediate(frame.gameObject);
        }
    }

    float InvokeCalculateOrbitAngle(Vector3 localOffset)
    {
        return (float)calculateOrbitAngle.Invoke(null, new object[] { localOffset });
    }

    Vector3 InvokeCalculateOrbitOffset(float angleDegrees, float radius, float tiltDegrees)
    {
        return (Vector3)calculateOrbitOffset.Invoke(null, new object[] { angleDegrees, radius, tiltDegrees });
    }
}
