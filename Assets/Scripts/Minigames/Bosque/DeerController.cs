using UnityEngine;

namespace BosqueEscape
{
    // Mobile joystick movement, camera-relative (same convention as ControladorCuidador).
    // Full joystick tilt runs, partial walks. Drives the Animator directly (Vert/State),
    // matching the ithappy rig's blend tree parameters - no CreatureMover involved.
    [RequireComponent(typeof(CharacterController))]
    public class DeerController : MonoBehaviour
    {
        [Header("Movement")]
        public float walkSpeed = 3f;
        public float runSpeed = 6.5f;
        public float rotateSpeed = 8f;
        public float gravity = -20f;
        [Range(0f, 1f)] public float runThreshold = 0.75f;

        [Header("Refs")]
        public VirtualJoystick joystick;

        private CharacterController _cc;
        private Animator _animator;
        private Transform _camera;
        private float _yVel;

        public bool IsRunning { get; private set; }
        public bool IsSheltered { get; private set; }

        private int _shelterOverlaps;

        private static readonly int AnimVert = Animator.StringToHash("Vert");
        private static readonly int AnimState = Animator.StringToHash("State");

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _animator = GetComponentInChildren<Animator>();
        }

        private void Start()
        {
            if (joystick == null) joystick = FindFirstObjectByType<VirtualJoystick>();
            if (Camera.main != null) _camera = Camera.main.transform;
        }

        private void Update()
        {
            Vector2 stick = joystick != null ? joystick.InputDirection : Vector2.zero;
            float mag = Mathf.Clamp01(stick.magnitude);
            IsRunning = mag >= runThreshold;

            Vector3 motion = Vector3.zero;
            if (mag > 0.05f && _camera != null)
            {
                Vector3 forward = Vector3.ProjectOnPlane(_camera.forward, Vector3.up).normalized;
                Vector3 right = Vector3.ProjectOnPlane(_camera.right, Vector3.up).normalized;
                Vector3 dir = (forward * stick.y + right * stick.x).normalized;

                float speed = IsRunning ? runSpeed : walkSpeed;
                motion = dir * speed * mag;

                Quaternion targetRot = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
            }

            if (_cc.isGrounded) _yVel = -2f;
            else _yVel += gravity * Time.deltaTime;
            motion.y = _yVel;

            _cc.Move(motion * Time.deltaTime);

            if (_animator != null)
            {
                float animSpeed = mag * (IsRunning ? 1f : 0.5f);
                _animator.SetFloat(AnimVert, animSpeed, 0.1f, Time.deltaTime);
                _animator.SetFloat(AnimState, mag > 0.05f ? 1f : 0f, 0.1f, Time.deltaTime);
            }
        }

        // CharacterController generates trigger events the same way a normal Collider does -
        // no extra collider needed on the Deer to detect ShelterZone overlap.
        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<ShelterZone>() == null) return;
            _shelterOverlaps++;
            IsSheltered = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<ShelterZone>() == null) return;
            _shelterOverlaps = Mathf.Max(0, _shelterOverlaps - 1);
            IsSheltered = _shelterOverlaps > 0;
        }
    }
}
