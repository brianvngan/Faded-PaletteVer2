using UnityEngine;

public class PlayerSpriteController : MonoBehaviour
{
    [Header("References")]
    public SpriteRenderer spriteRenderer;
    
    [Header("HP Sprites")]
    public Sprite normalSprite;        // CharacterSpriteSheet_0
    public Sprite lowHPSprite;         // CharacterSpriteSheet_3
    
    [Header("Settings")]
    [Range(0f, 1f)] public float lowHPThreshold = 0.4f; // 40% HP

    private bool isLowHP = false;

    void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Set initial sprite
        if (normalSprite != null && spriteRenderer != null)
            spriteRenderer.sprite = normalSprite;
    }

    void Update()
    {
        if (CombatManager.Instance == null) return;
        if (spriteRenderer == null) return;

        float hpPercent = (float)CombatManager.Instance.playerCurrentHP / CombatManager.Instance.playerMaxHP;
        bool shouldBeLowHP = hpPercent <= lowHPThreshold;

        // Only change sprite when state changes (not every frame)
        if (shouldBeLowHP && !isLowHP)
        {
            spriteRenderer.sprite = lowHPSprite;
            isLowHP = true;
            Debug.Log("HP low — switched to wounded sprite");
        }
        else if (!shouldBeLowHP && isLowHP)
        {
            spriteRenderer.sprite = normalSprite;
            isLowHP = false;
            Debug.Log("HP recovered — switched back to normal sprite");
        }
    }
}