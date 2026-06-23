using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class AutoSwipeFactsPanelTests
{
    Type panelType;
    MethodInfo sanitizePages;
    MethodInfo getNextPageIndex;
    GameObject panelObject;
    GameObject firstLabelObject;
    GameObject secondLabelObject;
    GameObject indicatorLabelObject;

    [SetUp]
    public void SetUp()
    {
        panelType = Type.GetType("AutoSwipeFactsPanel, Assembly-CSharp");
        Assert.That(panelType, Is.Not.Null);
        sanitizePages = panelType.GetMethod("SanitizePages", BindingFlags.Public | BindingFlags.Static);
        getNextPageIndex = panelType.GetMethod("GetNextPageIndex", BindingFlags.Public | BindingFlags.Static);
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(panelObject);
        UnityEngine.Object.DestroyImmediate(firstLabelObject);
        UnityEngine.Object.DestroyImmediate(secondLabelObject);
        UnityEngine.Object.DestroyImmediate(indicatorLabelObject);
    }

    [Test]
    public void SanitizePagesReturnsEmptyArrayWhenPagesAreNull()
    {
        var result = InvokeSanitizePages(null);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void SanitizePagesRemovesBlankPages()
    {
        var result = InvokeSanitizePages(new[] { "First fact", " ", null, "Second fact" });

        Assert.That(result, Is.EqualTo(new[] { "First fact", "Second fact" }));
    }

    [Test]
    public void GetNextPageIndexReturnsNextPage()
    {
        var result = InvokeGetNextPageIndex(0, 3);

        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void GetNextPageIndexWrapsBackToFirstPage()
    {
        var result = InvokeGetNextPageIndex(2, 3);

        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void GetNextPageIndexReturnsZeroWhenThereAreNoPages()
    {
        var result = InvokeGetNextPageIndex(0, 0);

        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void ConfigureShowsTheFirstFact()
    {
        var labels = CreateConfiguredPanel();

        Assert.That(labels.Item1.text, Is.EqualTo("First fact"));
    }

    [Test]
    public void ConfigureStagesTheSecondFact()
    {
        var labels = CreateConfiguredPanel();

        Assert.That(labels.Item2.text, Is.EqualTo("Second fact"));
    }

    [Test]
    public void ConfigureShowsTheFirstPageNumber()
    {
        var labels = CreateConfiguredPanel();

        Assert.That(labels.Item3.text, Is.EqualTo("1 / 2"));
    }

    string[] InvokeSanitizePages(string[] pages)
    {
        return (string[])sanitizePages.Invoke(null, new object[] { pages });
    }

    int InvokeGetNextPageIndex(int currentIndex, int pageCount)
    {
        return (int)getNextPageIndex.Invoke(null, new object[] { currentIndex, pageCount });
    }

    Tuple<Text, Text, Text> CreateConfiguredPanel()
    {
        panelObject = new GameObject("Panel", typeof(RectTransform));
        firstLabelObject = new GameObject("Facts", typeof(RectTransform), typeof(Text));
        secondLabelObject = new GameObject("NextFacts", typeof(RectTransform), typeof(Text));
        indicatorLabelObject = new GameObject("PageIndicator", typeof(RectTransform), typeof(Text));
        firstLabelObject.transform.SetParent(panelObject.transform, false);
        secondLabelObject.transform.SetParent(panelObject.transform, false);
        indicatorLabelObject.transform.SetParent(panelObject.transform, false);

        var panel = panelObject.AddComponent(panelType);
        var firstLabel = firstLabelObject.GetComponent<Text>();
        var secondLabel = secondLabelObject.GetComponent<Text>();
        var indicatorLabel = indicatorLabelObject.GetComponent<Text>();
        panelType.GetMethod("Configure").Invoke(panel, new object[]
        {
            firstLabel,
            secondLabel,
            indicatorLabel,
            new[] { "First fact", "Second fact" }
        });

        return Tuple.Create(firstLabel, secondLabel, indicatorLabel);
    }
}
