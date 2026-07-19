using UnityEngine;
using UnityEngine.EventSystems;

public class CaptureTapTarget : MonoBehaviour, IPointerClickHandler
{
    public ARFoundationCaptureController Owner { get; set; }

    public void OnPointerClick(PointerEventData eventData)
    {
        Owner?.RegisterTap();
    }
}
