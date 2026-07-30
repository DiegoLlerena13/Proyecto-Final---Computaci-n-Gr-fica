using UnityEngine;

public class ObstacleSpawner3D : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject[] obstaculos;

    [Header("Spawn")]
    public float tiempoEntreObstaculos = 1.5f;
    public float posicionX = 6f;
    public float minY = -3f;
    public float maxY = 3f;

    private float timer = 0f;
    private PinguinoGameManager gameManager;

    void Start()
    {
        gameManager = FindObjectOfType<PinguinoGameManager>();
    }

    void Update()
    {
        if (gameManager != null && !gameManager.juegoActivo)
            return;

        timer += Time.deltaTime;

        if (timer >= tiempoEntreObstaculos)
        {
            CrearObstaculo();
            timer = 0f;
        }
    }

    private void CrearObstaculo()
    {
        if (obstaculos == null || obstaculos.Length == 0)
            return;

        int indice = Random.Range(0, obstaculos.Length);
        float yRandom = Random.Range(minY, maxY);

        Vector3 posicion = new Vector3(posicionX, yRandom, 0f);

        Instantiate(
            obstaculos[indice],
            posicion,
            Quaternion.identity
        );
    }
}