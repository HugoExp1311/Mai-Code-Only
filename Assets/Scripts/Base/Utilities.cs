using Base;
using Base.Settings;
using System;
using UnityEngine;

namespace Base.Core
{
    [Serializable]
    public class KeyValuePair<TKey, TValue>
    {
        [field: SerializeField] public TKey Key { set; get; }
        [field: SerializeField] public TValue Value { set; get; }
    }
}