// Que acciones puede recibir cada especie. El juego es educativo (ensena sobre animales a chicos),
// asi que las especies peligrosas quedan fuera de "acariciar" - alimentar y minijuegos siguen
// disponibles para todas.
public static class AnimalActionRules
{
    public static bool CanBePetByHand(AnimalSpecies species)
    {
        switch (species)
        {
            case AnimalSpecies.SerpienteAzul:
            case AnimalSpecies.SerpienteBlanca:
            case AnimalSpecies.SerpienteCorn:
            case AnimalSpecies.SerpienteMarron:
            case AnimalSpecies.SerpienteRoja:
            case AnimalSpecies.SerpienteVerde:
                return false;
            default:
                return true;
        }
    }
}
