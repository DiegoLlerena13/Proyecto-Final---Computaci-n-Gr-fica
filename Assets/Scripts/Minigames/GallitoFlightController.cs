using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Infinite Flappy-Bird-style minigame for the Gallito de las Rocas: each tap gives an immediate
// upward flap, holding keeps climbing, releasing lets gravity pull the bird back down. Obstacles
// scroll forever and get faster the longer you survive - the only way the run ends is colliding
// with one. Reached from MascotaSelector.AbrirMinijuego() (non-additive SceneManager.LoadScene),
// so this scene owns its whole lifecycle - "Volver" goes back to 01.Mascota via SceneLoader.IrMascota().
public class GallitoFlightController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Pájaro")]
    [SerializeField] private RectTransform birdRect;
    [SerializeField] private Image birdImage;
    [SerializeField] private Sprite[] flapFrames;
    [SerializeField] private float flapFps = 8f;

    [Header("Física")]
    [SerializeField] private float gravity = 1600f;
    [SerializeField] private float liftAcceleration = 3200f;
    // Instant upward kick applied the moment a tap/press starts - without this, a quick tap barely
    // moved the bird (liftAcceleration only had a couple frames to act before release), which read
    // as "tapping doesn't do anything". Holding still works exactly as before on top of this kick.
    [SerializeField] private float flapImpulse = 550f;
    [SerializeField] private float maxRiseSpeed = 700f;
    [SerializeField] private float maxFallSpeed = 900f;
    [SerializeField] private float playAreaTop = 780f;
    [SerializeField] private float playAreaBottom = -780f;

    [Header("Obstáculos")]
    [SerializeField] private RectTransform obstacleLayer;
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private float obstacleSpeed = 400f;
    [SerializeField] private float maxObstacleSpeed = 800f;
    [SerializeField] private float speedIncreasePerObstacle = 15f;
    [SerializeField] private float spawnInterval = 1.8f;
    [SerializeField] private float minSpawnInterval = 1.1f;
    [SerializeField] private float spawnIntervalDecreasePerObstacle = 0.03f;
    [SerializeField] private float obstacleWidth = 140f;
    [SerializeField] private float minGapHeight = 420f;
    [SerializeField] private float maxGapHeight = 520f;
    [SerializeField] private float spawnX = 650f;
    [SerializeField] private float despawnX = -650f;
    [SerializeField] private float canvasTop = 960f;
    [SerializeField] private float canvasBottom = -960f;
    [SerializeField] private Color[] obstacleTints;

    [Header("Puntaje / Fin de partida")]
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private GameObject endPanel;
    [SerializeField] private TextMeshProUGUI endMessageText;
    [SerializeField] private GameObject retryButtonRoot;

    private const string DatoGallito = "El Gallito de las Rocas es el ave nacional del Perú y vive en los bosques nubosos de los Andes.";
    private const string HighScoreKey = "GallitoHighScore";
    private const string SceneName = "05.Minigame_gallito";

    private float verticalVelocity;
    private bool isHolding;
    private bool isRunning;
    private int obstaclesPassed;
    private float spawnTimer;
    private int flapFrameIndex;
    private float flapTimer;

    private class ObstaclePair
    {
        public GallitoObstacle Top;
        public GallitoObstacle Bottom;
        public bool Counted;
    }

    private readonly List<ObstaclePair> activePairs = new();

    private void Start()
    {
        isRunning = true;
        spawnTimer = spawnInterval;
        if (endPanel != null) endPanel.SetActive(false);
        UpdateProgressText();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isHolding = true;
        verticalVelocity = Mathf.Max(verticalVelocity, flapImpulse);
    }

    public void OnPointerUp(PointerEventData eventData) => isHolding = false;

    private void Update()
    {
        if (!isRunning) return;

        UpdatePhysics();
        UpdateFlapAnimation();
        UpdateSpawning();
        UpdateObstacles();
    }

    private void UpdatePhysics()
    {
        if (birdRect == null) return;

        verticalVelocity -= gravity * Time.deltaTime;
        if (isHolding) verticalVelocity += liftAcceleration * Time.deltaTime;
        verticalVelocity = Mathf.Clamp(verticalVelocity, -maxFallSpeed, maxRiseSpeed);

        var pos = birdRect.anchoredPosition;
        pos.y += verticalVelocity * Time.deltaTime;

        if (pos.y > playAreaTop)
        {
            pos.y = playAreaTop;
            verticalVelocity = 0f;
        }
        else if (pos.y < playAreaBottom)
        {
            pos.y = playAreaBottom;
            verticalVelocity = 0f;
        }

        birdRect.anchoredPosition = pos;
    }

    private void UpdateFlapAnimation()
    {
        if (birdImage == null || flapFrames == null || flapFrames.Length == 0 || flapFps <= 0f) return;

        flapTimer += Time.deltaTime;
        float frameDuration = 1f / flapFps;
        if (flapTimer < frameDuration) return;

        flapTimer -= frameDuration;
        flapFrameIndex = (flapFrameIndex + 1) % flapFrames.Length;
        birdImage.sprite = flapFrames[flapFrameIndex];
    }

    private void UpdateSpawning()
    {
        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f) return;

        spawnTimer = CurrentSpawnInterval();
        SpawnObstaclePair();
    }

    // Both speed and spawn cadence ramp up with score (capped) so the run keeps getting more
    // demanding the longer it goes - the "más dinamismo" an infinite runner needs instead of a flat
    // difficulty forever. Only newly spawned obstacles pick up the new pace; obstacles already on
    // screen keep whatever speed they were given, which reads as a smooth ramp rather than a jump.
    private float CurrentObstacleSpeed() => Mathf.Min(obstacleSpeed + obstaclesPassed * speedIncreasePerObstacle, maxObstacleSpeed);

    private float CurrentSpawnInterval() => Mathf.Max(spawnInterval - obstaclesPassed * spawnIntervalDecreasePerObstacle, minSpawnInterval);

    private void SpawnObstaclePair()
    {
        if (obstaclePrefab == null || obstacleLayer == null) return;

        float gapHeight = Random.Range(minGapHeight, maxGapHeight);
        float halfGap = gapHeight * 0.5f;
        // Keep the gap fully inside the play area (not the wider canvas bounds), so every gap is
        // actually reachable by a bird clamped to [playAreaBottom, playAreaTop].
        float gapCenterY = Random.Range(playAreaBottom + halfGap, playAreaTop - halfGap);
        Color tint = obstacleTints != null && obstacleTints.Length > 0
            ? obstacleTints[Random.Range(0, obstacleTints.Length)]
            : Color.white;
        float speed = CurrentObstacleSpeed();

        var pair = new ObstaclePair();

        var topGo = Instantiate(obstaclePrefab, obstacleLayer);
        var topRect = topGo.GetComponent<RectTransform>();
        float topHeight = canvasTop - (gapCenterY + halfGap);
        topRect.sizeDelta = new Vector2(obstacleWidth, topHeight);
        topRect.anchoredPosition = new Vector2(spawnX, canvasTop - topHeight * 0.5f);
        pair.Top = topGo.GetComponent<GallitoObstacle>();
        pair.Top.Configure(speed, despawnX, tint);

        var bottomGo = Instantiate(obstaclePrefab, obstacleLayer);
        var bottomRect = bottomGo.GetComponent<RectTransform>();
        float bottomHeight = (gapCenterY - halfGap) - canvasBottom;
        bottomRect.sizeDelta = new Vector2(obstacleWidth, bottomHeight);
        bottomRect.anchoredPosition = new Vector2(spawnX, canvasBottom + bottomHeight * 0.5f);
        pair.Bottom = bottomGo.GetComponent<GallitoObstacle>();
        pair.Bottom.Configure(speed, despawnX, tint);

        activePairs.Add(pair);
    }

    private void UpdateObstacles()
    {
        for (int i = activePairs.Count - 1; i >= 0; i--)
        {
            var pair = activePairs[i];

            if (pair.Top == null || pair.Bottom == null)
            {
                activePairs.RemoveAt(i);
                continue;
            }

            if (RectOverlap(birdRect, pair.Top.RectTransform) || RectOverlap(birdRect, pair.Bottom.RectTransform))
            {
                Lose();
                return;
            }

            if (!pair.Counted && pair.Top.RectTransform.anchoredPosition.x < birdRect.anchoredPosition.x)
            {
                pair.Counted = true;
                obstaclesPassed++;
                UpdateProgressText();
            }
        }
    }

    private void UpdateProgressText()
    {
        if (progressText != null) progressText.text = $"{obstaclesPassed}";
    }

    private static bool RectOverlap(RectTransform a, RectTransform b)
    {
        if (a == null || b == null) return false;

        var cornersA = new Vector3[4];
        var cornersB = new Vector3[4];
        a.GetWorldCorners(cornersA);
        b.GetWorldCorners(cornersB);

        var rectA = new Rect(cornersA[0].x, cornersA[0].y, cornersA[2].x - cornersA[0].x, cornersA[2].y - cornersA[0].y);
        var rectB = new Rect(cornersB[0].x, cornersB[0].y, cornersB[2].x - cornersB[0].x, cornersB[2].y - cornersB[0].y);
        return rectA.Overlaps(rectB);
    }

    private void Lose()
    {
        isRunning = false;

        int highScore = Mathf.Max(obstaclesPassed, PlayerPrefs.GetInt(HighScoreKey, 0));
        PlayerPrefs.SetInt(HighScoreKey, highScore);

        if (endPanel != null) endPanel.SetActive(true);
        if (endMessageText != null)
            endMessageText.text = $"Puntaje: {obstaclesPassed}  ·  Mejor: {highScore}\n{DatoGallito}";
        if (retryButtonRoot != null) retryButtonRoot.SetActive(true);
    }

    public void Retry() => SceneManager.LoadScene(SceneName);
}
