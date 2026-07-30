using UnityEngine;

// Minimal side-scroll follow: tracks the target's X only, fixed Y/Z offset, no rotation -
// unlike Bosque's CameraFollow.cs (which rotates to look at the target), a platformer
// camera should stay level.
public class SideScrollCameraFollow : MonoBehaviour
{
    public Vector3 offset = new Vector3(0f, 2f, -10f);
    public float smoothSpeed = 6f;

    private Transform target;

    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) target = player.transform;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = new Vector3(target.position.x + offset.x, offset.y, offset.z);
        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
    }
}
