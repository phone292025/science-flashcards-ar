using System.IO;
using UnityEditor;
using UnityEngine;

public static class ScienceFlashcardsSolarSystemPackageImporter
{
    const string PackageFileName = "Planets of the Solar System 3D.unitypackage";
    const string SunPrefabPath = "Assets/Planets of the Solar System 3D/Prefabs/Sun Sphere.prefab";
    const string MarsPrefabPath = "Assets/Planets of the Solar System 3D/Prefabs/Mars.prefab";
    const string ImportStartedKey = "ScienceFlashcards.SolarSystemPackageImportStarted";

    [InitializeOnLoadMethod]
    static void ImportSolarSystemPackageIfNeeded()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (SolarSystemPrefabsExist())
            {
                ScienceFlashcardsSceneSetup.SetupBasicVuforiaScene(false);
                return;
            }

            var packagePath = FindPackagePath();
            if (string.IsNullOrEmpty(packagePath) || SessionState.GetBool(ImportStartedKey, false))
            {
                return;
            }

            SessionState.SetBool(ImportStartedKey, true);
            AssetDatabase.importPackageCompleted += OnImportPackageCompleted;
            AssetDatabase.ImportPackage(packagePath, false);
        };
    }

    [MenuItem("Tools/Science Flashcards/Import Solar System 3D")]
    public static void ImportSolarSystemPackageFromMenu()
    {
        var packagePath = FindPackagePath();
        if (string.IsNullOrEmpty(packagePath))
        {
            EditorUtility.DisplayDialog(
                "Solar System Package Not Found",
                "Unity has not downloaded Planets of the Solar System 3D into the Asset Store cache yet. Open Package Manager, find the package, and click Download.",
                "OK");
            return;
        }

        AssetDatabase.importPackageCompleted += OnImportPackageCompleted;
        AssetDatabase.ImportPackage(packagePath, false);
    }

    static void OnImportPackageCompleted(string packageName)
    {
        AssetDatabase.importPackageCompleted -= OnImportPackageCompleted;
        SessionState.SetBool(ImportStartedKey, false);
        AssetDatabase.Refresh();
        ScienceFlashcardsSceneSetup.SetupBasicVuforiaScene(false);
        ScienceFlashcardsSceneSetup.SelectAndFrameTargets();
        Debug.Log("Planets of the Solar System 3D imported. SunTarget now uses Sun Sphere and MarsTarget now uses Mars.");
    }

    static bool SolarSystemPrefabsExist()
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(SunPrefabPath) != null
            && AssetDatabase.LoadAssetAtPath<GameObject>(MarsPrefabPath) != null;
    }

    static string FindPackagePath()
    {
        var localPackage = Path.Combine(Application.dataPath, "..", "Packages", PackageFileName);
        return File.Exists(localPackage) ? Path.GetFullPath(localPackage) : null;
    }
}
