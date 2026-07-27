using UnityEngine;

public static class BiomeConfig
{
    public struct DecorationDef
    {
        public string[] modelNames;
        public float scaleMin;
        public float scaleMax;
        public float yOffset;
    }

    public struct BiomeDef
    {
        public Color groundColor;
        public DecorationDef[] decorations;
        public int minDecorations;
        public int maxDecorations;
        public float animalSpawnChance;
    }

    public static BiomeDef Get(BiomeType biome)
    {
        return biome switch
        {
            BiomeType.Forest => new BiomeDef
            {
                groundColor = new Color(0.35f, 0.55f, 0.25f),
                minDecorations = 8, maxDecorations = 14, animalSpawnChance = 0.3f,
                decorations = new[]
                {
                    new DecorationDef { modelNames = new[] { "tree_default", "tree_oak", "tree_detailed" }, scaleMin = 1.6f, scaleMax = 2.6f },
                    new DecorationDef { modelNames = new[] { "plant_bush", "plant_bushLarge" }, scaleMin = 0.8f, scaleMax = 1.2f },
                    new DecorationDef { modelNames = new[] { "flower_redA", "flower_yellowA" }, scaleMin = 0.6f, scaleMax = 1f },
                    new DecorationDef { modelNames = new[] { "grass", "grass_large" }, scaleMin = 0.7f, scaleMax = 1.2f },
                    new DecorationDef { modelNames = new[] { "log", "rock_smallA" }, scaleMin = 0.8f, scaleMax = 1.1f },
                }
            },
            BiomeType.Pine => new BiomeDef
            {
                groundColor = new Color(0.2f, 0.4f, 0.2f),
                minDecorations = 10, maxDecorations = 16, animalSpawnChance = 0.2f,
                decorations = new[]
                {
                    new DecorationDef { modelNames = new[] { "tree_pineDefaultA", "tree_pineDefaultB", "tree_pineTallA", "tree_pineRoundA" }, scaleMin = 1.8f, scaleMax = 3.0f },
                    new DecorationDef { modelNames = new[] { "rock_smallA", "rock_smallB" }, scaleMin = 0.8f, scaleMax = 1.3f },
                    new DecorationDef { modelNames = new[] { "mushroom_red", "mushroom_tan" }, scaleMin = 0.7f, scaleMax = 1.1f },
                    new DecorationDef { modelNames = new[] { "grass", "grass_large" }, scaleMin = 0.6f, scaleMax = 1f },
                }
            },
            BiomeType.Desert => new BiomeDef
            {
                groundColor = new Color(0.76f, 0.70f, 0.50f),
                minDecorations = 3, maxDecorations = 6, animalSpawnChance = 0.1f,
                decorations = new[]
                {
                    new DecorationDef { modelNames = new[] { "cactus_short", "cactus_tall" }, scaleMin = 0.8f, scaleMax = 1.4f },
                    new DecorationDef { modelNames = new[] { "rock_tallA", "rock_tallB", "rock_largeA" }, scaleMin = 0.8f, scaleMax = 1.5f },
                    new DecorationDef { modelNames = new[] { "stone_smallA", "stone_smallB" }, scaleMin = 0.7f, scaleMax = 1.2f },
                }
            },
            BiomeType.Tropical => new BiomeDef
            {
                groundColor = new Color(0.3f, 0.65f, 0.2f),
                minDecorations = 8, maxDecorations = 14, animalSpawnChance = 0.35f,
                decorations = new[]
                {
                    new DecorationDef { modelNames = new[] { "tree_palm", "tree_palmBend", "tree_palmTall" }, scaleMin = 1.8f, scaleMax = 2.8f },
                    new DecorationDef { modelNames = new[] { "flower_purpleA", "flower_purpleB", "flower_yellowB" }, scaleMin = 0.6f, scaleMax = 1f },
                    new DecorationDef { modelNames = new[] { "plant_bushDetailed", "plant_bushLarge" }, scaleMin = 0.8f, scaleMax = 1.2f },
                    new DecorationDef { modelNames = new[] { "grass_leafs", "grass_large" }, scaleMin = 0.7f, scaleMax = 1.1f },
                    new DecorationDef { modelNames = new[] { "lily_large" }, scaleMin = 0.8f, scaleMax = 1.2f },
                }
            },
            BiomeType.Autumn => new BiomeDef
            {
                groundColor = new Color(0.55f, 0.40f, 0.25f),
                minDecorations = 8, maxDecorations = 13, animalSpawnChance = 0.25f,
                decorations = new[]
                {
                    new DecorationDef { modelNames = new[] { "tree_default_fall", "tree_detailed_fall", "tree_oak_fall", "tree_simple_fall" }, scaleMin = 1.6f, scaleMax = 2.6f },
                    new DecorationDef { modelNames = new[] { "stump_round", "stump_old" }, scaleMin = 0.8f, scaleMax = 1.2f },
                    new DecorationDef { modelNames = new[] { "mushroom_red", "mushroom_redGroup" }, scaleMin = 0.7f, scaleMax = 1.1f },
                    new DecorationDef { modelNames = new[] { "log", "log_large" }, scaleMin = 0.8f, scaleMax = 1.1f },
                }
            },
            BiomeType.Swamp => new BiomeDef
            {
                groundColor = new Color(0.25f, 0.30f, 0.20f),
                minDecorations = 7, maxDecorations = 12, animalSpawnChance = 0.15f,
                decorations = new[]
                {
                    new DecorationDef { modelNames = new[] { "tree_default_dark", "tree_detailed_dark" }, scaleMin = 1.6f, scaleMax = 2.8f },
                    new DecorationDef { modelNames = new[] { "stump_oldTall", "stump_old" }, scaleMin = 0.8f, scaleMax = 1.2f },
                    new DecorationDef { modelNames = new[] { "mushroom_tanGroup", "mushroom_tanTall" }, scaleMin = 0.7f, scaleMax = 1.1f },
                    new DecorationDef { modelNames = new[] { "grass_leafsLarge", "plant_flatShort" }, scaleMin = 0.7f, scaleMax = 1.1f },
                    new DecorationDef { modelNames = new[] { "lily_small", "lily_large" }, scaleMin = 0.8f, scaleMax = 1.2f },
                    new DecorationDef { modelNames = new[] { "log_stack" }, scaleMin = 0.8f, scaleMax = 1.1f },
                }
            },
            BiomeType.Plains => new BiomeDef
            {
                groundColor = new Color(0.45f, 0.65f, 0.30f),
                minDecorations = 5, maxDecorations = 10, animalSpawnChance = 0.4f,
                decorations = new[]
                {
                    new DecorationDef { modelNames = new[] { "grass", "grass_large", "grass_leafs" }, scaleMin = 0.6f, scaleMax = 1.2f },
                    new DecorationDef { modelNames = new[] { "flower_redA", "flower_redB", "flower_yellowA", "flower_yellowC", "flower_purpleC" }, scaleMin = 0.6f, scaleMax = 1f },
                    new DecorationDef { modelNames = new[] { "plant_bushSmall" }, scaleMin = 0.7f, scaleMax = 1.1f },
                    new DecorationDef { modelNames = new[] { "rock_smallFlatA", "rock_smallA" }, scaleMin = 0.6f, scaleMax = 1f },
                }
            },
            _ => Get(BiomeType.Plains)
        };
    }
}
