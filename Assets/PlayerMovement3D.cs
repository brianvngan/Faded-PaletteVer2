using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement3D : MonoBehaviour
{
    public float moveSpeed = 8f;
    public float tileSize = 1f;

    private Vector3 targetPosition;
    private bool isMoving = false;
    private Vector2 pendingMove = Vector2.zero;

    void Start()
    {
        targetPosition = transform.position;
    }

    void OnMove(InputValue value)
    {
        pendingMove = value.Get<Vector2>();
    }

    bool IsTileBlocked(Vector3 target)
{
    // Check trees
    int treeLayer = LayerMask.GetMask("Trees");
    Collider[] hits = Physics.OverlapBox(
        target + Vector3.up * 0.5f,
        new Vector3(0.3f, 0.3f, 0.3f),
        Quaternion.identity,
        treeLayer
    );
    if (hits.Length > 0) return true;

    // Check enemies
    if (EnemyManager.Instance != null && EnemyManager.Instance.IsEnemyTile(target, null))
        return true;

    return false;
}

    void Update()
{
    if (CombatManager.Instance != null && CombatManager.Instance.playerDead)
        return;
    if (CombatManager.Instance != null && CombatManager.Instance.floorCleared)
        return;
    if (CombatManager.Instance != null && CombatManager.Instance.playerFrozenForOneTurn)
    {
        if (pendingMove != Vector2.zero)
        {
            CombatManager.Instance.playerFrozenForOneTurn = false;
            pendingMove = Vector2.zero;
        }
        return;
    }
    if (!isMoving && pendingMove != Vector2.zero)
    {
        // Get camera's Y rotation (rounded to nearest 90)
        Camera cam = Camera.main;
        float camY = Mathf.Round(cam.transform.eulerAngles.y / 90f) * 90f;
        
        // Get input direction
        Vector3 inputDir = Vector3.zero;
        if (pendingMove.x > 0.1f) inputDir.x = 1;
        else if (pendingMove.x < -0.1f) inputDir.x = -1;
        if (pendingMove.y > 0.1f) inputDir.z = 1;
        else if (pendingMove.y < -0.1f) inputDir.z = -1;

        // Rotate input by camera's Y rotation
        Quaternion rotation = Quaternion.Euler(0, camY, 0);
        Vector3 direction = rotation * inputDir;
        
        // Round to nearest tile direction (no diagonal weirdness)
        direction.x = Mathf.Round(direction.x);
        direction.z = Mathf.Round(direction.z);

        Vector3 newTarget = transform.position + direction * tileSize;

        if (!IsTileBlocked(newTarget))
        {
            targetPosition = newTarget;
            isMoving = true;
        }
    }

    transform.position = Vector3.MoveTowards(
        transform.position,
        targetPosition,
        moveSpeed * Time.deltaTime
    );

    if (transform.position == targetPosition)
        isMoving = false;
}
}