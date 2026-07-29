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
    public CapturedAnimalRecord PetCareRecord { get; private set; }

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

    // GameManager is DontDestroyOnLoad, but its panel fields above are Inspector-wired to
    // whichever 00.Mapa scene instance loaded FIRST. Every subsequent SceneManager.LoadScene
    // ("00.Mapa" from BottomNav's Mapa button) creates a fresh set of panel GameObjects and a
    // fresh GameManager that immediately self-destructs (see the singleton guard in Awake),
    // taking its would-be panel references with it - leaving this surviving instance pointing at
    // panels that no longer exist. MapaPanelRegistrar (on 00.Mapa's UI GameObject) calls this in
    // its own Awake() every time the scene loads, re-pointing these fields at the current panels.
    public void RebindMapaPanels(GameObject world, GameObject arCapture, GameObject menagerie, GameObject petCare, GameObject minigame)
    {
        worldPanel = world;
        arCapturePanel = arCapture;
        menageriePanel = menagerie;
        petCarePanel = petCare;
        minigamePanel = minigame;
        SetState(CurrentState);
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
        PetCareRecord = null;
        SetState(UIState.PetCare);
    }

    public void OpenPetCare()
    {
        if (NearbyAnimal == null) return;
        OpenPetCare(NearbyAnimal.Species);
    }

    // Used by MenagerieUI/MascotaSelector to open a specific owned capture (as opposed to the
    // species-only overloads above, which are for the not-yet-captured "nearby wild animal" flow
    // and can't distinguish between multiple captures of the same species).
    public void OpenPetCare(CapturedAnimalRecord record)
    {
        PetCareRecord = record;
        PetCareSpecies = record != null ? record.Species : PetCareSpecies;
        SetState(UIState.PetCare);
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
        SetState(UIState.World);
    }

    // Preferred entry point for GestureCaptureController/CaptureQTEController: operates on the
    // exact AnimalInstance the capture minigame targeted, cached by the caller at spawn time -
    // NOT whatever NearbyAnimal currently points to. AnimalProximityDetector reassigns
    // NearbyAnimal every frame based on live distance to the player, and keeps running the whole
    // multi-second capture minigame (nothing pauses it), so by the time the minigame finishes
    // NearbyAnimal may already point at a completely different, closer animal. Recording
    // NearbyAnimal.Species at that point (the previous behavior) captured whatever species
    // happened to be nearest at the end, not the one actually shown/gestured at.
    public void OnCaptureSucceeded(AnimalInstance capturedAnimal, Sprite icon = null)
    {
        if (capturedAnimal == null) return;

        OnCaptureSucceeded(capturedAnimal.Species, icon);

        capturedAnimal.Captured = true;
        capturedAnimal.gameObject.SetActive(false);
        if (NearbyAnimal == capturedAnimal) NearbyAnimal = null;
    }
}
