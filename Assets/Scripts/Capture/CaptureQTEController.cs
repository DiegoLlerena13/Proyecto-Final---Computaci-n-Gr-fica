using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CaptureQTEController : MonoBehaviour
{
    [SerializeField] private GameObject mainCamera;
    [SerializeField] private GameObject qteStage;
    [SerializeField] private Transform animalStandPoint;
    [SerializeField] private RectTransform spawnArea;
    [SerializeField] private Button circleTargetPrefab;
    [SerializeField] private Text progressText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private int requiredTaps = 3;

    private readonly List<Button> activeTargets = new();
    private GameObject spawnedAnimal;
    private AnimalInstance targetAnimal;
    private int tappedCount;

    private void OnEnable()
    {
        tappedCount = 0;
        if (mainCamera != null) mainCamera.SetActive(false);
        if (qteStage != null) qteStage.SetActive(true);

        SpawnAnimal();
        SpawnCircles();
        UpdateProgressUI();
    }

    private void OnDisable()
    {
        foreach (var button in activeTargets)
            if (button != null) Destroy(button.gameObject);
        activeTargets.Clear();

        if (spawnedAnimal != null) Destroy(spawnedAnimal);
        spawnedAnimal = null;
        targetAnimal = null;

        if (qteStage != null) qteStage.SetActive(false);
        if (mainCamera != null) mainCamera.SetActive(true);
    }

    private void SpawnAnimal()
    {
        if (GameManager.Instance == null || GameManager.Instance.NearbyAnimal == null || animalStandPoint == null) return;

        // Frozen for the whole minigame - AnimalProximityDetector keeps reassigning
        // GameManager.Instance.NearbyAnimal every frame in the background, so Finish() must use
        // this cached reference instead of re-reading NearbyAnimal once the minigame ends.
        targetAnimal = GameManager.Instance.NearbyAnimal;

        var species = targetAnimal.Species;
        var prefab = AnimalResources.Load(species);
        if (prefab == null) return;

        spawnedAnimal = Instantiate(prefab, animalStandPoint.position, animalStandPoint.rotation);
        spawnedAnimal.AddComponent<CaptureTargetAggression>();
    }

    private void SpawnCircles()
    {
        if (circleTargetPrefab == null || spawnArea == null) return;

        for (int i = 0; i < requiredTaps; i++)
        {
            var button = Instantiate(circleTargetPrefab, spawnArea);
            button.gameObject.SetActive(true);
            var rect = button.GetComponent<RectTransform>();
            float x = Random.Range(-spawnArea.rect.width / 2f, spawnArea.rect.width / 2f);
            float y = Random.Range(-spawnArea.rect.height / 2f, spawnArea.rect.height / 2f);
            rect.anchoredPosition = new Vector2(x, y);

            button.onClick.AddListener(() => OnCircleTapped(button));
            activeTargets.Add(button);
        }
    }

    private void OnCircleTapped(Button button)
    {
        activeTargets.Remove(button);
        Destroy(button.gameObject);

        tappedCount++;
        UpdateProgressUI();

        if (tappedCount >= requiredTaps) Finish();
    }

    private void UpdateProgressUI()
    {
        if (progressText != null) progressText.text = $"{tappedCount}/{requiredTaps}";
    }

    private void Finish()
    {
        if (GameManager.Instance != null && targetAnimal != null)
            GameManager.Instance.OnCaptureSucceeded(targetAnimal);
    }
}
