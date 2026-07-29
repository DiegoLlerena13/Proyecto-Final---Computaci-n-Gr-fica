using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MascotaSelector : MonoBehaviour
{
    [Header("Textos UI")]
    public TextMeshProUGUI txtNombre;
    public TextMeshProUGUI txtEstado;
    public TextMeshProUGUI txtDescripcion;

    [Header("Acariciar")]
    public Button btnAcariciar;
    public GameObject panelOpcionesAcariciar;
    public GameObject panelGestoAcariciar;

    [Header("Vista 3D")]
    public Transform puntoSpawn;

    // Cada especie viene con un modelo de tamaño distinto (un conejo y un ciervo no miden lo
    // mismo en Blender). En vez de un Vector3 de escala fijo que quedaba gigante o diminuto según
    // la especie, se normaliza cada animal a esta altura (en unidades de mundo) calculando sus
    // renderer bounds reales - mismo patrón que usa AnimalGroundUtil para el ground-snap.
    public float alturaObjetivoAnimal = 2f;

    private const int AccionMonto = 15;
    private const string EscenaMinijuegoPorDefecto = "03.Juegos";
    private const string MensajeNoAcariciable = "Es peligroso acariciar a este animal - solo se puede observar y alimentar.";

    // Solo Conejo tiene minijuego propio armado hasta ahora; el resto cae al hub de Juegos.
    private static readonly Dictionary<AnimalSpecies, string> EscenasMinijuego = new()
    {
        { AnimalSpecies.Conejo, "04.Minigame_conejo" },
        { AnimalSpecies.GallitoDeLasRocas, "05.Minigame_gallito" },
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

        // Guard defensivo: cubre tanto el tap directo (con btnAcariciar ya deshabilitado) como la
        // llamada que hace AcariciarGestoController.Finish() al completar el gesto - un solo lugar
        // de verdad para la restricción por especie.
        if (!AnimalActionRules.CanBePetByHand(record.Species))
        {
            txtDescripcion.text = MensajeNoAcariciable;
            return;
        }

        record.Happiness = Mathf.Min(100, record.Happiness + AccionMonto);
        ActualizarTextoEstado(record);
        txtDescripcion.text = "El animal está tranquilo y confía más en el cuidador.";
    }

    public void AbrirOpcionesAcariciar()
    {
        var record = MascotaActual();
        if (record == null || !AnimalActionRules.CanBePetByHand(record.Species)) return;
        if (panelOpcionesAcariciar != null) panelOpcionesAcariciar.SetActive(true);
    }

    public void CerrarOpcionesAcariciar()
    {
        if (panelOpcionesAcariciar != null) panelOpcionesAcariciar.SetActive(false);
    }

    public void AbrirGestoAcariciar()
    {
        if (panelOpcionesAcariciar != null) panelOpcionesAcariciar.SetActive(false);
        if (panelGestoAcariciar != null) panelGestoAcariciar.SetActive(true);
    }

    public void CerrarGestoAcariciar()
    {
        if (panelGestoAcariciar != null) panelGestoAcariciar.SetActive(false);
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

        // Si el jugador cambia de mascota con un panel de acariciar abierto, cerrarlos - si no,
        // quedarían mostrando UI para el record anterior mientras el carrusel ya avanzó debajo.
        CerrarOpcionesAcariciar();
        CerrarGestoAcariciar();

        var record = MascotaActual();
        if (record == null)
        {
            txtNombre.text = "Sin mascotas";
            txtEstado.text = string.Empty;
            txtDescripcion.text = "Todavía no capturaste ningún animal. Volvé al mapa y capturá uno.";
            if (btnAcariciar != null) btnAcariciar.interactable = false;
            return;
        }

        bool acariciable = AnimalActionRules.CanBePetByHand(record.Species);
        if (btnAcariciar != null) btnAcariciar.interactable = acariciable;

        txtNombre.text = record.Species.ToString();
        txtDescripcion.text = acariciable ? $"Tu {record.Species} capturado." : MensajeNoAcariciable;
        ActualizarTextoEstado(record);

        var prefab = AnimalResources.Load(record.Species);
        if (prefab == null)
        {
            Debug.LogError($"[MascotaSelector] No se encontró el prefab para {record.Species}.");
            return;
        }

        animalActual = Instantiate(prefab, puntoSpawn.position, puntoSpawn.rotation);
        AjustarEscalaYApoyo(animalActual, puntoSpawn.position);
        PrepararAnimalParaVista(animalActual);
    }

    private void ActualizarTextoEstado(CapturedAnimalRecord record)
    {
        txtEstado.text = $"Hambre: {record.Hunger}%  ·  Felicidad: {record.Happiness}%";
    }

    // Escala el animal para que su altura real (renderer bounds, no un valor a ojo) coincida con
    // alturaObjetivoAnimal, y lo reposiciona para que sus pies queden apoyados en puntoBase - así
    // el mismo escenario sirve para un conejo chico o un ciervo grande sin que ninguno quede
    // cortado por la cámara ni flotando/hundido en el piso.
    private void AjustarEscalaYApoyo(GameObject animal, Vector3 puntoBase)
    {
        Renderer[] renderers = animal.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        if (bounds.size.y > 0.0001f)
        {
            float factor = alturaObjetivoAnimal / bounds.size.y;
            animal.transform.localScale *= factor;

            renderers = animal.GetComponentsInChildren<Renderer>();
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
        }

        float desnivel = puntoBase.y - bounds.min.y;
        animal.transform.position += new Vector3(0f, desnivel, 0f);
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

        // Disable every mover script BEFORE disabling colliders below - CharacterController is
        // itself a Collider subtype, so disabling colliders first (as this used to do) left
        // CreatureMover's Update() still calling CharacterController.Move() every frame on a
        // component that had just been disabled out from under it, spamming
        // "CharacterController.Move called on inactive controller" errors.
        MonoBehaviour[] scripts = animal.GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            string nombreScript = script.GetType().Name;
            if (nombreScript == "Controlador" || nombreScript == "ControladorCuidador" || nombreScript == "CreatureMover")
                script.enabled = false;
        }

        Collider[] colliders = animal.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
            col.enabled = false;
    }
}
