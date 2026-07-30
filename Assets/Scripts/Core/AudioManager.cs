using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instancia;

    private AudioSource audioSource;

    void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();

        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    public void CambiarVolumen(float volumen)
    {
        if (audioSource != null)
        {
            audioSource.volume = volumen;
        }
    }

    public void PausarMusica()
    {
        if (audioSource != null)
        {
            audioSource.Pause();
        }
    }

    public void ReanudarMusica()
    {
        if (audioSource != null)
        {
            audioSource.Play();
        }
    }
}