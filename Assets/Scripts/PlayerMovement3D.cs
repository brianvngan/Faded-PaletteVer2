using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement3D : MonoBehaviour
{
    public float moveSpeed = 8f;
    public float tileSize = 1f;
    
    [Header("Hold-to-Move Settings")]
    public float holdDelayBeforeRepeat = 0.3f; // Initial wait
    public float repeatRate = 0.15f; // Delay between tiles when held
    public float sustainHoldTime = 1.5f; // After holding this long, no more delays

    private Vector3 targetPosition;
    private bool isMoving = false;
    private Vector2 pendingMove = Vector2.zero;
    
    private float timeSinceLastMove = 0f;
    private float timeHoldingInput = 0f; // How long input has been held
    private bool justPressed = true;

    void Start()
    {
        targetPosition = transform.position;
    }

    void OnMove(InputValue value)
    {
        Vector2 newMove = value.Get<Vector2>();
        
        if (pendingMove == Vector2.zero && newMove != Vector2.zero)
        {
            justPressed = true;
            timeSinceLastMove = 0f;
            timeHoldingInput = 0f; // Reset hold timer
        }
        
        if (newMove == Vector2.zero)
        {
            justPressed = true;
            timeSinceLastMove = 0f;
            timeHoldingInput = 0f;
        }
        
        pendingMove = newMove;
    }

    bool IsTileBlocked(Vector3 target)
    {
        int treeLayer = LayerMask.GetMask("Trees");
        Collider[] hits = Physics.OverlapBox(
            target + Vector3.up * 0.5f,
            new Vector3(0.3f, 0.3f, 0.3f),
            Quaternion.identity,
            treeLayer
        );
        if (hits.Length > 0) return true;

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

        timeSinceLastMove += Time.deltaTime;
        
        // Track total hold time
        if (pendingMove != Vector2.zero)
            timeHoldingInput += Time.deltaTime;

        if (!isMoving && pendingMove != Vector2.zero)
        {
            bool canMove = false;
            
            if (justPressed)
            {
                canMove = true;
                justPressed = false;
                timeSinceLastMove = 0f;
            }
            else if (timeHoldingInput >= sustainHoldTime)
            {
                // Sustained hold - no delay, move every frame the player isn't already moving
                canMove = true;
                timeSinceLastMove = 0f;
            }
            else
            {
                // Normal repeat
                float delay = (timeSinceLastMove < holdDelayBeforeRepeat) ? holdDelayBeforeRepeat : repeatRate;
                if (timeSinceLastMove >= delay)
                {
                    canMove = true;
                    timeSinceLastMove = 0f;
                }
            }

            if (canMove)
            {
                Camera cam = Camera.main;
                float camY = Mathf.Round(cam.transform.eulerAngles.y / 90f) * 90f;
                
                Vector3 inputDir = Vector3.zero;
                if (pendingMove.x > 0.1f) inputDir.x = 1;
                else if (pendingMove.x < -0.1f) inputDir.x = -1;
                if (pendingMove.y > 0.1f) inputDir.z = 1;
                else if (pendingMove.y < -0.1f) inputDir.z = -1;

                Quaternion rotation = Quaternion.Euler(0, camY, 0);
                Vector3 direction = rotation * inputDir;
                
                direction.x = Mathf.Round(direction.x);
                direction.z = Mathf.Round(direction.z);

                Vector3 newTarget = transform.position + direction * tileSize;

                if (!IsTileBlocked(newTarget))
                {
                    targetPosition = newTarget;
                    isMoving = true;

                    if (CombatManager.Instance != null)
                        CombatManager.Instance.ClearCardCooldowns();
                }
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