using System.Collections;
using UnityEngine;
using Vuforia;

public class VuforiaVideoBackgroundFix : MonoBehaviour
{
    const string BackgroundName = "VuforiaGameViewBackground";

    Camera arCamera;

    IEnumerator Start()
    {
        DestroyOldBackgroundObjects();
        arCamera = GetComponent<Camera>();

        while (VuforiaBehaviour.Instance == null || VuforiaBehaviour.Instance.VideoBackground == null)
        {
            yield return null;
        }

        VuforiaBehaviour.Instance.VideoBackground.StartVideoBackgroundRendering();
        KeepNativeVideoBackgroundVisible();
        Debug.Log("Vuforia video background rendering forced on.");

        yield return new WaitForSeconds(1f);
        foreach (var activeCamera in Camera.allCameras)
        {
            Debug.Log($"Active camera: '{activeCamera.name}', depth={activeCamera.depth}, clearFlags={activeCamera.clearFlags}, cullingMask={activeCamera.cullingMask}, position={activeCamera.transform.position}, rotation={activeCamera.transform.eulerAngles}.");
        }
    }

    void LateUpdate()
    {
        KeepNativeVideoBackgroundVisible();
    }

    void DestroyOldBackgroundObjects()
    {
        foreach (var oldName in new[] { "WebcamVideoFallback", BackgroundName, "VuforiaGameViewBackgroundCanvas", "VuforiaWebcamBackgroundCamera" })
        {
            var oldBackground = transform.Find(oldName);
            if (oldBackground != null)
            {
                Destroy(oldBackground.gameObject);
            }
        }
    }

    void KeepNativeVideoBackgroundVisible()
    {
        if (arCamera != null)
        {
            arCamera.clearFlags = CameraClearFlags.Depth;
        }
    }
}
