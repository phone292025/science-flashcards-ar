using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class VuforiaLicenseBootstrap
{
    const string EnvironmentVariableName = "VUFORIA_LICENSE_KEY";

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    static void ApplyInEditorAfterPackageImport()
    {
        EditorApplication.delayCall += ApplyLicenseKey;
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ApplyBeforeSceneLoad()
    {
        ApplyLicenseKey();
    }

    public static void ApplyLicenseKey()
    {
        var licenseKey = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        if (string.IsNullOrWhiteSpace(licenseKey))
        {
            Debug.LogWarning($"Vuforia license key is not configured. Set {EnvironmentVariableName} or paste the key in Project Settings > Vuforia Engine.");
            return;
        }

        var configurationType = FindType("Vuforia.VuforiaConfiguration");
        var instance = configurationType?.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        var vuforiaSettings = instance?.GetType().GetProperty("Vuforia", BindingFlags.Public | BindingFlags.Instance)?.GetValue(instance);
        var licenseProperty = vuforiaSettings?.GetType().GetProperty("LicenseKey", BindingFlags.Public | BindingFlags.Instance);

        if (licenseProperty != null && licenseProperty.CanWrite)
        {
            var currentKey = licenseProperty.GetValue(vuforiaSettings) as string;
            if (currentKey != licenseKey)
            {
                licenseProperty.SetValue(vuforiaSettings, licenseKey);
            }
        }

#if UNITY_EDITOR
        if (instance is UnityEngine.Object unityObject)
        {
            var serializedObject = new SerializedObject(unityObject);
            var plainLicense = serializedObject.FindProperty("vuforia.vuforiaLicenseKey");

            if (plainLicense != null && plainLicense.stringValue != licenseKey)
            {
                plainLicense.stringValue = licenseKey;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(unityObject);
            AssetDatabase.SaveAssets();
        }
#endif
    }

    static Type FindType(string fullName)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(fullName))
            .FirstOrDefault(type => type != null);
    }
}
