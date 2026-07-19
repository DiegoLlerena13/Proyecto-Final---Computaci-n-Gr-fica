using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Management;

public class CaptureModeController : MonoBehaviour
{
    [SerializeField] private GameObject arModeRoot;
    [SerializeField] private GameObject nonArModeRoot;
    [SerializeField] private ARFoundationCaptureController arCaptureController;

    private void Awake()
    {
        if (arCaptureController != null)
            arCaptureController.OnTrackingTimedOut += HandleTrackingTimedOut;
    }

    private void OnEnable()
    {
        bool arSupported = IsARSupported();
        Debug.Log($"[CaptureModeController] AR supported on this device: {arSupported}");

        if (arSupported) ActivateArMode();
        else ActivateFallback();
    }

    private bool IsARSupported()
    {
        // In the Editor there is no ARCore, always fall back.
        if (Application.isEditor) return false;

        // On a real device, always try AR and let the 15-second tracking timeout decide.
        // During OnEnable, activeLoader is null because XR initialization has not started yet.
        return true;
    }

    private void ActivateArMode()
    {
        Debug.Log("[CaptureModeController] Attempting real AR mode");
        if (arModeRoot != null) arModeRoot.SetActive(true);
        if (nonArModeRoot != null) nonArModeRoot.SetActive(false);
    }

    private void ActivateFallback()
    {
        Debug.Log("[CaptureModeController] Falling back to non-AR QTE mode");
        if (arModeRoot != null) arModeRoot.SetActive(false);
        if (nonArModeRoot != null) nonArModeRoot.SetActive(true);
    }

    private void HandleTrackingTimedOut()
    {
        Debug.Log("[CaptureModeController] AR ground-lock timed out, falling back");
        ActivateFallback();
    }
}
