using UnityEngine;

public class WorldSpaceBillboard : MonoBehaviour
{
    void LateUpdate()
    {
        var cameraToFace = Camera.main;
        if (cameraToFace == null)
        {
            return;
        }

        transform.rotation = cameraToFace.transform.rotation;
    }
}
