using UnityEngine;

public class AnimadorSprite : MonoBehaviour
{
    [Header("Animaciones")]
    public Sprite[] idle;
    public Sprite[] run;
    public Sprite[] jump;
    public Sprite[] recieveDamage;

    [Header("Velocidad")]
    public float fps = 8f;

    private SpriteRenderer sr;
    private Rigidbody rb;
    private Sprite[] animActual;
    private int frameActual = 0;
    private float timer = 0f;
    private bool recibioDanio = false;
    private float timerDanio = 0f;
    private float duracionDanio = 0.5f;

    void Start()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        rb = GetComponentInParent<Rigidbody>();
        animActual = idle;
    }

    void Update()
    {
        ManejarAnimacion();
        AvanzarFrame();
    }

    void ManejarAnimacion()
    {
        if (recibioDanio)
        {
            timerDanio += Time.deltaTime;
            CambiarAnim(recieveDamage);
            if (timerDanio >= duracionDanio)
            {
                recibioDanio = false;
                timerDanio = 0f;
            }
            return;
        }

        float velH = new Vector2(rb.linearVelocity.x, rb.linearVelocity.z).magnitude;
        bool enAire = Mathf.Abs(rb.linearVelocity.y) > 1f;

        if (enAire) CambiarAnim(jump);
        else if (velH > 0.2f) CambiarAnim(run);
        else CambiarAnim(idle);
    }

    void AvanzarFrame()
    {
        if (animActual == null || animActual.Length == 0) return;

        timer += Time.deltaTime;
        if (timer >= 1f / fps)
        {
            timer = 0f;
            frameActual = (frameActual + 1) % animActual.Length;
            sr.sprite = animActual[frameActual];
        }
    }

    void CambiarAnim(Sprite[] nueva)
    {
        if (nueva == null || nueva.Length == 0) return;
        if (animActual == nueva) return;
        animActual = nueva;
        frameActual = 0;
        timer = 0f;
    }

    public void RecibirDanio()
    {
        recibioDanio = true;
        timerDanio = 0f;
    }
}   