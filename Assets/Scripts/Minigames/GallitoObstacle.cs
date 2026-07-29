using UnityEngine;
using UnityEngine.UI;

// One branch/net/smoke piece of an obstacle pair in the Gallito flight minigame. Self-contained
// mover: scrolls left at a constant speed and self-destructs once past despawnX. The controller
// only reads RectTransform for the AABB collision check and the "has this pair passed the bird
// yet" comparison - it doesn't otherwise drive this object.
public class GallitoObstacle : MonoBehaviour
{
    public RectTransform RectTransform { get; private set; }

    private float speed;
    private float despawnX;

    private void Awake()
    {
        RectTransform = GetComponent<RectTransform>();
    }

    public void Configure(float speed, float despawnX, Color tint)
    {
        this.speed = speed;
        this.despawnX = despawnX;

        var image = GetComponent<Image>();
        if (image != null) image.color = tint;
    }

    private void Update()
    {
        var pos = RectTransform.anchoredPosition;
        pos.x -= speed * Time.deltaTime;
        RectTransform.anchoredPosition = pos;

        if (pos.x <= despawnX)
            Destroy(gameObject);
    }
}
