using UnityEngine;
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
}

public class EcoDexManager : MonoBehaviour
{
    [Header("Datos de animales")]
    public EcoDexData[] animales;

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
            Debug.LogWarning("Índice de animal fuera de rango: " + indice);
            return;
        }

        if (txtNombre == null ||
            txtNombreCientifico == null ||
            txtHabitat == null ||
            txtAlimentacion == null ||
            txtEstado == null ||
            txtAmenazas == null ||
            txtDatoCurioso == null)
        {
            Debug.LogError("Falta asignar uno o más textos en EcoDexManager.");
            return;
        }

        EcoDexData animal = animales[indice];

        txtNombre.text = animal.nombre;
        txtNombreCientifico.text = "Nombre científico: " + animal.nombreCientifico;
        txtHabitat.text = "Hábitat: " + animal.habitat;
        txtAlimentacion.text = "Alimentación: " + animal.alimentacion;
        txtEstado.text = "Estado: " + animal.estado;
        txtAmenazas.text = "Amenazas: " + animal.amenazas;
        txtDatoCurioso.text = "Dato curioso: " + animal.datoCurioso;
    }
}