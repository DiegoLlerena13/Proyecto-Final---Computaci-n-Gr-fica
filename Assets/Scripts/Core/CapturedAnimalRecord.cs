using UnityEngine;

[System.Serializable]
public class CapturedAnimalRecord
{
    public AnimalSpecies Species;
    public string LedgerRecordId;
    public Sprite Icon;
    public int Hunger = 50;
    public int Happiness = 50;

    public CapturedAnimalRecord(AnimalSpecies species, string ledgerRecordId, Sprite icon)
    {
        Species = species;
        LedgerRecordId = ledgerRecordId;
        Icon = icon;
    }
}
