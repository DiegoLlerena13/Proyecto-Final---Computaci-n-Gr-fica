using UnityEngine;
using UnityEngine.InputSystem;

public class PinguinoMiniGame3D : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidadVertical = 5f;
    public float limiteArriba = 3.5f;
    public float limiteAbajo = -3.5f;

    [Header("Opcional para probar en PC")]
    public bool permitirTeclado = true;

    private Rigidbody rb;
    private float direccionVertical = 0f;
    private PinguinoGameManager gameManager;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        gameManager = FindObjectOfType<PinguinoGameManager>();

        rb.useGravity = false;

        rb.constraints =
            RigidbodyConstraints.FreezePositionX |
            RigidbodyConstraints.FreezePositionZ |
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationY |
            RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        if (permitirTeclado)
        {
            LeerTeclado();
        }

        Vector3 velocidad = new Vector3(
            0f,
            direccionVertical * velocidadVertical,
            0f
        );

        rb.linearVelocity = velocidad;

        LimitarMovimiento();
    }

    private void LeerTeclado()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
        {
            direccionVertical = 1f;
        }
        else if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
        {
            direccionVertical = -1f;
        }
        else
        {
            direccionVertical = 0f;
        }
    }

    private void LimitarMovimiento()
    {
        Vector3 pos = transform.position;

        pos.y = Mathf.Clamp(pos.y, limiteAbajo, limiteArriba);
        pos.x = -3f;
        pos.z = 0f;

        transform.position = pos;
    }

    public void PresionarArriba()
    {
        direccionVertical = 1f;
    }

    public void PresionarAbajo()
    {
        direccionVertical = -1f;
    }

    public void SoltarMovimiento()
    {
        direccionVertical = 0f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstaculo"))
        {
            if (gameManager != null)
            {
                gameManager.GameOver();
            }
        }
    }
}