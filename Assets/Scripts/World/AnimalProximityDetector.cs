using UnityEngine;

public class AnimalProximityDetector : MonoBehaviour
{
    [SerializeField] private float radius = 5f;

    private void Update()
    {
        AnimalInstance nearest = null;
        float nearestSqrDist = radius * radius;

        foreach (var animal in AnimalInstance.Active)
        {
            if (animal.Captured) continue;

            float sqrDist = (animal.transform.position - transform.position).sqrMagnitude;
            if (sqrDist <= nearestSqrDist)
            {
                nearestSqrDist = sqrDist;
                nearest = animal;
            }
        }

        if (GameManager.Instance != null)
            GameManager.Instance.NearbyAnimal = nearest;
    }
}
