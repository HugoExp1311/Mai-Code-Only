using System.Collections.Generic;
using System;
using Base.Dialogues;
using UnityEngine;

namespace Base.CG
{
    /// <summary>
    /// A node in a CG dialogue sequence containing lines and choices
    /// Similar to DialogueNodeData but uses CGDialogueLine
    /// </summary>
    [Serializable]
    public class CGDialogueNodeData
    {
        public string nodeID;
        public string nextNodeID; // Explicit next node override (from CSV NextNodeID column)
        public CGDialogueSequenceSO nextSequenceSO; // Node-level sequence transition (from CSV NextSequenceID column)
        public bool isEndNode; // True = branch/sequence terminal, don't do index+1 navigation
        public List<CGDialogueLine> lines = new List<CGDialogueLine>();
        public List<CGDialogueChoiceData> choices = new List<CGDialogueChoiceData>();
        public List<DialogueReward> rewardsOnNodeCompletion = new List<DialogueReward>();

#if UNITY_EDITOR
        [NonSerialized] public string _editor_nextSequenceID_temp; // Importer scratch field
#endif
    }
}
