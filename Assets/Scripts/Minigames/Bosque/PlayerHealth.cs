using UnityEngine;
using UnityEngine.Events;

namespace BosqueEscape
{
    // Health for the deer. Tiger does 1 damage per attack, 4 hits = game over.
    // Brief invincibility window after each hit so the tiger can't insta-kill.
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Stats")]
        public int maxHealth = 4;
        public float invulnerabilityTime = 1.5f;
        public float knockbackForce = 6f;
        public float knockbackUpward = 2f;

        [Header("Events")]
        public UnityEvent<int, int> OnHealthChanged; // (current, max)
        public UnityEvent OnDamaged;
        public UnityEvent OnDeath;

        public int CurrentHealth { get; private set; }
        public bool IsInvulnerable => Time.time < _invulnUntil;
        public bool IsDead => CurrentHealth <= 0;

        private float _invulnUntil;
        private CharacterController _cc;
        private Vector3 _knockVel;
        private float _knockTimer;

        private void Awake()
        {
            CurrentHealth = maxHealth;
            _cc = GetComponent<CharacterController>();
        }

        private void Start()
        {
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        private void Update()
        {
            if (_knockTimer > 0f && _cc != null)
            {
                _cc.Move(_knockVel * Time.deltaTime);
                _knockVel = Vector3.Lerp(_knockVel, Vector3.zero, 6f * Time.deltaTime);
                _knockTimer -= Time.deltaTime;
            }
        }

        // Called by TigerAI when the tiger reaches catch range.
        public void TakeDamage(int amount = 1, Vector3 attackerPos = default)
        {
            if (IsDead || IsInvulnerable) return;

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            _invulnUntil = Time.time + invulnerabilityTime;
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
            OnDamaged?.Invoke();

            Vector3 dir = transform.position - attackerPos;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) dir = -transform.forward;
            dir.Normalize();
            _knockVel = dir * knockbackForce + Vector3.up * knockbackUpward;
            _knockTimer = 0.3f;

            Debug.Log($"[PlayerHealth] Hit! HP {CurrentHealth}/{maxHealth}");

            if (CurrentHealth <= 0)
            {
                OnDeath?.Invoke();
                GameManager.Instance?.TriggerGameOver();
            }
        }

        // Heal (e.g. at alarm activations).
        public void Heal(int amount)
        {
            if (IsDead) return;
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }
    }
}
