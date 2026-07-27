using System;
using System.Collections.Generic;
using UnityEngine;

public enum GestureType
{
    Beckon,
    Affection,
    Agitation
}

// Plain C# heuristic classifier over a rolling window of MediaPipe-style 21-keypoint hand landmark
// frames (see HandLandmarkDetector). No Unity lifecycle is needed here - the owning MonoBehaviour just
// calls PushFrame() whenever a new detection lands.
//
// Standard 21-landmark hand topology: 0 = wrist, thumb = 1-4, index = 5-8, middle = 9-12,
// ring = 13-16, pinky = 17-20. Each finger's joints run MCP -> PIP -> DIP -> TIP.
public class HandGestureAnalyzer
{
    public const int KeypointCount = 21;

    const int WristIndex = 0;
    const int MiddleMcpIndex = 9;

    const int BufferSize = 30;

    const float ExtendedAngleDeg = 15f;
    const int AffectionMinExtendedFingers = 4;
    const int AffectionHoldFrames = 10;
    const float AffectionWristMoveRatio = 0.15f;

    const float BeckonLowAngleDeg = 20f;
    const float BeckonHighAngleDeg = 40f;
    const int BeckonMinCurlEvents = 2;

    const int AgitationMinSignChanges = 3;
    const float AgitationMinDeltaRatio = 0.03f;

    const float CooldownSeconds = 1.5f;

    static readonly int[] ThumbJoints = { 1, 2, 4 };
    static readonly int[] IndexJoints = { 5, 6, 8 };
    static readonly int[] MiddleJoints = { 9, 10, 12 };
    static readonly int[] RingJoints = { 13, 14, 16 };
    static readonly int[] PinkyJoints = { 17, 18, 20 };

    struct FrameData
    {
        public Vector3[] Landmarks;
        public float Timestamp;
        public int ExtendedFingerCount;
        public float AvgCurlFourFingers; // index + middle + ring + pinky, thumb excluded
        public float SizeProxy; // distance(wrist, middle MCP): bigger = hand closer/larger in frame
    }

    readonly List<FrameData> buffer = new(BufferSize);
    float lastFireTime = float.NegativeInfinity;

    public event Action<GestureType> OnGestureRecognized;

    public void PushFrame(Vector3[] landmarks, float timestamp)
    {
        if (landmarks == null || landmarks.Length != KeypointCount)
            throw new ArgumentException($"Expected {KeypointCount} landmarks, got {landmarks?.Length ?? 0}.", nameof(landmarks));

        var frame = BuildFrameData(landmarks, timestamp);

        buffer.Add(frame);
        while (buffer.Count > BufferSize)
            buffer.RemoveAt(0);

        // Cooldown: after firing any gesture, suppress further evaluation for CooldownSeconds.
        if (timestamp - lastFireTime < CooldownSeconds)
            return;

        if (CheckAffection(frame))
        {
            Fire(GestureType.Affection, timestamp);
        }
        else if (CheckBeckon())
        {
            Fire(GestureType.Beckon, timestamp);
        }
        else if (CheckAgitation())
        {
            Fire(GestureType.Agitation, timestamp);
        }
    }

    void Fire(GestureType gesture, float timestamp)
    {
        lastFireTime = timestamp;
        OnGestureRecognized?.Invoke(gesture);
    }

    static FrameData BuildFrameData(Vector3[] landmarks, float timestamp)
    {
        var thumbCurl = ComputeCurlAngle(landmarks, ThumbJoints);
        var indexCurl = ComputeCurlAngle(landmarks, IndexJoints);
        var middleCurl = ComputeCurlAngle(landmarks, MiddleJoints);
        var ringCurl = ComputeCurlAngle(landmarks, RingJoints);
        var pinkyCurl = ComputeCurlAngle(landmarks, PinkyJoints);

        var extendedCount = 0;
        if (thumbCurl < ExtendedAngleDeg) extendedCount++;
        if (indexCurl < ExtendedAngleDeg) extendedCount++;
        if (middleCurl < ExtendedAngleDeg) extendedCount++;
        if (ringCurl < ExtendedAngleDeg) extendedCount++;
        if (pinkyCurl < ExtendedAngleDeg) extendedCount++;

        var avgCurlFourFingers = (indexCurl + middleCurl + ringCurl + pinkyCurl) / 4f;
        var sizeProxy = Vector3.Distance(landmarks[WristIndex], landmarks[MiddleMcpIndex]);

        return new FrameData
        {
            Landmarks = landmarks,
            Timestamp = timestamp,
            ExtendedFingerCount = extendedCount,
            AvgCurlFourFingers = avgCurlFourFingers,
            SizeProxy = sizeProxy
        };
    }

    static float ComputeCurlAngle(Vector3[] landmarks, int[] joints)
    {
        int mcp = joints[0], pip = joints[1], tip = joints[2];
        Vector3 v1 = (landmarks[pip] - landmarks[mcp]).normalized;
        Vector3 v2 = (landmarks[tip] - landmarks[pip]).normalized;
        return Vector3.Angle(v1, v2);
    }

    // Open palm held still: at least 4 of 5 fingers extended continuously for the last
    // AffectionHoldFrames buffered frames, and the wrist barely moves relative to hand size.
    bool CheckAffection(FrameData current)
    {
        if (current.ExtendedFingerCount < AffectionMinExtendedFingers) return false;
        if (buffer.Count < AffectionHoldFrames) return false;

        var startIndex = buffer.Count - AffectionHoldFrames;
        for (var i = startIndex; i < buffer.Count; i++)
        {
            if (buffer[i].ExtendedFingerCount < AffectionMinExtendedFingers) return false;
        }

        var handSize = Vector3.Distance(current.Landmarks[WristIndex], current.Landmarks[MiddleMcpIndex]);
        if (handSize <= 0.0001f) return false;

        var maxWristDistance = 0f;
        for (var i = startIndex; i < buffer.Count; i++)
        {
            for (var j = i + 1; j < buffer.Count; j++)
            {
                var d = Vector3.Distance(buffer[i].Landmarks[WristIndex], buffer[j].Landmarks[WristIndex]);
                if (d > maxWristDistance) maxWristDistance = d;
            }
        }

        return maxWristDistance < handSize * AffectionWristMoveRatio;
    }

    // Repeated "come here" curl: count how many times the 4-finger average curl crosses from
    // below BeckonLowAngleDeg to above BeckonHighAngleDeg across the buffered window.
    bool CheckBeckon()
    {
        if (buffer.Count < 2) return false;

        var curlEvents = 0;
        var wasBelowLow = buffer[0].AvgCurlFourFingers < BeckonLowAngleDeg;

        for (var i = 1; i < buffer.Count; i++)
        {
            var avg = buffer[i].AvgCurlFourFingers;
            if (wasBelowLow && avg > BeckonHighAngleDeg)
            {
                curlEvents++;
                wasBelowLow = false;
            }
            else if (avg < BeckonLowAngleDeg)
            {
                wasBelowLow = true;
            }
        }

        return curlEvents >= BeckonMinCurlEvents;
    }

    // Forward wave: the apparent hand size (distance from wrist to middle MCP) oscillates
    // toward/away from the camera. Count direction reversals in the frame-to-frame delta.
    bool CheckAgitation()
    {
        if (buffer.Count < 4) return false;

        var signChanges = 0;
        var previousSign = 0;

        for (var i = 1; i < buffer.Count; i++)
        {
            var delta = buffer[i].SizeProxy - buffer[i - 1].SizeProxy;

            // Ignore sub-noise-floor deltas: a low-confidence/false hand detection (e.g. a face
            // instead of a real hand) still jitters frame to frame, which used to flip the sign
            // constantly and register as "agitation" from near-zero movement. Require each
            // direction change to be a meaningful fraction of the hand's own apparent size.
            var noiseFloor = buffer[i].SizeProxy * AgitationMinDeltaRatio;
            if (Mathf.Abs(delta) < noiseFloor) continue;

            var sign = delta > 0f ? 1 : -1;
            if (previousSign != 0 && sign != previousSign) signChanges++;
            previousSign = sign;
        }

        return signChanges >= AgitationMinSignChanges;
    }
}
