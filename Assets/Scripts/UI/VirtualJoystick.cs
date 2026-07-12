using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public float handleRange = 1f;

    public Vector2 InputDirection { get; private set; }

    private RectTransform background;
    private RectTransform handle;
    private Canvas canvas;
    private Camera cam;
    private Vector2 inputVector;

    private void Awake()
    {
        background = GetComponent<RectTransform>();
        handle = transform.GetChild(0).GetComponent<RectTransform>();
    }

    private void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
            cam = canvas.worldCamera;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background, eventData.position, cam, out Vector2 localPoint);

        Vector2 bgSize = background.sizeDelta;
        localPoint.x /= bgSize.x;
        localPoint.y /= bgSize.y;

        inputVector = new Vector2(localPoint.x * 2f, localPoint.y * 2f);
        inputVector = inputVector.magnitude > 1f ? inputVector.normalized : inputVector;

        handle.anchoredPosition = new Vector2(
            inputVector.x * (bgSize.x * 0.5f) * handleRange,
            inputVector.y * (bgSize.y * 0.5f) * handleRange);

        InputDirection = inputVector;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;
        InputDirection = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
    }
}
