using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    
    [Header("Settings")]
    public float moveSpeed = 8f;
    public float visibilityDistance = 5f;
    public LayerMask treeLayer;
    public float yPosition = 0.5f;

    [Header("Combat Stats")]
    public int maxHP = 5;
    public int currentHP = 5;
    public int attackDamage = 1;

    private Vector3 targetPosition;
    private bool isMoving = false;
    private Renderer[] renderers;

    void Start()
    {
        Vector3 pos = transform.position;
        pos.y = yPosition;
        transform.position = pos;
        targetPosition = transform.position;
        
        renderers = GetComponentsInChildren<Renderer>();
        currentHP = maxHP;
    }

    void Update()
    {
        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );

            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                isMoving = false;
            }
        }

        UpdateVisibility();
    }

    void UpdateVisibility()
    {
        if (player == null || renderers == null) return;

        float distance = Vector3.Distance(
            new Vector3(transform.position.x, 0, transform.position.z),
            new Vector3(player.position.x, 0, player.position.z)
        );
        bool shouldBeVisible = distance <= visibilityDistance;

        foreach (Renderer r in renderers)
        {
            if (r != null)
                r.enabled = shouldBeVisible;
        }
    }

    public Vector2Int GetTilePosition()
    {
        return new Vector2Int(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.z)
        );
    }

    public Vector2Int GetTargetTile()
    {
        return new Vector2Int(
            Mathf.RoundToInt(targetPosition.x),
            Mathf.RoundToInt(targetPosition.z)
        );
    }

    public void TakeDamage(int amount)
    {
        currentHP = Mathf.Max(0, currentHP - amount);
        Debug.Log($"Enemy took {amount} damage, HP: {currentHP}/{maxHP}");
    }

    public void TakeStep()
    {
        if (isMoving) return;
        if (player == null) return;

        float dx = player.position.x - transform.position.x;
        float dz = player.position.z - transform.position.z;

        // If adjacent to player (8 directions), ATTACK instead of moving
        if (Mathf.Abs(dx) <= 1.1f && Mathf.Abs(dz) <= 1.1f && 
            (Mathf.Abs(dx) > 0.1f || Mathf.Abs(dz) > 0.1f))
        {
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.TakeDamageFromEnemy(attackDamage);
                Debug.Log($"Enemy attacked player for {attackDamage} damage!");
            }
            return;
        }

        Vector3 direction = Vector3.zero;

        // Pick the larger axis
        if (Mathf.Abs(dx) > Mathf.Abs(dz))
        {
            direction.x = dx > 0 ? 1 : -1;
        }
        else if (Mathf.Abs(dz) > 0.1f)
        {
            direction.z = dz > 0 ? 1 : -1;
        }
        else if (Mathf.Abs(dx) > 0.1f)
        {
            direction.x = dx > 0 ? 1 : -1;
        }

        Vector3 newTarget = transform.position + direction;
        newTarget.y = yPosition;

        // Try primary direction
        if (CanMoveTo(newTarget))
        {
            targetPosition = newTarget;
            isMoving = true;
            return;
        }

        // Try alternate axis
        Vector3 altDirection = Vector3.zero;
        if (Mathf.Abs(dx) > Mathf.Abs(dz))
        {
            altDirection.z = dz > 0 ? 1 : -1;
        }
        else
        {
            altDirection.x = dx > 0 ? 1 : -1;
        }

        Vector3 altTarget = transform.position + altDirection;
        altTarget.y = yPosition;

        if (CanMoveTo(altTarget))
        {
            targetPosition = altTarget;
            isMoving = true;
        }
    }

    bool CanMoveTo(Vector3 target)
    {
        if (IsTileBlocked(target)) return false;

        if (EnemyManager.Instance != null && EnemyManager.Instance.IsPlayerTile(target))
            return false;

        if (EnemyManager.Instance != null && EnemyManager.Instance.IsEnemyTile(target, this))
            return false;

        return true;
    }

    bool IsTileBlocked(Vector3 target)
    {
        Collider[] hits = Physics.OverlapBox(
            new Vector3(target.x, 0.5f, target.z),
            new Vector3(0.3f, 0.3f, 0.3f),
            Quaternion.identity,
            treeLayer
        );
        return hits.Length > 0;
    }
}