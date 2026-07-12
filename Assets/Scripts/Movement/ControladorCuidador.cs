using UnityEngine;
using UnityEngine.InputSystem;


public class ControladorCuidador : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 5f;
    public float fuerzaSalto = 7f;

    [Header("Flip Paper Mario")]
    public Transform spriteTransform;
    public float velocidadFlip = 10f;

    [Header("Deteccion Suelo")]
    public LayerMask capaSuelo;
    public float rayLength = 0.3f;

    private Rigidbody rb;
    private Transform camara;
    private bool enSuelo = false;
    private bool girado = false;

    private Quaternion flipIzq = Quaternion.Euler(0, 180f, 0);
    private Quaternion flipDer = Quaternion.Euler(0, 0f, 0);

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (camara == null && Camera.main != null)
            camara = Camera.main.transform;
    }

    public void SetCamera(Transform camera)
    {
        camara = camera;
    }

    void Update()
    {
        if (camara == null) return;

        float h = 0f;
        float v = 0f;

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) h -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) h += 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) v += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) v -= 1f;
        }

        // Movimiento relativo a la camara
        Vector3 adelante = new Vector3(camara.forward.x, 0, camara.forward.z).normalized;
        Vector3 derecha = new Vector3(camara.right.x, 0, camara.right.z).normalized;
        Vector3 movimiento = (adelante * v + derecha * h) * velocidad;
        movimiento.y = rb.linearVelocity.y;
        rb.linearVelocity = movimiento;

        // Rotar hacia donde se mueve
        Vector3 dir = (adelante * v + derecha * h).normalized;
        if (dir.magnitude > 0.1f)
        {
            Quaternion rot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, rot, Time.deltaTime * 8f);
        }

        // Flip del sprite estilo Paper Mario
        if (!girado && h < -0.1f) girado = true;
        else if (girado && h > 0.1f) girado = false;

        Quaternion targetRot = girado ? flipIzq : flipDer;
        spriteTransform.rotation = Quaternion.Slerp(
            spriteTransform.rotation, targetRot, velocidadFlip * Time.deltaTime);

        // Salto
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame && enSuelo)
        {
            rb.linearVelocity = new Vector3(
                rb.linearVelocity.x, fuerzaSalto, rb.linearVelocity.z);
            enSuelo = false;
        }
    }

    void FixedUpdate()
    {
        enSuelo = Physics.Raycast(
            transform.position + Vector3.up * 0.1f,
            Vector3.down,
            1.2f,
            capaSuelo);
        Debug.DrawRay(
            transform.position + Vector3.up * 0.1f,
            Vector3.down * 1.2f,
            Color.red);
    }

    public void Bloquear(bool estado)
    {
        rb.linearVelocity = Vector3.zero;
        enabled = !estado;
    }
}