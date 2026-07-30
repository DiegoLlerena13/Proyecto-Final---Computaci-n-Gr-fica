using UnityEngine;

namespace BosqueEscape
{
    // Listens to GameManager.OnAlarmActivated and progressively brightens the world.
    // At 0 alarms = pitch black. At 5 alarms = full daylight (victory).
    public class DynamicAtmosphere : MonoBehaviour
    {
        [Header("Ambient Light (per alarm step 0..5)")]
        public Color[] ambientSteps = new Color[]
        {
            new Color(0.00f, 0.00f, 0.00f), // 0 alarms - pitch black
            new Color(0.05f, 0.05f, 0.08f), // 1
            new Color(0.10f, 0.12f, 0.15f), // 2
            new Color(0.18f, 0.22f, 0.25f), // 3
            new Color(0.30f, 0.35f, 0.38f), // 4
            new Color(0.55f, 0.60f, 0.65f), // 5 - bright
        };

        [Header("Sun")]
        public Light directionalSun;
        public float[] sunIntensitySteps = { 0f, 0.05f, 0.2f, 0.5f, 0.9f, 1.4f };

        [Header("Transition")]
        public float transitionDuration = 2.5f;

        private int _currentStep;
        private float _t = 1f;
        private Color _fromAmbient, _toAmbient;
        private float _fromSun, _toSun;

        private void Start()
        {
            if (directionalSun == null)
            {
                var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
                foreach (var l in lights) if (l.type == LightType.Directional) { directionalSun = l; break; }
            }

            ApplyStep(0, instant: true);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnAlarmActivated.AddListener(OnAlarm);
                GameManager.Instance.OnGameWin.AddListener(() => GoToStep(ambientSteps.Length - 1));
            }
        }

        private void OnAlarm(int count) => GoToStep(Mathf.Clamp(count, 0, ambientSteps.Length - 1));

        private void GoToStep(int step)
        {
            if (step == _currentStep) return;
            _fromAmbient = RenderSettings.ambientLight;
            _toAmbient = ambientSteps[step];
            _fromSun = directionalSun != null ? directionalSun.intensity : 0f;
            _toSun = step < sunIntensitySteps.Length ? sunIntensitySteps[step] : 1f;
            _currentStep = step;
            _t = 0f;
            if (directionalSun != null) directionalSun.enabled = step > 0;
        }

        private void Update()
        {
            if (_t >= 1f) return;
            _t += Time.deltaTime / transitionDuration;
            float k = Mathf.Clamp01(_t);
            RenderSettings.ambientLight = Color.Lerp(_fromAmbient, _toAmbient, k);
            if (directionalSun != null) directionalSun.intensity = Mathf.Lerp(_fromSun, _toSun, k);
        }

        private void ApplyStep(int step, bool instant)
        {
            _currentStep = step;
            RenderSettings.ambientLight = ambientSteps[step];
            if (directionalSun != null)
            {
                directionalSun.intensity = sunIntensitySteps[step];
                directionalSun.enabled = step > 0;
            }
            _t = 1f;
        }
    }
}
