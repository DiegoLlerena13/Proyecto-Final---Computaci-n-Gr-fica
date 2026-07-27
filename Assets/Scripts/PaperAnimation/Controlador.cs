using UnityEngine;
using UnityEngine.InputSystem;

public class Controlador : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 3f;
    public float jumpForce = 5f;

    [Header("Ground Check")]
    public Transform groundChecker;
    public LayerMask groundLayer;
    public float rayLength = 0.3f;

    [Header("Flip (Paper Mario)")]
    public float flipSpeed = 10f;

    // Privadas
    private Rigidbody rb;
    private Animator anim;
    private Vector2 moveInput;
    private bool jumpInput;
    private bool grounded;
    private bool backTurned;
    private bool flipped;

    private Quaternion flipLeft = Quaternion.Euler(0f, -180f, 0f);
    private Quaternion flipRight = Quaternion.Euler(0f, 0f, 0f);

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        // Input
        moveInput = Vector2.zero;
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) moveInput.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveInput.x += 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) moveInput.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) moveInput.y -= 1f;

            if (keyboard.spaceKey.wasPressedThisFrame && grounded)
                jumpInput = true;
        }

        // Back turned
        if (!backTurned && moveInput.y > 0f)
            backTurned = true;
        else if (backTurned && moveInput.y < 0f)
            backTurned = false;

        // Animaciones
        if (anim)
        {
            anim.SetFloat("MoveSpeed", rb.linearVelocity.magnitude);
            anim.SetBool("BackTurned", backTurned);
            anim.SetBool("Grounded", grounded);
        }

        // Flip Paper Mario
        if (!flipped && moveInput.x < 0f)
            flipped = true;
        else if (flipped && moveInput.x > 0f)
            flipped = false;

        if (flipped)
            transform.rotation = Quaternion.Slerp(transform.rotation, flipLeft, flipSpeed * Time.deltaTime);
        else
            transform.rotation = Quaternion.Slerp(transform.rotation, flipRight, flipSpeed * Time.deltaTime);
    }

    void FixedUpdate()
    {
        // Movimiento
        rb.linearVelocity = new Vector3(
            moveInput.x * moveSpeed,
            rb.linearVelocity.y,
            moveInput.y * moveSpeed
        );

        // Raycast suelo
        RaycastHit hit;
        grounded = Physics.Raycast(groundChecker.position, Vector3.down, out hit, rayLength, groundLayer);
        Debug.DrawRay(groundChecker.position, Vector3.down * rayLength, Color.red);

        // Salto
        if (jumpInput)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            jumpInput = false;
        }
    }
}