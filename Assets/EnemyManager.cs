using UnityEngine;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    [Header("Spawning")]
    public GameObject[] enemyPrefabs;
    public Transform player;
    public LayerMask treeLayer;
    
    [Header("Spawn Rules")]
    public int tilesPerSpawnCheck = 5;
    [Range(0f, 1f)] public float spawnChance = 0.4f;
    public int minSpawnDistance = 8;
    public int maxSpawnDistance = 15;
    public int maxEnemies = 5;
    
    [Header("Position Settings")]
    public float enemyYPosition = 0.5f;

    private Vector2Int lastPlayerTile;
    private int tilesMovedSinceLastCheck = 0;
    private List<EnemyAI> activeEnemies = new List<EnemyAI>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (player != null)
        {
            lastPlayerTile = GetPlayerTile();
        }
    }

    void Update()
    {
        if (player == null) return;

        Vector2Int currentTile = GetPlayerTile();

        if (currentTile != lastPlayerTile)
        {
            lastPlayerTile = currentTile;
            tilesMovedSinceLastCheck++;

            MoveAllEnemies();

            if (tilesMovedSinceLastCheck >= tilesPerSpawnCheck)
            {
                tilesMovedSinceLastCheck = 0;
                TrySpawnEnemy();
            }
        }

        activeEnemies.RemoveAll(e => e == null);
    }

    public Vector2Int GetPlayerTile()
    {
        return new Vector2Int(
            Mathf.RoundToInt(player.position.x),
            Mathf.RoundToInt(player.position.z)
        );
    }

    public bool IsPlayerTile(Vector3 target)
    {
        Vector2Int targetTile = new Vector2Int(
            Mathf.RoundToInt(target.x),
            Mathf.RoundToInt(target.z)
        );
        return targetTile == GetPlayerTile();
    }

    public bool IsEnemyTile(Vector3 target, EnemyAI requester)
    {
        Vector2Int targetTile = new Vector2Int(
            Mathf.RoundToInt(target.x),
            Mathf.RoundToInt(target.z)
        );

        foreach (EnemyAI enemy in activeEnemies)
        {
            if (enemy == null || enemy == requester) continue;

            if (enemy.GetTilePosition() == targetTile) return true;
            if (enemy.GetTargetTile() == targetTile) return true;
        }
        return false;
    }

    public EnemyAI GetAdjacentEnemy(Vector3 fromPosition)
    {
        Vector2Int fromTile = new Vector2Int(
            Mathf.RoundToInt(fromPosition.x),
            Mathf.RoundToInt(fromPosition.z)
        );

        foreach (EnemyAI enemy in activeEnemies)
        {
            if (enemy == null) continue;
            
            Vector2Int enemyTile = enemy.GetTilePosition();
            int dx = Mathf.Abs(enemyTile.x - fromTile.x);
            int dz = Mathf.Abs(enemyTile.y - fromTile.y);

            // Adjacent in 8 directions (including diagonals)
            if (dx <= 1 && dz <= 1 && (dx + dz) > 0)
            {
                return enemy;
            }
        }
        return null;
    }

    void MoveAllEnemies()
    {
        // Sort by distance to player (closer ones move first to prevent jamming)
        activeEnemies.Sort((a, b) =>
        {
            if (a == null) return 1;
            if (b == null) return -1;
            float distA = Vector3.Distance(a.transform.position, player.position);
            float distB = Vector3.Distance(b.transform.position, player.position);
            return distA.CompareTo(distB);
        });

        foreach (EnemyAI enemy in activeEnemies)
        {
            if (enemy != null)
                enemy.TakeStep();
        }
    }

    void TrySpawnEnemy()
    {
        if (activeEnemies.Count >= maxEnemies) return;
        if (Random.value > spawnChance) return;
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;

        for (int attempt = 0; attempt < 20; attempt++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            int distance = Random.Range(minSpawnDistance, maxSpawnDistance + 1);
            
            int xOffset = Mathf.RoundToInt(Mathf.Cos(angle) * distance);
            int zOffset = Mathf.RoundToInt(Mathf.Sin(angle) * distance);
            
            Vector2Int playerTile = GetPlayerTile();
            Vector2Int spawnTile = playerTile + new Vector2Int(xOffset, zOffset);
            Vector3 spawnPos = new Vector3(spawnTile.x, enemyYPosition, spawnTile.y);

            if (IsTileBlocked(spawnPos)) continue;
            if (IsEnemyTile(spawnPos, null)) continue;

            GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            if (prefab == null) continue;

            GameObject enemyObj = Instantiate(prefab, spawnPos, Quaternion.identity);
            
            EnemyAI enemyAI = enemyObj.GetComponent<EnemyAI>();
            if (enemyAI != null)
            {
                enemyAI.player = player;
                enemyAI.treeLayer = treeLayer;
                enemyAI.yPosition = enemyYPosition;
                activeEnemies.Add(enemyAI);
                Debug.Log($"✅ Spawned at {spawnTile}, dist {distance}, total: {activeEnemies.Count}");
            }
            else
            {
                Debug.LogError($"❌ Spawned prefab {prefab.name} has NO EnemyAI component!");
            }
            return;
        }
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
    public List<EnemyAI> GetAllAdjacentEnemies(Vector3 fromPosition)
{
    List<EnemyAI> result = new List<EnemyAI>();
    Vector2Int fromTile = new Vector2Int(
        Mathf.RoundToInt(fromPosition.x),
        Mathf.RoundToInt(fromPosition.z)
    );

    foreach (EnemyAI enemy in activeEnemies)
    {
        if (enemy == null) continue;
        
        Vector2Int enemyTile = enemy.GetTilePosition();
        int dx = Mathf.Abs(enemyTile.x - fromTile.x);
        int dz = Mathf.Abs(enemyTile.y - fromTile.y);

        if (dx <= 1 && dz <= 1 && (dx + dz) > 0)
        {
            result.Add(enemy);
        }
    }
    return result;
}
}