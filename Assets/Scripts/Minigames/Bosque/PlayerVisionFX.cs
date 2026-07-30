using UnityEngine;
using UnityEngine.UI;

namespace BosqueEscape
{
    // Simplified tension overlay: a single red-tinted full-screen Image whose alpha
    // rises as the tiger gets close and pulses on damage. No runtime vignette texture
    // generation (the source project built a radial-gradient Texture2D at runtime) and
    // no GameObject.Find by literal name - references are wired explicitly in the scene,
    // matching this project's convention of serialized references over runtime lookups.
    public class PlayerVisionFX : MonoBehaviour
    {
        [Header("Refs")]
        public Image overlayImage;
        public Transform tigerTransform;
        public Light flashlight;

        [Header("Tiger proximity")]
        public float warningDistance = 18f;
        public float dangerDistance = 7f;
        [Range(0f, 1f)] public float maxProximityAlpha = 0.35f;

        [Header("Damage flash")]
        public float damageFlashDuration = 0.4f;
        [Range(0f, 1f)] public float maxDamageAlpha = 0.55f;

        [Header("Flashlight flicker")]
        public float flickerProbabilityAtMax = 0.5f;

        private PlayerHealth _health;
        private float _damageT;

        private void Start()
        {
            _health = GetComponent<PlayerHealth>();
            if (_health != null) _health.OnDamaged?.AddListener(TriggerDamageFlash);

            // Same GameObject carries the Flashlight - no cross-scene wiring needed.
            if (flashlight == null) flashlight = GetComponentInChildren<Light>();

            // The overlay Image and the Tiger instance both live in the scene, not in this
            // prefab, so they cannot be wired at prefab-authoring time. This scene has exactly
            // one of each (self-contained minigame, not a reusable component), so a one-time
            // lookup by well-known scene object name is a safe, deliberate fallback here.
            if (tigerTransform == null)
            {
                var tiger = GameObject.Find("TigerChaser");
                if (tiger != null) tigerTransform = tiger.transform;
            }
            if (overlayImage == null)
            {
                var overlay = GameObject.Find("VisionOverlay");
                if (overlay != null) overlayImage = overlay.GetComponent<Image>();
            }
        }

        public void TriggerDamageFlash() => _damageT = damageFlashDuration;

        private void Update()
        {
            float proximity = 0f;
            if (tigerTransform != null)
            {
                float d = Vector3.Distance(transform.position, tigerTransform.position);
                proximity = Mathf.InverseLerp(warningDistance, dangerDistance, d);
            }

            if (flashlight != null && proximity > 0.05f)
            {
                float n = Mathf.PerlinNoise(Time.time * 25f, 0.3f);
                float dropChance = proximity * flickerProbabilityAtMax;
                if (n < dropChance) flashlight.intensity *= 0.2f;
            }

            if (_damageT > 0f)
                _damageT = Mathf.Max(0f, _damageT - Time.deltaTime);

            if (overlayImage != null)
            {
                float damageAlpha = (_damageT / damageFlashDuration) * maxDamageAlpha;
                float proximityAlpha = proximity * maxProximityAlpha;
                float alpha = Mathf.Clamp01(Mathf.Max(damageAlpha, proximityAlpha));
                overlayImage.color = Color.Lerp(overlayImage.color, new Color(1f, 0f, 0f, alpha), 8f * Time.deltaTime);
            }
        }
    }
}
