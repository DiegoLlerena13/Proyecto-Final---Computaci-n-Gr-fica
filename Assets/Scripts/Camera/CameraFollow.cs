using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Vector3 offset = new Vector3(0f, 3f, -6.5f);
    public float smoothSpeed = 5f;
    public float lookAheadDistance = 3f;

    private Transform target;

    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            target = player.transform;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 lookAhead = target.forward * lookAheadDistance;
        Vector3 desiredPosition = target.position + offset + lookAhead;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }
}
