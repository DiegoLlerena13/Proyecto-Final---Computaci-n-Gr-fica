using UnityEngine;
using UnityEngine.Events;

namespace BosqueEscape
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public int totalAlarmsNeeded = 5;

        public UnityEvent<int> OnAlarmActivated;
        public UnityEvent OnGameWin;
        public UnityEvent OnGameOver;

        private int _alarmsActivated;
        private bool _gameActive = true;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void RegisterAlarm()
        {
            if (!_gameActive) return;
            _alarmsActivated++;
            Debug.Log($"[GameManager] Alarm {_alarmsActivated}/{totalAlarmsNeeded}");
            OnAlarmActivated?.Invoke(_alarmsActivated);
            if (_alarmsActivated >= totalAlarmsNeeded)
                OnGameWin?.Invoke();
        }

        public void TriggerGameOver()
        {
            if (!_gameActive) return;
            _gameActive = false;
            Debug.Log("[GameManager] Game Over - deer was caught.");
            OnGameOver?.Invoke();
        }

        public int GetAlarmCount() => _alarmsActivated;
        public bool IsGameActive() => _gameActive;
    }
}
