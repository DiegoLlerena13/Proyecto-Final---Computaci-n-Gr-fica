using System.Collections;
using UnityEngine;

namespace BosqueEscape
{
    public class LightningFlash : MonoBehaviour
    {
        [Header("Timing")]
        public float minInterval = 8f;
        public float maxInterval = 25f;

        [Header("Flash")]
        public Light directionalLight;
        public float flashIntensity = 6f;
        public float flashDuration = 0.05f;
        public float fadeOutDuration = 0.35f;

        [Header("Thunder")]
        public AudioSource audioSource;
        public AudioClip thunderClip;
        public float thunderDelay = 0.6f;

        private float _baseIntensity;

        private void Awake()
        {
            if (directionalLight == null)
                directionalLight = FindFirstObjectByType<Light>();

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            _baseIntensity = directionalLight != null ? directionalLight.intensity : 1f;
            StartCoroutine(Loop());
        }

        private IEnumerator Loop()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
                yield return StartCoroutine(Flash());
            }
        }

        private IEnumerator Flash()
        {
            if (directionalLight == null) yield break;

            directionalLight.intensity = flashIntensity;
            yield return new WaitForSeconds(flashDuration);

            directionalLight.intensity = _baseIntensity;
            yield return new WaitForSeconds(0.06f);
            directionalLight.intensity = flashIntensity * 0.75f;
            yield return new WaitForSeconds(flashDuration);

            float t = 0f;
            float startI = flashIntensity * 0.75f;
            while (t < fadeOutDuration)
            {
                t += Time.deltaTime;
                directionalLight.intensity = Mathf.Lerp(startI, _baseIntensity, t / fadeOutDuration);
                yield return null;
            }
            directionalLight.intensity = _baseIntensity;

            if (audioSource != null && thunderClip != null)
            {
                yield return new WaitForSeconds(thunderDelay);
                audioSource.PlayOneShot(thunderClip);
            }
        }
    }
}
