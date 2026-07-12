using UnityEngine;
using UnityEngine.UI;

public class PetCareController : MonoBehaviour
{
    [SerializeField] private GameObject mainCamera;
    [SerializeField] private GameObject petCareStage;
    [SerializeField] private Transform animalStandPoint;
    [SerializeField] private Button backButton;

    private GameObject spawnedAnimal;

    private void Start()
    {
        if (backButton != null) backButton.onClick.AddListener(() => GameManager.Instance.ReturnToWorld());
    }

    private void OnEnable()
    {
        if (mainCamera != null) mainCamera.SetActive(false);
        if (petCareStage != null) petCareStage.SetActive(true);

        if (GameManager.Instance == null || animalStandPoint == null) return;

        var prefab = AnimalResources.Load(GameManager.Instance.PetCareSpecies);
        if (prefab == null) return;

        spawnedAnimal = Instantiate(prefab, animalStandPoint.position, animalStandPoint.rotation);
    }

    private void OnDisable()
    {
        if (spawnedAnimal != null)
        {
            Destroy(spawnedAnimal);
            spawnedAnimal = null;
        }

        if (petCareStage != null) petCareStage.SetActive(false);
        if (mainCamera != null) mainCamera.SetActive(true);
    }
}
