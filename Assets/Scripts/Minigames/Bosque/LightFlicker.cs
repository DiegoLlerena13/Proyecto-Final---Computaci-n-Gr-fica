using UnityEngine;

namespace BosqueEscape
{
    [RequireComponent(typeof(Light))]
    public class LightFlicker : MonoBehaviour
    {
        public float minIntensity = 0.3f;
        public float maxIntensity = 3f;
        public float flickerSpeed = 12f;

        private Light _light;

        private void Awake() => _light = GetComponent<Light>();

        private void Update()
        {
            _light.intensity = Mathf.Lerp(minIntensity, maxIntensity,
                Mathf.PerlinNoise(Time.time * flickerSpeed, 0f));
        }
    }
}
