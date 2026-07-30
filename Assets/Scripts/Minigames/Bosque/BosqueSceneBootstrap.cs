using UnityEngine;

namespace BosqueEscape
{
    // Spawns the gameplay actors/props from Resources at scene start, matching this
    // project's established AnimalResources.Load-style pattern instead of baking full
    // skinned-mesh characters directly into the scene file as prefab instances.
    public class BosqueSceneBootstrap : MonoBehaviour
    {
        private static readonly Vector3[] StationPositions =
        {
            new Vector3(25f, 0f, 0f),
            new Vector3(7.7f, 0f, 23.8f),
            new Vector3(-20.2f, 0f, 14.7f),
            new Vector3(-20.2f, 0f, -14.7f),
            new Vector3(7.7f, 0f, -23.8f),
        };

        private static readonly Vector3 DeerSpawn = new Vector3(0f, 0.2f, 0f);
        private static readonly Vector3 TigerSpawn = new Vector3(40f, 0.2f, 40f);

        // Kept away from the washing machine stations (25m ring), the 3 shelter huts
        // (roughly (-16,16), (16,-16), (-18,-18)), the exit door (0,45) and the spawn.
        private static readonly string[] NaturePrefabs =
        {
            "Nature/Tree_01", "Nature/Tree_02", "Nature/Tree_03",
            "Nature/Rock_01", "Nature/Rock_02", "Nature/Bush_01",
        };
        private const int NatureCount = 28;
        private const float NatureHalfExtent = 50f;
        private const float NatureExcludeCenter = 9f;

        private static readonly Vector3[] NatureAvoidPoints =
        {
            new Vector3(25f, 0f, 0f), new Vector3(7.7f, 0f, 23.8f), new Vector3(-20.2f, 0f, 14.7f),
            new Vector3(-20.2f, 0f, -14.7f), new Vector3(7.7f, 0f, -23.8f),
            new Vector3(-16f, 0f, 16f), new Vector3(16f, 0f, -16f), new Vector3(-18f, 0f, -18f),
            new Vector3(0f, 0f, 45f),
        };
        private const float NatureAvoidRadius = 6f;

        private void Awake()
        {
            SpawnDeer();
            SpawnTiger();
            SpawnStations();
            SpawnNature();
        }

        private void SpawnDeer()
        {
            var prefab = Resources.Load<GameObject>("Bosque/DeerPlayer");
            if (prefab == null) { Debug.LogError("[BosqueSceneBootstrap] DeerPlayer prefab not found in Resources/Bosque."); return; }
            // Strip the "(Clone)" suffix Instantiate() adds - PlayerVisionFX/GameUI look up
            // "TigerChaser" by exact name, and this project's tag lookups don't care about name,
            // but a stable name makes the scene easier to read in the Hierarchy during testing.
            var deer = Instantiate(prefab, DeerSpawn, Quaternion.identity);
            deer.name = "DeerPlayer";
        }

        private void SpawnTiger()
        {
            var prefab = Resources.Load<GameObject>("Bosque/TigerChaser");
            if (prefab == null) { Debug.LogError("[BosqueSceneBootstrap] TigerChaser prefab not found in Resources/Bosque."); return; }
            var tiger = Instantiate(prefab, TigerSpawn, Quaternion.identity);
            tiger.name = "TigerChaser";
        }

        private void SpawnStations()
        {
            var prefab = Resources.Load<GameObject>("Bosque/SovietWashMachine");
            if (prefab == null)
            {
                Debug.LogError("[BosqueSceneBootstrap] SovietWashMachine prefab not found in Resources/Bosque.");
                return;
            }

            for (int i = 0; i < StationPositions.Length; i++)
            {
                var rot = Quaternion.Euler(0f, i * 71f, 0f);
                var wm = Instantiate(prefab, StationPositions[i], rot);
                wm.name = $"WashingMachine_{i + 1}";

                if (wm.GetComponentInChildren<Collider>() == null)
                {
                    var col = wm.AddComponent<BoxCollider>();
                    col.center = new Vector3(0f, 0.6f, 0f);
                    col.size = new Vector3(1.2f, 1.2f, 1.2f);
                }

                var interactor = wm.AddComponent<WashingMachineInteractor>();
                interactor.stationName = $"Estacion {i + 1}";
            }
        }

        // Scatters trees/rocks/bushes (reused from the project's existing SimpleNaturePack,
        // the same pack the open world already uses) so the arena isn't a bare plane.
        // Spawned at runtime rather than baked into the scene, same reasoning as the actors -
        // note this means they won't be included when the NavMesh is baked in the Editor,
        // which is fine for loose decorative scatter like this.
        private void SpawnNature()
        {
            var prefabs = new GameObject[NaturePrefabs.Length];
            for (int i = 0; i < NaturePrefabs.Length; i++)
                prefabs[i] = Resources.Load<GameObject>(NaturePrefabs[i]);

            var root = new GameObject("WorldNature").transform;
            int placed = 0, attempts = 0, maxAttempts = NatureCount * 12;

            while (placed < NatureCount && attempts < maxAttempts)
            {
                attempts++;
                float x = Random.Range(-NatureHalfExtent, NatureHalfExtent);
                float z = Random.Range(-NatureHalfExtent, NatureHalfExtent);
                if (Mathf.Sqrt(x * x + z * z) < NatureExcludeCenter) continue;

                bool tooClose = false;
                foreach (var p in NatureAvoidPoints)
                {
                    if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(x, z)) < NatureAvoidRadius)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;

                var prefab = prefabs[Random.Range(0, prefabs.Length)];
                if (prefab == null) continue;

                var inst = Instantiate(prefab, new Vector3(x, 0f, z), Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), root);
                inst.transform.localScale = Vector3.one * Random.Range(0.8f, 1.4f);
                placed++;
            }
        }
    }
}
