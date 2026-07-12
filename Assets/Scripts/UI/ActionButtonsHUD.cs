using UnityEngine;
using UnityEngine.UI;

public class ActionButtonsHUD : MonoBehaviour
{
    [SerializeField] private Button captureButton;
    [SerializeField] private Button minigameButton;
    [SerializeField] private Button playButton;

    private void Start()
    {
        if (captureButton != null) captureButton.onClick.AddListener(OnCaptureClicked);
        if (minigameButton != null) minigameButton.onClick.AddListener(OnMinigameClicked);
        if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);
    }

    private void Update()
    {
        bool hasNearbyAnimal = GameManager.Instance != null && GameManager.Instance.NearbyAnimal != null;
        if (minigameButton != null) minigameButton.interactable = hasNearbyAnimal;
        if (playButton != null) playButton.interactable = hasNearbyAnimal;
    }

    private void OnCaptureClicked() => GameManager.Instance?.OpenCapture();
    private void OnMinigameClicked() => GameManager.Instance?.OpenMinigame();

    private void OnPlayClicked()
    {
        if (GameManager.Instance == null || GameManager.Instance.NearbyAnimal == null) return;
        GameManager.Instance.OpenPetCare();
    }
}
