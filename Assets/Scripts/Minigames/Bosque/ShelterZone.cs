using UnityEngine;

namespace BosqueEscape
{
    // Physical hideout: the deer is safe from the tiger while standing inside this volume
    // (DeerController tracks overlap via its own trigger events). No manual crouch/hide button -
    // shelter is purely "get inside", simpler than the source project's Hide-key + ShelterZone
    // combo since this port doesn't have a crouch mechanic.
    [RequireComponent(typeof(BoxCollider))]
    public class ShelterZone : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }
    }
}
