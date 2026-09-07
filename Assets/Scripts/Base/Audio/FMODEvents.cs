using FMODUnity;
using UnityEngine;

namespace Base
{
    public class FMODEvents : MonoBehaviour
    {
        [field: Header("Music")]
        [field: SerializeField] public EventReference Music { get; private set; }

        [field: Header("Button SFX")]
        [field: SerializeField] public EventReference OnAction { get; private set; }
        [field: SerializeField] public EventReference OnBack { get; private set; }
        [field: SerializeField] public EventReference OnButton { get; private set; }
        [field: SerializeField] public EventReference OnNotice { get; private set; }
        [field: SerializeField] public EventReference[] OnPlaces { get; private set; }

        [field: Header("Shop SFX")]
        [field: SerializeField] public EventReference ShopOpen { get; private set; }
        [field: SerializeField] public EventReference ShopClose { get; private set; }
        [field: SerializeField] public EventReference ShopBuy { get; private set; }

        [field: Header("Sex Simulation SFX")]
        [field: SerializeField] public EventReference SexSlow { get; private set; }
        [field: SerializeField] public EventReference SexFast { get; private set; }
        [field: SerializeField] public EventReference SexCumInside { get; private set; }
        [field: SerializeField] public EventReference SexSquirt { get; private set; }

        public static FMODEvents Instance { get; private set; }

        void Awake()
        {
            Instance = this;
        }
    }
}
