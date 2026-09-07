using Base.Dialogues;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Base.Dialogues
{
    public enum SequenceType
    {
        Linear,
        RandomOutcome
    }

    [CreateAssetMenu(fileName = "NewDialogueSequence", menuName = "Mai's Love Story/Dialogue Sequence")]
    public class DialogueSequenceSO : ScriptableObject, ISerializationCallbackReceiver
    {
        public string sequenceID;
        public SequenceType type = SequenceType.Linear;
        public string startingNodeID;

        public DialogueSequenceSO goodOutcomeSequence;
        public DialogueSequenceSO neutralOutcomeSequence;
        public DialogueSequenceSO badOutcomeSequence;
        public List<DialogueReward> rewardsOnCompletion = new List<DialogueReward>();
        public List<DialogueNodeData> nodeList = new List<DialogueNodeData>();

        private Dictionary<string, DialogueNodeData> _nodeLookup = new Dictionary<string, DialogueNodeData>();

        public DialogueNodeData GetNodeByID(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            if (_nodeLookup == null || _nodeLookup.Count == 0 && nodeList.Count > 0)
                BuildNodeLookup();

            _nodeLookup.TryGetValue(id, out DialogueNodeData foundNode);
            return foundNode;
        }

        private void BuildNodeLookup()
        {
            _nodeLookup = _nodeLookup ?? new Dictionary<string, DialogueNodeData>();
            _nodeLookup.Clear();

            foreach (var node in nodeList)
            {
                if (!string.IsNullOrEmpty(node.nodeID) && !_nodeLookup.ContainsKey(node.nodeID))
                    _nodeLookup.Add(node.nodeID, node);
                else if (!string.IsNullOrEmpty(node.nodeID))
                    Debug.LogWarning($"Duplicate NodeID '{node.nodeID}' in Sequence '{sequenceID}'.");
            }
        }

        public void OnAfterDeserialize()
        {
            BuildNodeLookup();
        }

        public void OnBeforeSerialize()
        {

        }

#if UNITY_EDITOR
        [NonSerialized] private Dictionary<string, DialogueNodeData> _editor_tempNodeCache;
        public DialogueNodeData GetOrCreateEditorNode(string nodeId)
        {
            _editor_tempNodeCache = _editor_tempNodeCache ?? new Dictionary<string, DialogueNodeData>();

            if (!_editor_tempNodeCache.TryGetValue(nodeId, out DialogueNodeData nodeData))
            {
                nodeData = nodeList.Find(n => n.nodeID == nodeId);

                if (nodeData == null)
                {
                    nodeData = new DialogueNodeData { nodeID = nodeId };
                    nodeList.Add(nodeData);
                }

                _editor_tempNodeCache[nodeId] = nodeData;
            }
            return nodeData;
        }

        public void ClearEditorTempCache()
        {
            _editor_tempNodeCache = null;
        }
#endif
    }
}