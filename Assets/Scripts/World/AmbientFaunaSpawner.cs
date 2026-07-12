using System.Collections.Generic;
using UnityEngine;

public static class AmbientFaunaSpawner
{
    private static readonly string[] Species =
    {
        "BirdFly", "Conejo", "Cuervo", "Paloma", "PerroLadrando", "Pinguino",
        "SerpienteAzul", "SerpienteBlanca", "SerpienteCorn", "SerpienteMarron",
        "SerpienteRoja", "SerpienteVerde", "YellowBird"
    };

    private static readonly Dictionary<string, GameObject> prefabCache = new();

    public static void SpawnFauna(Transform parent, Vector2Int coords, float chunkSize)
    {
        int seed = (coords.x * 73856093 ^ coords.y * 19349663) ^ 0x5F5F5F5F;
        var rng = new System.Random(seed);

        int count = rng.Next(0, 3);
        float half = chunkSize * 0.45f;

        for (int i = 0; i < count; i++)
        {
            string species = Species[rng.Next(Species.Length)];
            var prefab = LoadFauna(species);
            if (prefab == null) continue;

            float x = (float)(rng.NextDouble() * 2 - 1) * half;
            float z = (float)(rng.NextDouble() * 2 - 1) * half;

            var instance = Object.Instantiate(prefab, parent);
            instance.transform.localPosition = new Vector3(x, 0f, z);
            instance.transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
        }
    }

    private static GameObject LoadFauna(string name)
    {
        if (prefabCache.TryGetValue(name, out var cached))
            return cached;

        var prefab = Resources.Load<GameObject>($"Fauna/{name}");
        if (prefab == null)
            Debug.LogWarning($"[AmbientFaunaSpawner] Model not found: Fauna/{name}");

        prefabCache[name] = prefab;
        return prefab;
    }
}
