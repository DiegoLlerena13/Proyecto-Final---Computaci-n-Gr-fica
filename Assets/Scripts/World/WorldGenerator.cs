using System.Collections.Generic;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    public float chunkSize = 20f;
    public int viewDistance = 3;

    private Transform player;
    private BiomeManager biomeManager;
    private Dictionary<Vector2Int, WorldChunk> activeChunks = new();
    private Vector2Int lastPlayerChunk = new Vector2Int(int.MinValue, int.MinValue);

    private void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        biomeManager = GetComponent<BiomeManager>();
        if (biomeManager == null)
            biomeManager = gameObject.AddComponent<BiomeManager>();

        UpdateChunks();
    }

    private void Update()
    {
        if (player == null) return;

        Vector2Int currentChunk = WorldToChunk(player.position);
        if (currentChunk != lastPlayerChunk)
        {
            lastPlayerChunk = currentChunk;
            UpdateChunks();
        }
    }

    private void UpdateChunks()
    {
        HashSet<Vector2Int> needed = new();

        for (int x = -viewDistance; x <= viewDistance; x++)
        {
            for (int z = -viewDistance; z <= viewDistance; z++)
            {
                needed.Add(lastPlayerChunk + new Vector2Int(x, z));
            }
        }

        List<Vector2Int> toRemove = new();
        foreach (var kvp in activeChunks)
        {
            if (!needed.Contains(kvp.Key))
            {
                kvp.Value.Clear();
                Destroy(kvp.Value.gameObject);
                toRemove.Add(kvp.Key);
            }
        }
        foreach (var key in toRemove)
            activeChunks.Remove(key);

        foreach (var coord in needed)
        {
            if (!activeChunks.ContainsKey(coord))
                CreateChunk(coord);
        }
    }

    private void CreateChunk(Vector2Int coord)
    {
        Vector3 worldPos = new Vector3(coord.x * chunkSize, 0f, coord.y * chunkSize);

        var go = new GameObject($"Chunk_{coord.x}_{coord.y}");
        go.transform.SetParent(transform);
        go.transform.position = worldPos;

        var chunk = go.AddComponent<WorldChunk>();
        BiomeType biome = biomeManager.GetBiomeAt(worldPos.x, worldPos.z);
        bool isWater = biomeManager.IsWater(worldPos.x, worldPos.z);
        chunk.Generate(coord, chunkSize, biome, isWater);

        activeChunks[coord] = chunk;
    }

    private Vector2Int WorldToChunk(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPos.x / chunkSize),
            Mathf.RoundToInt(worldPos.z / chunkSize));
    }
}
