using System.Collections.Generic;
using System;

namespace Base.Dialogues
{
    [Serializable]
    public class DialogueNodeData
    {
        public string nodeID;
        public List<DialogueLine> lines = new List<DialogueLine>();
        public List<DialogueChoiceData> choices = new List<DialogueChoiceData>();
        public List<DialogueReward> rewardsOnNodeCompletion = new List<DialogueReward>();
    }
}