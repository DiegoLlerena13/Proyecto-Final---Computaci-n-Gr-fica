using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void IrMapa()
    {
        SceneManager.LoadScene("00.Mapa");
    }

    public void IrMascota()
    {
        SceneManager.LoadScene("01.Mascota");
    }

    public void IrEcoDex()
    {
        SceneManager.LoadScene("02.EcoDex");
    }

    public void IrJuegos()
    {
        SceneManager.LoadScene("03.Juegos");
    }

    public void IrMinijuegoConejo()
    {
        SceneManager.LoadScene("04.Minigame_conejo");
    }

    public void VolverAJuegos()
    {
        SceneManager.LoadScene("03.Juegos");
    }
}