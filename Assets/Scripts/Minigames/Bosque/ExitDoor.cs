using System.Collections;
using UnityEngine;

namespace BosqueEscape
{
    public class ExitDoor : MonoBehaviour
    {
        [Header("Door panels")]
        public Transform[] doorPanels;

        [Header("Animation")]
        public float openAngle = 90f;
        public float openDuration = 1.8f;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip doorOpenClip;

        [Header("Exit trigger (isTrigger BoxCollider child)")]
        public BoxCollider exitTrigger;

        private bool _open;

        private void Start()
        {
            if (exitTrigger != null) exitTrigger.enabled = false;

            if (GameManager.Instance != null)
                GameManager.Instance.OnGameWin.AddListener(OpenDoor);
            else
                Debug.LogWarning("[ExitDoor] GameManager not found - OnGameWin won't fire.");
        }

        [ContextMenu("Open Door (test)")]
        public void OpenDoor()
        {
            if (_open) return;
            _open = true;
            StartCoroutine(AnimateDoor());
            Debug.Log("[ExitDoor] Opening door - escape route activated!");
        }

        private IEnumerator AnimateDoor()
        {
            if (audioSource != null && doorOpenClip != null)
                audioSource.PlayOneShot(doorOpenClip);

            if (doorPanels == null || doorPanels.Length == 0) yield break;

            Quaternion[] startRots = new Quaternion[doorPanels.Length];
            Quaternion[] targetRots = new Quaternion[doorPanels.Length];

            for (int i = 0; i < doorPanels.Length; i++)
            {
                startRots[i] = doorPanels[i].localRotation;
                float dir = (i % 2 == 0) ? 1f : -1f;
                targetRots[i] = startRots[i] * Quaternion.Euler(0f, openAngle * dir, 0f);
            }

            float elapsed = 0f;
            while (elapsed < openDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / openDuration);
                for (int i = 0; i < doorPanels.Length; i++)
                    doorPanels[i].localRotation = Quaternion.Slerp(startRots[i], targetRots[i], t);
                yield return null;
            }

            if (exitTrigger != null) exitTrigger.enabled = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_open || !other.CompareTag("Player")) return;
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameWin.Invoke();
        }
    }
}
