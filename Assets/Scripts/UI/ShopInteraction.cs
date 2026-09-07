using UnityEngine;
using UnityEngine.InputSystem;
using Live2D.Cubism.Core;
using Live2D.Cubism.Framework.Raycasting;
using MaisLoveStory.Live2D;
using Base;
using FMODUnity;

/// <summary>
/// Handles clicking on the Live2D Staff character to open/close the shop
/// Integrates with UIPanel system and AudioManager for proper sound management
/// </summary>
public class ShopInteraction : MonoBehaviour
{
    [Header("Live2D Components")]
    [Tooltip("Reference to the CubismRaycaster on the Live2D Staff GameObject")]
    [SerializeField] private CubismRaycaster raycaster;

    [Tooltip("Reference to the Live2DMotionController on the Live2D Staff GameObject")]
    [SerializeField] private Live2DMotionController motionController;

    [Header("Shop Panel")]
    [Tooltip("Panel ID of the shop panel (e.g., 'Shop Popup')")]
    [SerializeField] private string shopPanelId = "Shop Popup";

    /// <summary>
    /// Buffer for raycast results
    /// </summary>
    private CubismRaycastHit[] raycastResults;

    /// <summary>
    /// Is the shop currently open?
    /// </summary>
    private bool isShopOpen = false;

    /// <summary>
    /// Cached main camera reference to avoid per-frame Camera.main lookups
    /// </summary>
    private Camera _mainCamera;

    private void Start()
    {
        // Cache the main camera
        _mainCamera = Camera.main;
        
        // Validate raycaster reference
        if (raycaster == null)
        {
            Debug.LogError("ShopInteraction: CubismRaycaster reference is not set! " +
                "Please assign the CubismRaycaster component in the Inspector.");
        }

        // Initialize raycast results buffer
        raycastResults = new CubismRaycastHit[4];

        // Subscribe to panel close events to reset Staff motion when shop closes
        if (UIPanelManager.Instance != null)
        {
            UIPanelManager.Instance.OnAnyPanelClosed.AddListener(OnPanelClosed);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (UIPanelManager.Instance != null)
        {
            UIPanelManager.Instance.OnAnyPanelClosed.RemoveListener(OnPanelClosed);
        }
    }

    private void OnPanelClosed(string panelId)
    {
        // If the shop panel was closed, reset Staff motion
        if (panelId == shopPanelId && isShopOpen)
        {
            // Reset Staff motion to default
            if (motionController != null)
            {
                motionController.PlayDefaultMotion();
            }

            isShopOpen = false;
        }
    }

    private void Update()
    {
        // Only check for clicks when the raycaster is available and active
        if (raycaster == null || !raycaster.gameObject.activeInHierarchy)
        {
            return;
        }

        // Check if hovering over Staff to change cursor
        CheckStaffHover();

        // Check for mouse click
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            CheckStaffClick();
        }
    }

    /// <summary>
    /// Checks if the player is hovering over the Live2D Staff character and updates cursor
    /// </summary>
    private void CheckStaffHover()
    {
        if (CursorManager.Instance == null)
        {
            return;
        }

        // Check if mouse is over UI - if so, don't change cursor for Live2D
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            CursorManager.Instance.DisableExternalControl();
            return;
        }

        // Cast ray from mouse position
        Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        Ray ray = _mainCamera.ScreenPointToRay(mousePos);
        int hitCount = raycaster.Raycast(ray, raycastResults);

        // If we hit any part of the staff model, show click cursor
        if (hitCount > 0)
        {
            CursorManager.Instance.EnableExternalControl();
            if (!CursorManager.Instance.IsOverClickable)
            {
                CursorManager.Instance.SetClickCursor();
            }
        }
        else
        {
            // Not hovering over Staff, reset cursor and disable external control
            CursorManager.Instance.DisableExternalControl();
            if (CursorManager.Instance.IsOverClickable)
            {
                CursorManager.Instance.SetDefaultCursor();
            }
        }
    }

    /// <summary>
    /// Checks if the player clicked on the Live2D Staff character
    /// </summary>
    private void CheckStaffClick()
    {
        if (raycaster == null)
        {
            return;
        }

        // Check if mouse is over UI - if so, ignore Live2D clicks
        // if (UnityEngine.EventSystems.EventSystem.current != null &&
        //     UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        // {
        //     if (debugMode)
        //     {
        //         Debug.Log("Mouse is over UI - ignoring Live2D click");
        //     }
        //     return;
        // }

        // Cast ray from mouse position
        Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        Ray ray = _mainCamera.ScreenPointToRay(mousePos);
        int hitCount = raycaster.Raycast(ray, raycastResults);

        // If we hit any part of the staff model
        if (hitCount > 0)
        {
            // if (debugMode)
            // {
            //     // Log all clicked parts
            //     Debug.Log($"Clicked {hitCount} part(s):");
            //     for (int i = 0; i < hitCount; i++)
            //     {
            //         CubismRaycastHit hit = raycastResults[i];
            //         Debug.Log($"  [{i}] Drawable: {hit.Drawable.name}");
            //         Debug.Log($"      Distance: {hit.Distance}");
            //         Debug.Log($"      World Position: {hit.WorldPosition}");
            //         Debug.Log($"      Local Position: {hit.LocalPosition}");
            //     }
            // }

            // Get the closest hit (first in array is usually closest)
            CubismDrawable clickedDrawable = raycastResults[0].Drawable;

            // Handle the click based on which part was clicked
            HandlePartClick(clickedDrawable, raycastResults[0]);
        }
    }

    /// <summary>
    /// Handles clicks on specific parts of the Live2D model
    /// </summary>
    /// <param name="drawable">The drawable that was clicked</param>
    /// <param name="hit">The raycast hit information</param>
    private void HandlePartClick(CubismDrawable drawable, CubismRaycastHit hit)
    {
        string partName = drawable.name.ToLower();

        // if (debugMode)
        // {
        //     Debug.Log($"Clicked on {drawable.name} - Toggling shop!");
        // }

        // Any part of the staff opens/closes the shop
        // You can add specific logic here if needed:
        // if (partName.Contains("head") || partName.Contains("face")) { ... }

        ToggleShop();
    }

    /// <summary>
    /// Toggles the shop open/closed state
    /// </summary>
    private void ToggleShop()
    {
        // Check actual panel state instead of relying on local flag
        bool isPanelVisible = UIPanelManager.Instance != null &&
                             UIPanelManager.Instance.IsPanelVisible(shopPanelId);

        if (isPanelVisible)
        {
            // Panel is open, close it
            CloseShop();
            isShopOpen = false;
        }
        else
        {
            // Panel is closed, open it
            OpenShop();
            isShopOpen = true;
        }
    }

    /// <summary>
    /// Opens the shop and plays the open sound
    /// </summary>
    private void OpenShop()
    {
        if (UIPanelManager.Instance == null)
        {
            Debug.LogError("ShopInteraction: UIPanelManager.Instance is null!");
            return;
        }

        // Play smile motion when shop opens
        if (motionController != null)
        {
            // Try to play "Smile" motion - check for Staff-specific parameters first
            if (motionController.HasParameter("Staff Smile Loop"))
            {
                motionController.PlayMotion("Staff Smile Loop", true);
            }
            else if (motionController.HasParameter("Smile"))
            {
                motionController.PlayMotion("Smile", false);
            }
            else if (motionController.HasParameter("MaiSmileTeeth"))
            {
                motionController.PlayMotion("MaiSmileTeeth", false);
            }
            else if (motionController.HasParameter("Happy"))
            {
                motionController.PlayMotion("Happy", false);
            }
            else
            {
                Debug.LogWarning("[ShopInteraction] No smile-related parameters found in animator!");
            }

            // if (debugMode)
            // {
            //     Debug.Log("Staff playing smile motion");
            // }
        }

        // Show shop panel using UIPanel system
        UIPanelManager.Instance.ShowPanel(shopPanelId);

        // Shop open sound is played by ShopPanel.OnShow()

        // if (debugMode)
        // {
        //     Debug.Log("Shop opened!");
        // }
    }

    /// <summary>
    /// Closes the shop and plays the close sound
    /// </summary>
    private void CloseShop()
    {
        if (UIPanelManager.Instance == null)
        {
            Debug.LogError("ShopInteraction: UIPanelManager.Instance is null!");
            return;
        }

        // Return to default motion when shop closes
        if (motionController != null)
        {
            motionController.PlayDefaultMotion();

            // if (debugMode)
            // {
            //     Debug.Log("Staff returning to default motion");
            // }
        }

        // Hide shop panel using UIPanel system
        UIPanelManager.Instance.HidePanel(shopPanelId);

        // Shop close sound is played by ShopPanel.OnHide()

        // if (debugMode)
        // {
        //     Debug.Log("Shop closed!");
        // }
    }

    /// <summary>
    /// Public method to open the shop (can be called from other scripts)
    /// </summary>
    public void ForceOpenShop()
    {
        if (!isShopOpen)
        {
            isShopOpen = true;
            OpenShop();
        }
    }

    /// <summary>
    /// Public method to close the shop (can be called from other scripts)
    /// </summary>
    public void ForceCloseShop()
    {
        if (isShopOpen)
        {
            isShopOpen = false;
            CloseShop();
        }
    }

    /// <summary>
    /// Gets all the drawable parts that were clicked
    /// </summary>
    /// <returns>Array of clicked drawable names, or empty array if nothing was clicked</returns>
    public string[] GetClickedParts()
    {
        if (raycaster == null)
        {
            return new string[0];
        }

        Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        Ray ray = _mainCamera.ScreenPointToRay(mousePos);
        int hitCount = raycaster.Raycast(ray, raycastResults);

        if (hitCount == 0)
        {
            return new string[0];
        }

        string[] partNames = new string[hitCount];
        for (int i = 0; i < hitCount; i++)
        {
            partNames[i] = raycastResults[i].Drawable.name;
        }

        return partNames;
    }

    /// <summary>
    /// Checks if a specific part name was clicked (case-insensitive, supports partial matching)
    /// </summary>
    /// <param name="partNameToCheck">The part name to check for (e.g., "head", "body")</param>
    /// <returns>True if any clicked part contains the specified name</returns>
    public bool WasPartClicked(string partNameToCheck)
    {
        string[] clickedParts = GetClickedParts();

        foreach (string partName in clickedParts)
        {
            if (partName.ToLower().Contains(partNameToCheck.ToLower()))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Get the shop panel's current visibility state
    /// </summary>
    public bool IsShopOpen()
    {
        return isShopOpen;
    }
}
