using UnityEngine;

public class ObstacleMover3D : MonoBehaviour
{
    public float velocidad = 4f;
    public float limiteIzquierda = -7f;

    void Update()
    {
        transform.position += Vector3.left * velocidad * Time.deltaTime;

        if (transform.position.x < limiteIzquierda)
        {
            Destroy(gameObject);
        }
    }
}