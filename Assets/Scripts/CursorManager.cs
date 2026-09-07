using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance;
    
    [Header("Cursor Textures")]
    public Texture2D cursorMouse;
    public Texture2D cursorClick;
    
    [Header("Cursor Settings")]
    public Vector2 cursorHotspot = Vector2.zero;
    
    private bool isOverClickable = false;
    private bool isExternalControl = false; // Flag to indicate external scripts are controlling cursor
    private Vector2 _lastMousePosition; // OPT-1: Skip raycast when mouse hasn't moved
    
    // Property to access isOverClickable (prevents CS0414 warning)
    public bool IsOverClickable => isOverClickable;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad only works on root GameObjects
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
            DontDestroyOnLoad(gameObject);
            LoadCursorTextures();
            SetDefaultCursor();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        SetupEventTriggers();
    }
    
    void Update()
    {
        // Skip automatic cursor management if external script is controlling it
        if (isExternalControl) return;
        if (!isOverClickable) return;
        
        // OPT-1: Only raycast when mouse has actually moved
        Vector2 currentPos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        if (currentPos == _lastMousePosition) return;
        _lastMousePosition = currentPos;
        
        // Check if we're still over a valid UI element
        if (!IsPointerOverValidUIElement())
        {
            SetDefaultCursor();
        }
    }
    
    // Cached for IsPointerOverValidUIElement to avoid per-frame allocations
    private PointerEventData _cachedPointerData;
    private readonly System.Collections.Generic.List<RaycastResult> _cachedRaycastResults = new System.Collections.Generic.List<RaycastResult>();
    
    private bool IsPointerOverValidUIElement()
    {
        // Check if pointer is over any UI element
        if (EventSystem.current == null)
            return false;
            
        // Reuse cached PointerEventData
        if (_cachedPointerData == null)
        {
            _cachedPointerData = new PointerEventData(EventSystem.current);
        }
        _cachedPointerData.position = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        
        // Reuse cached results list
        _cachedRaycastResults.Clear();
        EventSystem.current.RaycastAll(_cachedPointerData, _cachedRaycastResults);
        
        // OPT-2: Single GetComponent<Selectable> covers both Button and Selectable (Button inherits Selectable)
        foreach (var result in _cachedRaycastResults)
        {
            if (result.gameObject == null) continue;
            
            Selectable selectable = result.gameObject.GetComponent<Selectable>();
            if (selectable != null && selectable.interactable && selectable.gameObject.activeInHierarchy)
                return true;
        }
        
        return false;
    }
    
    private void LoadCursorTextures()
    {
        if (cursorMouse == null)
            cursorMouse = Resources.Load<Texture2D>("Image/In-Game/Cursor/Cursor_Mouse");
        
        if (cursorClick == null)
            cursorClick = Resources.Load<Texture2D>("Image/In-Game/Cursor/Cursor_Click");
            
        if (cursorMouse == null)
            Debug.LogError("CursorManager: Cursor_Mouse texture not found!");
        if (cursorClick == null)
            Debug.LogError("CursorManager: Cursor_Click texture not found!");
    }
    
    private void SetupEventTriggers()
    {
        // OPT-3: Single FindObjectsByType call (removed duplicate second loop)
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button button in buttons)
        {
            AddCursorEvents(button.gameObject);
        }
    }
    
    private void AddCursorEvents(GameObject obj)
    {
        // Check if this is part of a Base panel
        bool isInBasePanel = IsPartOfBasePanel(obj);
        
        // Check if this GameObject has an interactive component (Button, Selectable, etc.)
        bool hasInteractiveComponent = obj.GetComponent<Button>() != null || 
                                      obj.GetComponent<Selectable>() != null;
        
        // Skip non-interactive elements in Base panels (backgrounds, etc.)
        // But allow buttons and other interactive elements even in Base panels
        if (isInBasePanel && !hasInteractiveComponent)
        {
            return;
        }
        
        // OPT-6: OrdinalIgnoreCase avoids string allocation from ToLower()
        if (obj.name.IndexOf("background", System.StringComparison.OrdinalIgnoreCase) >= 0 || 
            obj.GetComponent<Image>()?.raycastTarget == true && obj.GetComponent<Button>() == null)
        {
            return;
        }
        
        EventTrigger trigger = obj.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = obj.AddComponent<EventTrigger>();
            
        // Add pointer enter event
        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => SetClickCursor());
        trigger.triggers.Add(enterEntry);
        
        // Add pointer exit event
        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => SetDefaultCursor());
        trigger.triggers.Add(exitEntry);
    }
    
    /// <summary>
    /// Check if a GameObject or any of its parents belongs to a Base panel
    /// </summary>
    private bool IsPartOfBasePanel(GameObject obj)
    {
        Transform current = obj.transform;
        
        // Traverse up the hierarchy to find UIPanel component
        while (current != null)
        {
            UIPanel panel = current.GetComponent<UIPanel>();
            if (panel != null)
            {
                // Check if this panel is a Base panel
                return panel.BehaviorType == PanelBehaviorType.Base;
            }
            current = current.parent;
        }
        
        return false;
    }
    
    public void SetDefaultCursor()
    {
        if (cursorMouse != null)
        {
            Cursor.SetCursor(cursorMouse, cursorHotspot, CursorMode.Auto);
            isOverClickable = false;
        }
    }
    
    public void SetClickCursor()
    {
        if (cursorClick != null)
        {
            Cursor.SetCursor(cursorClick, cursorHotspot, CursorMode.Auto);
            isOverClickable = true;
        }
    }
    
    /// <summary>
    /// Enable external control mode - prevents CursorManager from auto-resetting cursor
    /// Call this when external scripts (like Live2D interactions) want to control the cursor
    /// </summary>
    public void EnableExternalControl()
    {
        isExternalControl = true;
    }
    
    /// <summary>
    /// Disable external control mode - allows CursorManager to resume auto-management
    /// </summary>
    public void DisableExternalControl()
    {
        isExternalControl = false;
    }
    
    // Call this method when new UI elements are created dynamically
    public void RefreshCursorEvents()
    {
        SetupEventTriggers();
    }
    
    void OnDestroy()
    {
        if (Instance == this)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            Instance = null;
        }
    }
}