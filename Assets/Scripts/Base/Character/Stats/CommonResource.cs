using UnityEngine;

namespace Base.Character.Stats
{
    public class CommonResource : IResource<int>
    {
        private readonly string _prefsKey; // OPT-37: Cached key, avoids string alloc per read/write

        public CommonResource(BasicResource basicResource, int initialValue)
        {
            _resource = basicResource;
            _initialValue = initialValue;
            _prefsKey = $"BasicResource_{basicResource}"; // Cache once in constructor
            _currentValue = PlayerPrefs.GetInt(_prefsKey, initialValue);
        }

        private readonly BasicResource _resource;
        private readonly int _initialValue;
        private int _currentValue;

        public int GetValues()
        {
            return _currentValue;
        }

        public void UpdateValues(int newValue)
        {
            PlayerPrefs.SetInt(_prefsKey, newValue);
            _currentValue = newValue;
        }

        /// <summary>
        /// Reset to initial value and clear PlayerPrefs. Called on new game start.
        /// </summary>
        public void ResetToDefault()
        {
            _currentValue = _initialValue;
            PlayerPrefs.DeleteKey(_prefsKey);
        }
    }
}
