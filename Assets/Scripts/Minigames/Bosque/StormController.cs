using UnityEngine;

namespace BosqueEscape
{
    // Drives the storm ambient: fades it in once the tiger activates (first alarm),
    // and cross-fades the forest ambient out so they don't overlap.
    public class StormController : MonoBehaviour
    {
        [Header("Volume targets per alarm step (0..5)")]
        public float[] stormVolumePerAlarm = { 0.00f, 0.25f, 0.40f, 0.55f, 0.70f, 0.85f };
        public float[] forestVolumePerAlarm = { 0.40f, 0.25f, 0.12f, 0.05f, 0.00f, 0.00f };

        [Header("Fade")]
        public float fadeSpeed = 0.4f;

        private AudioSource _stormAmbient;
        private AudioSource _forestAmbient;
        private float _targetStorm;
        private float _targetForest;

        private void Start()
        {
            // The storm ambient is the second AudioSource on this GameObject
            var sources = GetComponents<AudioSource>();
            foreach (var s in sources)
            {
                if (s.clip != null && s.clip.name.ToLower().Contains("storm")) _stormAmbient = s;
            }

            if (AudioManager.Instance != null) _forestAmbient = AudioManager.Instance.ambientSource;

            _targetStorm = stormVolumePerAlarm[0];
            _targetForest = forestVolumePerAlarm[0];

            if (_stormAmbient != null)
            {
                _stormAmbient.volume = _targetStorm;
                if (_targetStorm > 0.01f && !_stormAmbient.isPlaying) _stormAmbient.Play();
            }
            if (_forestAmbient != null)
            {
                _forestAmbient.volume = _targetForest;
                if (_targetForest > 0.01f && !_forestAmbient.isPlaying) _forestAmbient.Play();
            }

            if (GameManager.Instance != null)
                GameManager.Instance.OnAlarmActivated.AddListener(OnAlarm);
        }

        private void OnAlarm(int count)
        {
            int idx = Mathf.Clamp(count, 0, stormVolumePerAlarm.Length - 1);
            _targetStorm = stormVolumePerAlarm[idx];
            _targetForest = forestVolumePerAlarm[idx];
            if (_stormAmbient != null && _targetStorm > 0.01f && !_stormAmbient.isPlaying)
                _stormAmbient.Play();
        }

        private void Update()
        {
            if (_stormAmbient != null)
                _stormAmbient.volume = Mathf.MoveTowards(_stormAmbient.volume, _targetStorm, fadeSpeed * Time.deltaTime);
            if (_forestAmbient != null)
                _forestAmbient.volume = Mathf.MoveTowards(_forestAmbient.volume, _targetForest, fadeSpeed * Time.deltaTime);
        }
    }
}
