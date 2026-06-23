using System;
using UnityEngine;
using UnityEngine.UI;

public class AutoSwipeFactsPanel : MonoBehaviour
{
    public Text currentLabel;
    public Text nextLabel;
    public Text pageIndicatorLabel;
    public string[] pages = Array.Empty<string>();
    public float pageDuration = 3f;
    public float swipeDuration = 4f;

    int currentPageIndex;
    float pageTimer;
    bool isSwiping;
    RectTransform currentRect;
    RectTransform nextRect;

    public void Configure(Text firstLabel, Text secondLabel, Text indicatorLabel, string[] factPages)
    {
        currentLabel = firstLabel;
        nextLabel = secondLabel;
        pageIndicatorLabel = indicatorLabel;
        pages = SanitizePages(factPages);
        currentRect = currentLabel != null ? currentLabel.rectTransform : null;
        nextRect = nextLabel != null ? nextLabel.rectTransform : null;
        ResetToFirstPage();
    }

    void OnEnable()
    {
        ResetToFirstPage();
    }

    void Update()
    {
        if (pages == null || pages.Length < 2 || currentRect == null || nextRect == null)
        {
            return;
        }

        pageTimer += Time.deltaTime;
        if (!isSwiping && pageTimer >= pageDuration)
        {
            BeginSwipe();
        }

        if (isSwiping)
        {
            UpdateSwipe();
        }
    }

    void BeginSwipe()
    {
        isSwiping = true;
        pageTimer = 0f;
        nextLabel.text = pages[GetNextPageIndex(currentPageIndex, pages.Length)];
        SetHorizontalOffset(nextRect, GetPanelWidth());
    }

    void UpdateSwipe()
    {
        var duration = Mathf.Max(0.01f, swipeDuration);
        var progress = Mathf.Clamp01(pageTimer / duration);
        var easedProgress = progress * progress * (3f - 2f * progress);
        var panelWidth = GetPanelWidth();

        SetHorizontalOffset(currentRect, -panelWidth * easedProgress);
        SetHorizontalOffset(nextRect, panelWidth * (1f - easedProgress));

        if (progress < 1f)
        {
            return;
        }

        currentPageIndex = GetNextPageIndex(currentPageIndex, pages.Length);
        currentLabel.text = pages[currentPageIndex];
        UpdatePageIndicator();
        SetHorizontalOffset(currentRect, 0f);
        SetHorizontalOffset(nextRect, panelWidth);
        isSwiping = false;
        pageTimer = 0f;
    }

    void ResetToFirstPage()
    {
        pages = SanitizePages(pages);
        currentPageIndex = 0;
        pageTimer = 0f;
        isSwiping = false;
        currentRect = currentLabel != null ? currentLabel.rectTransform : null;
        nextRect = nextLabel != null ? nextLabel.rectTransform : null;

        if (currentLabel != null)
        {
            currentLabel.text = pages.Length > 0 ? pages[0] : string.Empty;
        }

        if (nextLabel != null)
        {
            nextLabel.text = pages.Length > 1 ? pages[1] : string.Empty;
            SetHorizontalOffset(nextRect, GetPanelWidth());
        }

        UpdatePageIndicator();
        SetHorizontalOffset(currentRect, 0f);
    }

    void UpdatePageIndicator()
    {
        if (pageIndicatorLabel != null)
        {
            pageIndicatorLabel.text = pages.Length > 0 ? $"{currentPageIndex + 1} / {pages.Length}" : string.Empty;
        }
    }

    float GetPanelWidth()
    {
        var rect = transform as RectTransform;
        return rect != null && rect.rect.width > 0f ? rect.rect.width : 280f;
    }

    static void SetHorizontalOffset(RectTransform rect, float offset)
    {
        if (rect != null)
        {
            rect.anchoredPosition = new Vector2(offset, rect.anchoredPosition.y);
        }
    }

    public static string[] SanitizePages(string[] factPages)
    {
        if (factPages == null || factPages.Length == 0)
        {
            return Array.Empty<string>();
        }

        return Array.FindAll(factPages, page => !string.IsNullOrWhiteSpace(page));
    }

    public static int GetNextPageIndex(int currentIndex, int pageCount)
    {
        return pageCount <= 0 ? 0 : (currentIndex + 1) % pageCount;
    }
}
