using ithappy.Animals_FREE;
using UnityEngine;

public class CaptureTargetAggression : MonoBehaviour
{
    private void Awake()
    {
        // Destroy rather than disable: CreatureMover applies gravity via CharacterController.Move()
        // every Update() while grounded, which fights the ground-snap placement and reads as
        // constant vertical jitter. Disabling should already stop Update() from firing, but
        // destroying the component removes any doubt.
        // MovePlayerInput requires CreatureMover ([RequireComponent]) - Unity refuses to destroy
        // the dependency while the dependent still exists, so destroy MovePlayerInput first.
        if (TryGetComponent<MovePlayerInput>(out var input)) Destroy(input);
        if (TryGetComponent<CreatureMover>(out var mover)) Destroy(mover);
    }

    // The position-jitter "agitated" effect (and, before that, Animator State/Vert
    // puppeteering) is disabled for now while we confirm the base capture loop - grounding,
    // camera framing, gesture/voice detection - works correctly with zero movement noise.
    // Re-add a purely horizontal (x, 0, z) jitter here once that's confirmed.
}
