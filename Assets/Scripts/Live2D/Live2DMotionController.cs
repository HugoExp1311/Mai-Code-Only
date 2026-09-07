using UnityEngine;
using Live2D.Cubism.Framework.Motion;
using Live2D.Cubism.Core;
using System.Collections.Generic;
using System.Linq;

namespace MaisLoveStory.Live2D
{
    /// <summary>
    /// Controls Live2D model motions
    /// Handles motion playback for different character states
    /// </summary>
    public class Live2DMotionController : MonoBehaviour
    {
        [Header("Live2D Components")]
        [SerializeField] private CubismModel cubismModel;
        [SerializeField] private Animator animator;
        
        private string currentMotionName = "";
        private string defaultStateName = "";
        private Dictionary<string, AnimatorControllerParameterType> parameterCache;
        
        private void Awake()
        {
            // Auto-find components if not assigned
            if (cubismModel == null)
            {
                cubismModel = GetComponent<CubismModel>();
            }
            
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
            
            if (cubismModel == null)
            {
                Debug.LogError("[Live2DMotionController] CubismModel not found!");
            }
            
            if (animator == null)
            {
                Debug.LogWarning("[Live2DMotionController] Animator not found! Motion playback may not work.");
            }
            
            // Cache animator parameters and default state
            CacheAnimatorParameters();
            CacheDefaultState();
        }
        
        private void Start()
        {
            // Don't play default motion on Start - let the animator stay in its default state
            // The animator will automatically be in the default state when enabled
            // This prevents NullReferenceException in CubismFadeStateObserver
            // Animator is now in default state
        }
        
        /// <summary>
        /// Cache the default state name from animator
        /// </summary>
        private void CacheDefaultState()
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }
            
#if UNITY_EDITOR
            // Get the default state name from the first layer (for logging purposes only)
            var controller = animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
            if (controller != null && controller.layers.Length > 0)
            {
                var defaultState = controller.layers[0].stateMachine.defaultState;
                if (defaultState != null)
                {
                    defaultStateName = defaultState.name;
                }
            }
#endif
        }
        
        /// <summary>
        /// Cache all animator parameters for quick lookup
        /// </summary>
        private void CacheAnimatorParameters()
        {
            parameterCache = new Dictionary<string, AnimatorControllerParameterType>();
            
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning("[Live2DMotionController] Cannot cache parameters - animator or controller is null");
                return;
            }
            
#if UNITY_EDITOR
            Debug.Log("[Live2DMotionController] ========== Caching Animator Parameters ==========");
            Debug.Log($"[Live2DMotionController] Total parameters: {animator.parameters.Length}");
#endif
            
            foreach (var param in animator.parameters)
            {
                parameterCache[param.name] = param.type;
#if UNITY_EDITOR
                Debug.Log($"[Live2DMotionController] Parameter: '{param.name}' | Type: {param.type}");
#endif
            }
            
#if UNITY_EDITOR
            Debug.Log("[Live2DMotionController] ========== Parameter Cache Complete ==========");
#endif
        }
        
        /// <summary>
        /// Play motion by parameter name
        /// Supports three animation patterns:
        /// 
        /// Pattern 1 (Trigger + Bool): For animations with default and loop versions
        /// 1. Set Trigger to play default version
        /// 2. Set Bool to true to queue loop version
        /// 3. Default plays, then transitions to loop (because Bool is true)
        /// 4. When skipping, set Bool to false to exit and return to Mai Normal
        /// 
        /// Pattern 2 (Bool only with "Loop" suffix): For loop-only animations (e.g., MaiTalkLoop)
        /// 1. Check if "{motionName}Loop" Bool parameter exists
        /// 2. If yes, set it to true directly (no Trigger needed)
        /// 3. Animation loops continuously until Bool is set to false
        /// 
        /// Pattern 3 (Trigger only): For one-shot animations
        /// 1. Set Trigger to play animation once
        /// 2. Animation returns to default state automatically
        /// </summary>
        /// <param name="motionName">Animator parameter name (base name without "Loop" suffix)</param>
        /// <param name="loop">Should the motion loop? If true, sets Bool to enable loop transition</param>
        /// <param name="floatValue">Float value (for Float parameters with "Value" suffix)</param>
        public void PlayMotion(string motionName, bool loop = false, float floatValue = 0f)
        {
#if UNITY_EDITOR
            Debug.Log($"[Live2DMotionController] PlayMotion called: motionName='{motionName}', loop={loop}, floatValue={floatValue}");
            
#endif
            
            if (animator == null || !animator.isActiveAndEnabled)
            {
                Debug.LogWarning($"[Live2DMotionController] Animator is null or not active! Cannot play motion: {motionName}");
                return;
            }
            
            if (string.IsNullOrEmpty(motionName))
            {
                Debug.LogWarning("[Live2DMotionController] Motion name is empty!");
                return;
            }
            
            // Step 1: Reset previous expression's Bool to false (exit current animation)
            if (!string.IsNullOrEmpty(currentMotionName) && currentMotionName != motionName)
            {
#if UNITY_EDITOR
                Debug.Log($"[Live2DMotionController] Resetting previous motion: '{currentMotionName}'");
#endif
                
                if (currentMotionName.EndsWith("Loop"))
                {
                    if (parameterCache.ContainsKey(currentMotionName) && 
                        parameterCache[currentMotionName] == AnimatorControllerParameterType.Bool)
                    {
                        animator.SetBool(currentMotionName, false);
#if UNITY_EDITOR
                        Debug.Log($"[Live2DMotionController] Reset Bool parameter (Pattern 2): '{currentMotionName}' = false");
#endif
                    }
                }
                else
                {
                    if (parameterCache.ContainsKey(currentMotionName) && 
                        parameterCache[currentMotionName] == AnimatorControllerParameterType.Bool)
                    {
                        animator.SetBool(currentMotionName, false);
#if UNITY_EDITOR
                        Debug.Log($"[Live2DMotionController] Reset Bool parameter (Pattern 1): '{currentMotionName}' = false");
#endif
                    }
#if UNITY_EDITOR
                    else
                    {
                        Debug.Log($"[Live2DMotionController] No Bool parameter found for '{currentMotionName}' (Pattern 1 without Bool, or Trigger-only)");
                    }
#endif
                }
            }
#if UNITY_EDITOR
            else
            {
                Debug.Log($"[Live2DMotionController] No previous motion to reset (currentMotionName='{currentMotionName}')");
            }
#endif
            
            // Step 2: Set Float parameter if it exists (for blend trees)
            string floatParameterName = motionName + "Value";
            if (parameterCache.ContainsKey(floatParameterName) && 
                parameterCache[floatParameterName] == AnimatorControllerParameterType.Float)
            {
                animator.SetFloat(floatParameterName, floatValue);
#if UNITY_EDITOR
                Debug.Log($"[Live2DMotionController] Set Float parameter: '{floatParameterName}' = {floatValue}");
#endif
            }
            
            // Step 3: Determine animation pattern and apply parameters
            bool hasTrigger = parameterCache.ContainsKey(motionName) && 
                             parameterCache[motionName] == AnimatorControllerParameterType.Trigger;
            bool hasBool = parameterCache.ContainsKey(motionName) && 
                          parameterCache[motionName] == AnimatorControllerParameterType.Bool;
            
            string loopParameterName = motionName + "Loop";
            bool hasLoopBool = parameterCache.ContainsKey(loopParameterName) && 
                              parameterCache[loopParameterName] == AnimatorControllerParameterType.Bool;
            
            // Pattern 1 can use EITHER base Bool OR Loop Bool for looping
            bool hasAnyLoopBool = hasBool || hasLoopBool;
            
            // Pattern 2: Loop-only animation (e.g., MaiTalkLoop)
            if (!hasTrigger && !hasBool && hasLoopBool)
            {
#if UNITY_EDITOR
                Debug.Log($"[Live2DMotionController] Using Pattern 2 (Loop-only): '{loopParameterName}'");
#endif
                if (loop)
                {
                    animator.SetBool(loopParameterName, true);
#if UNITY_EDITOR
                    Debug.Log($"[Live2DMotionController] Set Bool parameter: '{loopParameterName}' = true");
#endif
                    currentMotionName = loopParameterName;
                }
                else
                {
                    Debug.LogWarning($"[Live2DMotionController] Animation '{motionName}' only has Loop variant ('{loopParameterName}'). One-shot playback not supported. Playing loop instead.");
                    animator.SetBool(loopParameterName, true);
#if UNITY_EDITOR
                    Debug.Log($"[Live2DMotionController] Set Bool parameter: '{loopParameterName}' = true (forced loop)");
#endif
                    currentMotionName = loopParameterName;
                }
#if UNITY_EDITOR
                Debug.Log($"[Live2DMotionController] Animation started successfully. currentMotionName='{currentMotionName}'");
#endif
                return;
            }
            
            // Pattern 1: Trigger + Bool (standard pattern)
            if (hasTrigger)
            {
#if UNITY_EDITOR
                Debug.Log($"[Live2DMotionController] Using Pattern 1 (Trigger + Bool): '{motionName}' (hasBool={hasBool}, hasLoopBool={hasLoopBool})");
#endif
                animator.SetTrigger(motionName);
#if UNITY_EDITOR
                Debug.Log($"[Live2DMotionController] Set Trigger parameter: '{motionName}'");
#endif
                
                if (loop && hasAnyLoopBool)
                {
                    string boolToSet = hasLoopBool ? loopParameterName : motionName;
                    animator.SetBool(boolToSet, true);
#if UNITY_EDITOR
                    Debug.Log($"[Live2DMotionController] Set Bool parameter: '{boolToSet}' = true");
#endif
                    currentMotionName = boolToSet;
                }
                else if (loop && !hasAnyLoopBool)
                {
                    Debug.LogWarning($"[Live2DMotionController] Loop requested but no Bool parameter '{motionName}' or '{loopParameterName}' found! Animation will play once and return to default. Add Bool parameter to animator for looping.");
                    currentMotionName = motionName;
                }
                else
                {
                    currentMotionName = motionName;
                }
                
#if UNITY_EDITOR
                Debug.Log($"[Live2DMotionController] Animation started successfully. currentMotionName='{currentMotionName}'");
#endif
                return;
            }
            
            // Pattern 3: Bool only (direct loop control)
            if (hasBool)
            {
#if UNITY_EDITOR
                Debug.Log($"[Live2DMotionController] Using Pattern 3 (Bool only): '{motionName}'");
#endif
                animator.SetBool(motionName, loop);
#if UNITY_EDITOR
                Debug.Log($"[Live2DMotionController] Set Bool parameter: '{motionName}' = {loop}");
#endif
                currentMotionName = motionName;
#if UNITY_EDITOR
                Debug.Log($"[Live2DMotionController] Animation started successfully. currentMotionName='{currentMotionName}'");
#endif
                return;
            }
            
            // No valid parameters found
            Debug.LogWarning($"[Live2DMotionController] No valid animator parameters found for motion '{motionName}'. Checked: Trigger '{motionName}', Bool '{motionName}', Bool '{loopParameterName}'");
        }
        
        /// <summary>
        /// Play default motion (from animator's default state)
        /// </summary>
        public void PlayDefaultMotion()
        {
#if UNITY_EDITOR
            Debug.Log($"[Live2DMotionController] PlayDefaultMotion called. Current motion: '{currentMotionName}'");
#endif
            
            ResetAllBoolParameters();
            
            if (animator != null && !string.IsNullOrEmpty(defaultStateName))
            {
                animator.Play(defaultStateName, 0, 0f);
#if UNITY_EDITOR
                Debug.Log($"[Live2DMotionController] Forced transition to default state: '{defaultStateName}'");
#endif
            }
            
            currentMotionName = "";
#if UNITY_EDITOR
            Debug.Log("[Live2DMotionController] Reset to default motion complete");
#endif
        }
        
        /// <summary>
        /// Stop current motion and return to default
        /// </summary>
        public void StopCurrentMotion()
        {
            // Just reset all bools - animator will return to default state automatically
            PlayDefaultMotion();
        }
        
        /// <summary>
        /// Set a trigger parameter directly
        /// </summary>
        public void SetTrigger(string parameterName)
        {
            if (animator == null || !animator.isActiveAndEnabled) return;
            animator.SetTrigger(parameterName);
        }

        /// <summary>
        /// Reset a trigger parameter directly.
        /// </summary>
        public void ResetTrigger(string parameterName)
        {
            if (animator == null || !animator.isActiveAndEnabled) return;
            animator.ResetTrigger(parameterName);
        }
        
        /// <summary>
        /// Set a bool parameter directly
        /// </summary>
        public void SetBool(string parameterName, bool value)
        {
            if (animator == null || !animator.isActiveAndEnabled) return;
            animator.SetBool(parameterName, value);
        }
        
        /// <summary>
        /// Set a float parameter directly
        /// </summary>
        public void SetFloat(string parameterName, float value)
        {
            if (animator == null || !animator.isActiveAndEnabled) return;
            animator.SetFloat(parameterName, value);
        }
        
        /// <summary>
        /// Reset all bool parameters in animator to false
        /// </summary>
        public void ResetAllBoolParameters()
        {
            if (animator == null || parameterCache == null) return;
            
            foreach (var kvp in parameterCache)
            {
                if (kvp.Value == AnimatorControllerParameterType.Bool)
                {
                    animator.SetBool(kvp.Key, false);
                }
            }
        }
        
        /// <summary>
        /// Check if a parameter exists in the animator
        /// </summary>
        public bool HasParameter(string parameterName)
        {
            return parameterCache.ContainsKey(parameterName);
        }
        
        /// <summary>
        /// Get parameter type
        /// </summary>
        public AnimatorControllerParameterType? GetParameterType(string parameterName)
        {
            if (parameterCache.ContainsKey(parameterName))
            {
                return parameterCache[parameterName];
            }
            return null;
        }
        
        /// <summary>
        /// Get all available parameter names
        /// </summary>
        public string[] GetAvailableParameters()
        {
            return parameterCache.Keys.ToArray();
        }
        
        /// <summary>
        /// Get current motion name
        /// </summary>
        public string GetCurrentMotionName()
        {
            return currentMotionName;
        }
        
        /// <summary>
        /// Get default state name
        /// </summary>
        public string GetDefaultStateName()
        {
            return defaultStateName;
        }
    }
}
