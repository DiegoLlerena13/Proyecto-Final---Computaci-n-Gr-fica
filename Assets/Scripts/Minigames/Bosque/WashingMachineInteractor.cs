using UnityEngine;

namespace BosqueEscape
{
    public class WashingMachineInteractor : MonoBehaviour
    {
        [HideInInspector] public string stationName = "Estacion";

        public bool IsActivated { get; private set; }

        [Header("Audio")]
        public AudioClip activateSound;

        private AudioSource _audio;
        private Light _indicator;

        private void Awake()
        {
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.spatialBlend = 1f;
            _audio.maxDistance = 10f;

            // Small red indicator light on top - turns green when activated
            GameObject lightGO = new GameObject("Indicator");
            lightGO.transform.SetParent(transform);
            lightGO.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            _indicator = lightGO.AddComponent<Light>();
            _indicator.type = LightType.Point;
            _indicator.color = Color.red;
            _indicator.intensity = 0.8f;
            _indicator.range = 2.5f;
        }

        public void Activate()
        {
            if (IsActivated) return;
            IsActivated = true;

            if (activateSound != null) _audio.PlayOneShot(activateSound);

            _indicator.color = Color.green;
            _indicator.intensity = 2f;

            AudioManager.Instance?.PlayAlarm();

            if (GameManager.Instance != null)
                GameManager.Instance.RegisterAlarm();
            else
                Debug.LogWarning("[WashingMachine] GameManager not found in scene.");
        }
    }
}
