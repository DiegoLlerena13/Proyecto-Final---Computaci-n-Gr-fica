using UnityEngine;

[System.Serializable]
public class CapturedAnimalRecord
{
    public AnimalSpecies Species;
    public string LedgerRecordId;
    public Sprite Icon;

    public CapturedAnimalRecord(AnimalSpecies species, string ledgerRecordId, Sprite icon)
    {
        Species = species;
        LedgerRecordId = ledgerRecordId;
        Icon = icon;
    }
}
