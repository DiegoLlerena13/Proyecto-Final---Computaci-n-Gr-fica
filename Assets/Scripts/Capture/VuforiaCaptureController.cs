using UnityEngine;
using UnityEngine.UI;
using Vuforia;

public class VuforiaCaptureController : MonoBehaviour
{
    [SerializeField] private GameObject mainCamera;
    [SerializeField] private GameObject arCamera;
    [SerializeField] private PlaneFinderBehaviour planeFinder;
    [SerializeField] private Button backButton;

    private Camera arCameraComponent;
    private GameObject previewInstance;
    private AnimalSpecies previewSpecies;

    private void Start()
    {
        if (backButton != null) backButton.onClick.AddListener(() => GameManager.Instance.ReturnToWorld());
        if (arCamera != null) arCameraComponent = arCamera.GetComponent<Camera>();
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
        ClearPreview();
        if (arCamera != null) arCamera.SetActive(false);
        if (mainCamera != null) mainCamera.SetActive(true);
    }

    private void Update()
    {
        if (previewInstance == null || arCameraComponent == null) return;
        if (!Input.GetMouseButtonDown(0)) return;

        var ray = arCameraComponent.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit) && hit.collider.transform.IsChildOf(previewInstance.transform))
        {
            ConfirmCapture();
        }
    }

    private void OnGroundHit(HitTestResult hit)
    {
        if (previewInstance != null) return;
        if (GameManager.Instance == null || GameManager.Instance.NearbyAnimal == null) return;

        previewSpecies = GameManager.Instance.NearbyAnimal.Species;
        var prefab = AnimalResources.Load(previewSpecies);
        if (prefab == null) return;

        previewInstance = Instantiate(prefab, hit.Position, hit.Rotation);
        if (previewInstance.GetComponentInChildren<Collider>() == null)
        {
            var box = previewInstance.AddComponent<BoxCollider>();
            var renderer = previewInstance.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                box.center = previewInstance.transform.InverseTransformPoint(renderer.bounds.center);
                box.size = renderer.bounds.size;
            }
        }
    }

    private void ConfirmCapture()
    {
        ClearPreview();
        GameManager.Instance.OnCaptureSucceeded(previewSpecies);
    }

    private void ClearPreview()
    {
        if (previewInstance != null) Destroy(previewInstance);
        previewInstance = null;
    }
}
