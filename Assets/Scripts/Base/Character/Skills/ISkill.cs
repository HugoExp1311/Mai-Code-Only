namespace Base.Character.Skills
{
    public interface ISkill
    {
        public SkillType SkillType { get; }
        public int SkillValue();
        public int SkillLevel();
        public bool IsUnlocked();
        public bool SetUnlocked(bool unlocked);
        public bool Upgrade();
        public bool Downgrade();
        public int SkillPointRequired();
        public SkillEffect Used();
    }

    public interface ISkillManager
    {
        public int GetSkillPoint();

        public UIAction UpdateSkill(SkillType skillType);
        
        public UIAction DowngradeSkill(SkillType skillType);

        public SkillEffect UseSkill(SkillType skillType);
        
        public ISkill GetSkill(SkillType skillType);

        public bool IsSkillUnlocked(SkillType skillType);

        public bool SetSkillUnlocked(SkillType skillType, bool unlocked);
        
        public int GetStaminaLimitBonus();
        
        

        public void SetSkillPointDirect(int value);
        public void IncreaseSkillPointDirect(int amount);

        public bool TrySpendSkillPoints(int amount, out int newValue);
    }

    public abstract record SkillEffect
    {
        public record CumIncreaseProcess(int Amount) : SkillEffect;
        
        public record OrgasmIncrease(int Amount) : SkillEffect;
        public record UseEnergy(int Amount, int EnergyTake) : SkillEffect;
        public record BulletCount(int Amount) : SkillEffect;
        public record StaminaLimitBonus(int BonusPercent) : SkillEffect;
    }
    
    public enum SkillType
    {
        Hand,       // Magic Hand - Orgasm rate with hand
        Tongue,     // Hmmm … delicious! - Orgasm rate with mouth/tongue
        F,          // You like my dick, huh? - Orgasm rate dick→pussy
        A,          // You like anal, don't you? - Orgasm rate dick→butthole
        Cum,        // I'm bout to CUM - Cumming bar increase rate (lower is better)
        Bullet,     // I need more bullet! - Number of cum shots
        LongNight,  // Long Night - Stamina limit bonus
        Size,       // Big Dick - Stamina consumption reduction (lower is better)
    }
}
