using UnityEngine;
using System.Collections.Generic;

public class GridSpawner : MonoBehaviour
{
    [Header("Tile Prefabs")]
    public GameObject tilePrefab;
    public GameObject tileChunk1Prefab;
    public GameObject tileChunk2Prefab;
    public GameObject tileChunk3Prefab;
    public GameObject tileChunk4Prefab;
    public GameObject tileChunk5Prefab;
    public GameObject clearTilePrefab;

    [Header("Spawn Chances (0-1)")]
    [Range(0f, 1f)] public float chunk1Chance = 0.15f;
    [Range(0f, 1f)] public float chunk2Chance = 0.15f;
    [Range(0f, 1f)] public float chunk3Chance = 0.10f;
    [Range(0f, 1f)] public float chunk4Chance = 0.10f;
    [Range(0f, 1f)] public float chunk5Chance = 0.10f;

    [Header("Clear Tile Settings")]
    public int clearTileMinDistance = 15;
    public int clearTileMaxDistance = 25;

    [Header("Spawning Settings")]
    public Transform player;
    public int spawnRadius = 3;

    private Dictionary<Vector2Int, GameObject> activeTiles = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, int> tileMemory = new Dictionary<Vector2Int, int>();
    private Vector2Int lastPlayerTile;
    private Vector2Int clearTilePosition;

    // Tile type IDs
    private const int TILE_FLOOR = 0;
    private const int TILE_CHUNK1 = 1;
    private const int TILE_CHUNK2 = 2;
    private const int TILE_CHUNK3 = 3;
    private const int TILE_CHUNK4 = 4;
    private const int TILE_CHUNK5 = 5;
    private const int TILE_CLEAR = 6;

    void Start()
    {
        // Pick a random position for the clear tile
        PickClearTilePosition();
        UpdateTiles();
    }

    void PickClearTilePosition()
    {
        // Random angle and distance from player start
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        int distance = Random.Range(clearTileMinDistance, clearTileMaxDistance + 1);
        
        int xOffset = Mathf.RoundToInt(Mathf.Cos(angle) * distance);
        int zOffset = Mathf.RoundToInt(Mathf.Sin(angle) * distance);
        
        Vector2Int playerStart = new Vector2Int(
            Mathf.RoundToInt(player.position.x),
            Mathf.RoundToInt(player.position.z)
        );
        
        clearTilePosition = playerStart + new Vector2Int(xOffset, zOffset);
        
        // Lock this position in memory as the clear tile
        tileMemory[clearTilePosition] = TILE_CLEAR;
        
        //Debug.Log($"Clear tile placed at {clearTilePosition}, distance {distance}");
    }

    void Update()
    {
        Vector2Int currentTile = new Vector2Int(
            Mathf.RoundToInt(player.position.x),
            Mathf.RoundToInt(player.position.z)
        );

        if (currentTile != lastPlayerTile)
        {
            lastPlayerTile = currentTile;
            UpdateTiles();
        }
    }

    int DecideTileType(Vector2Int tilePos)
    {
        // Return remembered type if exists (this also locks the clear tile in place)
        if (tileMemory.ContainsKey(tilePos))
            return tileMemory[tilePos];

        // Roll for new tile
        float roll = Random.value;
        int tileType = TILE_FLOOR;

        float cumulative = 0f;
        cumulative += chunk1Chance;
        if (roll < cumulative) tileType = TILE_CHUNK1;
        else
        {
            cumulative += chunk2Chance;
            if (roll < cumulative) tileType = TILE_CHUNK2;
            else
            {
                cumulative += chunk3Chance;
                if (roll < cumulative) tileType = TILE_CHUNK3;
                else
                {
                    cumulative += chunk4Chance;
                    if (roll < cumulative) tileType = TILE_CHUNK4;
                    else
                    {
                        cumulative += chunk5Chance;
                        if (roll < cumulative) tileType = TILE_CHUNK5;
                    }
                }
            }
        }

        tileMemory[tilePos] = tileType;
        return tileType;
    }

    GameObject GetPrefabForType(int tileType)
    {
        switch (tileType)
        {
            case TILE_CHUNK1: return tileChunk1Prefab != null ? tileChunk1Prefab : tilePrefab;
            case TILE_CHUNK2: return tileChunk2Prefab != null ? tileChunk2Prefab : tilePrefab;
            case TILE_CHUNK3: return tileChunk3Prefab != null ? tileChunk3Prefab : tilePrefab;
            case TILE_CHUNK4: return tileChunk4Prefab != null ? tileChunk4Prefab : tilePrefab;
            case TILE_CHUNK5: return tileChunk5Prefab != null ? tileChunk5Prefab : tilePrefab;
            case TILE_CLEAR: return clearTilePrefab != null ? clearTilePrefab : tilePrefab;
            case TILE_FLOOR:
            default: return tilePrefab;
        }
    }

    void UpdateTiles()
    {
        Vector2Int playerTile = new Vector2Int(
            Mathf.RoundToInt(player.position.x),
            Mathf.RoundToInt(player.position.z)
        );

        // Spawn missing tiles around player
        for (int x = -spawnRadius; x <= spawnRadius; x++)
        {
            for (int z = -spawnRadius; z <= spawnRadius; z++)
            {
                Vector2Int tilePos = playerTile + new Vector2Int(x, z);
                if (!activeTiles.ContainsKey(tilePos))
                {
                    int tileType = DecideTileType(tilePos);
                    GameObject prefabToSpawn = GetPrefabForType(tileType);

                    Vector3 worldPos = new Vector3(tilePos.x, 0, tilePos.y);
                    GameObject tile = Instantiate(prefabToSpawn, worldPos, Quaternion.identity, transform);
                    tile.name = $"Tile_{tilePos.x}_{tilePos.y}";
                    activeTiles[tilePos] = tile;

                    TileAnimation anim = tile.GetComponent<TileAnimation>();
                    if (anim != null) anim.Reveal();
                }
            }
        }

        // Find tiles too far from player
        List<Vector2Int> toRemove = new List<Vector2Int>();
        foreach (var pair in activeTiles)
        {
            float distance = Mathf.Max(
                Mathf.Abs(pair.Key.x - playerTile.x),
                Mathf.Abs(pair.Key.y - playerTile.y)
            );
            if (distance > spawnRadius)
                toRemove.Add(pair.Key);
        }

        foreach (var pos in toRemove)
        {
            if (activeTiles[pos] != null)
            {
                TileAnimation anim = activeTiles[pos].GetComponent<TileAnimation>();
                if (anim != null)
                    anim.DisappearAndDestroy();
                else
                    Destroy(activeTiles[pos]);
            }
            activeTiles.Remove(pos);
        }
    }

    public Vector2 GetClearTilePosition()
    {
        return new Vector2(clearTilePosition.x, clearTilePosition.y);
    }
    public bool IsPlayerOnClearTile(Vector3 playerPosition)
{
    Vector2Int playerTile = new Vector2Int(
        Mathf.RoundToInt(playerPosition.x),
        Mathf.RoundToInt(playerPosition.z)
    );
    return playerTile == clearTilePosition;
}
}