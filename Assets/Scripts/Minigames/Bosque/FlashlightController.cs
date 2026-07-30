using UnityEngine;

namespace BosqueEscape
{
    // Dynamic flashlight: flickers frantically when the deer is running.
    // Intensity drops as the deer takes damage (panic). Brightens when alarms activate.
    [RequireComponent(typeof(Light))]
    public class FlashlightController : MonoBehaviour
    {
        [Header("Tuning")]
        public float baseIntensity = 8f;
        public float runFlickerSpeed = 22f;
        public float runFlickerAmount = 0.45f;
        public float damageDimDuration = 0.5f;
        public float damageDimAmount = 0.5f;
        public float alarmBoostPerStep = 0.6f;

        private Light _light;
        private DeerController _deer;
        private PlayerHealth _health;
        private float _alarmBoost;
        private float _damageDimT;
        private float _flickerSeed;

        private void Awake()
        {
            _light = GetComponent<Light>();
            _flickerSeed = Random.Range(0f, 1000f);
        }

        private void Start()
        {
            var deerGO = GameObject.FindGameObjectWithTag("Player");
            if (deerGO != null)
            {
                _deer = deerGO.GetComponent<DeerController>();
                _health = deerGO.GetComponent<PlayerHealth>();
                if (_health != null) _health.OnDamaged?.AddListener(OnDamaged);
            }
            if (GameManager.Instance != null)
                GameManager.Instance.OnAlarmActivated.AddListener(c => _alarmBoost = c * alarmBoostPerStep);
        }

        private void OnDamaged() => _damageDimT = damageDimDuration;

        private void Update()
        {
            float intensity = baseIntensity + _alarmBoost;

            // Damage dim
            if (_damageDimT > 0f)
            {
                float k = _damageDimT / damageDimDuration;
                intensity *= (1f - damageDimAmount * k);
                _damageDimT -= Time.deltaTime;
            }

            // Health-based panic dim (low HP -> weaker flashlight)
            if (_health != null)
            {
                float hpRatio = (float)_health.CurrentHealth / Mathf.Max(1, _health.maxHealth);
                intensity *= Mathf.Lerp(0.5f, 1f, hpRatio);
            }

            // Running flicker
            if (_deer != null && _deer.IsRunning)
            {
                float n = Mathf.PerlinNoise(Time.time * runFlickerSpeed, _flickerSeed);
                intensity *= (1f - runFlickerAmount * n);
            }

            _light.intensity = intensity;
        }
    }
}
