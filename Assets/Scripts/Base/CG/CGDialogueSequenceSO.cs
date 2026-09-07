using Base.Dialogues;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Base.CG
{
    /// <summary>
    /// ScriptableObject for CG dialogue sequences
    /// Similar to DialogueSequenceSO but uses CGDialogueLine with sprite references
    /// Does not control Live2D animations - only displays CG background images
    /// </summary>
    [CreateAssetMenu(fileName = "NewCGDialogueSequence", menuName = "Mai's Love Story/CG Dialogue Sequence")]
    public class CGDialogueSequenceSO : ScriptableObject, ISerializationCallbackReceiver
    {
        [Header("Sequence Info")]
        public string sequenceID;
        public string startingNodeID;
        
        [Header("Rewards")]
        public List<DialogueReward> rewardsOnCompletion = new List<DialogueReward>();
        
        [Header("Nodes")]
        public List<CGDialogueNodeData> nodeList = new List<CGDialogueNodeData>();

        private Dictionary<string, CGDialogueNodeData> _nodeLookup = new Dictionary<string, CGDialogueNodeData>();

        /// <summary>
        /// Get a node by its ID
        /// </summary>
        public CGDialogueNodeData GetNodeByID(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            if (_nodeLookup == null || _nodeLookup.Count == 0 && nodeList.Count > 0)
                BuildNodeLookup();

            _nodeLookup.TryGetValue(id, out CGDialogueNodeData foundNode);
            return foundNode;
        }

        private void BuildNodeLookup()
        {
            _nodeLookup = _nodeLookup ?? new Dictionary<string, CGDialogueNodeData>();
            _nodeLookup.Clear();

            foreach (var node in nodeList)
            {
                if (!string.IsNullOrEmpty(node.nodeID) && !_nodeLookup.ContainsKey(node.nodeID))
                    _nodeLookup.Add(node.nodeID, node);
                else if (!string.IsNullOrEmpty(node.nodeID))
                    Debug.LogWarning($"Duplicate NodeID '{node.nodeID}' in CG Sequence '{sequenceID}'.");
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
        [NonSerialized] private Dictionary<string, CGDialogueNodeData> _editor_tempNodeCache;
        
        public CGDialogueNodeData GetOrCreateEditorNode(string nodeId)
        {
            _editor_tempNodeCache = _editor_tempNodeCache ?? new Dictionary<string, CGDialogueNodeData>();

            if (!_editor_tempNodeCache.TryGetValue(nodeId, out CGDialogueNodeData nodeData))
            {
                nodeData = nodeList.Find(n => n.nodeID == nodeId);

                if (nodeData == null)
                {
                    nodeData = new CGDialogueNodeData { nodeID = nodeId };
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
