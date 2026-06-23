using System;
using System.Reflection;
using NUnit.Framework;

public class VuforiaTargetVisibilityTests
{
    MethodInfo shouldStayVisibleDuringTrackingGap;

    [SetUp]
    public void SetUp()
    {
        var visibilityType = Type.GetType("VuforiaTargetVisibility, Assembly-CSharp");
        Assert.That(visibilityType, Is.Not.Null);
        shouldStayVisibleDuringTrackingGap = visibilityType.GetMethod(
            "ShouldStayVisibleDuringTrackingGap",
            BindingFlags.Public | BindingFlags.Static);
    }

    [Test]
    public void TrackingGapKeepsVisibleContentDuringGracePeriod()
    {
        var result = Invoke(true, 0.4f, 0.75f);

        Assert.That(result, Is.True);
    }

    [Test]
    public void TrackingGapHidesContentAfterGracePeriod()
    {
        var result = Invoke(true, 0.8f, 0.75f);

        Assert.That(result, Is.False);
    }

    [Test]
    public void TrackingGapDoesNotRevealHiddenContent()
    {
        var result = Invoke(false, 0.1f, 0.75f);

        Assert.That(result, Is.False);
    }

    [Test]
    public void TrackingGapRejectsNegativeElapsedTime()
    {
        var result = Invoke(true, -0.1f, 0.75f);

        Assert.That(result, Is.False);
    }

    bool Invoke(bool currentlyVisible, float secondsSinceVisibleTracking, float graceSeconds)
    {
        return (bool)shouldStayVisibleDuringTrackingGap.Invoke(
            null,
            new object[] { currentlyVisible, secondsSinceVisibleTracking, graceSeconds });
    }
}
