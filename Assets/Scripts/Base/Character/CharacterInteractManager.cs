using UnityEngine;
using Base.Dialogues;

namespace Base.Character
{
    public class CharacterInteractManager : MonoBehaviour
    {
        public static CharacterInteractManager Instance { get; private set; }
        private ITarget _currentTargetBoss;

        [Header("Default Interaction Sequences")]
        [SerializeField] private DialogueSequenceSO defaultTalkSequence;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            // Try to get target boss at start, but don't worry if DataManager isn't ready yet
            if (GameManager.Instance?.DataManager != null)
            {
                _currentTargetBoss = GameManager.Instance.DataManager.GetCurrentBoss();
            }
        }

        public void InitiateTalk()
        {
            // Try to get the target boss if not already assigned (fallback for timing issues)
            if (_currentTargetBoss == null && GameManager.Instance?.DataManager != null)
            {
                _currentTargetBoss = GameManager.Instance.DataManager.GetCurrentBoss();
            }
            
            if (_currentTargetBoss == null)
            {
                Debug.LogError("CharacterInteractManager.InitiateTalk: No target boss assigned.");
                return;
            }
            if (DialogueManager.Instance == null)
            {
                Debug.LogError("CharacterInteractManager.InitiateTalk: DialogueManager instance not found.");
                return;
            }
            if (DialogueManager.Instance.IsDialogueActive)
            {
                Debug.LogWarning("CharacterInteractManager.InitiateTalk: Dialogue is already active.");
                return;
            }

            DialogueSequenceSO sequenceToPlay = defaultTalkSequence;

            if (sequenceToPlay == null)
            {
                Debug.LogError("CharacterInteractManager.InitiateTalk: defaultTalkSequence is null. Please assign a DialogueSequenceSO in the Inspector.");
                return;
            }

            DialogueManager.Instance.StartDialogue(sequenceToPlay);
        }

        public void InitiateGift()
        {
            // Try to get the target boss if not already assigned (fallback for timing issues)
            if (_currentTargetBoss == null && GameManager.Instance?.DataManager != null)
            {
                _currentTargetBoss = GameManager.Instance.DataManager.GetCurrentBoss();
            }

            if (_currentTargetBoss == null) { Debug.LogError("CIM: No target boss for gift."); return; }
            Debug.Log($"CharacterInteractManager: Gift interaction initiated with '{_currentTargetBoss.GetName()}'. (Not Implemented Yet)");
            // TODO: Implement gift UI, item selection, then call _currentTargetBoss.DoAction(new CharacterActions.ReceiveItem(...));
        }

        public void InitiateSex()
        {
            // Try to get the target boss if not already assigned (fallback for timing issues)
            if (_currentTargetBoss == null && GameManager.Instance?.DataManager != null)
            {
                _currentTargetBoss = GameManager.Instance.DataManager.GetCurrentBoss();
            }

            if (_currentTargetBoss == null) { Debug.LogError("CIM: No target boss for sex."); return; }
            Debug.Log($"CharacterInteractManager: Sex interaction initiated with '{_currentTargetBoss.GetName()}'. (Not Implemented Yet)");
            // TODO: Implement sex minigame/sequence
        }

        public void InitiateFood()
        {
            // Try to get the target boss if not already assigned (fallback for timing issues)
            if (_currentTargetBoss == null && GameManager.Instance?.DataManager != null)
            {
                _currentTargetBoss = GameManager.Instance.DataManager.GetCurrentBoss();
            }

            if (_currentTargetBoss == null) { Debug.LogError("CIM: No target boss for food."); return; }
            Debug.Log($"CharacterInteractManager: Food interaction initiated with '{_currentTargetBoss.GetName()}'. (Not Implemented Yet)");
            // TODO: Implement food/feeding sequence
        }
    }
}