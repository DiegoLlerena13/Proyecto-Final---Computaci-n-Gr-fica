using UnityEngine;

public class BiomeManager : MonoBehaviour
{
    public float noiseScale = 0.01f;
    public int seed = 42;
    public float waterThreshold = 0.72f;
    public float waterNoiseScale = 0.008f;

    private float seedOffsetX;
    private float seedOffsetZ;

    private void Awake()
    {
        var rng = new System.Random(seed);
        seedOffsetX = (float)rng.NextDouble() * 10000f;
        seedOffsetZ = (float)rng.NextDouble() * 10000f;
    }

    public BiomeType GetBiomeAt(float worldX, float worldZ)
    {
        float temperature = Mathf.PerlinNoise(
            (worldX + seedOffsetX) * noiseScale,
            (worldZ + seedOffsetZ) * noiseScale);

        float moisture = Mathf.PerlinNoise(
            (worldX + seedOffsetX + 5000f) * noiseScale * 1.3f,
            (worldZ + seedOffsetZ + 5000f) * noiseScale * 1.3f);

        bool hot = temperature > 0.6f;
        bool cold = temperature < 0.35f;
        bool wet = moisture > 0.55f;
        bool dry = moisture < 0.4f;

        if (hot && dry) return BiomeType.Desert;
        if (hot && wet) return BiomeType.Tropical;
        if (cold && dry) return BiomeType.Pine;
        if (cold && wet) return BiomeType.Swamp;
        if (dry) return BiomeType.Autumn;
        if (wet) return BiomeType.Forest;
        return BiomeType.Plains;
    }

    public bool IsWater(float worldX, float worldZ)
    {
        float water = Mathf.PerlinNoise(
            (worldX + seedOffsetX + 9000f) * waterNoiseScale,
            (worldZ + seedOffsetZ + 9000f) * waterNoiseScale);
        return water > waterThreshold;
    }
}
