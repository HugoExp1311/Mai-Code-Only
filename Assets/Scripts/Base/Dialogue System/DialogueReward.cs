using System;
using Base.Character.Stats;

namespace Base.Dialogues
{
    [Serializable]
    public class DialogueReward
    {
        public RewardTarget target = RewardTarget.Player;
        public BasicStats stat;
        public int amount;
    }
}