using UnityEngine;

public class ObstacleSpawner3D : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject[] obstaculos;

    [Header("Spawn")]
    public float tiempoEntreObstaculos = 1.5f;
    public float minSpawnInterval = 0.7f;
    public float spawnIntervalDecreasePerObstacle = 0.03f;
    public float posicionX = 6f;
    public float minY = -3f;
    public float maxY = 3f;

    [Header("Velocidad (rampa de dificultad)")]
    public float velocidadBase = 4f;
    public float velocidadMaxima = 9f;
    public float incrementoVelocidadPorObstaculo = 0.15f;

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

        if (timer >= CurrentSpawnInterval())
        {
            CrearObstaculo();
            timer = 0f;
        }
    }

    private float CurrentSpawnInterval()
    {
        int pasados = gameManager != null ? gameManager.ObstaclesPassed : 0;
        return Mathf.Max(minSpawnInterval, tiempoEntreObstaculos - pasados * spawnIntervalDecreasePerObstacle);
    }

    private float CurrentSpeed()
    {
        int pasados = gameManager != null ? gameManager.ObstaclesPassed : 0;
        return Mathf.Min(velocidadMaxima, velocidadBase + pasados * incrementoVelocidadPorObstaculo);
    }

    private void CrearObstaculo()
    {
        if (obstaculos == null || obstaculos.Length == 0)
            return;

        int indice = Random.Range(0, obstaculos.Length);
        float yRandom = Random.Range(minY, maxY);

        Vector3 posicion = new Vector3(posicionX, yRandom, 0f);

        GameObject instancia = Instantiate(obstaculos[indice], posicion, Quaternion.identity);
        ObstacleMover3D mover = instancia.GetComponent<ObstacleMover3D>();
        if (mover != null) mover.Configure(CurrentSpeed());
    }
}
