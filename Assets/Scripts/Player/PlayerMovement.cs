using UnityEngine;

public enum MovementMode { Joystick, GPS }

public class PlayerMovement : MonoBehaviour
{
    [Header("Mode")]
    public MovementMode mode = MovementMode.GPS;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 720f;
    public float gpsSmoothing = 5f;

    private CharacterController controller;
    private VirtualJoystick joystick;
    private GPSLocator gps;
    private Vector3 gpsTargetPosition;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Start()
    {
        joystick = FindFirstObjectByType<VirtualJoystick>();
        gps = GetComponent<GPSLocator>();

        if (gps == null)
            gps = gameObject.AddComponent<GPSLocator>();

        #if UNITY_EDITOR
        if (mode == MovementMode.GPS)
        {
            Debug.Log("[PlayerMovement] Editor detected — falling back to Joystick mode.");
            mode = MovementMode.Joystick;
        }
        #endif
    }

    private void Update()
    {
        switch (mode)
        {
            case MovementMode.Joystick:
                HandleJoystick();
                break;
            case MovementMode.GPS:
                HandleGPS();
                break;
        }
    }

    private void HandleJoystick()
    {
        Vector2 input = joystick != null ? joystick.InputDirection : Vector2.zero;
        Vector3 moveDir = new Vector3(input.x, 0f, input.y);

        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        float gravity = controller.isGrounded ? -0.5f : -9.81f * Time.deltaTime;
        Vector3 velocity = moveDir * moveSpeed + Vector3.up * gravity;
        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleGPS()
    {
        if (gps == null || !gps.IsRunning) return;

        gpsTargetPosition = gps.WorldPosition;
        gpsTargetPosition.y = transform.position.y;

        Vector3 direction = gpsTargetPosition - transform.position;

        if (direction.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        transform.position = Vector3.Lerp(transform.position, gpsTargetPosition, gpsSmoothing * Time.deltaTime);
    }
}
