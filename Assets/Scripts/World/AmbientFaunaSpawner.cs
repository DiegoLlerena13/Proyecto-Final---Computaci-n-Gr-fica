using System.Collections.Generic;
using UnityEngine;

public static class AmbientFaunaSpawner
{
    private static readonly AnimalSpecies[] Species =
    {
        AnimalSpecies.BirdFly, AnimalSpecies.Conejo, AnimalSpecies.Cuervo, AnimalSpecies.GallitoDeLasRocas,
        AnimalSpecies.Paloma, AnimalSpecies.PerroLadrando, AnimalSpecies.Pinguino, AnimalSpecies.SerpienteAzul,
        AnimalSpecies.SerpienteBlanca, AnimalSpecies.SerpienteCorn, AnimalSpecies.SerpienteMarron,
        AnimalSpecies.SerpienteRoja, AnimalSpecies.SerpienteVerde, AnimalSpecies.YellowBird
    };

    // BirdFly's and GallitoDeLasRocas' source art (both reuse FreeAnimalPack/BirdFly.png) is drawn
    // mid-flight, unlike Cuervo/Paloma/Pinguino (Crow-Idle, Pigeon-Idle, YellowBird-Idle art - all
    // standing poses despite "bird" being in the species name) - these are the species that should
    // float above ground instead of being foot-aligned to it.
    private static readonly HashSet<AnimalSpecies> FlyingSpecies = new() { AnimalSpecies.BirdFly, AnimalSpecies.GallitoDeLasRocas };

    private static readonly Dictionary<AnimalSpecies, GameObject> prefabCache = new();

    public static void SpawnFauna(Transform parent, Vector2Int coords, float chunkSize)
    {
        int seed = (coords.x * 73856093 ^ coords.y * 19349663) ^ 0x5F5F5F5F;
        var rng = new System.Random(seed);

        int count = rng.Next(0, 3);
        float half = chunkSize * 0.45f;

        for (int i = 0; i < count; i++)
        {
            var species = Species[rng.Next(Species.Length)];
            var prefab = LoadFauna(species);
            if (prefab == null) continue;

            float x = (float)(rng.NextDouble() * 2 - 1) * half;
            float z = (float)(rng.NextDouble() * 2 - 1) * half;

            var instance = Object.Instantiate(prefab, parent);
            instance.transform.localPosition = new Vector3(x, 0f, z);
            // These are flat paper-cutout sprites, not real 3D models - a random full
            // spin would face many of them edge-on to the camera (near-invisible sliver).
            // Only flip left/right, like their own walk-animation convention does.
            instance.transform.localRotation = Quaternion.Euler(0f, rng.Next(2) == 0 ? 0f : 180f, 0f);

            // The Fauna prefabs share Player.prefab's template: an active, non-kinematic
            // Rigidbody with gravity on, meant to be driven by their own Controlador script -
            // which is disabled here since ambient fauna don't walk. Left as-is, the unsimulated
            // Rigidbody falls and can catch on nearby decoration colliders (trees/rocks spawned
            // at independent random positions in the same chunk) and get physics-launched away
            // the instant it spawns, so it's never actually visible to the player.
            if (instance.TryGetComponent<Rigidbody>(out var rb))
                rb.isKinematic = true;

            // The prefab's root pivot sits well above the sprite's feet (it was authored
            // around the disabled Controlador's ground-raycast). Ground-align using the
            // CapsuleCollider's raw serialized center/height/radius - NOT Collider.bounds,
            // which reflects PhysX's internally cached transform and can still be stale right
            // after Instantiate + a transform move on what was, until a few lines up, an
            // active non-kinematic Rigidbody (the same staleness trap already documented in
            // AnimalGroundUtil.SnapToGround for the capture-mode animal). Renderer bounds
            // don't have that staleness problem, so they're the fallback for any species with
            // no CapsuleCollider.
            if (FlyingSpecies.Contains(species))
            {
                // Roughly tree-canopy height, so it reads as flying over the scenery rather
                // than standing on the ground or floating above the treetops.
                float flyHeight = Mathf.Lerp(2.5f, 4f, (float)rng.NextDouble());
                instance.transform.position += new Vector3(0f, flyHeight, 0f);
            }
            else
            {
                var capsule = instance.GetComponentInChildren<CapsuleCollider>();
                float? colliderBottomLocalY = capsule != null
                    ? capsule.center.y - Mathf.Max(capsule.height / 2f, capsule.radius)
                    : (float?)null;

                if (colliderBottomLocalY.HasValue)
                {
                    instance.transform.position += new Vector3(0f, -colliderBottomLocalY.Value, 0f);
                }
                else
                {
                    var spriteRenderer = instance.GetComponentInChildren<SpriteRenderer>();
                    if (spriteRenderer != null)
                    {
                        float feetOffset = parent.position.y - spriteRenderer.bounds.min.y;
                        instance.transform.position += new Vector3(0f, feetOffset, 0f);
                    }
                }
            }

            var animalInstance = instance.GetComponent<AnimalInstance>();
            if (animalInstance == null)
                animalInstance = instance.AddComponent<AnimalInstance>();
            animalInstance.Initialize(species);
        }
    }

    private static GameObject LoadFauna(AnimalSpecies species)
    {
        if (prefabCache.TryGetValue(species, out var cached))
            return cached;

        var prefab = Resources.Load<GameObject>($"Fauna/{species}");
        if (prefab == null)
            Debug.LogWarning($"[AmbientFaunaSpawner] Model not found: Fauna/{species}");

        prefabCache[species] = prefab;
        return prefab;
    }
}
