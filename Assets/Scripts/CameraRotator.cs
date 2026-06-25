using UnityEngine;
using UnityEngine.InputSystem;

public class CameraRotator : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform; // Drag Main Camera here
    
    [Header("Settings")]
    public float rotationSpeed = 360f; // Degrees per second
    
    private float targetYRotation = 0f;
    private float currentYRotation = 0f;
    private bool isRotating = false;

    void Start()
    {
        // Initialize to current camera rotation
        targetYRotation = cameraTransform.localEulerAngles.y;
        currentYRotation = targetYRotation;
    }

    void OnRotateCameraLeft()
    {
        if (!isRotating)
        {
            targetYRotation -= 90f;
            isRotating = true;
        }
    }

    void OnRotateCameraRight()
    {
        if (!isRotating)
        {
            targetYRotation += 90f;
            isRotating = true;
        }
    }

    void Update()
    {
        if (isRotating)
        {
            // Smoothly rotate toward target
            currentYRotation = Mathf.MoveTowardsAngle(
                currentYRotation, 
                targetYRotation, 
                rotationSpeed * Time.deltaTime
            );

            // Apply rotation around player at Y axis
            // We rotate the camera's position around the player AND its facing direction
            Vector3 offset = new Vector3(
                cameraTransform.localPosition.x,
                cameraTransform.localPosition.y,
                cameraTransform.localPosition.z
            );

            // Get the original offset distance (radius from player)
            float radius = new Vector2(offset.x, offset.z).magnitude;
            
            // Calculate new position based on rotation
            float angleRad = currentYRotation * Mathf.Deg2Rad;
            float newX = -Mathf.Sin(angleRad) * radius;
            float newZ = -Mathf.Cos(angleRad) * radius;
            
            cameraTransform.localPosition = new Vector3(newX, offset.y, newZ);
            
            // Make camera look at player (keep tilt)
            Vector3 currentEuler = cameraTransform.localEulerAngles;
            cameraTransform.localEulerAngles = new Vector3(
                currentEuler.x, // Keep X tilt (35.923)
                currentYRotation,
                0
            );

            // Check if reached target
            if (Mathf.Abs(Mathf.DeltaAngle(currentYRotation, targetYRotation)) < 0.1f)
            {
                currentYRotation = targetYRotation;
                isRotating = false;
            }
        }
    }
}