using UnityEngine;

// Shared by GestureCaptureController (initial spawn placement) and HidingAnimalBehavior
// (continuous ground-snapping while walking) - ForestStage's Ground_01 is an uneven terrain
// mound, not a flat plane, so a hardcoded Y easily floats above or clips into the mesh depending
// on XZ position.
public static class AnimalGroundUtil
{
    public static Vector3 SnapToGround(GameObject animal, Vector3 desiredPosition)
    {
        var t = animal.transform;

        // Prefer the CharacterController's capsule (center/height are fixed, serialized values -
        // always correct instantly) over Renderer.bounds, which can be stale on the very first
        // frame after Instantiate for an animated model that hasn't run its first Animator update.
        float bottomLocalY, topLocalY;
        if (animal.TryGetComponent<CharacterController>(out var cc))
        {
            bottomLocalY = cc.center.y - cc.height / 2f;
            topLocalY = cc.center.y + cc.height / 2f;
        }
        else
        {
            var renderers = animal.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return desiredPosition;
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            bottomLocalY = bounds.min.y - t.position.y;
            topLocalY = bounds.max.y - t.position.y;
        }

        var rayOrigin = new Vector3(desiredPosition.x, desiredPosition.y + topLocalY + 10f, desiredPosition.z);

        // RaycastAll + only accept a hit on the actual terrain ("Ground_01") - a plain Raycast
        // from above hits whatever solid collider comes first, which is very often the animal's
        // OWN collider, or (for hiding spots placed close to a tree trunk) the tree's own
        // BoxCollider spanning its full trunk+canopy height - both were mistaken for "the ground",
        // landing the animal on top of a tree or back where it already stood.
        var hits = Physics.RaycastAll(rayOrigin, Vector3.down, 50f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            if (!hit.collider.name.StartsWith("Ground_01")) continue;
            return new Vector3(desiredPosition.x, hit.point.y - bottomLocalY, desiredPosition.z);
        }

        return desiredPosition;
    }
}
