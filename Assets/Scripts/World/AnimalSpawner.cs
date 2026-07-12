using System.Collections.Generic;
using UnityEngine;

public static class AnimalSpawner
{
    private static readonly AnimalSpecies[] AllSpecies =
    {
        AnimalSpecies.Chicken, AnimalSpecies.Deer, AnimalSpecies.Dog, AnimalSpecies.Horse,
        AnimalSpecies.Kitty, AnimalSpecies.Pinguin, AnimalSpecies.Tiger
    };

    private static readonly Dictionary<AnimalSpecies, GameObject> prefabCache = new();

    public static void TrySpawnAnimal(Transform parent, Vector2Int coords, float chunkSize, BiomeConfig.BiomeDef biomeDef)
    {
        int seed = (coords.x * 73856093 ^ coords.y * 19349663) ^ 0xA1A1A1;
        var rng = new System.Random(seed);

        if (rng.NextDouble() > biomeDef.animalSpawnChance)
            return;

        var species = AllSpecies[rng.Next(AllSpecies.Length)];
        var prefab = LoadAnimal(species);
        if (prefab == null) return;

        float half = chunkSize * 0.3f;
        float x = (float)(rng.NextDouble() * 2 - 1) * half;
        float z = (float)(rng.NextDouble() * 2 - 1) * half;

        var instance = Object.Instantiate(prefab, parent);
        instance.transform.localPosition = new Vector3(x, 0f, z);
        instance.transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);

        var animalInstance = instance.GetComponent<AnimalInstance>();
        if (animalInstance == null)
            animalInstance = instance.AddComponent<AnimalInstance>();
        animalInstance.Initialize(species);
    }

    private static GameObject LoadAnimal(AnimalSpecies species)
    {
        if (prefabCache.TryGetValue(species, out var cached))
            return cached;

        var prefab = AnimalResources.Load(species);
        if (prefab == null)
            Debug.LogWarning($"[AnimalSpawner] Model not found for {species}");

        prefabCache[species] = prefab;
        return prefab;
    }
}
