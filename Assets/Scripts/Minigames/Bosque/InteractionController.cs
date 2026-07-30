using UnityEngine;

namespace BosqueEscape
{
    // Mobile: no keyboard "Interact" action - a screen button calls Interact() directly
    // (wired via Button.OnClick in the scene), same on-screen-button pattern as the rest
    // of this project's minigame input (e.g. GallitoFlightController's input catcher).
    public class InteractionController : MonoBehaviour
    {
        public float interactionDistance = 3f;

        public void Interact()
        {
            Vector3 origin = transform.position + Vector3.up * 1f;
            if (!Physics.Raycast(origin, transform.forward, out RaycastHit hit, interactionDistance)) return;

            WashingMachineInteractor wm = hit.collider.GetComponentInParent<WashingMachineInteractor>();
            if (wm != null) wm.Activate();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position + Vector3.up, transform.forward * interactionDistance);
        }
    }
}
