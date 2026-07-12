using UnityEngine;
using UnityEngine.UI;

public class MenagerieUI : MonoBehaviour
{
    [SerializeField] private RectTransform listContainer;
    [SerializeField] private Button backButton;

    private void Start()
    {
        if (backButton != null) backButton.onClick.AddListener(() => GameManager.Instance.ReturnToWorld());
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void Refresh()
    {
        for (int i = listContainer.childCount - 1; i >= 0; i--)
            Destroy(listContainer.GetChild(i).gameObject);

        if (GameManager.Instance == null) return;

        float y = -40f;
        foreach (var record in GameManager.Instance.CapturedAnimals)
        {
            var entryGO = new GameObject(record.Species + "Entry", typeof(RectTransform), typeof(Image), typeof(Button));
            entryGO.transform.SetParent(listContainer, false);

            var rect = (RectTransform)entryGO.transform;
            rect.sizeDelta = new Vector2(400, 60);
            rect.anchoredPosition = new Vector2(0, y);
            y -= 70f;

            var labelGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGO.transform.SetParent(entryGO.transform, false);
            var labelRect = (RectTransform)labelGO.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var text = labelGO.GetComponent<Text>();
            text.text = record.Species.ToString();
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.black;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var species = record.Species;
            entryGO.GetComponent<Button>().onClick.AddListener(() => GameManager.Instance.OpenPetCare(species));
        }
    }
}
