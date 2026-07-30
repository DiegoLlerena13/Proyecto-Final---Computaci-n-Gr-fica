using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MinigameMenu : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI txtSeleccionado;

    private string escenaSeleccionada = "04.Minigame_conejo";
    private string nombreSeleccionado = "Salto de la vizcacha";

    void Start()
    {
        ActualizarTexto();
    }

    public void SeleccionarVizcacha()
    {
        escenaSeleccionada = "04.Minigame_conejo";
        nombreSeleccionado = "Salto de la vizcacha";
        ActualizarTexto();
    }

    public void SeleccionarPinguino()
    {
        escenaSeleccionada = "06.Minigame_pinguino";
        nombreSeleccionado = "Deslizamiento costero";
        ActualizarTexto();
    }

    public void SeleccionarTaruca()
    {
        escenaSeleccionada = ""; 
        nombreSeleccionado = "Ruta de la taruca - Próximamente";
        ActualizarTexto();
    }

    public void SeleccionarAve()
    {
        escenaSeleccionada = "";
        nombreSeleccionado = "Vuelo andino - Próximamente";
        ActualizarTexto();
    }

    public void SeleccionarSerpiente()
    {
        escenaSeleccionada = "";
        nombreSeleccionado = "Laberinto amazónico - Próximamente";
        ActualizarTexto();
    }

    public void SeleccionarPerro()
    {
        escenaSeleccionada = "";
        nombreSeleccionado = "Camino al refugio - Próximamente";
        ActualizarTexto();
    }

    public void Jugar()
    {
        if (string.IsNullOrEmpty(escenaSeleccionada))
        {
            txtSeleccionado.text = "Seleccionado: " + nombreSeleccionado;
            return;
        }

        SceneManager.LoadScene(escenaSeleccionada);
    }

    private void ActualizarTexto()
    {
        if (txtSeleccionado != null)
        {
            txtSeleccionado.text = "Seleccionado: " + nombreSeleccionado;
        }
        else
        {
            Debug.LogWarning("Falta asignar TxtSeleccionado en MinigameMenu.");
        }
    }
}