using UnityEngine;
using UnityEngine.UI;
using Vuforia;

public class VuforiaCaptureController : MonoBehaviour
{
    [SerializeField] private GameObject mainCamera;
    [SerializeField] private GameObject arCamera;
    [SerializeField] private PlaneFinderBehaviour planeFinder;
    [SerializeField] private Button backButton;

    private void Start()
    {
        if (backButton != null) backButton.onClick.AddListener(() => GameManager.Instance.ReturnToWorld());
    }

    private void OnEnable()
    {
        if (mainCamera != null) mainCamera.SetActive(false);
        if (arCamera != null) arCamera.SetActive(true);
        if (planeFinder != null) planeFinder.OnInteractiveHitTest.AddListener(OnGroundHit);
    }

    private void OnDisable()
    {
        if (planeFinder != null) planeFinder.OnInteractiveHitTest.RemoveListener(OnGroundHit);
        if (arCamera != null) arCamera.SetActive(false);
        if (mainCamera != null) mainCamera.SetActive(true);
    }

    private void OnGroundHit(HitTestResult hit)
    {
        if (GameManager.Instance == null || GameManager.Instance.NearbyAnimal == null) return;

        var species = GameManager.Instance.NearbyAnimal.Species;
        var prefab = Resources.Load<GameObject>($"Animals/{species}");
        if (prefab == null) return;

        Instantiate(prefab, hit.Position, hit.Rotation);
        GameManager.Instance.OnCaptureSucceeded(species);
    }
}
