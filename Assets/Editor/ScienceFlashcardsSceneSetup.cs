using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public static class ScienceFlashcardsSceneSetup
{
    const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    static readonly Color MoonOrbitLineColor = new Color(0.72f, 0.9f, 1f, 0.7f);

    [InitializeOnLoadMethod]
    static void AutoRestoreScienceFlashcardsScene()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var activeScene = EditorSceneManager.GetActiveScene();
            if (!string.Equals(activeScene.path, SampleScenePath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            SelectAndFrameTargets();
        };
    }

    [MenuItem("Tools/Science Flashcards/Setup Basic Vuforia Scene")]
    public static void SetupBasicVuforiaScene()
    {
        SetupBasicVuforiaScene(true);
    }

    public static void SetupBasicVuforiaScene(bool showDialog)
    {
        if (!CanModifyActiveScene(showDialog))
        {
            return;
        }

        ScienceFlashcardsMaterialCreator.CreateMaterials(false);

        DeleteDefaultMainCamera();

        var arCamera = GetOrCreateArCamera();
        TryAddVuforiaBehaviour(arCamera);

        var setup = GetOrCreateRuntimeSetup();
        setup.databasePath = "Vuforia/ScienceFlashcards.xml";
        setup.modelLocalPosition = new Vector3(0f, 0.2f, 0f);
        setup.modelLocalScale = new Vector3(0.35f, 0.35f, 0.35f);
        setup.prefabModelDiameter = 0.48f;
        setup.targets = new[]
        {
            CreateTarget("sun", "SunTarget", "SunModel", "SunMaterial", "Assets/Planets of the Solar System 3D/Prefabs/Sun Sphere.prefab", "SunARMaterial", true, new Color(1f, 0.58f, 0.02f),
                "The Sun is the star at the center of our Solar System.",
                "Sunlight takes about 8 minutes to reach Earth.",
                "The Sun contains about 99.8% of the Solar System's mass."),
            CreateTarget("earth", "EarthTarget", "EarthModel", "EarthMaterial", "Assets/Planet Earth Free/Prefabs/EarthHigh.prefab", null, false, new Color(0.08f, 0.48f, 1f),
                "Earth is the only known planet that supports life.",
                "About 71% of Earth's surface is covered by water.",
                "Earth has one natural satellite: the Moon."),
            CreateTarget("mars", "MarsTarget", "MarsModel", "MarsMaterial", "Assets/Planets of the Solar System 3D/Prefabs/Mars.prefab", "MarsARMaterial", false, new Color(0.9f, 0.28f, 0.08f),
                "Mars is known as the Red Planet.",
                "Mars has two moons: Phobos and Deimos.",
                "Olympus Mons on Mars is the tallest volcano in the Solar System.")
        };

        EnsureSceneTargetHierarchy(setup.targets[0], new Vector3(-1.3f, 0f, 1f), setup.modelLocalPosition, setup.modelLocalScale);
        EnsureSceneTargetHierarchy(setup.targets[1], new Vector3(0f, 0f, 1f), setup.modelLocalPosition, setup.modelLocalScale);
        EnsureSceneTargetHierarchy(setup.targets[2], new Vector3(1.3f, 0f, 1f), setup.modelLocalPosition, setup.modelLocalScale);

        EditorUtility.SetDirty(setup);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        if (FindType("Vuforia.VuforiaBehaviour") == null)
        {
            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Vuforia Engine Needed",
                    "The basic scene helper was added, but Vuforia Engine is not installed yet.\n\nInstall Vuforia Engine first. Then paste your license key in Edit > Project Settings > Vuforia Engine > App License Key. After that, run this menu item again.",
                    "OK");
            }
            return;
        }

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Science Flashcards Ready",
                "Basic scene setup is ready.\n\nThe Hierarchy now contains SunTarget, EarthTarget, and MarsTarget with model and info panel children. Press Play after Vuforia Engine is installed.",
                "OK");
        }
    }

    static bool CanModifyActiveScene(bool showDialog)
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        if (string.Equals(activeScene.path, SampleScenePath, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var message = string.IsNullOrEmpty(activeScene.path)
            ? "Open Assets/Scenes/SampleScene.unity before running Science Flashcards setup. The current scene is Untitled, so setup was skipped to avoid the Save Scene popup."
            : $"Open {SampleScenePath} before running Science Flashcards setup. The active scene is {activeScene.path}, so setup was skipped.";

        if (showDialog)
        {
            EditorUtility.DisplayDialog("Open SampleScene First", message, "OK");
        }

        Debug.LogWarning(message);
        return false;
    }

    [MenuItem("Tools/Science Flashcards/Select And Frame Targets")]
    public static void SelectAndFrameTargets()
    {
        var targets = new[]
        {
            GameObject.Find("SunTarget"),
            GameObject.Find("EarthTarget"),
            GameObject.Find("MarsTarget")
        }.Where(target => target != null).Cast<UnityEngine.Object>().ToArray();

        if (targets.Length == 0)
        {
            return;
        }

        Selection.objects = targets;
        SceneView.FrameLastActiveSceneView();
    }

    static void DeleteDefaultMainCamera()
    {
        var mainCamera = GameObject.Find("Main Camera");
        if (mainCamera != null)
        {
            UnityEngine.Object.DestroyImmediate(mainCamera);
        }
    }

    static GameObject GetOrCreateArCamera()
    {
        var arCamera = GameObject.Find("AR Camera");
        if (arCamera == null)
        {
            arCamera = new GameObject("AR Camera");
        }

        arCamera.tag = "MainCamera";
        arCamera.transform.position = Vector3.zero;
        arCamera.transform.rotation = Quaternion.identity;
        arCamera.transform.localScale = Vector3.one;

        if (arCamera.GetComponent<Camera>() == null)
        {
            arCamera.AddComponent<Camera>();
        }

        EnsureUrpCameraData(arCamera);

        if (arCamera.GetComponent<AudioListener>() == null)
        {
            arCamera.AddComponent<AudioListener>();
        }

        if (arCamera.GetComponent<VuforiaVideoBackgroundFix>() == null)
        {
            arCamera.AddComponent<VuforiaVideoBackgroundFix>();
        }

        return arCamera;
    }

    static void EnsureUrpCameraData(GameObject arCamera)
    {
        var cameraData = arCamera.GetComponent<UniversalAdditionalCameraData>();
        if (cameraData == null)
        {
            cameraData = arCamera.AddComponent<UniversalAdditionalCameraData>();
        }

        cameraData.renderType = CameraRenderType.Base;
        cameraData.renderPostProcessing = false;
    }

    static void TryAddVuforiaBehaviour(GameObject arCamera)
    {
        var vuforiaBehaviourType = FindType("Vuforia.VuforiaBehaviour");
        if (vuforiaBehaviourType == null)
        {
            return;
        }

        if (arCamera.GetComponent(vuforiaBehaviourType) == null)
        {
            arCamera.AddComponent(vuforiaBehaviourType);
        }
    }

    static ScienceFlashcardsRuntimeSetup GetOrCreateRuntimeSetup()
    {
        var setupObject = GameObject.Find("ScienceFlashcardsRuntimeSetup");
        if (setupObject == null)
        {
            setupObject = new GameObject("ScienceFlashcardsRuntimeSetup");
        }

        var setup = setupObject.GetComponent<ScienceFlashcardsRuntimeSetup>();
        if (setup == null)
        {
            setup = setupObject.AddComponent<ScienceFlashcardsRuntimeSetup>();
        }

        return setup;
    }

    static ScienceFlashcardsRuntimeSetup.TargetConfig CreateTarget(string targetName, string targetObjectName, string modelObjectName, string materialName, string prefabPath, string prefabOverrideMaterialName, bool disablePrefabParticleRenderers, Color fallbackColor, params string[] factPages)
    {
        return new ScienceFlashcardsRuntimeSetup.TargetConfig
        {
            targetName = targetName,
            targetObjectName = targetObjectName,
            modelObjectName = modelObjectName,
            modelPrefab = string.IsNullOrEmpty(prefabPath) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath),
            modelMaterial = AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/{materialName}.mat"),
            prefabOverrideMaterial = string.IsNullOrEmpty(prefabOverrideMaterialName) ? null : AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/{prefabOverrideMaterialName}.mat"),
            disablePrefabParticleRenderers = disablePrefabParticleRenderers,
            fallbackColor = fallbackColor,
            factPages = factPages
        };
    }

    static void EnsureSceneTargetHierarchy(ScienceFlashcardsRuntimeSetup.TargetConfig target, Vector3 previewPosition, Vector3 modelLocalPosition, Vector3 modelLocalScale)
    {
        var targetObject = GameObject.Find(target.targetObjectName);
        if (targetObject == null)
        {
            targetObject = new GameObject(target.targetObjectName);
        }

        targetObject.transform.position = previewPosition;

        var visibility = targetObject.GetComponent<VuforiaTargetVisibility>();
        if (visibility == null)
        {
            visibility = targetObject.AddComponent<VuforiaTargetVisibility>();
        }

        visibility.showWhenExtendedTracking = false;
        visibility.showWhenLimitedTracking = true;
        visibility.lostTrackingGraceSeconds = 0.75f;

        if (targetObject.GetComponent<VuforiaTargetPoseStabilizer>() == null)
        {
            targetObject.AddComponent<VuforiaTargetPoseStabilizer>();
        }

        var model = targetObject.transform.Find(target.modelObjectName)?.gameObject;
        if (target.modelPrefab != null && !IsDesiredPrefabInstance(model, target.modelPrefab))
        {
            if (model != null)
            {
                UnityEngine.Object.DestroyImmediate(model);
            }

            model = PrefabUtility.InstantiatePrefab(target.modelPrefab, targetObject.transform) as GameObject;
            if (model == null)
            {
                model = UnityEngine.Object.Instantiate(target.modelPrefab, targetObject.transform);
            }

            model.name = target.modelObjectName;
            if (model.GetComponent<ScienceFlashcardsPrefabModel>() == null)
            {
                model.AddComponent<ScienceFlashcardsPrefabModel>();
            }
        }
        else if (model == null)
        {
            model = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            model.name = target.modelObjectName;
            model.transform.SetParent(targetObject.transform, false);
        }

        model.transform.localPosition = modelLocalPosition;
        if (target.modelPrefab != null)
        {
            FitModelToDiameter(model, 0.48f);
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
        if (target.modelPrefab == null && renderer != null)
        {
            renderer.sharedMaterial = target.modelMaterial;
        }

        CreateOrUpdateInfoPanel(targetObject.transform, GetInfoPanelName(target), target.targetName.ToUpperInvariant(), target.factPages);
        EnsureOrbitingMoon(targetObject.transform, model.transform, target);
    }

    static void EnsureOrbitingMoon(Transform targetParent, Transform orbitCenter, ScienceFlashcardsRuntimeSetup.TargetConfig target)
    {
        if (!string.Equals(target.targetName, "earth", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        target.orbitingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Planets of the Solar System 3D/Prefabs/Moon.prefab");
        target.orbitingObjectName = "MoonModel";

        var moon = targetParent.Find(target.orbitingObjectName)?.gameObject;
        if (moon == null)
        {
            moon = target.orbitingPrefab != null
                ? PrefabUtility.InstantiatePrefab(target.orbitingPrefab, targetParent) as GameObject
                : null;

            if (moon == null)
            {
                moon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                moon.transform.SetParent(targetParent, false);
            }

            moon.name = target.orbitingObjectName;
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

    static void EnsureMoonOrbitPath(Transform targetParent, Transform orbitCenter)
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
        line.sharedMaterial = CreateEditorOrbitMaterial();
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

    static Material CreateEditorOrbitMaterial()
    {
        const string materialPath = "Assets/Materials/MoonOrbitLine.mat";

        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material != null)
        {
            ConfigureOrbitLineMaterial(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        material = new Material(FindOrbitLineShader())
        {
            name = "MoonOrbitLine",
            color = MoonOrbitLineColor
        };
        ConfigureOrbitLineMaterial(material);

        AssetDatabase.CreateAsset(material, materialPath);
        return material;
    }

    static Shader FindOrbitLineShader()
    {
        return Shader.Find("Sprites/Default")
            ?? Shader.Find("Universal Render Pipeline/Particles/Unlit")
            ?? Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Color")
            ?? Shader.Find("Standard");
    }

    static void ConfigureOrbitLineMaterial(Material material)
    {
        material.shader = FindOrbitLineShader();
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

    static void ApplyPrefabRenderingOverrides(GameObject model, ScienceFlashcardsRuntimeSetup.TargetConfig target)
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

    static bool IsDesiredPrefabInstance(GameObject model, GameObject desiredPrefab)
    {
        if (model == null || model.GetComponent<ScienceFlashcardsPrefabModel>() == null)
        {
            return false;
        }

        return PrefabUtility.GetCorrespondingObjectFromSource(model) == desiredPrefab;
    }

    static void FitModelToDiameter(GameObject model, float desiredDiameter)
    {
        model.transform.localScale = Vector3.one;

        var renderers = model.GetComponentsInChildren<Renderer>()
            .Where(renderer => renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
            .ToArray();
        if (renderers.Length == 0)
        {
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
            return;
        }

        model.transform.localScale = Vector3.one * (desiredDiameter / largestSide);
    }

    static void CreateOrUpdateInfoPanel(Transform parent, string panelName, string title, string[] factPages)
    {
        var canvasObject = parent.Find(panelName)?.gameObject;
        if (canvasObject == null)
        {
            canvasObject = new GameObject(panelName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(WorldSpaceBillboard));
            canvasObject.transform.SetParent(parent, false);
        }

        canvasObject.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        canvasObject.transform.localScale = Vector3.one * 0.0032f;

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

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

        var pages = AutoSwipeFactsPanel.SanitizePages(factPages);
        if (pages.Length == 0)
        {
            pages = new[] { "Explore this object in augmented reality." };
        }

        CreateText(background.transform, "Title", title, 22, FontStyle.Bold, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -34f), new Vector2(-12f, -6f));
        var currentLabel = CreateText(background.transform, "Facts", pages[0], 16, FontStyle.Normal, Vector2.zero, Vector2.one, new Vector2(12f, 20f), new Vector2(-12f, -40f));
        var nextLabel = CreateText(background.transform, "NextFacts", pages.Length > 1 ? pages[1] : string.Empty, 16, FontStyle.Normal, Vector2.zero, Vector2.one, new Vector2(12f, 20f), new Vector2(-12f, -40f));
        var pageIndicator = CreateText(background.transform, "PageIndicator", string.Empty, 12, FontStyle.Normal, Vector2.zero, Vector2.one, new Vector2(12f, 6f), new Vector2(-10f, -6f));

        var swipePanel = background.GetComponent<AutoSwipeFactsPanel>();
        if (swipePanel == null)
        {
            swipePanel = background.AddComponent<AutoSwipeFactsPanel>();
        }

        swipePanel.Configure(currentLabel, nextLabel, pageIndicator, pages);
    }

    static Text CreateText(Transform parent, string objectName, string text, int fontSize, FontStyle style, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
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

    static string GetInfoPanelName(ScienceFlashcardsRuntimeSetup.TargetConfig target)
    {
        if (target.targetObjectName.EndsWith("Target", StringComparison.Ordinal))
        {
            return target.targetObjectName.Substring(0, target.targetObjectName.Length - "Target".Length) + "InfoPanel";
        }

        return target.modelObjectName + "InfoPanel";
    }

    static Type FindType(string fullName)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(fullName))
            .FirstOrDefault(type => type != null);
    }
}
