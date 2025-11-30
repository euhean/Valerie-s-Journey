using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BeatVisualizer : MonoBehaviour
{
    [Header("UI References")]
    public Image[] bars; // las 4 barras
    public Color beatColor = Color.yellow;   // color cuando "golpea" el beat (solo música)
    public Color baseColor = Color.white;    // color normal
    public Color onBeatColor = Color.green;  // color cuando input es ON-BEAT (ACIERTO)
    public Color offBeatColor = Color.red;   // color cuando input es OFF-BEAT (FALLO)
    public float flashDuration = 0.3f;

    private TimeManager timeManager;
    private InputManager inputManager;
    private int currentBar = 0;
    private bool inputProcessedThisFrame = false;
    private Coroutine activeCoroutine; // Guarda la corrutina activa
    private bool isPaused = false;
    private bool isInitialized = false;

    [System.Obsolete]
    private void Start()
    {
        timeManager = FindObjectOfType<TimeManager>();
        inputManager = FindObjectOfType<InputManager>();

        if (bars == null || bars.Length == 0)
        {
            DebugHelper.LogWarning("[BeatVisualizer] Bars array not assigned. Disabling component.");
            enabled = false;
            return;
        }

        // Suscribirse a los eventos
        if (timeManager != null)
            timeManager.OnBeat += HandleBeat;

        if (inputManager != null)
            inputManager.OnBasicPressedDSP += HandleInput;

        // Inicializar barras con color base
        foreach (var bar in bars)
        {
            if (bar != null)
                bar.color = baseColor;
        }

        isInitialized = true;
    }

    private void OnDestroy()
    {
        // Desuscribirse de los eventos
        if (timeManager != null)
            timeManager.OnBeat -= HandleBeat;

        if (inputManager != null)
            inputManager.OnBasicPressedDSP -= HandleInput;
    }

    /// <summary>
    /// Se ejecuta cada vez que hay un beat en la música
    /// </summary>
    private void HandleBeat(int beatIndex)
    {
        if (isPaused || !isInitialized) return;

        // Si no presionaste nada en el beat anterior, la barra actual se pone ROJA (fallo)
        if (!inputProcessedThisFrame)
        {
            FlashBar(bars[currentBar], offBeatColor);
            Debug.Log($"✗ FALLO: No presionaste en el beat - Barra[{currentBar}] en ROJO");
        }

        // Pasar a la siguiente barra
        currentBar = (currentBar + 1) % bars.Length;

        // La nueva barra se pone AMARILLA
        FlashBar(bars[currentBar], beatColor);

        // Resetea el flag de input para permitir nuevo input en el siguiente beat
        inputProcessedThisFrame = false;
    }

    /// <summary>
    /// Se ejecuta cuando el jugador presiona el botón Basic Action
    /// Solo procesa UN input por beat
    /// </summary>
    private void HandleInput(double dspTime)
    {
        if (isPaused || timeManager == null || !isInitialized)
            return;

        // Si ya procesamos un input en este beat, ignora los demás
        if (inputProcessedThisFrame)
        {
            Debug.Log("⚠️ Input ignorado: Ya hay un input procesado en este beat");
            return;
        }

        // Verificar si el input está dentro de la ventana "on beat"
        bool onBeat = timeManager.IsOnBeat(dspTime);

        // Elegir color según si fue on-beat u off-beat
        Color colorToFlash = onBeat ? onBeatColor : offBeatColor;
        string resultText = onBeat ? "✓ ACIERTO (ON-BEAT)" : "✗ FALLO (OFF-BEAT)";

        // Cambiar la barra actual al color del resultado
        FlashBar(bars[currentBar], colorToFlash);

        // Marcar que ya procesamos un input en este beat
        inputProcessedThisFrame = true;

        // Debug
        Debug.Log($"{resultText} - DSP: {dspTime:F4}");
    }

    /// <summary>
    /// Ejecuta un flash de color en una barra (detiene cualquier corrutina anterior)
    /// </summary>
    private void FlashBar(Image bar, Color flashColor)
    {
        if (isPaused || bar == null || !isInitialized) return;
        // Detener la corrutina anterior si existe
        if (activeCoroutine != null)
            StopCoroutine(activeCoroutine);

        // Resetear todos los colores a blanco primero
        foreach (var b in bars)
            b.color = baseColor;

        // Iniciar la nueva corrutina
        activeCoroutine = StartCoroutine(FlashCoroutine(bar, flashColor));
    }

    /// <summary>
    /// Corrutina que anima el flash de color
    /// </summary>
    private IEnumerator FlashCoroutine(Image bar, Color flashColor)
    {
        if (bar == null) yield break;
        bar.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        if (bar != null)
            bar.color = baseColor;
    }

    /// <summary>
    /// Allows external systems (GameManager) to pause all visual updates, e.g., after player death.
    /// </summary>
    public void SetPaused(bool paused)
    {
        isPaused = paused;
        if (paused)
        {
            if (activeCoroutine != null)
            {
                StopCoroutine(activeCoroutine);
                activeCoroutine = null;
            }

            if (bars != null)
            {
                foreach (var bar in bars)
                {
                    if (bar != null)
                        bar.color = baseColor;
                }
            }
        }
    }
}