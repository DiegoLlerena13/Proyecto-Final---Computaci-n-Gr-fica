using UnityEngine;
using UnityEngine.UI;

public class TapMinigameController : MonoBehaviour
{
    [SerializeField] private RectTransform spawnArea;
    [SerializeField] private Button targetButtonPrefab;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text timeText;
    [SerializeField] private float duration = 15f;

    private int score;
    private float timeRemaining;
    private Button activeTarget;

    private void OnEnable()
    {
        score = 0;
        timeRemaining = duration;
        SpawnTarget();
        UpdateUI();
    }

    private void Update()
    {
        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0f)
        {
            Finish();
            return;
        }
        UpdateUI();
    }

    private void SpawnTarget()
    {
        if (activeTarget != null)
            Destroy(activeTarget.gameObject);

        activeTarget = Instantiate(targetButtonPrefab, spawnArea);
        activeTarget.onClick.AddListener(OnTargetTapped);

        var rect = activeTarget.GetComponent<RectTransform>();
        float x = Random.Range(-spawnArea.rect.width / 2f, spawnArea.rect.width / 2f);
        float y = Random.Range(-spawnArea.rect.height / 2f, spawnArea.rect.height / 2f);
        rect.anchoredPosition = new Vector2(x, y);
    }

    private void OnTargetTapped()
    {
        score++;
        SpawnTarget();
    }

    private void UpdateUI()
    {
        if (scoreText != null) scoreText.text = $"Score: {score}";
        if (timeText != null) timeText.text = $"Time: {Mathf.CeilToInt(timeRemaining)}";
    }

    private void Finish()
    {
        if (activeTarget != null)
            Destroy(activeTarget.gameObject);

        if (GameManager.Instance != null)
            GameManager.Instance.OnMinigameFinished(score);
    }
}
