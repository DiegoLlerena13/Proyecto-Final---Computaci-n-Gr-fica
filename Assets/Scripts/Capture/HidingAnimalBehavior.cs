using UnityEngine;

// Moves the captured animal along a multi-point path (starting behind a tree, far from the
// camera, ending at the capture point) in discrete steps - one step per registered gesture/shout
// action. GestureCaptureController calls AdvanceStep() each time RegisterAction() fires, so the
// animal visibly approaches the player with every action until the final one lands it at the
// capture point. The path has a sidestep waypoint so it walks around the starting tree's trunk
// instead of straight through it.
public class HidingAnimalBehavior : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float turnSpeedDegrees = 180f;

    private const float RegroundIntervalSeconds = 0.2f;

    private Animator animator;
    private Vector3[] pathPoints;
    private float[] cumulativeDistances;
    private float totalPathLength;
    private int totalSteps;
    private int currentStep;
    private Vector3 targetPosition;
    private float regroundTimer;

    public bool IsMoving { get; private set; }

    public void Initialize(Vector3[] path, int steps)
    {
        animator = GetComponent<Animator>();

        pathPoints = path;
        totalSteps = Mathf.Max(steps, 1);
        currentStep = 0;

        cumulativeDistances = new float[pathPoints.Length];
        cumulativeDistances[0] = 0f;
        for (var i = 1; i < pathPoints.Length; i++)
            cumulativeDistances[i] = cumulativeDistances[i - 1] + Vector3.Distance(pathPoints[i - 1], pathPoints[i]);
        totalPathLength = cumulativeDistances[pathPoints.Length - 1];

        transform.position = AnimalGroundUtil.SnapToGround(gameObject, pathPoints[0]);
        targetPosition = transform.position;

        // Face toward the first leg of the path immediately, instead of waiting for the first
        // AdvanceStep() - until then targetPosition == current position, so Update()'s
        // move-direction rotation never runs and the model is stuck at its spawn rotation.
        var initialFaceDir = pathPoints[Mathf.Min(1, pathPoints.Length - 1)] - transform.position;
        initialFaceDir.y = 0f;
        if (initialFaceDir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(initialFaceDir.normalized, Vector3.up);
    }

    // Moves the target one step further along the path. Called by GestureCaptureController each
    // time a gesture/shout is registered - by the requiredActions-th call, the target lands
    // exactly on the path's final point.
    public void AdvanceStep()
    {
        currentStep = Mathf.Min(currentStep + 1, totalSteps);
        targetPosition = PointAtFraction((float)currentStep / totalSteps);
    }

    private Vector3 PointAtFraction(float fraction)
    {
        var targetDist = Mathf.Clamp01(fraction) * totalPathLength;
        for (var i = 1; i < pathPoints.Length; i++)
        {
            if (targetDist > cumulativeDistances[i]) continue;

            var segStart = cumulativeDistances[i - 1];
            var segLen = cumulativeDistances[i] - segStart;
            var segT = segLen > 0.0001f ? (targetDist - segStart) / segLen : 0f;
            return Vector3.Lerp(pathPoints[i - 1], pathPoints[i], segT);
        }

        return pathPoints[pathPoints.Length - 1];
    }

    private void Update()
    {
        var next = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        // Re-ground periodically while walking, not just at the path's waypoints - the mound
        // terrain height varies along the path too.
        regroundTimer -= Time.deltaTime;
        if (regroundTimer <= 0f)
        {
            regroundTimer = RegroundIntervalSeconds;
            next = AnimalGroundUtil.SnapToGround(gameObject, next);
        }

        var moveDir = targetPosition - transform.position;
        moveDir.y = 0f;
        IsMoving = moveDir.sqrMagnitude > 0.01f;

        if (IsMoving)
        {
            var targetRotation = Quaternion.LookRotation(moveDir.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeedDegrees * Time.deltaTime);
        }

        transform.position = next;

        // Drives the same "Vert"/"State" locomotion blend-tree parameters CreatureMover used to
        // (before CaptureTargetAggression destroys it) - without this the animal slides across
        // the ground with no walk-cycle animation playing.
        if (animator != null)
        {
            animator.SetFloat("Vert", IsMoving ? 1f : 0f);
            animator.SetFloat("State", 0f);
        }
    }
}
