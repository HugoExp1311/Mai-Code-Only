using System;

namespace Base.Character.Stats
{
    public class CommonStat<T> : IStats<T>
    {
        public CommonStat(BasicStats key, T initialValue, T maxValue = default)
        {
            _currentValue = initialValue;
            _maxValue = maxValue;
        }
        
        private T _currentValue;
        private T _maxValue;

        
        public T GetValues()
        {
            return _currentValue;
        }

        public void UpdateValues(T newValue)
        {
            _currentValue = newValue;
        }

        // OPT-9: Removed ?? operator — it doesn't work for non-nullable value types (int default=0, not null)
        public T MaxValue()
        {
            return _maxValue;
        }

        /// <summary>
        /// Update the maximum value for this stat
        /// </summary>
        public void UpdateMaxValue(T newMaxValue)
        {
            _maxValue = newMaxValue;
        }

        /// <summary>
        /// Get the current maximum value
        /// </summary>
        public T GetMaxValue()
        {
            return _maxValue;
        }
    }
}