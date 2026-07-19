using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class AnimalInstance : MonoBehaviour, IPointerClickHandler
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
        AnimalResources.EnsureBoxCollider(gameObject);
    }

    private void OnDisable()
    {
        Active.Remove(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Captured || GameManager.Instance == null) return;

        GameManager.Instance.NearbyAnimal = this;
        GameManager.Instance.OpenCapture();
    }
}
