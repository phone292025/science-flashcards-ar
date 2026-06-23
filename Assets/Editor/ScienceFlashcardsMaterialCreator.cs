using System.IO;
using UnityEditor;
using UnityEngine;

public static class ScienceFlashcardsMaterialCreator
{
    const string MaterialFolder = "Assets/Materials";
    const string MarsTexturePath = "Assets/Planets of the Solar System 3D/Textures/Planets/Mars_2k.png";

    [InitializeOnLoadMethod]
    static void RestoreCompatiblePackagePreviews()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            ConvertSolarSystemPackageMaterials();
            AssetDatabase.SaveAssets();
        };
    }

    [MenuItem("Tools/Science Flashcards/Create Test Materials")]
    public static void CreateMaterials()
    {
        CreateMaterials(true);
    }

    public static void CreateMaterials(bool showDialog)
    {
        Directory.CreateDirectory(MaterialFolder);

        CreateMaterial("SunMaterial", new Color(1f, 0.58f, 0.02f));
        CreateMaterial("EarthMaterial", new Color(0.08f, 0.48f, 1f));
        CreateMaterial("MarsMaterial", new Color(0.9f, 0.28f, 0.08f));
        CreateSunArMaterial();
        CreateMarsArMaterial();
        ConvertSolarSystemPackageMaterials();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Science Flashcards",
                "Created SunMaterial, EarthMaterial, and MarsMaterial in Assets/Materials.",
                "OK");
        }
    }

    static void CreateMaterial(string materialName, Color color)
    {
        var path = $"{MaterialFolder}/{materialName}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            var shader = Shader.Find("ScienceFlashcards/PlanetUnlit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Universal Render Pipeline/Unlit");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = Shader.Find("ScienceFlashcards/PlanetUnlit")
                ?? Shader.Find("Standard")
                ?? material.shader;
        }

        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
        EditorUtility.SetDirty(material);
    }

    static void CreateSunArMaterial()
    {
        var shader = Shader.Find("ScienceFlashcards/Sun") ?? GetPlanetShader();
        var material = GetOrCreateMaterial("SunARMaterial", shader);
        ConfigureSunMaterial(material);
    }

    static void ConfigureSunMaterial(Material material)
    {
        var sunColor = new Color(1f, 0.28f, 0.01f, 1f);

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", sunColor);
        }
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", sunColor);
        }
        if (material.HasProperty("_HotColor"))
        {
            material.SetColor("_HotColor", new Color(1f, 0.95f, 0.28f, 1f));
        }
        if (material.HasProperty("_DarkColor"))
        {
            material.SetColor("_DarkColor", new Color(0.5f, 0.035f, 0.005f, 1f));
        }
        if (material.HasProperty("_DetailScale"))
        {
            material.SetFloat("_DetailScale", 7f);
        }
        if (material.HasProperty("_DetailSpeed"))
        {
            material.SetFloat("_DetailSpeed", 0.08f);
        }
        if (material.HasProperty("_GlowStrength"))
        {
            material.SetFloat("_GlowStrength", 0.75f);
        }

        EditorUtility.SetDirty(material);
    }

    static void ConvertSolarSystemPackageMaterials()
    {
        var planetShader = GetPlanetShader();
        var transparentShader = Shader.Find("ScienceFlashcards/TransparentUnlit")
            ?? Shader.Find("Unlit/Transparent")
            ?? planetShader;

        var texturedMaterials = new[,]
        {
            { "URP_Asteroid", "Asteroids.png" },
            { "URP_Earth_2k", "Earth_4k.png" },
            { "URP_Earth_Lod", "Earth_Lod.png" },
            { "URP_Jupiter_2k", "Jupiter_2k.png" },
            { "URP_Jupiter_Lod", "Jupiter_Lod.png" },
            { "URP_Mars_2k", "Mars_2k.png" },
            { "URP_Mars_Lod", "Mars_Lod.png" },
            { "URP_Mercury_2k", "Mercury_2k.png" },
            { "URP_Mercury_Lod", "Mercury_Lod.png" },
            { "URP_Moon_2k", "Moon_2k.png" },
            { "URP_Moon_Lod", "Moon_Lod.png" },
            { "URP_Neptune_2k", "Neptune_2k.png" },
            { "URP_Neptune_Lod", "Neptune_Lod.png" },
            { "URP_Pluto_2k", "Pluto_2k.png" },
            { "URP_Pluto_Lod", "Pluto_Lod.png" },
            { "URP_Saturn_2k", "Saturn_2k.png" },
            { "URP_Saturn_Lod", "Saturn_Lod.png" },
            { "URP_Uranus_2k", "Uranus_2k.png" },
            { "URP_Uranus_Lod", "Uranus_Lod.png" },
            { "URP_Venus_2k", "Venus_2K.png" },
            { "URP_Venus_Lod", "Venus_Lod.png" }
        };

        for (var index = 0; index < texturedMaterials.GetLength(0); index++)
        {
            ConvertPackageMaterial(texturedMaterials[index, 0], texturedMaterials[index, 1], planetShader);
        }

        ConvertPackageMaterial("URP_Saturn_Ring", "Saturn_Ring.png", transparentShader);
        ConvertPackageEffectMaterial("Sun FX", transparentShader);
        ConvertPackageEffectMaterial("URP_Sun_Smoke", transparentShader);

        foreach (var materialName in new[] { "URP_Sun", "URP_Sun Sphere" })
        {
            var material = LoadPackageMaterial(materialName);
            var sunShader = Shader.Find("ScienceFlashcards/Sun") ?? planetShader;
            if (material == null || sunShader == null)
            {
                continue;
            }

            material.shader = sunShader;
            ConfigureSunMaterial(material);
        }
    }

    static void ConvertPackageMaterial(string materialName, string textureName, Shader shader)
    {
        var material = LoadPackageMaterial(materialName);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Planets of the Solar System 3D/Textures/Planets/{textureName}");
        if (material == null || texture == null || shader == null)
        {
            return;
        }

        material.shader = shader;
        material.color = Color.white;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", Color.white);
        }
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", Color.white);
        }

        material.mainTexture = texture;
        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texture);
        }

        EditorUtility.SetDirty(material);
    }

    static void ConvertPackageEffectMaterial(string materialName, Shader shader)
    {
        var material = LoadPackageMaterial(materialName);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Planets of the Solar System 3D/Textures/FX/Cloud.png");
        if (material == null || texture == null || shader == null)
        {
            return;
        }

        material.shader = shader;
        material.mainTexture = texture;
        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texture);
        }

        EditorUtility.SetDirty(material);
    }

    static Material LoadPackageMaterial(string materialName)
    {
        return AssetDatabase.LoadAssetAtPath<Material>($"Assets/Planets of the Solar System 3D/Materials/{materialName}.mat");
    }

    static void CreateMarsArMaterial()
    {
        var shader = GetPlanetShader();
        var material = GetOrCreateMaterial("MarsARMaterial", shader);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(MarsTexturePath);

        material.color = Color.white;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", Color.white);
        }
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", Color.white);
        }

        material.mainTexture = texture;
        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texture);
        }

        EditorUtility.SetDirty(material);
    }

    static Shader GetPlanetShader()
    {
        return Shader.Find("ScienceFlashcards/PlanetUnlit")
            ?? Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Texture")
            ?? Shader.Find("Standard");
    }

    static Material GetOrCreateMaterial(string materialName, Shader shader)
    {
        var path = $"{MaterialFolder}/{materialName}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        else if (material.shader != shader)
        {
            material.shader = shader;
        }

        return material;
    }
}
