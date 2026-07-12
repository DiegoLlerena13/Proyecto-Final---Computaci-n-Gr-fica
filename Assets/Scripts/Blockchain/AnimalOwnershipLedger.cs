using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Local, simulated ownership record for captured animals - not a real blockchain
// (no network, no wallet, no consensus). Each record hashes the previous record's
// hash together with its own data so tampering with the local file is detectable.
public class AnimalOwnershipLedger
{
    private const string GenesisHash = "GENESIS";
    private static readonly string FilePath = Path.Combine(Application.persistentDataPath, "ownership_ledger.json");

    [Serializable]
    private class LedgerFile
    {
        public List<LedgerRecord> Records = new();
    }

    private readonly LedgerFile file;

    public AnimalOwnershipLedger()
    {
        file = Load();
    }

    public IReadOnlyList<LedgerRecord> Records => file.Records;

    public string AddCapture(AnimalSpecies species)
    {
        string previousHash = file.Records.Count > 0 ? file.Records[^1].Hash : GenesisHash;

        var record = new LedgerRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            Species = species.ToString(),
            TimestampUtc = DateTime.UtcNow.ToString("o"),
            PreviousHash = previousHash
        };
        record.Hash = HashUtility.Sha256(record.PreviousHash + record.Species + record.TimestampUtc + record.Id);

        file.Records.Add(record);
        Save();

        return record.Id;
    }

    public bool VerifyChain()
    {
        string expectedPrevious = GenesisHash;
        foreach (var record in file.Records)
        {
            if (record.PreviousHash != expectedPrevious)
                return false;

            string expectedHash = HashUtility.Sha256(record.PreviousHash + record.Species + record.TimestampUtc + record.Id);
            if (record.Hash != expectedHash)
                return false;

            expectedPrevious = record.Hash;
        }
        return true;
    }

    private static LedgerFile Load()
    {
        if (!File.Exists(FilePath))
            return new LedgerFile();

        try
        {
            string json = File.ReadAllText(FilePath);
            return JsonUtility.FromJson<LedgerFile>(json) ?? new LedgerFile();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AnimalOwnershipLedger] Failed to load ledger, starting fresh: {e.Message}");
            return new LedgerFile();
        }
    }

    private void Save()
    {
        File.WriteAllText(FilePath, JsonUtility.ToJson(file, true));
    }
}
