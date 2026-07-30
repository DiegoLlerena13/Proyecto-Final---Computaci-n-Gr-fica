using UnityEngine;

public class ObstacleMover3D : MonoBehaviour
{
    public float velocidad = 4f;
    public float limiteIzquierda = -7f;
    public float posicionJugadorX = -3f;

    private bool _pasado;
    private PinguinoGameManager _gameManager;

    private void Start()
    {
        _gameManager = FindObjectOfType<PinguinoGameManager>();
    }

    // Called by ObstacleSpawner3D right after Instantiate() so each new obstacle picks up
    // the current ramped speed instead of the prefab's fixed default.
    public void Configure(float velocidadNueva)
    {
        velocidad = velocidadNueva;
    }

    void Update()
    {
        transform.position += Vector3.left * velocidad * Time.deltaTime;

        if (!_pasado && transform.position.x < posicionJugadorX)
        {
            _pasado = true;
            _gameManager?.RegisterObstaclePassed();
        }

        if (transform.position.x < limiteIzquierda)
        {
            Destroy(gameObject);
        }
    }
}
