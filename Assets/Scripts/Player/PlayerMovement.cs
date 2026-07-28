using UnityEngine;

public enum MovementMode { Joystick, GPS }

public class PlayerMovement : MonoBehaviour
{
    [Header("Mode")]
    public MovementMode mode = MovementMode.Joystick;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float gpsSmoothing = 5f;

    [Header("Paper Sprite Flip")]
    [SerializeField] private Transform spriteTransform;
    public float flipSpeed = 10f;

    private static readonly Quaternion FacingRight = Quaternion.identity;
    private static readonly Quaternion FacingLeft = Quaternion.Euler(0f, 180f, 0f);

    private const float MinDistanceFromAnimals = 2.5f;

    private CharacterController controller;
    private VirtualJoystick joystick;
    private GPSLocator gps;
    private Vector3 gpsTargetPosition;
    private bool facingLeft;
    private bool hasSnappedToSpawn;

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

        // The teammate-authored Player.prefab is a flat paper cutout with no "back" artwork -
        // spriteTransform is auto-resolved instead of requiring manual Inspector wiring, matching
        // this session's established preference for auto-resolved refs over fragile serialized ones.
        if (spriteTransform == null)
        {
            var spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
                spriteTransform = spriteRenderer.transform;
        }

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
        // Diego's BottomNav does a real SceneManager.LoadScene("00.Mapa") on every "Mapa" tap, not
        // just a panel toggle - so the Player's baked scene position is a fresh spawn every time,
        // not just its initial placement. Run this on the first Update() (guaranteed to fire after
        // every scene object's Start(), including WorldGenerator's, unlike Start() itself whose
        // relative ordering across objects isn't guaranteed) so the origin chunk already exists.
        if (!hasSnappedToSpawn)
        {
            hasSnappedToSpawn = true;
            SnapToSpawnGround();
        }

        switch (mode)
        {
            case MovementMode.Joystick:
                HandleJoystick();
                break;
            case MovementMode.GPS:
                HandleGPS();
                break;
        }

        UpdateSpriteFacing();
    }

    private void HandleJoystick()
    {
        Vector2 input = joystick != null ? joystick.InputDirection : Vector2.zero;
        Vector3 moveDir = new Vector3(input.x, 0f, input.y);

        if (Mathf.Abs(input.x) > 0.1f)
            facingLeft = input.x < 0f;

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

        if (Mathf.Abs(direction.x) > 0.1f)
            facingLeft = direction.x < 0f;

        transform.position = Vector3.Lerp(transform.position, gpsTargetPosition, gpsSmoothing * Time.deltaTime);
    }

    // The open world's chunk ground is always a flat plane at world Y=0 (WorldGenerator.CreateChunk
    // never varies chunk height), so there's no terrain to raycast against - just place the
    // CharacterController's own bottom (center.y - height/2) exactly on that plane. Without this,
    // the player spawns wherever it happened to be sitting in the .unity file the last time it was
    // saved in the Editor, which reliably lands it half-buried since that saved Y rarely matches 0.
    private void SnapToSpawnGround()
    {
        var pos = transform.position;
        pos.y = -(controller.center.y - controller.height / 2f);
        transform.position = pos;

        PushAwayFromNearbyAnimals();
    }

    // The fixed spawn point and the deterministic per-chunk animal seeding (AnimalSpawner /
    // AmbientFaunaSpawner both hash chunk coordinates) mean the same animal can land right on top
    // of the spawn point every single time the scene reloads - not a rare fluke, a guaranteed
    // collision for whichever animal that chunk's seed happens to place nearby.
    private void PushAwayFromNearbyAnimals()
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            AnimalInstance blocking = null;
            foreach (var animal in AnimalInstance.Active)
            {
                if (animal == null) continue;
                var toPlayer = transform.position - animal.transform.position;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude < MinDistanceFromAnimals * MinDistanceFromAnimals)
                {
                    blocking = animal;
                    break;
                }
            }

            if (blocking == null) return;

            var away = transform.position - blocking.transform.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = Vector3.forward;
            transform.position += away.normalized * MinDistanceFromAnimals;
        }
    }

    // Flat 2D sprite: only mirror left/right (Paper Mario style), never a full 3D yaw rotation -
    // the same convention ControladorCuidador.cs and the prefab's own Controlador.cs already use
    // elsewhere in this project. A full rotation has no "back" artwork to show and used to leave
    // the character edge-on or mirrored depending on which way the camera happened to sit.
    private void UpdateSpriteFacing()
    {
        if (spriteTransform == null) return;

        Quaternion targetRotation = facingLeft ? FacingLeft : FacingRight;
        spriteTransform.localRotation = Quaternion.Slerp(
            spriteTransform.localRotation, targetRotation, flipSpeed * Time.deltaTime);
    }
}
