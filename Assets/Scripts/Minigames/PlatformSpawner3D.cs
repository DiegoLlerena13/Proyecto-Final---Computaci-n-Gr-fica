using System.Collections.Generic;
using UnityEngine;

// Procedural endless platform generator for the Vizcacha minigame. Platforms are plain
// primitive cubes (GameObject.CreatePrimitive already gives them a BoxCollider, tagged
// "Plataforma" to match VizcachaMiniGame3D's existing ground-detection code untouched) -
// no new prefab asset needed. Gaps and height offsets stay within what a single jump
// (fuerzaSalto=8) can clear, widening slightly as the player gets further.
public class PlatformSpawner3D : MonoBehaviour
{
    [Header("Refs")]
    public Transform jugador;
    public VizcachaGameManager gameManager;

    [Header("Plataforma")]
    public float anchoPlataforma = 3f;
    public float profundidadPlataforma = 2f;
    public float altoPlataforma = 0.5f;
    public Material materialPlataforma;

    [Header("Generacion")]
    public float distanciaAdelante = 15f;
    public float distanciaDespawn = 12f;
    public float gapMinimo = 2.5f;
    public float gapMaximo = 4f;
    public float gapMaximoTope = 5.5f;
    public float gapIncrementoPorPlataforma = 0.02f;
    public float desnivelMaximo = 1f;
    public float desnivelMaximoTope = 1.8f;
    public float desnivelIncrementoPorPlataforma = 0.01f;

    private class PlataformaActiva
    {
        public Transform transform;
        public float bordeDerechoX;
        public bool contada;
    }

    private readonly List<PlataformaActiva> _activas = new List<PlataformaActiva>();
    private float _proximoInicioX;
    private float _ultimaY;

    private void Start()
    {
        if (jugador == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) jugador = go.transform;
        }
        if (gameManager == null)
            gameManager = FindObjectOfType<VizcachaGameManager>();

        float inicioX = jugador != null ? jugador.position.x - anchoPlataforma * 0.5f : -anchoPlataforma * 0.5f;
        _ultimaY = jugador != null ? jugador.position.y - 1f : 0f;
        SpawnPlataforma(inicioX, _ultimaY);
    }

    private void Update()
    {
        if (jugador == null) return;
        if (gameManager != null && !gameManager.juegoActivo) return;

        while (_proximoInicioX < jugador.position.x + distanciaAdelante)
        {
            int pasadas = gameManager != null ? gameManager.PlataformasPasadas : 0;

            float gapMax = Mathf.Min(gapMaximoTope, gapMaximo + pasadas * gapIncrementoPorPlataforma);
            float gap = Random.Range(gapMinimo, gapMax);

            float desnivelMax = Mathf.Min(desnivelMaximoTope, desnivelMaximo + pasadas * desnivelIncrementoPorPlataforma);
            float nuevaY = Mathf.Clamp(_ultimaY + Random.Range(-desnivelMax, desnivelMax), -2f, 4f);

            float inicioX = _proximoInicioX + gap;
            SpawnPlataforma(inicioX, nuevaY);
            _ultimaY = nuevaY;
        }

        for (int i = _activas.Count - 1; i >= 0; i--)
        {
            var p = _activas[i];
            if (p.transform == null) { _activas.RemoveAt(i); continue; }

            if (!p.contada && p.bordeDerechoX < jugador.position.x)
            {
                p.contada = true;
                gameManager?.RegisterPlatformPassed();
            }

            if (p.bordeDerechoX < jugador.position.x - distanciaDespawn)
            {
                Destroy(p.transform.gameObject);
                _activas.RemoveAt(i);
            }
        }
    }

    private void SpawnPlataforma(float inicioX, float y)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Plataforma";
        go.tag = "Plataforma";
        go.transform.SetParent(transform);
        go.transform.localScale = new Vector3(anchoPlataforma, altoPlataforma, profundidadPlataforma);
        go.transform.position = new Vector3(inicioX + anchoPlataforma * 0.5f, y, 0f);

        if (materialPlataforma != null)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = materialPlataforma;
        }

        _activas.Add(new PlataformaActiva
        {
            transform = go.transform,
            bordeDerechoX = inicioX + anchoPlataforma,
        });

        _proximoInicioX = inicioX + anchoPlataforma;
    }
}
