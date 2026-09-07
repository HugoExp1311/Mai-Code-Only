using System;

namespace EventBus
{
    /// <summary>
    /// Event binding that connects an event handler to an event type
    /// Used to register and deregister event handlers
    /// </summary>
    public class EventBinding<TEvent> : IEventBinding where TEvent : IEvent
    {
        private Action<TEvent> _onEvent;
        
        public Action<TEvent> OnEvent
        {
            get => _onEvent;
            set => _onEvent = value;
        }
        
        public EventBinding(Action<TEvent> onEvent)
        {
            _onEvent = onEvent;
        }
        
        public void Add(Action<TEvent> action)
        {
            _onEvent += action;
        }
        
        public void Remove(Action<TEvent> action)
        {
            _onEvent -= action;
        }
    }
    
    /// <summary>
    /// Internal interface for event bindings (non-generic)
    /// </summary>
    internal interface IEventBinding
    {
    }
}
