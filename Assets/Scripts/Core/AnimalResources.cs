using UnityEngine;

public static class AnimalResources
{
    public static bool Is2D(AnimalSpecies species)
    {
        switch (species)
        {
            case AnimalSpecies.Chicken:
            case AnimalSpecies.Deer:
            case AnimalSpecies.Dog:
            case AnimalSpecies.Horse:
            case AnimalSpecies.Kitty:
            case AnimalSpecies.Pinguin:
            case AnimalSpecies.Tiger:
                return false;
            default:
                return true;
        }
    }

    public static GameObject Load(AnimalSpecies species)
    {
        string folder = Is2D(species) ? "Fauna" : "Animals";
        return Resources.Load<GameObject>($"{folder}/{species}");
    }
}
