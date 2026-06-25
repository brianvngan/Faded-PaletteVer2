using UnityEngine;
using System.Collections.Generic;

public class CameraTransparency : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    
    [Header("Settings")]
    public LayerMask treeLayer; // Set to "Trees" layer
    [Range(0f, 1f)] public float transparencyAlpha = 0.3f;
    public float fadeSpeed = 5f;

    // Track which renderers are currently faded
    private Dictionary<Renderer, float> fadedRenderers = new Dictionary<Renderer, float>();
    private HashSet<Renderer> currentlyBlocking = new HashSet<Renderer>();

    void LateUpdate()
    {
        if (player == null) return;

        // Clear the "currently blocking" list
        currentlyBlocking.Clear();

        // Cast a ray from camera to player
        Vector3 direction = player.position - transform.position;
        float distance = direction.magnitude;
        
        // Get all things hit by the ray on the Trees layer
        RaycastHit[] hits = Physics.RaycastAll(transform.position, direction.normalized, distance, treeLayer);

        // For each tree hit, get all its sprite renderers and mark them as blocking
        foreach (RaycastHit hit in hits)
        {
            // Get all renderers in the hit object (children too, since sprites are usually children)
            Renderer[] renderers = hit.collider.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                currentlyBlocking.Add(r);
                
                // Add to faded dictionary if not already there
                if (!fadedRenderers.ContainsKey(r))
                    fadedRenderers[r] = 1f; // Start at full opacity
            }
        }

        // Update all tracked renderers
        List<Renderer> toRemove = new List<Renderer>();
        foreach (var pair in new Dictionary<Renderer, float>(fadedRenderers))
        {
            Renderer r = pair.Key;
            
            // Check if renderer was destroyed
            if (r == null)
            {
                toRemove.Add(r);
                continue;
            }

            float currentAlpha = pair.Value;
            float targetAlpha = currentlyBlocking.Contains(r) ? transparencyAlpha : 1f;

            // Smoothly fade toward target
            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
            fadedRenderers[r] = currentAlpha;

            // Apply alpha to material
            SetRendererAlpha(r, currentAlpha);

            // If back to full opacity and not blocking, stop tracking
            if (currentAlpha >= 0.99f && !currentlyBlocking.Contains(r))
                toRemove.Add(r);
        }

        // Clean up renderers no longer needed
        foreach (Renderer r in toRemove)
            fadedRenderers.Remove(r);
    }

    void SetRendererAlpha(Renderer r, float alpha)
    {
        if (r.material.HasProperty("_Color"))
        {
            Color c = r.material.color;
            c.a = alpha;
            r.material.color = c;
        }
    }
}