using System.Collections.Generic;
using UnityEngine;

public class WorldChunk : MonoBehaviour
{
    private static Shader litShader;
    private static readonly Dictionary<string, GameObject> prefabCache = new();

    public void Generate(Vector2Int coords, float chunkSize, BiomeType biome, bool isWater)
    {
        if (litShader == null)
            litShader = Shader.Find("Universal Render Pipeline/Lit");

        if (isWater)
        {
            CreateWaterSurface(chunkSize);
            SpawnWaterDecorations(coords, chunkSize);
            return;
        }

        var biomeDef = BiomeConfig.Get(biome);
        CreateGround(chunkSize, biomeDef.groundColor);
        SpawnDecorations(coords, chunkSize, biomeDef);
        AmbientFaunaSpawner.SpawnFauna(transform, coords, chunkSize);
    }

    private void CreateGround(float size, Color color)
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.transform.SetParent(transform);
        ground.transform.localPosition = Vector3.zero;
        ground.transform.localScale = new Vector3(size / 10f, 1f, size / 10f);

        var mat = new Material(litShader);
        mat.SetColor("_BaseColor", color);
        ground.GetComponent<Renderer>().sharedMaterial = mat;
        ground.layer = gameObject.layer;
    }

    private void CreateWaterSurface(float size)
    {
        var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
        water.transform.SetParent(transform);
        water.transform.localPosition = new Vector3(0f, -0.3f, 0f);
        water.transform.localScale = new Vector3(size / 10f, 1f, size / 10f);

        var mat = new Material(litShader);
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3000;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.SetColor("_BaseColor", new Color(0.15f, 0.4f, 0.7f, 0.6f));
        water.GetComponent<Renderer>().sharedMaterial = mat;
        water.layer = gameObject.layer;
    }

    private void SpawnWaterDecorations(Vector2Int coords, float chunkSize)
    {
        int seed = coords.x * 73856093 ^ coords.y * 19349663;
        var rng = new System.Random(seed);
        int count = rng.Next(1, 4);
        float half = chunkSize * 0.4f;

        for (int i = 0; i < count; i++)
        {
            float x = (float)(rng.NextDouble() * 2 - 1) * half;
            float z = (float)(rng.NextDouble() * 2 - 1) * half;
            string model = rng.Next(2) == 0 ? "lily_large" : "lily_small";

            var prefab = LoadModel(model);
            if (prefab == null) continue;

            var instance = Instantiate(prefab, transform);
            float scale = Mathf.Lerp(0.8f, 1.3f, (float)rng.NextDouble());
            instance.transform.localPosition = new Vector3(x, -0.2f, z);
            instance.transform.localScale = Vector3.one * scale;
            instance.transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
        }
    }

    private void SpawnDecorations(Vector2Int coords, float chunkSize, BiomeConfig.BiomeDef biomeDef)
    {
        int seed = coords.x * 73856093 ^ coords.y * 19349663;
        var rng = new System.Random(seed);

        int count = rng.Next(biomeDef.minDecorations, biomeDef.maxDecorations + 1);
        float half = chunkSize * 0.45f;

        for (int i = 0; i < count; i++)
        {
            float x = (float)(rng.NextDouble() * 2 - 1) * half;
            float z = (float)(rng.NextDouble() * 2 - 1) * half;

            var def = biomeDef.decorations[rng.Next(biomeDef.decorations.Length)];
            string modelName = def.modelNames[rng.Next(def.modelNames.Length)];
            float scale = Mathf.Lerp(def.scaleMin, def.scaleMax, (float)rng.NextDouble());

            var prefab = LoadModel(modelName);
            if (prefab == null) continue;

            var instance = Instantiate(prefab, transform);
            instance.transform.localPosition = new Vector3(x, def.yOffset, z);
            instance.transform.localScale = Vector3.one * scale;
            instance.transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
        }
    }

    private static GameObject LoadModel(string name)
    {
        if (prefabCache.TryGetValue(name, out var cached))
            return cached;

        var prefab = Resources.Load<GameObject>($"Decorations/{name}");
        if (prefab == null)
            Debug.LogWarning($"[WorldChunk] Model not found: Decorations/{name}");

        prefabCache[name] = prefab;
        return prefab;
    }

    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
    }
}
