using UnityEngine;

public class CamaraSeguidora : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform objetivo;

    [Header("Configuracion 3D")]
    public float distancia = 6f;
    public float altura = 4f;
    public float suavizado = 5f;

    [Header("Configuracion Torre 2D")]
    public float tamañoOrtografico = 3f;
    public float offsetYTorre = 2f;

    [Header("Configuracion Torre")]
    public Transform torreCentro;
    public float distanciaTorre = 6f;
    public float alturaTorre = 2f;

    private Camera cam;
    private bool modoTorre = false;

    void Start()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = false;
    }

    void LateUpdate()
    {
        if (objetivo == null) return;

        if (!modoTorre)
        {
            // Modo 3D - vista Paper Mario
            Vector3 posObjetivo = new Vector3(
                objetivo.position.x,
                objetivo.position.y + altura,
                objetivo.position.z - distancia
            );

            transform.position = Vector3.Lerp(
                transform.position, posObjetivo, Time.deltaTime * suavizado);

            transform.LookAt(objetivo);
        }
        else
        {
            // Sigue al conejo en X, Y fija con offset, Z fija
            Vector3 posObjetivo = new Vector3(
                objetivo.position.x,
                objetivo.position.y + offsetYTorre,
                objetivo.position.z - 10f
            );

            transform.position = Vector3.Lerp(
                transform.position, posObjetivo, Time.deltaTime * suavizado);

            transform.rotation = Quaternion.Euler(0, 0, 0);
        }
    }

    public void ActivarModoTorre(Transform nuevoObjetivo)
    {
        objetivo = nuevoObjetivo;
        modoTorre = true;
        cam.orthographic = true;
        cam.orthographicSize = 3f;

        // Posicion inicial correcta
        transform.position = new Vector3(
            nuevoObjetivo.position.x,
            nuevoObjetivo.position.y + 2f,
            nuevoObjetivo.position.z - 10f);

        transform.rotation = Quaternion.Euler(0, 0, 0);
    }

    public void DesactivarModoTorre(Transform cuidador)
    {
        objetivo = cuidador;
        modoTorre = false;
        cam.orthographic = false;
    }
}

