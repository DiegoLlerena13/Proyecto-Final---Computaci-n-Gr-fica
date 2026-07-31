using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class EcoDexData
{
    public string nombre;
    public string nombreCientifico;

    [TextArea(2, 4)]
    public string habitat;

    [TextArea(2, 4)]
    public string alimentacion;

    [TextArea(2, 4)]
    public string estado;

    [TextArea(2, 4)]
    public string amenazas;

    [TextArea(2, 4)]
    public string datoCurioso;

    [Header("Imagen del animal")]
    public Sprite imagenAnimal;
}

public class EcoDexManager : MonoBehaviour
{
    [Header("Datos de animales")]
    public EcoDexData[] animales;

    [Header("Imagen UI")]
    public Image imgAnimalGrande;

    [Header("Textos UI")]
    public TextMeshProUGUI txtNombre;
    public TextMeshProUGUI txtNombreCientifico;
    public TextMeshProUGUI txtHabitat;
    public TextMeshProUGUI txtAlimentacion;
    public TextMeshProUGUI txtEstado;
    public TextMeshProUGUI txtAmenazas;
    public TextMeshProUGUI txtDatoCurioso;

    void Start()
    {
        MostrarAnimal(0);
    }

    public void MostrarAnimal(int indice)
    {
        if (animales == null || animales.Length == 0)
        {
            Debug.LogWarning("No hay animales registrados en la EcoDex.");
            return;
        }

        if (indice < 0 || indice >= animales.Length)
        {
            Debug.LogWarning("Índice fuera de rango: " + indice);
            return;
        }

        EcoDexData animal = animales[indice];

        if (txtNombre != null)
            txtNombre.text = animal.nombre;

        if (txtNombreCientifico != null)
            txtNombreCientifico.text = "Nombre científico: " + animal.nombreCientifico;

        if (txtHabitat != null)
            txtHabitat.text = "Hábitat: " + animal.habitat;

        if (txtAlimentacion != null)
            txtAlimentacion.text = "Alimentación: " + animal.alimentacion;

        if (txtEstado != null)
            txtEstado.text = "Estado: " + animal.estado;

        if (txtAmenazas != null)
            txtAmenazas.text = "Amenazas: " + animal.amenazas;

        if (txtDatoCurioso != null)
            txtDatoCurioso.text = "Dato curioso: " + animal.datoCurioso;

        if (imgAnimalGrande != null)
        {
            imgAnimalGrande.sprite = animal.imagenAnimal;
            imgAnimalGrande.enabled = animal.imagenAnimal != null;
            imgAnimalGrande.preserveAspect = true;
        }
    }
}