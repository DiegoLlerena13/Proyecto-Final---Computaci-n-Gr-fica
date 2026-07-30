using System.Collections;
using UnityEngine;

namespace BosqueEscape
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Ambient")]
        public AudioSource ambientSource;
        public AudioClip forestAmbient;
        public AudioClip stormAmbient;

        [Header("SFX")]
        public AudioSource sfxSource;
        public AudioClip alarmActivate;
        public AudioClip doorOpen;
        public AudioClip victory;
        public AudioClip gameOver;

        [Header("Crossfade")]
        public float crossfadeDuration = 2f;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;

            AudioSource[] sources = GetComponents<AudioSource>();
            if (ambientSource == null) ambientSource = sources.Length > 0 ? sources[0] : gameObject.AddComponent<AudioSource>();
            if (sfxSource == null) sfxSource = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();

            ambientSource.loop = true;
            ambientSource.spatialBlend = 0f;
            ambientSource.volume = 0.4f;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.volume = 0.8f;
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameWin.AddListener(PlayVictory);
                GameManager.Instance.OnGameOver.AddListener(PlayGameOver);
            }

            PlayForestAmbient();
        }

        // -- Ambient --

        public void PlayForestAmbient() => CrossfadeTo(forestAmbient);
        public void PlayStormAmbient() => CrossfadeTo(stormAmbient);

        private void CrossfadeTo(AudioClip clip)
        {
            if (clip == null || ambientSource == null) return;
            if (ambientSource.clip == clip) return;
            StopAllCoroutines();
            StartCoroutine(Crossfade(clip));
        }

        private IEnumerator Crossfade(AudioClip clip)
        {
            float startVol = ambientSource.volume;
            float t = 0f;

            while (t < crossfadeDuration * 0.5f)
            {
                t += Time.deltaTime;
                ambientSource.volume = Mathf.Lerp(startVol, 0f, t / (crossfadeDuration * 0.5f));
                yield return null;
            }

            ambientSource.clip = clip;
            ambientSource.Play();
            t = 0f;

            while (t < crossfadeDuration * 0.5f)
            {
                t += Time.deltaTime;
                ambientSource.volume = Mathf.Lerp(0f, startVol, t / (crossfadeDuration * 0.5f));
                yield return null;
            }

            ambientSource.volume = startVol;
        }

        // -- SFX --

        public void PlayAlarm() => Play(alarmActivate);
        public void PlayDoorOpen() => Play(doorOpen);
        public void PlayVictory() => Play(victory);
        public void PlayGameOver() => Play(gameOver);

        private void Play(AudioClip clip)
        {
            if (clip != null && sfxSource != null)
                sfxSource.PlayOneShot(clip);
        }
    }
}
