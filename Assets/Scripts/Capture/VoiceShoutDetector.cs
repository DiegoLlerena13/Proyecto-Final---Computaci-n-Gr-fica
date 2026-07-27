using System;
using System.Collections;
using UnityEngine;

// Simplified "shout to call" mechanic driven purely by microphone RMS volume - no speech-to-text.
// Loops a 5-second microphone buffer and watches for RMS staying above shoutThreshold for at least
// sustainSeconds, with a cooldown afterwards to stop one long shout from firing repeatedly.
public class VoiceShoutDetector : MonoBehaviour
{
    [SerializeField] private float shoutThreshold = 0.2f;
    [SerializeField] private float sustainSeconds = 0.25f;
    [SerializeField] private float cooldownSeconds = 1f;

    const int WindowSize = 512;
    const int RecordingLengthSeconds = 5;
    const int RecordingFrequency = 16000;

    enum State { Idle, Rising, Cooldown }

    AudioClip micClip;
    State state = State.Idle;
    float risingStartTime;
    float cooldownEndTime;
    bool droppedBelowThresholdSinceFire;

    public event Action OnShoutDetected;
    public float CurrentLevel01 { get; private set; }
    public bool IsReady { get; private set; }

    void Start()
    {
        StartCoroutine(RequestPermissionAndStartMicrophone());
    }

    IEnumerator RequestPermissionAndStartMicrophone()
    {
#if UNITY_ANDROID
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
        {
            Debug.Log("[VoiceShoutDetector] Microphone permission not granted. Requesting...");
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
            yield return new WaitForSeconds(1f);
        }

        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
        {
            Debug.LogWarning("[VoiceShoutDetector] Microphone permission denied. Voice detection disabled.");
            IsReady = false;
            yield break;
        }
#endif

        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("[VoiceShoutDetector] No microphone device available. Voice detection disabled.");
            IsReady = false;
            yield break;
        }

        micClip = Microphone.Start(null, true, RecordingLengthSeconds, RecordingFrequency);
        IsReady = true;
    }

    void Update()
    {
        if (!IsReady || micClip == null) return;

        var rms = ComputeRms();
        CurrentLevel01 = Mathf.Clamp01(rms / (shoutThreshold * 1.5f));

        switch (state)
        {
            case State.Idle:
                if (rms >= shoutThreshold)
                {
                    state = State.Rising;
                    risingStartTime = Time.time;
                }
                break;

            case State.Rising:
                if (rms < shoutThreshold)
                {
                    state = State.Idle;
                }
                else if (Time.time - risingStartTime >= sustainSeconds)
                {
                    OnShoutDetected?.Invoke();
                    cooldownEndTime = Time.time + cooldownSeconds;
                    droppedBelowThresholdSinceFire = false;
                    state = State.Cooldown;
                }
                break;

            case State.Cooldown:
                if (rms < shoutThreshold)
                    droppedBelowThresholdSinceFire = true;

                if (Time.time >= cooldownEndTime && droppedBelowThresholdSinceFire)
                    state = State.Idle;
                break;
        }
    }

    float ComputeRms()
    {
        int micPos = Microphone.GetPosition(null) - WindowSize;
        if (micPos < 0) return 0f;

        var samples = new float[WindowSize];
        micClip.GetData(samples, micPos);

        var sumSquares = 0f;
        for (var i = 0; i < samples.Length; i++)
            sumSquares += samples[i] * samples[i];

        return Mathf.Sqrt(sumSquares / samples.Length);
    }

    void OnDestroy()
    {
        if (Microphone.IsRecording(null)) Microphone.End(null);
    }
}
