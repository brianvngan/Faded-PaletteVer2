using UnityEngine;
using UnityEngine.UI;

public class Compass : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform cameraTransform; // ADD THIS - drag Main Camera here
    public RectTransform compassRect;
    public RectTransform dotIndicator;
    public RectTransform arrowIndicator;
    public GridSpawner gridSpawner;

    [Header("Settings")]
    public float compassRadius = 60f;
    public float worldRange = 30f;

    void Start()
    {
        if (dotIndicator != null)
        {
            dotIndicator.anchorMin = new Vector2(0.5f, 0.5f);
            dotIndicator.anchorMax = new Vector2(0.5f, 0.5f);
            dotIndicator.pivot = new Vector2(0.5f, 0.5f);
        }
        if (arrowIndicator != null)
        {
            arrowIndicator.anchorMin = new Vector2(0.5f, 0.5f);
            arrowIndicator.anchorMax = new Vector2(0.5f, 0.5f);
            arrowIndicator.pivot = new Vector2(0.5f, 0.5f);
        }
    }

    void Update()
    {
        if (player == null || gridSpawner == null) return;

        Vector2 clearPos = gridSpawner.GetClearTilePosition();
        Vector2 playerPos = new Vector2(player.position.x, player.position.z);
        
        Vector2 toTarget = clearPos - playerPos;
        float distance = toTarget.magnitude;

        // Get camera Y rotation to rotate the compass display
        float cameraYRotation = 0f;
        if (cameraTransform != null)
            cameraYRotation = cameraTransform.eulerAngles.y;


        //Debug.Log($"Camera Y Rotation: {cameraYRotation}");

        // Rotate toTarget by negative camera rotation so "up" on compass = camera forward
        toTarget = RotateVector2(toTarget, -cameraYRotation);

        if (distance <= worldRange)
        {
            dotIndicator.gameObject.SetActive(true);
            arrowIndicator.gameObject.SetActive(false);

            Vector2 compassPos = (toTarget / worldRange) * compassRadius;
            
            if (compassPos.magnitude > compassRadius)
                compassPos = compassPos.normalized * compassRadius;
            
            dotIndicator.anchoredPosition = compassPos;
        }
        else
        {
            dotIndicator.gameObject.SetActive(false);
            arrowIndicator.gameObject.SetActive(true);

            Vector2 dirNormalized = toTarget.normalized;
            arrowIndicator.anchoredPosition = dirNormalized * compassRadius;
            
            float angle = Mathf.Atan2(dirNormalized.y, dirNormalized.x) * Mathf.Rad2Deg;
            arrowIndicator.localRotation = Quaternion.Euler(0, 0, angle - 90f);
        }
    }

    // Rotates a 2D vector by a given angle in degrees
    Vector2 RotateVector2(Vector2 v, float degrees)
{
    float rad = degrees * Mathf.Deg2Rad;
    float cos = Mathf.Cos(rad);
    float sin = Mathf.Sin(rad);
    return new Vector2(
        v.x * cos + v.y * sin,   // Changed from `- v.y * sin`
        -v.x * sin + v.y * cos   // Changed from `v.x * sin + v.y * cos`
    );
}
}