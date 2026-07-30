using UnityEngine;
using UnityEngine.AI;

namespace BosqueEscape
{
    // NavMesh chase AI. Drives the Animator directly (Vert/State) - matches the ithappy
    // rig's blend tree params, same scheme CreatureMover.AnimationHandler uses, so the
    // shared Tiger rig/AnimatorController animates correctly without CreatureMover attached
    // (CreatureMover + MovePlayerInput are destroyed on this clone, same pattern
    // CaptureTargetAggression already uses elsewhere in this project to hand-drive an
    // ithappy animal).
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    public class TigerAI : MonoBehaviour
    {
        [Header("Chase")]
        public float normalSpeed = 3.5f;
        public float alertSpeed = 6.5f;
        public float catchRadius = 1.8f;

        [Header("Attack")]
        public float attackCooldown = 2.5f;

        [Header("Activation")]
        public bool requireFirstAlarm = true; // Tiger only chases AFTER first alarm activated
        public float spawnDistanceFromPlayer = 60f; // warp far from player on Start

        [Header("Progression - gets more dangerous over time and per alarm")]
        public float speedPerAlarm = 0.6f;
        public float cooldownReductionPerAlarm = 0.25f;
        public float speedPerMinute = 0.4f;
        public float catchRadiusPerAlarm = 0.10f;

        private NavMeshAgent _agent;
        private Animator _animator;
        private Transform _target;
        private DeerController _deer;
        private float _nextAttackTime;

        private float _baseAlertSpeed;
        private float _baseCooldown;
        private float _baseCatchRadius;
        private int _alarmsCount;
        private float _startTime;

        private static readonly int AnimVert = Animator.StringToHash("Vert");
        private static readonly int AnimState = Animator.StringToHash("State");

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _animator = GetComponent<Animator>();
        }

        private void Start()
        {
            _baseAlertSpeed = alertSpeed;
            _baseCooldown = attackCooldown;
            _baseCatchRadius = catchRadius;
            _startTime = Time.time;

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _target = player.transform;
                _deer = player.GetComponent<DeerController>();
            }
            else
                Debug.LogWarning("[TigerAI] No GameObject with tag 'Player' found.");

            if (GameManager.Instance != null)
                GameManager.Instance.OnAlarmActivated.AddListener(OnAlarm);

            // Warp tiger far from player at game start (random side, fixed distance)
            if (_target != null && _agent != null && _agent.isOnNavMesh)
            {
                float ang = Random.Range(0f, Mathf.PI * 2f);
                Vector3 spawn = _target.position + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * spawnDistanceFromPlayer;
                if (NavMesh.SamplePosition(spawn, out NavMeshHit hit, 30f, NavMesh.AllAreas))
                    _agent.Warp(hit.position);
            }

            if (_agent != null && !_agent.isOnNavMesh)
            {
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 50f, NavMesh.AllAreas))
                {
                    _agent.Warp(hit.position);
                    Debug.Log($"[TigerAI] Warped Tiger to NavMesh at {hit.position}");
                }
                else
                {
                    Debug.LogError("[TigerAI] Could not find NavMesh near Tiger!");
                }
            }
        }

        private void OnAlarm(int count)
        {
            _alarmsCount = count;
            alertSpeed = _baseAlertSpeed + count * speedPerAlarm;
            attackCooldown = Mathf.Max(0.6f, _baseCooldown - count * cooldownReductionPerAlarm);
            catchRadius = _baseCatchRadius + count * catchRadiusPerAlarm;
            Debug.Log($"[TigerAI] Aggression up - speed={alertSpeed:F1}, cooldown={attackCooldown:F1}, radius={catchRadius:F1}");
        }

        private float _nextPathUpdateTime;
        public float pathUpdateInterval = 0.2f;

        private void Update()
        {
            if (_target == null || GameManager.Instance == null || !GameManager.Instance.IsGameActive())
            {
                if (_agent.isOnNavMesh) _agent.ResetPath();
                return;
            }

            // Dormant until the first alarm is activated
            if (requireFirstAlarm && _alarmsCount == 0)
            {
                if (_agent.isOnNavMesh && _agent.hasPath) _agent.ResetPath();
                _animator.SetFloat(AnimVert, 0f);
                _animator.SetFloat(AnimState, 0f);
                return;
            }

            bool deerSheltered = _deer != null && _deer.IsSheltered;

            float minutesElapsed = (Time.time - _startTime) / 60f;
            float liveSpeed = alertSpeed + minutesElapsed * speedPerMinute;
            _agent.speed = deerSheltered ? normalSpeed : liveSpeed;

            // Only update path at intervals to prevent shaking/stuttering
            if (Time.time >= _nextPathUpdateTime)
            {
                _nextPathUpdateTime = Time.time + pathUpdateInterval;
                if (_agent.isOnNavMesh)
                    _agent.SetDestination(_target.position);
            }

            // Sheltered deer can't be caught - matches the "resguardarse" mechanic: get inside
            // a shelter hut and the tiger backs off to a slow patrol pace instead of attacking.
            float dist = Vector3.Distance(transform.position, _target.position);
            if (!deerSheltered && dist <= catchRadius && Time.time >= _nextAttackTime)
            {
                _nextAttackTime = Time.time + attackCooldown;
                var health = _target.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.TakeDamage(1, transform.position);
                    // Back off so the player has space to escape
                    if (_agent.isOnNavMesh)
                        _agent.Warp(transform.position - transform.forward * 4f);
                }
                else
                {
                    GameManager.Instance.TriggerGameOver();
                }
            }

            // Animator - matches ithappy animal animator params
            float speed = _agent.velocity.magnitude;
            _animator.SetFloat(AnimVert, speed / Mathf.Max(alertSpeed, 0.01f));
            _animator.SetFloat(AnimState, 1f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, catchRadius);
        }
    }
}
