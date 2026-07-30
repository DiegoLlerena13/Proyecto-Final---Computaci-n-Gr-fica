using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class PinguinoGameManager : MonoBehaviour
{
    [Header("Estado")]
    public bool juegoActivo = true;

    [Header("UI")]
    public TextMeshProUGUI txtPuntaje;
    public GameObject panelGameOver;

    private float puntaje = 0f;

    void Start()
    {
        juegoActivo = true;
        puntaje = 0f;

        if (panelGameOver != null)
            panelGameOver.SetActive(false);
    }

    void Update()
    {
        if (!juegoActivo)
            return;

        puntaje += Time.deltaTime * 10f;

        if (txtPuntaje != null)
        {
            txtPuntaje.text = "Puntos: " + Mathf.FloorToInt(puntaje).ToString();
        }
    }

    public void GameOver()
    {
        juegoActivo = false;

        if (panelGameOver != null)
            panelGameOver.SetActive(true);
    }

    public void Reintentar()
    {
        SceneManager.LoadScene("06.Minigame_pinguino");
    }

    public void VolverAJuegos()
    {
        SceneManager.LoadScene("03.Juegos");
    }
}