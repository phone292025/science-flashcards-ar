using UnityEngine;
using Vuforia;

public class VuforiaTargetVisibility : MonoBehaviour
{
    public bool showWhenExtendedTracking;
    public bool showWhenLimitedTracking;
    public float lostTrackingGraceSeconds = 0.75f;

    ObserverBehaviour observerBehaviour;
    bool currentVisibility;
    bool hasAppliedVisibility;
    float lastVisibleTrackingTime = float.NegativeInfinity;

    void Awake()
    {
        SetAugmentationsVisible(false);
    }

    void Start()
    {
        FindAndSubscribeObserver();
        UpdateVisibilityFromCurrentStatus();
    }

    void Update()
    {
        if (observerBehaviour == null)
        {
            FindAndSubscribeObserver();
        }

        UpdateVisibilityFromCurrentStatus();
    }

    void OnDestroy()
    {
        if (observerBehaviour != null)
        {
            observerBehaviour.OnTargetStatusChanged -= HandleTargetStatusChanged;
            observerBehaviour.OnBehaviourDestroyed -= HandleObserverDestroyed;
        }
    }

    public void SetVisibleOnStart(bool visible)
    {
        SetAugmentationsVisible(visible);
    }

    void FindAndSubscribeObserver()
    {
        observerBehaviour = GetComponent<ObserverBehaviour>();
        if (observerBehaviour == null)
        {
            return;
        }

        observerBehaviour.OnTargetStatusChanged -= HandleTargetStatusChanged;
        observerBehaviour.OnTargetStatusChanged += HandleTargetStatusChanged;
        observerBehaviour.OnBehaviourDestroyed -= HandleObserverDestroyed;
        observerBehaviour.OnBehaviourDestroyed += HandleObserverDestroyed;
    }

    void HandleTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus targetStatus)
    {
        UpdateVisibility(ShouldShow(targetStatus));
    }

    void HandleObserverDestroyed(ObserverBehaviour behaviour)
    {
        if (observerBehaviour != null)
        {
            observerBehaviour.OnTargetStatusChanged -= HandleTargetStatusChanged;
            observerBehaviour.OnBehaviourDestroyed -= HandleObserverDestroyed;
        }

        observerBehaviour = null;
        SetAugmentationsVisible(false);
    }

    void UpdateVisibilityFromCurrentStatus()
    {
        UpdateVisibility(observerBehaviour != null && ShouldShow(observerBehaviour.TargetStatus));
    }

    void UpdateVisibility(bool hasVisibleTracking)
    {
        if (hasVisibleTracking)
        {
            lastVisibleTrackingTime = Time.unscaledTime;
            SetAugmentationsVisible(true);
            return;
        }

        var secondsSinceVisibleTracking = Time.unscaledTime - lastVisibleTrackingTime;
        if (ShouldStayVisibleDuringTrackingGap(currentVisibility, secondsSinceVisibleTracking, lostTrackingGraceSeconds))
        {
            return;
        }

        SetAugmentationsVisible(false);
    }

    bool ShouldShow(TargetStatus targetStatus)
    {
        if (targetStatus.Status == Status.TRACKED)
        {
            return true;
        }

        if (showWhenExtendedTracking && targetStatus.Status == Status.EXTENDED_TRACKED)
        {
            return true;
        }

        return showWhenLimitedTracking
            && targetStatus.Status == Status.LIMITED
            && targetStatus.StatusInfo == StatusInfo.NORMAL;
    }

    public static bool ShouldStayVisibleDuringTrackingGap(bool currentlyVisible, float secondsSinceVisibleTracking, float graceSeconds)
    {
        return currentlyVisible
            && secondsSinceVisibleTracking >= 0f
            && secondsSinceVisibleTracking <= Mathf.Max(0f, graceSeconds);
    }

    void SetAugmentationsVisible(bool visible)
    {
        if (hasAppliedVisibility && currentVisibility == visible)
        {
            return;
        }

        hasAppliedVisibility = true;
        currentVisibility = visible;

        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(visible);
        }
    }
}
