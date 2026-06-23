using System.IO;
using UnityEditor;
using UnityEngine;

public static class ScienceFlashcardsEarthPackageImporter
{
    const string PackageFileName = "Planet Earth Free.unitypackage";
    const string EarthPrefabPath = "Assets/Planet Earth Free/Prefabs/EarthHigh.prefab";
    const string ImportStartedKey = "ScienceFlashcards.EarthPackageImportStarted";

    [InitializeOnLoadMethod]
    static void ImportEarthPackageIfNeeded()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(EarthPrefabPath) != null)
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

    [MenuItem("Tools/Science Flashcards/Import Planet Earth Free")]
    public static void ImportEarthPackageFromMenu()
    {
        var packagePath = FindPackagePath();
        if (string.IsNullOrEmpty(packagePath))
        {
            EditorUtility.DisplayDialog(
                "Planet Earth Free Not Found",
                "Unity has not downloaded Planet Earth Free into the Asset Store cache yet. Open Package Manager, find Planet Earth Free, and click Download/Import.",
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
        Debug.Log("Planet Earth Free imported. EarthTarget now uses the EarthHigh 3D prefab when the earth marker is tracked.");
    }

    static string FindPackagePath()
    {
        var localPackage = Path.Combine(Application.dataPath, "..", "Packages", PackageFileName);
        return File.Exists(localPackage) ? Path.GetFullPath(localPackage) : null;
    }
}
