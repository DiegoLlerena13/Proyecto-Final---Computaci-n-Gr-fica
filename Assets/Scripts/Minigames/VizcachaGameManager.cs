using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class VizcachaGameManager : MonoBehaviour
{
    private const string HighScoreKey = "VizcachaHighScore";

    [Header("Estado")]
    public bool juegoActivo = true;

    [Header("UI")]
    public TextMeshProUGUI txtPuntaje;
    public GameObject panelGameOver;
    public TextMeshProUGUI txtMensajeFin;

    public int PlataformasPasadas { get; private set; }

    void Start()
    {
        juegoActivo = true;
        PlataformasPasadas = 0;

        if (panelGameOver != null)
            panelGameOver.SetActive(false);

        ActualizarTexto();
    }

    // Called by PlatformSpawner3D each time a platform falls behind the player.
    public void RegisterPlatformPassed()
    {
        if (!juegoActivo) return;
        PlataformasPasadas++;
        ActualizarTexto();
    }

    private void ActualizarTexto()
    {
        if (txtPuntaje != null)
            txtPuntaje.text = "Plataformas: " + PlataformasPasadas;
    }

    public void GameOver()
    {
        if (!juegoActivo) return;
        juegoActivo = false;

        int highScore = Mathf.Max(PlataformasPasadas, PlayerPrefs.GetInt(HighScoreKey, 0));
        PlayerPrefs.SetInt(HighScoreKey, highScore);

        if (txtMensajeFin != null)
            txtMensajeFin.text = $"Plataformas: {PlataformasPasadas}  -  Mejor: {highScore}";

        if (panelGameOver != null)
            panelGameOver.SetActive(true);
    }

    public void Reintentar()
    {
        SceneManager.LoadScene("04.Minigame_conejo");
    }

    public void VolverAJuegos()
    {
        SceneManager.LoadScene("03.Juegos");
    }
}
