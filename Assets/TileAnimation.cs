using UnityEngine;
using System.Collections;

public class TileAnimation : MonoBehaviour
{
    public float animDuration = 0.4f;
    private Vector3 fullScale;
    private bool isRevealed = false;
    private bool isDying = false;
    private GameObject visualHolder;

    void Awake()
    {
        fullScale = transform.localScale;
        // Hide everything by scaling to 0
        transform.localScale = Vector3.zero;
    }

    public void Reveal()
    {
        if (!isRevealed)
        {
            isRevealed = true;
            StartCoroutine(GrowIn());
        }
    }

    IEnumerator GrowIn()
    {
        transform.localScale = Vector3.zero; // Make sure starts at 0
        float elapsed = 0;
        while (elapsed < animDuration)
        {
            float t = elapsed / animDuration;
            t = 1f - Mathf.Pow(1f - t, 3); // ease out
            transform.localScale = fullScale * t;
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = fullScale;
    }

    public void DisappearAndDestroy()
    {
        if (!isDying)
        {
            isDying = true;
            StartCoroutine(ShrinkOut());
        }
    }

    IEnumerator ShrinkOut()
    {
        float elapsed = 0;
        Vector3 startScale = transform.localScale;
        while (elapsed < animDuration)
        {
            float t = 1f - (elapsed / animDuration);
            transform.localScale = startScale * t;
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }
}