using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public enum UIState { World, ARCapture, Menagerie, PetCare, Minigame }

    public static GameManager Instance { get; private set; }

    public UIState CurrentState { get; private set; } = UIState.World;
    public readonly List<CapturedAnimalRecord> CapturedAnimals = new();
    public AnimalInstance NearbyAnimal { get; set; }

    [SerializeField] private GameObject worldPanel;
    [SerializeField] private GameObject arCapturePanel;
    [SerializeField] private GameObject menageriePanel;
    [SerializeField] private GameObject petCarePanel;
    [SerializeField] private GameObject minigamePanel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        SetState(UIState.World);
    }

    public void SetState(UIState state)
    {
        CurrentState = state;
        if (worldPanel != null) worldPanel.SetActive(state == UIState.World);
        if (arCapturePanel != null) arCapturePanel.SetActive(state == UIState.ARCapture);
        if (menageriePanel != null) menageriePanel.SetActive(state == UIState.Menagerie);
        if (petCarePanel != null) petCarePanel.SetActive(state == UIState.PetCare);
        if (minigamePanel != null) minigamePanel.SetActive(state == UIState.Minigame);
    }

    public void OpenCapture() => SetState(UIState.ARCapture);
    public void OpenMenagerie() => SetState(UIState.Menagerie);
    public void OpenMinigame() => SetState(UIState.Minigame);
    public void OpenPetCare() => SetState(UIState.PetCare);

    public void OnCaptureSucceeded(AnimalSpecies species, string ledgerRecordId = null, Sprite icon = null)
    {
        CapturedAnimals.Add(new CapturedAnimalRecord(species, ledgerRecordId, icon));

        if (NearbyAnimal != null && NearbyAnimal.Species == species)
        {
            NearbyAnimal.Captured = true;
            NearbyAnimal.gameObject.SetActive(false);
            NearbyAnimal = null;
        }

        SetState(UIState.World);
    }
}
