using System;
using System.Collections.Generic;

namespace EventBus
{
    /// <summary>
    /// Static generic event bus for publishing and subscribing to events
    /// Provides decoupled communication between different parts of the application
    /// </summary>
    public static class EventBus<TEvent> where TEvent : IEvent
    {
        private static readonly HashSet<IEventBinding> _bindings = new HashSet<IEventBinding>();
        
        /// <summary>
        /// Register an event binding to receive events of type TEvent
        /// </summary>
        public static void Register(EventBinding<TEvent> binding)
        {
            if (binding == null)
            {
                UnityEngine.Debug.LogWarning("[EventBus] Attempted to register null binding");
                return;
            }
            
            _bindings.Add(binding);
        }
        
        /// <summary>
        /// Deregister an event binding to stop receiving events
        /// </summary>
        public static void Deregister(EventBinding<TEvent> binding)
        {
            if (binding == null)
            {
                return;
            }
            
            _bindings.Remove(binding);
        }
        
        // Reusable list to avoid heap allocation on every Raise() call
        private static readonly List<EventBinding<TEvent>> _bindingsToNotify = new List<EventBinding<TEvent>>();
        
        /// <summary>
        /// Raise an event, notifying all registered bindings
        /// </summary>
        public static void Raise(TEvent eventData)
        {
            _bindingsToNotify.Clear();
            
            foreach (var binding in _bindings)
            {
                if (binding is EventBinding<TEvent> typedBinding)
                {
                    _bindingsToNotify.Add(typedBinding);
                }
            }
            
            foreach (var binding in _bindingsToNotify)
            {
                if (binding.OnEvent == null)
                {
                    continue;
                }
                
                try
                {
                    binding.OnEvent.Invoke(eventData);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[EventBus] Exception during event invocation for {typeof(TEvent).Name}: {ex}");
                }
            }
        }
        
        /// <summary>
        /// Clear all bindings (useful for cleanup or testing)
        /// </summary>
        public static void Clear()
        {
            _bindings.Clear();
        }
    }
}
