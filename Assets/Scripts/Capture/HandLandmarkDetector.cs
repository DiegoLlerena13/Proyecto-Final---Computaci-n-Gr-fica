using System;
using System.Collections;
using Unity.Mathematics;
using Unity.InferenceEngine;
using UnityEngine;
using UnityEngine.UI;

// Adapted from Unity's official BlazeDetectionSample (sentis-samples, BlazeDetectionSample/Hand/HandDetection.cs).
// Unlike the reference sample, this drives the two-stage palm-detector + landmarker pipeline off a live
// back-facing WebCamTexture instead of a static Texture2D, paces inference to run every
// `inferenceIntervalFrames` frames instead of every frame, and forwards the 21 raw landmark positions to a
// HandGestureAnalyzer instead of a debug HandPreview visualization.
public class HandLandmarkDetector : MonoBehaviour
{
    [SerializeField] private ModelAsset handDetector;
    [SerializeField] private ModelAsset handLandmarker;
    [SerializeField] private TextAsset anchorsCSV;
    [SerializeField] private RawImage cameraFeedImage;

    [SerializeField] private float scoreThreshold = 0.75f;
    [SerializeField] private int inferenceIntervalFrames = 6;

    [SerializeField] private int requestedWidth = 640;
    [SerializeField] private int requestedHeight = 480;
    [SerializeField] private int requestedFps = 30;

    const int k_NumAnchors = 2016;
    const int k_NumKeypoints = 21;
    const int detectorInputSize = 192;
    const int landmarkerInputSize = 224;

    float[,] m_Anchors;

    Worker m_HandDetectorWorker;
    Worker m_HandLandmarkerWorker;
    Tensor<float> m_DetectorInput;
    Tensor<float> m_LandmarkerInput;
    Awaitable m_RunLoopAwaitable;

    WebCamTexture m_WebCamTexture;
    bool m_DiagnosticLogged;

    public bool IsReady { get; private set; }
    public bool HandVisible { get; private set; }
    public HandGestureAnalyzer Analyzer { get; private set; }

    void Awake()
    {
        Analyzer = new HandGestureAnalyzer();
    }

    void Start()
    {
        StartCoroutine(RequestPermissionAndStart());
    }

    IEnumerator RequestPermissionAndStart()
    {
#if UNITY_ANDROID
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
        {
            Debug.Log("[HandLandmarkDetector] Camera permission not granted. Requesting...");
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
            yield return new WaitForSeconds(1f);
        }

        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
        {
            Debug.LogWarning("[HandLandmarkDetector] Camera permission denied. Hand detection disabled.");
            IsReady = false;
            yield break;
        }
#endif

        if (!TryStartWebCamTexture())
        {
            Debug.LogWarning("[HandLandmarkDetector] No suitable camera device found. Hand detection disabled.");
            IsReady = false;
            yield break;
        }

        // WebCamTexture reports a 16x16 placeholder until the device actually starts streaming.
        float elapsed = 0f;
        while (m_WebCamTexture != null && m_WebCamTexture.width <= 16 && elapsed < 5f)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (m_WebCamTexture == null || m_WebCamTexture.width <= 16)
        {
            Debug.LogWarning("[HandLandmarkDetector] Webcam did not start in time. Hand detection disabled.");
            IsReady = false;
            yield break;
        }

        m_RunLoopAwaitable = RunModelsAndDetectionLoop();
    }

    bool TryStartWebCamTexture()
    {
        var devices = WebCamTexture.devices;
        if (devices == null || devices.Length == 0) return false;

        string deviceName = devices[0].name;
        foreach (var device in devices)
        {
            if (!device.isFrontFacing)
            {
                deviceName = device.name;
                break;
            }
        }

        m_WebCamTexture = new WebCamTexture(deviceName, requestedWidth, requestedHeight, requestedFps);
        m_WebCamTexture.Play();
        return true;
    }

    async Awaitable RunModelsAndDetectionLoop()
    {
        if (!InitializeModels())
        {
            IsReady = false;
            return;
        }

        IsReady = true;

        var frameCounter = 0;
        while (true)
        {
            try
            {
                if (frameCounter >= inferenceIntervalFrames)
                {
                    frameCounter = 0;
                    await Detect(m_WebCamTexture);
                }
                else
                {
                    frameCounter++;
                    await Awaitable.NextFrameAsync();
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        IsReady = false;

        m_HandDetectorWorker?.Dispose();
        m_HandLandmarkerWorker?.Dispose();
        m_DetectorInput?.Dispose();
        m_LandmarkerInput?.Dispose();
    }

    bool InitializeModels()
    {
        if (handDetector == null || handLandmarker == null || anchorsCSV == null)
        {
            Debug.LogError("[HandLandmarkDetector] Missing handDetector/handLandmarker/anchorsCSV reference(s). Assign them in the Inspector.");
            return false;
        }

        try
        {
            m_Anchors = BlazeUtils.LoadAnchors(anchorsCSV.text, k_NumAnchors);

            var handDetectorModel = ModelLoader.Load(handDetector);
            var detectorInputShape = handDetectorModel.inputs[0].shape;

            // post process the model to filter scores + argmax select the best hand
            var graph = new FunctionalGraph();
            var input = graph.AddInput(handDetectorModel, 0);
            var outputs = Functional.Forward(handDetectorModel, input);
            var boxes = outputs[0]; // (1, 2016, 18)
            var scores = outputs[1]; // (1, 2016, 1)
            var idx_scores_boxes = BlazeUtils.ArgMaxFiltering(boxes, scores);
            handDetectorModel = graph.Compile(idx_scores_boxes.Item1, idx_scores_boxes.Item2, idx_scores_boxes.Item3);

            m_HandDetectorWorker = new Worker(handDetectorModel, BackendType.GPUCompute);

            var handLandmarkerModel = ModelLoader.Load(handLandmarker);
            var landmarkerInputShape = handLandmarkerModel.inputs[0].shape;
            m_HandLandmarkerWorker = new Worker(handLandmarkerModel, BackendType.GPUCompute);

            m_DetectorInput = new Tensor<float>(new TensorShape(1, detectorInputSize, detectorInputSize, 3));
            m_LandmarkerInput = new Tensor<float>(new TensorShape(1, landmarkerInputSize, landmarkerInputSize, 3));

            if (!m_DiagnosticLogged)
            {
                Debug.Log($"[HandLandmarkDetector] detector input shape: {detectorInputShape}, landmarker input shape: {landmarkerInputShape}");
                m_DiagnosticLogged = true;
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[HandLandmarkDetector] Failed to load hand-tracking models: {e}");
            return false;
        }
    }

    void Update()
    {
        if (cameraFeedImage != null && m_WebCamTexture != null && m_WebCamTexture.isPlaying)
            cameraFeedImage.texture = m_WebCamTexture;
    }

    async Awaitable Detect(Texture texture)
    {
        if (texture == null) return;

        var size = Mathf.Max(texture.width, texture.height);

        // The affine transformation matrix to go from tensor coordinates to image coordinates
        var scale = size / (float)detectorInputSize;
        var M = BlazeUtils.mul(BlazeUtils.TranslationMatrix(0.5f * (new Vector2(texture.width, texture.height) + new Vector2(-size, size))), BlazeUtils.ScaleMatrix(new Vector2(scale, -scale)));
        BlazeUtils.SampleImageAffine(texture, m_DetectorInput, M);

        m_HandDetectorWorker.Schedule(m_DetectorInput);

        var outputIdxAwaitable = (m_HandDetectorWorker.PeekOutput(0) as Tensor<int>).ReadbackAndCloneAsync();
        var outputScoreAwaitable = (m_HandDetectorWorker.PeekOutput(1) as Tensor<float>).ReadbackAndCloneAsync();
        var outputBoxAwaitable = (m_HandDetectorWorker.PeekOutput(2) as Tensor<float>).ReadbackAndCloneAsync();

        using var outputIdx = await outputIdxAwaitable;
        using var outputScore = await outputScoreAwaitable;
        using var outputBox = await outputBoxAwaitable;

        var scorePassesThreshold = outputScore[0] >= scoreThreshold;
        HandVisible = scorePassesThreshold;

        if (!scorePassesThreshold)
            return;

        var idx = outputIdx[0];

        var anchorPosition = detectorInputSize * new float2(m_Anchors[idx, 0], m_Anchors[idx, 1]);

        var boxCentre_TensorSpace = anchorPosition + new float2(outputBox[0, 0, 0], outputBox[0, 0, 1]);
        var boxSize_TensorSpace = math.max(outputBox[0, 0, 2], outputBox[0, 0, 3]);

        var kp0_TensorSpace = anchorPosition + new float2(outputBox[0, 0, 4 + 2 * 0 + 0], outputBox[0, 0, 4 + 2 * 0 + 1]);
        var kp2_TensorSpace = anchorPosition + new float2(outputBox[0, 0, 4 + 2 * 2 + 0], outputBox[0, 0, 4 + 2 * 2 + 1]);
        var delta_TensorSpace = kp2_TensorSpace - kp0_TensorSpace;
        var up_TensorSpace = delta_TensorSpace / math.length(delta_TensorSpace);
        var theta = math.atan2(delta_TensorSpace.y, delta_TensorSpace.x);
        var rotation = 0.5f * Mathf.PI - theta;
        boxCentre_TensorSpace += 0.5f * boxSize_TensorSpace * up_TensorSpace;
        boxSize_TensorSpace *= 2.6f;

        var origin2 = new float2(0.5f * landmarkerInputSize, 0.5f * landmarkerInputSize);
        var scale2 = boxSize_TensorSpace / landmarkerInputSize;
        var M2 = BlazeUtils.mul(M, BlazeUtils.mul(BlazeUtils.mul(BlazeUtils.mul(BlazeUtils.TranslationMatrix(boxCentre_TensorSpace), BlazeUtils.ScaleMatrix(new float2(scale2, -scale2))), BlazeUtils.RotationMatrix(rotation)), BlazeUtils.TranslationMatrix(-origin2)));
        BlazeUtils.SampleImageAffine(texture, m_LandmarkerInput, M2);

        m_HandLandmarkerWorker.Schedule(m_LandmarkerInput);

        var landmarksAwaitable = (m_HandLandmarkerWorker.PeekOutput("Identity") as Tensor<float>).ReadbackAndCloneAsync();
        using var landmarks = await landmarksAwaitable;

        var landmarkPositions = new Vector3[k_NumKeypoints];
        for (var i = 0; i < k_NumKeypoints; i++)
        {
            var position_ImageSpace = BlazeUtils.mul(M2, new float2(landmarks[3 * i + 0], landmarks[3 * i + 1]));
            landmarkPositions[i] = new Vector3(position_ImageSpace.x, position_ImageSpace.y, landmarks[3 * i + 2]);
        }

        Analyzer?.PushFrame(landmarkPositions, Time.time);
    }

    void OnDestroy()
    {
        m_RunLoopAwaitable?.Cancel();

        if (m_WebCamTexture != null)
        {
            m_WebCamTexture.Stop();
            Destroy(m_WebCamTexture);
            m_WebCamTexture = null;
        }
    }
}
