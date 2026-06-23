using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class ScienceFlashcardsRuntimeSetup : MonoBehaviour
{
    static readonly Color MoonOrbitLineColor = new Color(0.72f, 0.9f, 1f, 0.7f);
    static Material runtimeMoonOrbitLineMaterial;

    [Serializable]
    public class TargetConfig
    {
        public string targetName;
        public string targetObjectName;
        public string modelObjectName;
        public GameObject modelPrefab;
        public Material modelMaterial;
        public Material prefabOverrideMaterial;
        public bool disablePrefabParticleRenderers;
        public GameObject orbitingPrefab;
        public string orbitingObjectName;
        public Color fallbackColor;
        [TextArea(2, 4)] public string funFacts;
        public string[] factPages;
    }

    public string databasePath = "Vuforia/ScienceFlashcards.xml";
    public Vector3 modelLocalPosition = new Vector3(0f, 0.2f, 0f);
    public Vector3 modelLocalScale = new Vector3(0.35f, 0.35f, 0.35f);
    public float prefabModelDiameter = 0.48f;
    public TargetConfig[] targets =
    {
        new TargetConfig
        {
            targetName = "sun",
            targetObjectName = "SunTarget",
            modelObjectName = "SunModel",
            fallbackColor = new Color(1f, 0.58f, 0.02f),
            factPages = new[]
            {
                "The Sun is the star at the center of our Solar System.",
                "Sunlight takes about 8 minutes to reach Earth.",
                "The Sun contains about 99.8% of the Solar System's mass."
            }
        },
        new TargetConfig
        {
            targetName = "earth",
            targetObjectName = "EarthTarget",
            modelObjectName = "EarthModel",
            fallbackColor = new Color(0.08f, 0.48f, 1f),
            factPages = new[]
            {
                "Earth is the only known planet that supports life.",
                "About 71% of Earth's surface is covered by water.",
                "Earth has one natural satellite: the Moon."
            }
        },
        new TargetConfig
        {
            targetName = "mars",
            targetObjectName = "MarsTarget",
            modelObjectName = "MarsModel",
            fallbackColor = new Color(0.9f, 0.28f, 0.08f),
            factPages = new[]
            {
                "Mars is known as the Red Planet.",
                "Mars has two moons: Phobos and Deimos.",
                "Olympus Mons on Mars is the tallest volcano in the Solar System."
            }
        }
    };

    IEnumerator Start()
    {
        var vuforiaBehaviourType = FindType("Vuforia.VuforiaBehaviour");
        if (vuforiaBehaviourType == null)
        {
            Debug.LogError("Vuforia Engine is not installed yet. Install Vuforia Engine first, then press Play again.");
            yield break;
        }

        EnsureVuforiaBehaviour(vuforiaBehaviourType);

        object vuforiaInstance = null;
        object observerFactory = null;
        yield return WaitForVuforiaToRun(vuforiaBehaviourType, 20f, resultInstance => vuforiaInstance = resultInstance, resultFactory => observerFactory = resultFactory);

        if (vuforiaInstance == null)
        {
            Debug.LogError("Vuforia did not start. Check that the AR Camera has Vuforia Behaviour and that the App License Key is pasted in Project Settings > Vuforia Engine.");
            yield break;
        }

        if (observerFactory == null)
        {
            Debug.LogError("Vuforia ObserverFactory was not available. Stop Play Mode, wait for Unity to finish compiling, then press Play again.");
            yield break;
        }

        var createImageTarget = observerFactory.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(method =>
            {
                if (method.Name != "CreateImageTarget")
                {
                    return false;
                }

                var parameters = method.GetParameters();
                return parameters.Length == 2
                    && parameters[0].ParameterType == typeof(string)
                    && parameters[1].ParameterType == typeof(string);
            });

        if (createImageTarget == null)
        {
            Debug.LogError("Could not find Vuforia's CreateImageTarget(databasePath, targetName) method. Use the manual Image Target setup if your Vuforia version changed this API.");
            yield break;
        }

        Debug.Log("Vuforia is running. Creating Science Flashcards image targets now.");

        foreach (var target in targets)
        {
            CreateTarget(observerFactory, createImageTarget, target);
        }
    }

    void CreateTarget(object observerFactory, MethodInfo createImageTarget, TargetConfig target)
    {
        var placeholder = GameObject.Find(target.targetObjectName);
        if (placeholder != null && FindVuforiaImageTarget(placeholder) != null)
        {
            EnsureTargetChildren(placeholder, target);
            EnsureVisibilityController(placeholder);
            return;
        }

        if (placeholder != null)
        {
            placeholder.name = $"{target.targetObjectName}_Placeholder";
        }

        Component imageTargetObject;
        try
        {
            imageTargetObject = createImageTarget.Invoke(observerFactory, new object[] { databasePath, target.targetName }) as Component;
        }
        catch (TargetInvocationException exception)
        {
            Debug.LogError($"Vuforia failed while creating target '{target.targetName}'. Make sure {databasePath} is imported and the target name is exactly '{target.targetName}'.\n{exception.InnerException?.Message ?? exception.Message}");
            if (placeholder != null)
            {
                placeholder.name = target.targetObjectName;
            }
            return;
        }
        catch (Exception exception)
        {
            Debug.LogError($"Could not create Vuforia target '{target.targetName}'.\n{exception.Message}");
            if (placeholder != null)
            {
                placeholder.name = target.targetObjectName;
            }
            return;
        }

        if (imageTargetObject == null)
        {
            Debug.LogError($"Vuforia could not create target '{target.targetName}' from {databasePath}.");
            if (placeholder != null)
            {
                placeholder.name = target.targetObjectName;
            }
            return;
        }

        var targetGameObject = imageTargetObject.gameObject;
        targetGameObject.name = target.targetObjectName;

        if (placeholder != null)
        {
            while (placeholder.transform.childCount > 0)
            {
                placeholder.transform.GetChild(0).SetParent(targetGameObject.transform, false);
            }

            Destroy(placeholder);
        }

        EnsureTargetChildren(targetGameObject, target);
        EnsureVisibilityController(targetGameObject);

        Debug.Log($"Created {target.targetObjectName} from target '{target.targetName}' with child {target.modelObjectName}.");
    }

    void EnsureTargetChildren(GameObject targetGameObject, TargetConfig target)
    {
        var model = targetGameObject.transform.Find(target.modelObjectName)?.gameObject;
        var shouldUsePrefab = target.modelPrefab != null;
        if (shouldUsePrefab && (model == null || !IsModelPrefabInstance(model)))
        {
            if (model != null)
            {
                Destroy(model);
            }

            model = Instantiate(target.modelPrefab, targetGameObject.transform, false);
            model.name = target.modelObjectName;
            model.AddComponent<ScienceFlashcardsPrefabModel>();
        }
        else if (model == null)
        {
            model = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            model.name = target.modelObjectName;
            model.transform.SetParent(targetGameObject.transform, false);
        }

        model.transform.localPosition = modelLocalPosition;
        if (shouldUsePrefab)
        {
            FitModelToDiameter(model, prefabModelDiameter);
            ApplyPrefabRenderingOverrides(model, target);
        }
        else
        {
            model.transform.localScale = modelLocalScale;
        }

        if (model.GetComponent<RotatePlanet>() == null)
        {
            model.AddComponent<RotatePlanet>();
        }

        var renderer = model.GetComponent<Renderer>();
        if (!shouldUsePrefab && renderer != null)
        {
            renderer.sharedMaterial = target.modelMaterial != null ? target.modelMaterial : CreateRuntimeMaterial(target);
        }

        CreateOrUpdateInfoPanel(targetGameObject.transform, target);
        EnsureOrbitingMoon(targetGameObject.transform, model.transform, target);
    }

    void EnsureOrbitingMoon(Transform targetParent, Transform orbitCenter, TargetConfig target)
    {
        if (!string.Equals(target.targetName, "earth", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var moonName = string.IsNullOrWhiteSpace(target.orbitingObjectName) ? "MoonModel" : target.orbitingObjectName;
        var moon = targetParent.Find(moonName)?.gameObject;
        if (moon == null)
        {
            var moonPrefab = GetOrbitingMoonPrefab(target);
            moon = moonPrefab != null
                ? Instantiate(moonPrefab, targetParent, false)
                : GameObject.CreatePrimitive(PrimitiveType.Sphere);
            moon.name = moonName;
        }

        moon.transform.localScale = Vector3.one * 0.08f;
        moon.transform.localPosition = orbitCenter.localPosition + MoonOrbit.CalculateOrbitOffset(0f, 0.42f, 12f);

        var orbit = moon.GetComponent<MoonOrbit>();
        if (orbit == null)
        {
            orbit = moon.AddComponent<MoonOrbit>();
        }

        orbit.orbitCenter = orbitCenter;
        orbit.orbitFrame = targetParent;
        orbit.orbitRadius = 0.42f;
        orbit.orbitSpeed = 35f;
        orbit.selfRotationSpeed = 45f;
        orbit.orbitTiltDegrees = 12f;

        EnsureMoonOrbitPath(targetParent, orbitCenter);
    }

    GameObject GetOrbitingMoonPrefab(TargetConfig target)
    {
        if (target.orbitingPrefab != null)
        {
            return target.orbitingPrefab;
        }

#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Planets of the Solar System 3D/Prefabs/Moon.prefab");
#else
        return null;
#endif
    }

    void EnsureMoonOrbitPath(Transform targetParent, Transform orbitCenter)
    {
        var path = targetParent.Find("MoonOrbitPath")?.gameObject;
        if (path == null)
        {
            path = new GameObject("MoonOrbitPath");
            path.transform.SetParent(targetParent, false);
        }

        path.transform.localPosition = orbitCenter.localPosition;
        path.transform.localRotation = Quaternion.identity;
        path.transform.localScale = Vector3.one;

        var line = path.GetComponent<LineRenderer>();
        if (line == null)
        {
            line = path.AddComponent<LineRenderer>();
        }

        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 96;
        line.widthMultiplier = 0.005f;
        line.sharedMaterial = CreateRuntimeOrbitMaterial();
        line.startColor = MoonOrbitLineColor;
        line.endColor = MoonOrbitLineColor;

        var orbitRotation = Quaternion.Euler(12f, 0f, 0f);
        for (var i = 0; i < line.positionCount; i++)
        {
            var angle = (Mathf.PI * 2f * i) / line.positionCount;
            var offset = new Vector3(Mathf.Cos(angle) * 0.42f, 0f, Mathf.Sin(angle) * 0.42f);
            line.SetPosition(i, orbitRotation * offset);
        }
    }

    Material CreateRuntimeOrbitMaterial()
    {
        if (runtimeMoonOrbitLineMaterial != null)
        {
            return runtimeMoonOrbitLineMaterial;
        }

        runtimeMoonOrbitLineMaterial = new Material(FindOrbitLineShader())
        {
            name = "MoonOrbitRuntimeMaterial",
            color = MoonOrbitLineColor
        };
        ConfigureOrbitLineMaterial(runtimeMoonOrbitLineMaterial);
        return runtimeMoonOrbitLineMaterial;
    }

    Shader FindOrbitLineShader()
    {
        return Shader.Find("Sprites/Default")
            ?? Shader.Find("Universal Render Pipeline/Particles/Unlit")
            ?? Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Color")
            ?? Shader.Find("Standard");
    }

    void ConfigureOrbitLineMaterial(Material material)
    {
        material.renderQueue = 3000;
        material.SetOverrideTag("RenderType", "Transparent");

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }

        if (material.HasProperty("_Blend"))
        {
            material.SetFloat("_Blend", 0f);
        }

        if (material.HasProperty("_SrcBlend"))
        {
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        }

        if (material.HasProperty("_DstBlend"))
        {
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        }

        if (material.HasProperty("_ZWrite"))
        {
            material.SetFloat("_ZWrite", 0f);
        }

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_ALPHABLEND_ON");

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", MoonOrbitLineColor);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", MoonOrbitLineColor);
        }

        if (material.HasProperty("_TintColor"))
        {
            material.SetColor("_TintColor", MoonOrbitLineColor);
        }
    }

    void ApplyPrefabRenderingOverrides(GameObject model, TargetConfig target)
    {
        if (target.prefabOverrideMaterial != null)
        {
            foreach (var meshRenderer in model.GetComponentsInChildren<MeshRenderer>(true))
            {
                meshRenderer.sharedMaterial = target.prefabOverrideMaterial;
            }

            foreach (var skinnedMeshRenderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                skinnedMeshRenderer.sharedMaterial = target.prefabOverrideMaterial;
            }
        }

        if (target.disablePrefabParticleRenderers)
        {
            foreach (var particleRenderer in model.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                particleRenderer.enabled = false;
            }
        }
    }

    bool IsModelPrefabInstance(GameObject model)
    {
        return model.GetComponent<ScienceFlashcardsPrefabModel>() != null;
    }

    void FitModelToDiameter(GameObject model, float desiredDiameter)
    {
        model.transform.localScale = Vector3.one;

        var renderers = model.GetComponentsInChildren<Renderer>()
            .Where(renderer => renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
            .ToArray();
        if (renderers.Length == 0)
        {
            model.transform.localScale = modelLocalScale;
            return;
        }

        var bounds = renderers[0].bounds;
        foreach (var childRenderer in renderers.Skip(1))
        {
            bounds.Encapsulate(childRenderer.bounds);
        }

        var largestSide = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (largestSide <= 0.001f)
        {
            model.transform.localScale = modelLocalScale;
            return;
        }

        var scale = desiredDiameter / largestSide;
        model.transform.localScale = Vector3.one * scale;
    }

    void EnsureVisibilityController(GameObject targetGameObject)
    {
        var visibility = targetGameObject.GetComponent<VuforiaTargetVisibility>();
        if (visibility == null)
        {
            visibility = targetGameObject.AddComponent<VuforiaTargetVisibility>();
        }

        visibility.showWhenExtendedTracking = false;
        visibility.showWhenLimitedTracking = true;
        visibility.lostTrackingGraceSeconds = 0.75f;
        visibility.SetVisibleOnStart(false);

        if (targetGameObject.GetComponent<VuforiaTargetPoseStabilizer>() == null)
        {
            targetGameObject.AddComponent<VuforiaTargetPoseStabilizer>();
        }
    }

    Component FindVuforiaImageTarget(GameObject gameObjectToCheck)
    {
        return gameObjectToCheck.GetComponents<Component>()
            .FirstOrDefault(component => component != null
                && component.GetType().FullName == "Vuforia.ImageTargetBehaviour");
    }

    GameObject CreateOrUpdateInfoPanel(Transform parent, TargetConfig target)
    {
        var canvasObject = parent.Find(GetInfoPanelName(target))?.gameObject;
        if (canvasObject == null)
        {
            canvasObject = new GameObject(GetInfoPanelName(target), typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(WorldSpaceBillboard));
            canvasObject.transform.SetParent(parent, false);
        }

        canvasObject.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        canvasObject.transform.localScale = Vector3.one * 0.0032f;

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;

        var canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(285f, 118f);

        var background = canvasObject.transform.Find("Panel")?.gameObject;
        if (background == null)
        {
            background = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            background.transform.SetParent(canvasObject.transform, false);
        }
        else if (background.GetComponent<RectMask2D>() == null)
        {
            background.AddComponent<RectMask2D>();
        }

        var backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        background.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.06f, 0.92f);

        var pages = GetFactPages(target);
        CreateText(background.transform, "Title", target.targetName.ToUpperInvariant(), 22, FontStyle.Bold, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -34f), new Vector2(-12f, -6f));
        var currentLabel = CreateText(background.transform, "Facts", pages[0], 16, FontStyle.Normal, Vector2.zero, Vector2.one, new Vector2(12f, 20f), new Vector2(-12f, -40f));
        var nextLabel = CreateText(background.transform, "NextFacts", pages.Length > 1 ? pages[1] : string.Empty, 16, FontStyle.Normal, Vector2.zero, Vector2.one, new Vector2(12f, 20f), new Vector2(-12f, -40f));
        var pageIndicator = CreateText(background.transform, "PageIndicator", string.Empty, 12, FontStyle.Normal, Vector2.zero, Vector2.one, new Vector2(12f, 6f), new Vector2(-10f, -6f));

        var swipePanel = background.GetComponent<AutoSwipeFactsPanel>();
        if (swipePanel == null)
        {
            swipePanel = background.AddComponent<AutoSwipeFactsPanel>();
        }

        swipePanel.Configure(currentLabel, nextLabel, pageIndicator, pages);

        return canvasObject;
    }

    Text CreateText(Transform parent, string objectName, string text, int fontSize, FontStyle style, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var textObject = parent.Find(objectName)?.gameObject;
        if (textObject == null)
        {
            textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
        }

        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        var label = textObject.GetComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.color = Color.white;
        label.alignment = objectName == "Title"
            ? TextAnchor.MiddleLeft
            : objectName == "PageIndicator"
                ? TextAnchor.LowerRight
                : TextAnchor.UpperLeft;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        return label;
    }

    string[] GetFactPages(TargetConfig target)
    {
        var pages = AutoSwipeFactsPanel.SanitizePages(target.factPages);
        if (pages.Length > 0)
        {
            return pages;
        }

        return new[] { string.IsNullOrWhiteSpace(target.funFacts) ? "Explore this object in augmented reality." : target.funFacts };
    }

    string GetInfoPanelName(TargetConfig target)
    {
        if (target.targetObjectName.EndsWith("Target", StringComparison.Ordinal))
        {
            return target.targetObjectName.Substring(0, target.targetObjectName.Length - "Target".Length) + "InfoPanel";
        }

        return target.modelObjectName + "InfoPanel";
    }

    Material CreateRuntimeMaterial(TargetConfig target)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = new Material(shader)
        {
            name = $"{target.modelObjectName}RuntimeMaterial",
            color = target.fallbackColor
        };
        return material;
    }

    void EnsureVuforiaBehaviour(Type vuforiaBehaviourType)
    {
        if (FindSceneComponent(vuforiaBehaviourType) != null)
        {
            var existingCamera = Camera.main;
            if (existingCamera != null && existingCamera.GetComponent<VuforiaVideoBackgroundFix>() == null)
            {
                existingCamera.gameObject.AddComponent<VuforiaVideoBackgroundFix>();
            }

            return;
        }

        var mainCamera = Camera.main;
        if (mainCamera == null)
        {
            var cameraObject = new GameObject("AR Camera");
            mainCamera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<AudioListener>();
        }

        if (mainCamera.GetComponent<VuforiaVideoBackgroundFix>() == null)
        {
            mainCamera.gameObject.AddComponent<VuforiaVideoBackgroundFix>();
        }

        mainCamera.gameObject.AddComponent(vuforiaBehaviourType);
    }

    Component FindSceneComponent(Type componentType)
    {
        return Resources.FindObjectsOfTypeAll<Component>()
            .FirstOrDefault(component => component != null
                && component.gameObject.scene.IsValid()
                && componentType.IsInstanceOfType(component));
    }

    object GetVuforiaInstance(Type vuforiaBehaviourType)
    {
        return vuforiaBehaviourType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
    }

    IEnumerator WaitForVuforiaToRun(Type vuforiaBehaviourType, float timeoutSeconds, Action<object> setInstance, Action<object> setObserverFactory)
    {
        var deadline = Time.realtimeSinceStartup + timeoutSeconds;

        while (Time.realtimeSinceStartup < deadline)
        {
            var vuforiaInstance = GetVuforiaInstance(vuforiaBehaviourType);
            var observerFactory = vuforiaInstance != null ? GetPropertyValue(vuforiaInstance, "ObserverFactory") : null;

            if (vuforiaInstance != null && observerFactory != null && IsVuforiaApplicationRunning())
            {
                setInstance(vuforiaInstance);
                setObserverFactory(observerFactory);
                yield break;
            }

            yield return null;
        }

        setInstance(null);
        setObserverFactory(null);
    }

    bool IsVuforiaApplicationRunning()
    {
        var vuforiaApplicationType = FindType("Vuforia.VuforiaApplication");
        if (vuforiaApplicationType == null)
        {
            return true;
        }

        var applicationInstance = vuforiaApplicationType
            .GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)
            ?.GetValue(null);

        if (applicationInstance == null)
        {
            return false;
        }

        var isInitialized = GetBoolProperty(applicationInstance, "IsInitialized");
        var isRunning = GetBoolProperty(applicationInstance, "IsRunning");
        return isInitialized && isRunning;
    }

    bool GetBoolProperty(object instance, string propertyName)
    {
        var value = instance.GetType()
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)
            ?.GetValue(instance);

        return value is bool booleanValue && booleanValue;
    }

    object GetPropertyValue(object instance, string propertyName)
    {
        return instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)?.GetValue(instance);
    }

    Type FindType(string fullName)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(fullName))
            .FirstOrDefault(type => type != null);
    }
}
