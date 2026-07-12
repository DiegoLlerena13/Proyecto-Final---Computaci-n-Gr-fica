using System.Collections;
using UnityEngine;

public class GPSLocator : MonoBehaviour
{
    public float updateInterval = 0.5f;
    public float metersPerUnit = 1f;

    public bool IsRunning { get; private set; }
    public Vector2 OriginCoords { get; private set; }
    public Vector3 WorldPosition { get; private set; }
    public Vector3 MovementDelta { get; private set; }

    private Vector3 lastWorldPosition;
    private bool originSet;

    private const float MetersPerDegreeLat = 111139f;

    private void Start()
    {
        StartCoroutine(InitGPS());
    }

    private IEnumerator InitGPS()
    {
        if (!Input.location.isEnabledByUser)
        {
            Debug.LogWarning("[GPSLocator] Location services disabled by user.");
            yield break;
        }

        Input.location.Start(1f, 0.5f);
        Input.compass.enabled = true;

        int timeout = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && timeout > 0)
        {
            yield return new WaitForSeconds(1);
            timeout--;
        }

        if (Input.location.status != LocationServiceStatus.Running)
        {
            Debug.LogWarning($"[GPSLocator] Failed to start. Status: {Input.location.status}");
            yield break;
        }

        IsRunning = true;
        Debug.Log("[GPSLocator] GPS active.");

        SetOrigin(Input.location.lastData.latitude, Input.location.lastData.longitude);

        StartCoroutine(PollGPS());
    }

    private void SetOrigin(float lat, float lon)
    {
        OriginCoords = new Vector2(lat, lon);
        originSet = true;
        lastWorldPosition = Vector3.zero;
        WorldPosition = Vector3.zero;
        Debug.Log($"[GPSLocator] Origin set: {lat}, {lon}");
    }

    private IEnumerator PollGPS()
    {
        while (IsRunning)
        {
            float lat = Input.location.lastData.latitude;
            float lon = Input.location.lastData.longitude;

            WorldPosition = CoordsToWorld(lat, lon);
            MovementDelta = WorldPosition - lastWorldPosition;
            lastWorldPosition = WorldPosition;

            yield return new WaitForSeconds(updateInterval);
        }
    }

    private Vector3 CoordsToWorld(float lat, float lon)
    {
        float dLat = lat - OriginCoords.x;
        float dLon = lon - OriginCoords.y;

        float metersZ = dLat * MetersPerDegreeLat;
        float metersX = dLon * MetersPerDegreeLat * Mathf.Cos(OriginCoords.x * Mathf.Deg2Rad);

        return new Vector3(metersX / metersPerUnit, 0f, metersZ / metersPerUnit);
    }

    private void OnDisable()
    {
        if (Input.location.status == LocationServiceStatus.Running)
            Input.location.Stop();
    }
}
