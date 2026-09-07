using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using EventBus;

#nullable enable

/// <summary>
/// Handles screen transition animations with fade in/out effects.
/// Used for area changes and game section transitions.
/// Listens to TransitionRequestEvent to start transitions.
/// </summary>
[RequireComponent(typeof(Image))]
[RequireComponent(typeof(CanvasGroup))]
public sealed class TransitionPanel : MonoBehaviour
{
    public static TransitionPanel? Instance { get; private set; }
    
    [Header("Transition Settings")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float midPointWaitDuration = 1f;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    [Header("References")]
    [SerializeField] private Image transitionImage = null!;
    [SerializeField] private CanvasGroup canvasGroup = null!;
    
    private Coroutine? currentTransition;
    private EventBinding<TransitionRequestEvent>? transitionRequestBinding;
    
    private void Awake()
    {
        Log("Awake called");
        
        if (Instance != null)
        {
            Debug.LogWarning("[TransitionPanel] Instance already exists, destroying duplicate");
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        Log("Instance set");
        
        if (this.transitionImage == null)
            this.transitionImage = GetComponent<Image>();
        
        if (this.canvasGroup == null)
            this.canvasGroup = GetComponent<CanvasGroup>();
        
        this.canvasGroup.alpha = 0f;
        this.canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
        
        Log("Registering for TransitionRequestEvent");
        transitionRequestBinding = new EventBinding<TransitionRequestEvent>(OnTransitionRequest);
        EventBus<TransitionRequestEvent>.Register(transitionRequestBinding);
        Log("Initialization complete");
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        
        if (transitionRequestBinding != null)
        {
            EventBus<TransitionRequestEvent>.Deregister(transitionRequestBinding);
            transitionRequestBinding = null;
        }
    }
    
    private void OnTransitionRequest(TransitionRequestEvent evt)
    {
        Log("OnTransitionRequest received!");
        
        // CRITICAL: Activate GameObject before starting coroutine
        if (!gameObject.activeSelf)
        {
            Log("GameObject was inactive, activating now");
            gameObject.SetActive(true);
        }
        
        if (this.currentTransition != null)
        {
            Log("Stopping existing transition");
            StopCoroutine(this.currentTransition);
        }
        
        Log("Starting TransitionSequence");
        this.currentTransition = StartCoroutine(TransitionSequence());
    }
    
    private IEnumerator TransitionSequence()
    {
        Log("TransitionSequence started");
        
        gameObject.SetActive(true);
        this.canvasGroup.blocksRaycasts = true;
        Log("GameObject activated, starting fade in");
        
        yield return FadeIn();
        
        Log($"Fade in complete, raising TransitionMidPointEvent and waiting {midPointWaitDuration}s");
        EventBus<TransitionMidPointEvent>.Raise(new TransitionMidPointEvent());
        
        // Wait at mid-point for setup to complete
        yield return new WaitForSeconds(midPointWaitDuration);
        
        Log("Mid-point wait complete, starting fade out");
        yield return FadeOut();
        
        Log("Fade out complete");
        this.canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
        
        Log("Raising TransitionCompleteEvent");
        EventBus<TransitionCompleteEvent>.Raise(new TransitionCompleteEvent());
        Log("TransitionSequence complete");
    }
    
    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        
        while (elapsed < this.fadeDuration)
        {
            elapsed += Time.deltaTime;
            this.canvasGroup.alpha = Mathf.Clamp01(elapsed / this.fadeDuration);
            yield return null;
        }
        
        this.canvasGroup.alpha = 1f;
    }
    
    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        float startAlpha = this.canvasGroup.alpha;
        
        while (elapsed < this.fadeDuration)
        {
            elapsed += Time.deltaTime;
            this.canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / this.fadeDuration);
            yield return null;
        }
        
        this.canvasGroup.alpha = 0f;
    }
    
    private void Log(string message)
    {
        if (enableDebugLogs) Debug.Log($"[TransitionPanel] {message}");
    }
}
