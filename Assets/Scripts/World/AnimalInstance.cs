using System.Collections.Generic;
using UnityEngine;

public class AnimalInstance : MonoBehaviour
{
    public static readonly List<AnimalInstance> Active = new();

    public AnimalSpecies Species { get; private set; }
    public bool Captured { get; set; }

    public void Initialize(AnimalSpecies species)
    {
        Species = species;
    }

    private void OnEnable()
    {
        Active.Add(this);
    }

    private void OnDisable()
    {
        Active.Remove(this);
    }
}
