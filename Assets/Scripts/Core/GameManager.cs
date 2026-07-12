using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public enum UIState { World, ARCapture, Menagerie, PetCare, Minigame }

    private const string MinigameSceneName = "TapMinigame";

    public static GameManager Instance { get; private set; }

    public UIState CurrentState { get; private set; } = UIState.World;
    public readonly List<CapturedAnimalRecord> CapturedAnimals = new();
    public AnimalInstance NearbyAnimal { get; set; }
    public AnimalSpecies PetCareSpecies { get; private set; }

    [SerializeField] private GameObject worldPanel;
    [SerializeField] private GameObject arCapturePanel;
    [SerializeField] private GameObject menageriePanel;
    [SerializeField] private GameObject petCarePanel;
    [SerializeField] private GameObject minigamePanel;

    private AnimalOwnershipLedger ledger;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        ledger = new AnimalOwnershipLedger();
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
    public void ReturnToWorld() => SetState(UIState.World);

    public void OpenPetCare(AnimalSpecies species)
    {
        PetCareSpecies = species;
        SetState(UIState.PetCare);
    }

    public void OpenPetCare()
    {
        if (NearbyAnimal == null) return;
        OpenPetCare(NearbyAnimal.Species);
    }

    public void OpenMinigame()
    {
        SetState(UIState.Minigame);
        SceneManager.LoadScene(MinigameSceneName, LoadSceneMode.Additive);
    }

    public void OnMinigameFinished(int score)
    {
        SceneManager.UnloadSceneAsync(MinigameSceneName);
        SetState(UIState.World);
    }

    public void OnCaptureSucceeded(AnimalSpecies species, Sprite icon = null)
    {
        string ledgerRecordId = ledger.AddCapture(species);
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
