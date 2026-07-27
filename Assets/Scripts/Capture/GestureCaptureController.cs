using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Structural analog of CaptureQTEController, but driven by real hand-gesture/voice-shout events
// (via HandLandmarkDetector/VoiceShoutDetector) instead of button taps. Falls back to the sibling
// QTE controller if the gesture pipeline (camera permission, model load, webcam start) doesn't
// become ready within readyTimeoutSeconds.
public class GestureCaptureController : MonoBehaviour
{
    [SerializeField] private GameObject mainCamera;
    [SerializeField] private HandLandmarkDetector handDetector;
    [SerializeField] private VoiceShoutDetector voiceDetector;
    [SerializeField] private Transform animalStandPoint;
    [SerializeField] private Text progressText;
    [SerializeField] private Image micLevelBar;
    [SerializeField] private Image handIndicatorImage;
    [SerializeField] private Image voiceIndicatorImage;
    [SerializeField] private int requiredActions = 3;
    [SerializeField] private GameObject gestureModeRoot;
    [SerializeField] private GameObject fallbackModeRoot;
    [SerializeField] private Button backButton;
    [SerializeField] private float readyTimeoutSeconds = 20f;

    private static readonly Color HandIdleColor = new(1f, 1f, 1f, 0.35f);
    private static readonly Color HandSeenColor = new(0.4f, 0.85f, 1f, 0.9f);
    private static readonly Color HandFlashColor = new(0.3f, 1f, 0.4f, 1f);
    private static readonly Color VoiceIdleColor = new(1f, 1f, 1f, 0.35f);
    private static readonly Color VoiceFlashColor = new(1f, 0.85f, 0.3f, 1f);
    private const float FlashSeconds = 0.5f;

    private Transform gestureCameraTransform;
    private GameObject spawnedAnimal;
    private HidingAnimalBehavior currentHidingBehavior;
    private int completedCount;
    private bool hasFinished;
    private bool subscribed;
    private Coroutine readyWaitRoutine;
    private Coroutine handFlashRoutine;
    private Coroutine voiceFlashRoutine;

    private void Start()
    {
        if (backButton != null) backButton.onClick.AddListener(() => GameManager.Instance.ReturnToWorld());
    }

    private void OnEnable()
    {
        completedCount = 0;
        hasFinished = false;

        // GestureModeRoot (and therefore ForestStage/Ground_01's collider) must be active BEFORE
        // SpawnAnimal() runs its ground raycast - otherwise Physics.RaycastAll finds nothing and
        // the animal keeps whatever ungrounded position it started at.
        if (mainCamera != null) mainCamera.SetActive(false);
        if (gestureModeRoot != null) gestureModeRoot.SetActive(true);

        // Found by name instead of a manually-dragged Inspector reference: that field kept
        // ending up null (never saved) or pointing at the wrong object (GestureCameraCanvas
        // instead of GestureCamera, same-ish name) across several test sessions. Resolving it
        // here removes that entire class of mistake.
        if (gestureCameraTransform == null && gestureModeRoot != null)
        {
            var cam = gestureModeRoot.GetComponentInChildren<Camera>(true);
            if (cam != null) gestureCameraTransform = cam.transform;
            else Debug.LogWarning("[GestureCaptureController] No Camera component found under gestureModeRoot.");
        }

        SpawnAnimal();
        UpdateProgressUI();

        Subscribe();

        readyWaitRoutine = StartCoroutine(WaitForReadyOrFallback());
    }

    private void Update()
    {
        if (micLevelBar != null && voiceDetector != null)
            micLevelBar.fillAmount = voiceDetector.CurrentLevel01;

        // Continuous "I can see your hand" feedback, independent of whether a full gesture has
        // been recognized yet - lets the player know to keep their hand in frame.
        if (handIndicatorImage != null && handDetector != null && handFlashRoutine == null)
            handIndicatorImage.color = handDetector.HandVisible ? HandSeenColor : HandIdleColor;
    }

    private void OnDisable()
    {
        Debug.Log("[GestureCaptureController] OnDisable() - gesture capture mode is shutting down.");

        if (readyWaitRoutine != null)
        {
            StopCoroutine(readyWaitRoutine);
            readyWaitRoutine = null;
        }

        Unsubscribe();

        if (spawnedAnimal != null) Destroy(spawnedAnimal);
        spawnedAnimal = null;
        currentHidingBehavior = null;

        if (gestureModeRoot != null) gestureModeRoot.SetActive(false);
        if (mainCamera != null) mainCamera.SetActive(true);
    }

    private void SpawnAnimal()
    {
        if (GameManager.Instance == null || GameManager.Instance.NearbyAnimal == null || animalStandPoint == null) return;

        var species = GameManager.Instance.NearbyAnimal.Species;
        var prefab = AnimalResources.Load(species);
        if (prefab == null) return;

        spawnedAnimal = Instantiate(prefab, animalStandPoint.position, animalStandPoint.rotation);
        spawnedAnimal.AddComponent<CaptureTargetAggression>();

        var endPosition = AnimalGroundUtil.SnapToGround(spawnedAnimal, animalStandPoint.position);
        var path = ComputeApproachPath(endPosition);

        if (gestureCameraTransform != null)
        {
            // Aim the camera at the fixed capture point, not at wherever the animal starts - it
            // begins far away (behind the back tree) and walks INTO this framing one step per
            // registered action, so the camera should already be pointed at the arrival spot.
            var lookTarget = endPosition + Vector3.up * 0.8f;
            gestureCameraTransform.LookAt(lookTarget, Vector3.up);
        }

        currentHidingBehavior = spawnedAnimal.AddComponent<HidingAnimalBehavior>();
        currentHidingBehavior.Initialize(path, requiredActions);
    }

    // Finds the "Tree_*" object under gestureModeRoot (skipping "_LOD*" mesh children) that sits
    // farthest from the camera, and builds a 3-point path: a spot just behind its trunk (the
    // animal's starting position) -> a sidestep point clear of the trunk's collider -> the fixed
    // capture point. The sidestep keeps the animal from walking straight through the tree it just
    // spawned behind. Computed at runtime instead of hand-placed in the scene: hand-placed
    // Transforms have to be re-guessed by hand every time the stage or camera changes, and (as
    // seen repeatedly this session) Inspector references are easy to wire wrong or lose entirely.
    private Vector3[] ComputeApproachPath(Vector3 endPosition)
    {
        if (gestureModeRoot == null || gestureCameraTransform == null || spawnedAnimal == null)
            return new[] { endPosition };

        const float hideDistanceBehindTree = 0.6f;
        const float sidestepDistance = 2.2f;
        var cameraPos = gestureCameraTransform.position;
        var allTransforms = gestureModeRoot.GetComponentsInChildren<Transform>(true);

        Transform farthestTree = null;
        var farthestDistSqr = -1f;

        foreach (var tr in allTransforms)
        {
            if (!tr.name.StartsWith("Tree_") || tr.name.Contains("_LOD")) continue;

            var distSqr = (tr.position - cameraPos).sqrMagnitude;
            if (distSqr > farthestDistSqr)
            {
                farthestDistSqr = distSqr;
                farthestTree = tr;
            }
        }

        if (farthestTree == null) return new[] { endPosition };

        var dirFromCamera = farthestTree.position - cameraPos;
        dirFromCamera.y = 0f;
        if (dirFromCamera.sqrMagnitude < 0.0001f) return new[] { endPosition };
        dirFromCamera.Normalize();

        var startPoint = farthestTree.position + dirFromCamera * hideDistanceBehindTree;

        // Sidestep further out along world X, away from the stage's center line, clearing the
        // tree's BoxCollider (which spans its whole trunk+canopy) before heading toward the camera.
        var sideSign = Mathf.Sign(farthestTree.position.x == 0f ? 1f : farthestTree.position.x);
        var sidePoint = new Vector3(farthestTree.position.x + sideSign * sidestepDistance, farthestTree.position.y, farthestTree.position.z);

        return new[]
        {
            AnimalGroundUtil.SnapToGround(spawnedAnimal, startPoint),
            AnimalGroundUtil.SnapToGround(spawnedAnimal, sidePoint),
            endPosition
        };
    }

    private void Subscribe()
    {
        if (subscribed) return;

        if (handDetector != null && handDetector.Analyzer != null)
            handDetector.Analyzer.OnGestureRecognized += HandleGestureRecognized;

        if (voiceDetector != null)
            voiceDetector.OnShoutDetected += HandleShoutDetected;

        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;

        if (handDetector != null && handDetector.Analyzer != null)
            handDetector.Analyzer.OnGestureRecognized -= HandleGestureRecognized;

        if (voiceDetector != null)
            voiceDetector.OnShoutDetected -= HandleShoutDetected;

        subscribed = false;
    }

    private IEnumerator WaitForReadyOrFallback()
    {
        var elapsed = 0f;
        while (elapsed < readyTimeoutSeconds)
        {
            if (handDetector != null && handDetector.IsReady)
            {
                readyWaitRoutine = null;
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Debug.LogWarning("[GestureCaptureController] Hand detector was not ready in time, falling back to QTE capture.");
        readyWaitRoutine = null;
        FallbackToQTE();
    }

    private void HandleGestureRecognized(GestureType gesture)
    {
        Debug.Log($"[GestureCaptureController] Gesture recognized: {gesture} (count will become {completedCount + 1}/{requiredActions})");

        if (handIndicatorImage != null)
        {
            if (handFlashRoutine != null) StopCoroutine(handFlashRoutine);
            handFlashRoutine = StartCoroutine(FlashIndicator(handIndicatorImage, HandFlashColor, () => handFlashRoutine = null));
        }

        RegisterAction();
    }

    private void HandleShoutDetected()
    {
        Debug.Log($"[GestureCaptureController] Shout detected (count will become {completedCount + 1}/{requiredActions})");

        if (voiceIndicatorImage != null)
        {
            if (voiceFlashRoutine != null) StopCoroutine(voiceFlashRoutine);
            voiceFlashRoutine = StartCoroutine(FlashIndicator(voiceIndicatorImage, VoiceFlashColor, () => voiceFlashRoutine = null));
        }

        RegisterAction();
    }

    // Briefly pops an indicator badge to a bright color so the player gets clear feedback that
    // the action they just performed (hand gesture or shout) actually registered.
    private IEnumerator FlashIndicator(Image indicator, Color flashColor, Action onDone)
    {
        indicator.color = flashColor;
        yield return new WaitForSeconds(FlashSeconds);
        indicator.color = VoiceIdleColor;
        onDone();
    }

    private void RegisterAction()
    {
        // Without this guard, every gesture/shout that arrives after the 3rd one kept
        // incrementing completedCount and re-calling Finish() (seen in testing: the counter
        // reached 104/3) because nothing ever stopped listening once the target was reached.
        if (hasFinished) return;

        completedCount++;
        UpdateProgressUI();
        currentHidingBehavior?.AdvanceStep();

        if (completedCount >= requiredActions)
        {
            // Guard immediately so no stray gesture/shout sneaks in while the celebration plays,
            // then let the animal actually arrive and settle before cutting away - previously
            // Finish() fired the instant the 3rd action registered, destroying the animal
            // mid-stride instead of letting the approach read as "captured".
            hasFinished = true;
            Unsubscribe();
            StartCoroutine(CelebrateThenFinish());
        }
    }

    private void UpdateProgressUI()
    {
        if (progressText != null) progressText.text = $"{completedCount}/{requiredActions}";
    }

    private IEnumerator CelebrateThenFinish()
    {
        var timeout = 3f;
        while (currentHidingBehavior != null && currentHidingBehavior.IsMoving && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        if (spawnedAnimal != null)
            yield return StartCoroutine(PlayCaptureHighlight(spawnedAnimal));

        Finish();
    }

    // Asset-free "captured!" cue: a quick scale pop plus a small sparkle burst. Works regardless
    // of the species prefab's shaders/materials, since it never touches materials directly (that
    // would risk bleeding a color change onto other objects sharing the same material).
    private IEnumerator PlayCaptureHighlight(GameObject animal)
    {
        var t = animal.transform;
        var baseScale = t.localScale;

        SpawnCaptureSparkle(t.position + Vector3.up * 0.5f);

        const float popDuration = 0.35f;
        var elapsed = 0f;
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            var pingPong = Mathf.Sin(elapsed / popDuration * Mathf.PI);
            t.localScale = baseScale * (1f + pingPong * 0.15f);
            yield return null;
        }

        t.localScale = baseScale;
        yield return new WaitForSeconds(0.6f);
    }

    private static void SpawnCaptureSparkle(Vector3 position)
    {
        var go = new GameObject("CaptureSparkle");
        go.transform.position = position;

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.startLifetime = 0.5f;
        main.startSpeed = 2f;
        main.startSize = 0.15f;
        main.startColor = new Color(1f, 0.9f, 0.3f);

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 20) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        UnityEngine.Object.Destroy(go, 1.5f);
    }

    private void Finish()
    {
        Debug.Log("[GestureCaptureController] Finish() called - all required actions completed, capture succeeding.");

        if (GameManager.Instance != null && GameManager.Instance.NearbyAnimal != null)
            GameManager.Instance.OnCaptureSucceeded(GameManager.Instance.NearbyAnimal.Species);
    }

    // Permission denied or model load failed: hand off to the button-tap QTE fallback instead of
    // leaving the player stuck with a dead camera feed. The fallback's own OnEnable spawns and
    // manages its own animal once its root becomes active, so we must not duplicate that here.
    private void FallbackToQTE()
    {
        Unsubscribe();

        if (spawnedAnimal != null) Destroy(spawnedAnimal);
        spawnedAnimal = null;

        if (gestureModeRoot != null) gestureModeRoot.SetActive(false);
        if (fallbackModeRoot != null) fallbackModeRoot.SetActive(true);
    }
}
