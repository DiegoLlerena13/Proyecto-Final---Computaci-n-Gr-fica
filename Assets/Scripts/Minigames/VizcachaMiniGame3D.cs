using UnityEngine;
using UnityEngine.InputSystem;

public class VizcachaMiniGame3D : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 5f;
    public float fuerzaSalto = 8f;

    [Header("Estado")]
    public bool enSuelo;

    [Header("Opcional: permitir teclado en PC para probar")]
    public bool permitirTeclado = true;

    [Header("Modo infinito")]
    public float alturaMuerte = -6f;

    private Rigidbody rb;
    private float movimientoHorizontal = 0f;
    private VizcachaGameManager gameManager;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        gameManager = FindObjectOfType<VizcachaGameManager>();

        rb.constraints =
            RigidbodyConstraints.FreezePositionZ |
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationY |
            RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        if (gameManager != null && !gameManager.juegoActivo)
            return;

        if (permitirTeclado)
        {
            LeerTecladoNuevoInputSystem();
        }

        rb.linearVelocity = new Vector3(
            movimientoHorizontal * velocidad,
            rb.linearVelocity.y,
            0f
        );

        GirarVisualmente();

        if (transform.position.y < alturaMuerte)
        {
            gameManager?.GameOver();
        }
    }

    private void LeerTecladoNuevoInputSystem()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        // Solo modifica si realmente se presiona una tecla.
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            movimientoHorizontal = -1f;
        }
        else if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            movimientoHorizontal = 1f;
        }
        else
        {
            // Si no se toca teclado ni botón móvil, se detiene.
            movimientoHorizontal = 0f;
        }

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            Saltar();
        }
    }

    private void GirarVisualmente()
    {
        if (movimientoHorizontal > 0.1f)
        {
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }
        else if (movimientoHorizontal < -0.1f)
        {
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }
    }

    // Estas funciones las llamarán los botones táctiles.
    public void PresionarIzquierda()
    {
        movimientoHorizontal = -1f;
    }

    public void PresionarDerecha()
    {
        movimientoHorizontal = 1f;
    }

    public void SoltarMovimiento()
    {
        movimientoHorizontal = 0f;
    }

    public void Saltar()
    {
        if (enSuelo)
        {
            rb.linearVelocity = new Vector3(
                rb.linearVelocity.x,
                fuerzaSalto,
                0f
            );

            enSuelo = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Plataforma"))
        {
            enSuelo = true;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Plataforma"))
        {
            enSuelo = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Plataforma"))
        {
            enSuelo = false;
        }
    }
}