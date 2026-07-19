using System;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARFoundationCaptureController : MonoBehaviour
{
    [SerializeField] private GameObject mainCamera;
    [SerializeField] private GameObject arRoot; // Holds ARSession + XROrigin
    [SerializeField] private ARSession arSession;
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private XROrigin xrOrigin;
    [SerializeField] private Button backButton;
    [SerializeField] private Text progressText;
    [SerializeField] private Text statusText;
    [SerializeField] private int requiredTaps = 3;
    [SerializeField] private float groundLockTimeout = 25f;
    [SerializeField] private float previewScale = 0.15f; // World prefabs are sized for the far-away World camera, not real-world AR meters.
    [SerializeField] private float followSmoothing = 10f; // Smooths out FeaturePoint hit jitter from the broader AllTypes raycast.
    [SerializeField] private float relockDistance = 0.05f; // Raycast noise below this (meters) is ignored, not treated as a new spot.
    [SerializeField] private float relockDelay = 0.3f; // A new spot must be sustained this long before the preview actually moves there.
    [SerializeField] private Transform debugReticle; // Marks the raw raycast hit each frame, for diagnosing detection issues.

    public event Action OnTrackingTimedOut;

    private Camera arCamera;
    private GameObject previewInstance;
    private AnimalSpecies previewSpecies;
    private bool hasCaptured;
    private int tapCount;
    private Vector3 targetPosition;
    private Vector3 pendingPosition;
    private float pendingTimer;
    private Coroutine activeRoutine;
    private readonly List<ARRaycastHit> raycastHits = new();

    private void Start()
    {
        if (backButton != null) backButton.onClick.AddListener(() => GameManager.Instance.ReturnToWorld());
        if (xrOrigin != null) arCamera = xrOrigin.Camera;
    }

    private void OnEnable()
    {
        hasCaptured = false;
        tapCount = 0;
        UpdateProgressUI();
        SetStatus("Buscando superficie...");

        if (mainCamera != null) mainCamera.SetActive(false);
        if (arRoot != null) arRoot.SetActive(true);
        if (planeManager != null) planeManager.enabled = true;

        activeRoutine = StartCoroutine(WaitForInitialLock());
    }

    private void Update()
    {
        bool hasHit = TryRaycast(ScreenCenter(), out var pose);

        if (debugReticle != null)
        {
            debugReticle.gameObject.SetActive(hasHit);
            if (hasHit) debugReticle.position = pose.position;
        }

        if (previewInstance == null || hasCaptured) return;

        if (hasHit) UpdateTargetPosition(pose.position);

        previewInstance.transform.position = Vector3.Lerp(
            previewInstance.transform.position, targetPosition, Time.deltaTime * followSmoothing);
        FaceCamera();
    }

    // Debounces raw raycast hits: small jitter is ignored, and a genuinely new
    // spot only takes over once it has been the closest hit for a short while,
    // so the preview stays put instead of vibrating between noisy FeaturePoint hits.
    private void UpdateTargetPosition(Vector3 hitPosition)
    {
        if (Vector3.Distance(hitPosition, targetPosition) <= relockDistance)
        {
            pendingTimer = 0f;
            return;
        }

        if (Vector3.Distance(hitPosition, pendingPosition) > relockDistance)
        {
            pendingPosition = hitPosition;
            pendingTimer = 0f;
        }

        pendingTimer += Time.deltaTime;
        if (pendingTimer >= relockDelay)
            targetPosition = pendingPosition;
    }

    private void OnDisable()
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        ClearPreview();
        if (debugReticle != null) debugReticle.gameObject.SetActive(false);
        if (planeManager != null) planeManager.enabled = false;
        if (arRoot != null) arRoot.SetActive(false);
        if (mainCamera != null) mainCamera.SetActive(true);
    }

    private IEnumerator WaitForInitialLock()
    {
#if UNITY_ANDROID
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
        {
            Debug.Log("[ARFoundationCaptureController] Camera permission not granted. Requesting...");
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
            // Wait for a second for the OS dialog to show up
            yield return new WaitForSeconds(1f);
        }
#endif

        float initElapsed = 0f;
        ARSessionState lastLoggedState = ARSessionState.None;

        // Wait for session to start tracking or fail (e.g. permission prompt)
        while (ARSession.state != ARSessionState.SessionTracking)
        {
            if (ARSession.state != lastLoggedState)
            {
                lastLoggedState = ARSession.state;
                Debug.Log($"[ARFoundationCaptureController] ARSession state changed to: {lastLoggedState}");
            }

            if (ARSession.state == ARSessionState.Unsupported)
            {
                Debug.LogWarning("[ARFoundationCaptureController] ARSession state is Unsupported, falling back.");
                OnTrackingTimedOut?.Invoke();
                yield break;
            }

            SetStatus($"Inicializando AR... ({ARSession.state})");
            initElapsed += Time.deltaTime;
            // 30-second safety timeout for initialization (gives time to allow camera permission)
            if (initElapsed > 30f)
            {
                Debug.LogWarning($"[ARFoundationCaptureController] ARSession initialization timed out in state {ARSession.state}, falling back.");
                OnTrackingTimedOut?.Invoke();
                yield break;
            }
            yield return null;
        }

        Debug.Log("[ARFoundationCaptureController] ARSession is tracking! Starting plane scan...");

        // Once tracking, start the 15-second scanning timer
        float scanElapsed = 0f;
        while (scanElapsed < groundLockTimeout)
        {
            if (TryRaycast(ScreenCenter(), out var pose))
            {
                SpawnPreview(pose);
                SetStatus(string.Empty);
                yield break;
            }
            SetStatus("Movete lento sobre una superficie plana...");
            scanElapsed += Time.deltaTime;
            yield return null;
        }

        SetStatus("No pude detectar superficie");
        OnTrackingTimedOut?.Invoke();
    }

    private bool TryRaycast(Vector2 screenPoint, out Pose pose)
    {
        pose = default;
        if (raycastManager == null) return false;

        if (raycastManager.Raycast(screenPoint, raycastHits, TrackableType.AllTypes))
        {
            pose = raycastHits[0].pose;
            return true;
        }
        return false;
    }

    private void SpawnPreview(Pose pose)
    {
        if (GameManager.Instance == null || GameManager.Instance.NearbyAnimal == null) return;

        previewSpecies = GameManager.Instance.NearbyAnimal.Species;
        var prefab = AnimalResources.Load(previewSpecies);
        if (prefab == null) return;

        previewInstance = Instantiate(prefab, pose.position, Quaternion.identity);
        targetPosition = pose.position;
        pendingPosition = pose.position;
        pendingTimer = 0f;
        previewInstance.transform.localScale *= previewScale;
        AnimalResources.EnsureBoxCollider(previewInstance);
        previewInstance.AddComponent<CaptureTargetAggression>();
        previewInstance.AddComponent<CaptureTapTarget>().Owner = this;
        SetLayerRecursively(previewInstance, LayerMask.NameToLayer("ARCapture"));
        FaceCamera();
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        foreach (Transform child in root.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    public void RegisterTap()
    {
        if (hasCaptured || previewInstance == null) return;

        tapCount++;
        UpdateProgressUI();

        if (tapCount >= requiredTaps) ConfirmCapture();
    }

    private void FaceCamera()
    {
        if (arCamera == null) return;

        Vector3 toCamera = arCamera.transform.position - previewInstance.transform.position;
        toCamera.y = 0f;
        if (toCamera.sqrMagnitude > 0.0001f)
            previewInstance.transform.rotation = Quaternion.LookRotation(toCamera.normalized);
    }

    private Vector2 ScreenCenter() => new Vector2(Screen.width / 2f, Screen.height / 2f);

    private void UpdateProgressUI()
    {
        if (progressText != null) progressText.text = $"{tapCount}/{requiredTaps}";
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    private void ConfirmCapture()
    {
        hasCaptured = true;
        ClearPreview();
        GameManager.Instance.OnCaptureSucceeded(previewSpecies);
    }

    private void ClearPreview()
    {
        if (previewInstance != null) Destroy(previewInstance);
        previewInstance = null;
    }
}
