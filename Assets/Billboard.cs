using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        // Only rotate around Y axis - keeps character upright
        Vector3 cameraForward = mainCamera.transform.forward;
        cameraForward.y = 0;
        transform.rotation = Quaternion.LookRotation(cameraForward);
    }
}