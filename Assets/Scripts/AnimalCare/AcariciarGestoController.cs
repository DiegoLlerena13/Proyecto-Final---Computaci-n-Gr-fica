using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Version reducida de GestureCaptureController (Assets/Scripts/Capture/) adaptada a "acariciar":
// no spawnea ni destruye animal (el que ya esta en el carrusel de MascotaSelector se queda quieto
// de fondo), y solo cuenta el gesto Affection (palma abierta quieta) - a diferencia de
// GestureCaptureController, que reacciona a cualquier GestureType sin filtrar.
public class AcariciarGestoController : MonoBehaviour
{
    [SerializeField] private HandLandmarkDetector handDetector;
    [SerializeField] private MascotaSelector mascotaSelector;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI mensajeText;
    [SerializeField] private Image handIndicatorImage;
    [SerializeField] private int requiredActions = 3;
    [SerializeField] private float readyTimeoutSeconds = 20f;

    private static readonly Color HandIdleColor = new(1f, 1f, 1f, 0.35f);
    private static readonly Color HandSeenColor = new(0.4f, 0.85f, 1f, 0.9f);
    private static readonly Color HandFlashColor = new(0.3f, 1f, 0.4f, 1f);
    private const float FlashSeconds = 0.5f;
    private const string MensajeCargando = "Preparando la cámara...";
    private const string MensajeEspera = "Mostrá tu mano abierta y quieta frente a la cámara para acariciar.";
    private const string MensajeSinCamara = "No se pudo activar la cámara. Cerrá esta ventana y usá 'En pantalla'.";

    private int completedCount;
    private bool hasFinished;
    private bool subscribed;
    private Coroutine readyWaitRoutine;
    private Coroutine handFlashRoutine;

    private void OnEnable()
    {
        completedCount = 0;
        hasFinished = false;
        // Show a distinct "loading" message until the detector is actually ready - the webcam +
        // Sentis model take a few seconds to start on every fresh open, and PushFrame() never
        // runs during that window, so gestures made while MensajeEspera was shown too early were
        // silently dropped and looked like "detection isn't counting."
        if (mensajeText != null)
            mensajeText.text = (handDetector != null && handDetector.IsReady) ? MensajeEspera : MensajeCargando;
        if (handIndicatorImage != null) handIndicatorImage.color = HandIdleColor;
        UpdateProgressUI();

        // Same fix as GestureCaptureController: the analyzer's buffer/cooldown survive between
        // sessions since HandLandmarkDetector's loop never truly pauses, so clear it before reading
        // new gestures - otherwise leftover motion from before this panel opened gets judged first.
        handDetector?.Analyzer?.Reset();

        Subscribe();
        readyWaitRoutine = StartCoroutine(WaitForReadyOrTimeout());
    }

    private void OnDisable()
    {
        if (readyWaitRoutine != null)
        {
            StopCoroutine(readyWaitRoutine);
            readyWaitRoutine = null;
        }
        if (handFlashRoutine != null)
        {
            StopCoroutine(handFlashRoutine);
            handFlashRoutine = null;
        }
        Unsubscribe();
    }

    private void Update()
    {
        if (handIndicatorImage == null || handDetector == null || handFlashRoutine != null) return;
        handIndicatorImage.color = handDetector.HandVisible ? HandSeenColor : HandIdleColor;
    }

    private void Subscribe()
    {
        if (subscribed) return;
        if (handDetector != null && handDetector.Analyzer != null)
            handDetector.Analyzer.OnGestureRecognized += HandleGestureRecognized;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;
        if (handDetector != null && handDetector.Analyzer != null)
            handDetector.Analyzer.OnGestureRecognized -= HandleGestureRecognized;
        subscribed = false;
    }

    private IEnumerator WaitForReadyOrTimeout()
    {
        float elapsed = 0f;
        while (elapsed < readyTimeoutSeconds)
        {
            if (handDetector != null && handDetector.IsReady)
            {
                if (mensajeText != null) mensajeText.text = MensajeEspera;
                readyWaitRoutine = null;
                yield break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        readyWaitRoutine = null;
        if (mensajeText != null) mensajeText.text = MensajeSinCamara;
    }

    private void HandleGestureRecognized(GestureType gesture)
    {
        if (gesture != GestureType.Affection || hasFinished) return;

        if (handIndicatorImage != null)
        {
            if (handFlashRoutine != null) StopCoroutine(handFlashRoutine);
            handFlashRoutine = StartCoroutine(FlashIndicator());
        }

        completedCount++;
        UpdateProgressUI();

        if (completedCount >= requiredActions)
        {
            hasFinished = true;
            Unsubscribe();
            Finish();
        }
    }

    private IEnumerator FlashIndicator()
    {
        handIndicatorImage.color = HandFlashColor;
        yield return new WaitForSeconds(FlashSeconds);
        handIndicatorImage.color = HandIdleColor;
        handFlashRoutine = null;
    }

    private void UpdateProgressUI()
    {
        if (progressText != null) progressText.text = $"{completedCount}/{requiredActions}";
    }

    private void Finish()
    {
        if (mascotaSelector == null) return;
        mascotaSelector.Acariciar();
        mascotaSelector.CerrarGestoAcariciar();
    }
}
