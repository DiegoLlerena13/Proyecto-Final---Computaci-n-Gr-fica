using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MascotaSelector : MonoBehaviour
{
    [Header("Textos UI")]
    public TextMeshProUGUI txtNombre;
    public TextMeshProUGUI txtEstado;
    public TextMeshProUGUI txtDescripcion;

    [Header("Vista 3D")]
    public Transform puntoSpawn;
    public Vector3 escalaAnimal = new Vector3(2f, 2f, 2f);

    private const int AccionMonto = 15;
    private const string EscenaMinijuegoPorDefecto = "03.Juegos";

    // Solo Conejo tiene minijuego propio armado hasta ahora; el resto cae al hub de Juegos.
    private static readonly Dictionary<AnimalSpecies, string> EscenasMinijuego = new()
    {
        { AnimalSpecies.Conejo, "04.Minigame_conejo" },
    };

    private List<CapturedAnimalRecord> mascotas;
    private int indiceActual;
    private GameObject animalActual;

    private void OnEnable()
    {
        mascotas = GameManager.Instance != null ? GameManager.Instance.CapturedAnimals : new List<CapturedAnimalRecord>();

        indiceActual = 0;
        var preferido = GameManager.Instance != null ? GameManager.Instance.PetCareRecord : null;
        if (preferido != null)
        {
            int idx = mascotas.IndexOf(preferido);
            if (idx >= 0) indiceActual = idx;
        }

        MostrarMascota();
    }

    public void Siguiente()
    {
        if (mascotas == null || mascotas.Count == 0) return;
        indiceActual = (indiceActual + 1) % mascotas.Count;
        MostrarMascota();
    }

    public void Anterior()
    {
        if (mascotas == null || mascotas.Count == 0) return;
        indiceActual = (indiceActual - 1 + mascotas.Count) % mascotas.Count;
        MostrarMascota();
    }

    public void Alimentar()
    {
        var record = MascotaActual();
        if (record == null) return;

        record.Hunger = Mathf.Min(100, record.Hunger + AccionMonto);
        ActualizarTextoEstado(record);
        txtDescripcion.text = "El animal recibió alimento y se siente mejor.";
    }

    public void Acariciar()
    {
        var record = MascotaActual();
        if (record == null) return;

        record.Happiness = Mathf.Min(100, record.Happiness + AccionMonto);
        ActualizarTextoEstado(record);
        txtDescripcion.text = "El animal está tranquilo y confía más en el cuidador.";
    }

    public void AbrirMinijuego()
    {
        var record = MascotaActual();
        if (record == null) return;

        string escena = EscenasMinijuego.TryGetValue(record.Species, out var nombre) ? nombre : EscenaMinijuegoPorDefecto;
        SceneManager.LoadScene(escena);
    }

    private CapturedAnimalRecord MascotaActual()
    {
        if (mascotas == null || mascotas.Count == 0) return null;
        return mascotas[indiceActual];
    }

    private void MostrarMascota()
    {
        if (puntoSpawn == null || txtNombre == null || txtEstado == null || txtDescripcion == null)
        {
            Debug.LogError("[MascotaSelector] Falta asignar puntoSpawn/txtNombre/txtEstado/txtDescripcion en el Inspector.");
            return;
        }

        if (animalActual != null)
        {
            Destroy(animalActual);
            animalActual = null;
        }

        var record = MascotaActual();
        if (record == null)
        {
            txtNombre.text = "Sin mascotas";
            txtEstado.text = string.Empty;
            txtDescripcion.text = "Todavía no capturaste ningún animal. Volvé al mapa y capturá uno.";
            return;
        }

        txtNombre.text = record.Species.ToString();
        txtDescripcion.text = $"Tu {record.Species} capturado.";
        ActualizarTextoEstado(record);

        var prefab = AnimalResources.Load(record.Species);
        if (prefab == null)
        {
            Debug.LogError($"[MascotaSelector] No se encontró el prefab para {record.Species}.");
            return;
        }

        animalActual = Instantiate(prefab, puntoSpawn.position, puntoSpawn.rotation);
        animalActual.transform.localScale = escalaAnimal;
        PrepararAnimalParaVista(animalActual);
    }

    private void ActualizarTextoEstado(CapturedAnimalRecord record)
    {
        txtEstado.text = $"Hambre: {record.Hunger}%  ·  Felicidad: {record.Happiness}%";
    }

    private void PrepararAnimalParaVista(GameObject animal)
    {
        Rigidbody rb = animal.GetComponentInChildren<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        Collider[] colliders = animal.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
            col.enabled = false;

        MonoBehaviour[] scripts = animal.GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            string nombreScript = script.GetType().Name;
            if (nombreScript == "Controlador" || nombreScript == "ControladorCuidador")
                script.enabled = false;
        }
    }
}
