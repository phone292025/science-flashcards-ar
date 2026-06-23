using System;
using System.Reflection;
using NUnit.Framework;

public class VuforiaTargetPoseStabilizerTests
{
    MethodInfo calculateBlend;
    MethodInfo shouldSnap;

    [SetUp]
    public void SetUp()
    {
        var stabilizerType = Type.GetType("VuforiaTargetPoseStabilizer, Assembly-CSharp");
        Assert.That(stabilizerType, Is.Not.Null);
        calculateBlend = stabilizerType.GetMethod("CalculateBlend", BindingFlags.Public | BindingFlags.Static);
        shouldSnap = stabilizerType.GetMethod("ShouldSnap", BindingFlags.Public | BindingFlags.Static);
    }

    [Test]
    public void CalculateBlendReturnsZeroWhenDeltaTimeIsZero()
    {
        var result = InvokeCalculateBlend(14f, 0f);

        Assert.That(result, Is.EqualTo(0f));
    }

    [Test]
    public void CalculateBlendReturnsValueBetweenZeroAndOneDuringMotion()
    {
        var result = InvokeCalculateBlend(14f, 1f / 60f);

        Assert.That(result, Is.InRange(0.001f, 0.999f));
    }

    [Test]
    public void ShouldSnapReturnsFalseForSmallPoseChanges()
    {
        var result = InvokeShouldSnap(0.02f, 3f, 0.35f, 45f);

        Assert.That(result, Is.False);
    }

    [Test]
    public void ShouldSnapReturnsTrueForLargePositionChanges()
    {
        var result = InvokeShouldSnap(0.5f, 3f, 0.35f, 45f);

        Assert.That(result, Is.True);
    }

    [Test]
    public void ShouldSnapReturnsTrueForLargeRotationChanges()
    {
        var result = InvokeShouldSnap(0.02f, 55f, 0.35f, 45f);

        Assert.That(result, Is.True);
    }

    float InvokeCalculateBlend(float sharpness, float deltaTime)
    {
        return (float)calculateBlend.Invoke(null, new object[] { sharpness, deltaTime });
    }

    bool InvokeShouldSnap(float positionDistance, float rotationAngle, float maxPositionDistance, float maxRotationAngle)
    {
        return (bool)shouldSnap.Invoke(
            null,
            new object[] { positionDistance, rotationAngle, maxPositionDistance, maxRotationAngle });
    }
}
