using ithappy.Animals_FREE;
using UnityEngine;

public class CaptureTargetAggression : MonoBehaviour
{
    [SerializeField] private float agitationFrequency = 6f;
    [SerializeField] private float shakeAmplitude = 0.05f;

    private Animator animator;
    private Vector3 previousShakeOffset = Vector3.zero;

    private void Awake()
    {
        if (TryGetComponent<CreatureMover>(out var mover)) mover.enabled = false;
        if (TryGetComponent<MovePlayerInput>(out var input)) input.enabled = false;

        animator = GetComponent<Animator>();

        // Keep the shake proportional to the model's actual size: World-mode scale (1)
        // gets the full amplitude, but AR previews are scaled way down (see
        // ARFoundationCaptureController.previewScale) and would otherwise shake by a
        // huge fraction of their own body size every frame.
        shakeAmplitude *= transform.localScale.x;
    }

    private void Update()
    {
        if (animator != null)
        {
            animator.SetFloat("State", 1f);
            animator.SetFloat("Vert", 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(Time.time * agitationFrequency)));
        }

        // No real attack/aggro animation exists in the ithappy pack - this small
        // additive jitter is the only available way to sell "agitated" while a
        // reposition (an external transform.position change) can happen any frame.
        transform.localPosition -= previousShakeOffset;
        Vector3 shake = (Vector3)(Random.insideUnitCircle * shakeAmplitude);
        transform.localPosition += shake;
        previousShakeOffset = shake;
    }
}
