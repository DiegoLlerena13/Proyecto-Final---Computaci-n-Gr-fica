using UnityEngine;

// Placed on 00.Mapa's "UI" GameObject. GameManager.Instance survives scene reloads (it's
// DontDestroyOnLoad), but this component doesn't - it gets destroyed and recreated every time
// SceneManager.LoadScene("00.Mapa") runs (via BottomNav's Mapa button), same as the panels
// themselves. Re-registering on every Awake() keeps GameManager pointed at whichever panel
// instances are actually alive in the currently-loaded scene instead of the very first one.
public class MapaPanelRegistrar : MonoBehaviour
{
    [SerializeField] private GameObject worldPanel;
    [SerializeField] private GameObject arCapturePanel;
    [SerializeField] private GameObject menageriePanel;
    [SerializeField] private GameObject petCarePanel;
    [SerializeField] private GameObject minigamePanel;

    private void Awake()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RebindMapaPanels(worldPanel, arCapturePanel, menageriePanel, petCarePanel, minigamePanel);
    }
}
