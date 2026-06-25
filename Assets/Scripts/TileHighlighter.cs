using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TileHighlighter : MonoBehaviour
{
    public static TileHighlighter Instance { get; private set; }

    [Header("Settings")]
    public Color attackColor = Color.red;
    public Color healColor = Color.green;
    public float flashDuration = 0.4f;

    // Track original colors per renderer so we always restore correctly
    private Dictionary<Renderer, Color> originalColors = new Dictionary<Renderer, Color>();
    
    // Track active coroutines so we can stop them when re-flashing
    private Dictionary<Renderer, Coroutine> activeFlashes = new Dictionary<Renderer, Coroutine>();

    void Awake()
    {
        Instance = this;
    }

    public void FlashTileAt(Vector3 worldPosition, Color color)
    {
        Collider[] hits = Physics.OverlapBox(
            new Vector3(worldPosition.x, 0, worldPosition.z),
            new Vector3(0.4f, 1f, 0.4f),
            Quaternion.identity
        );

        foreach (Collider hit in hits)
        {
            Renderer rend = hit.GetComponent<Renderer>();
            if (rend == null) rend = hit.GetComponentInChildren<Renderer>();
            
            if (rend != null)
            {
                // Save original color the FIRST time we see this renderer
                if (!originalColors.ContainsKey(rend))
                {
                    originalColors[rend] = rend.material.color;
                }

                // Stop any flash already in progress on this renderer
                if (activeFlashes.ContainsKey(rend) && activeFlashes[rend] != null)
                {
                    StopCoroutine(activeFlashes[rend]);
                }

                // Start new flash and track it
                activeFlashes[rend] = StartCoroutine(FlashRenderer(rend, color));
            }
        }
    }

    IEnumerator FlashRenderer(Renderer rend, Color flashColor)
    {
        if (rend == null) yield break;

        Color originalColor = originalColors[rend];
        
        // Set to flash color
        rend.material.color = flashColor;

        float elapsed = 0;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flashDuration;
            
            if (rend != null)
                rend.material.color = Color.Lerp(flashColor, originalColor, t);
            
            yield return null;
        }

        if (rend != null)
            rend.material.color = originalColor;

        // Clear the active flash tracker
        activeFlashes.Remove(rend);
    }
}