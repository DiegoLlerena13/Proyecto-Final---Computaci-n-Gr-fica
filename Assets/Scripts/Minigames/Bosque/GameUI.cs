using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace BosqueEscape
{
    public class GameUI : MonoBehaviour
    {
        [Header("HUD")]
        public TextMeshProUGUI alarmText;
        public GameObject interactionPrompt; // "Activar alarma"

        [Header("End screen (shared by game over / victory, like Gallito's endPanel)")]
        public GameObject endScreen;
        public TextMeshProUGUI endMessageText;

        [Header("Buttons")]
        public Button restartButton;
        public Button backButton;

        private InteractionController _interact;

        private void Start()
        {
            SetActive(endScreen, false);
            SetActive(interactionPrompt, false);

            UpdateAlarm(0);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnAlarmActivated.AddListener(UpdateAlarm);
                GameManager.Instance.OnGameOver.AddListener(ShowGameOver);
                GameManager.Instance.OnGameWin.AddListener(ShowVictory);
            }

            if (restartButton != null)
                restartButton.onClick.AddListener(Restart);
            if (backButton != null)
                backButton.onClick.AddListener(BackToMenu);

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                _interact = player.GetComponent<InteractionController>();
        }

        private void Update()
        {
            SetActive(interactionPrompt, CheckNearWM());
        }

        // Wired to the on-screen Interact button's OnClick - InteractionController lives on the
        // Player prefab instance, so it's looked up once here instead of referenced directly
        // (a scene Button can't cleanly reference a component inside a prefab instance).
        public void OnInteractPressed()
        {
            _interact?.Interact();
        }

        // -- Helpers --

        private bool CheckNearWM()
        {
            if (_interact == null) return false;
            Vector3 origin = _interact.transform.position + Vector3.up;
            if (!Physics.Raycast(origin, _interact.transform.forward, out RaycastHit hit, 3f)) return false;
            var wm = hit.collider.GetComponentInParent<WashingMachineInteractor>();
            return wm != null && !wm.IsActivated;
        }

        private void UpdateAlarm(int count)
        {
            if (alarmText == null) return;
            int needed = GameManager.Instance != null ? GameManager.Instance.totalAlarmsNeeded : 5;
            alarmText.text = $"Alarmas: {count} / {needed}";
        }

        private void ShowGameOver()
        {
            if (endMessageText != null) endMessageText.text = "El ciervo fue atrapado...";
            SetActive(endScreen, true);
            Time.timeScale = 0f;
        }

        private void ShowVictory()
        {
            if (endMessageText != null) endMessageText.text = "Escapaste de la zona!";
            SetActive(endScreen, true);
            Time.timeScale = 0f;
        }

        private void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void BackToMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("03.Juegos");
        }

        private static void SetActive(GameObject go, bool on) { if (go != null) go.SetActive(on); }
    }
}
