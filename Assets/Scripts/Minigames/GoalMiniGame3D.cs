using UnityEngine;
using UnityEngine.SceneManagement;

public class GoalMiniGame3D : MonoBehaviour
{
    public string escenaRegreso = "03.Juegos";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerPrefs.SetInt("VizcachaCompletado", 1);
            PlayerPrefs.Save();

            SceneManager.LoadScene(escenaRegreso);
        }
    }
}