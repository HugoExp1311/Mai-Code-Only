using System;
using Base;
using Base.Character;
using Base.Character.Stats;
using UnityEngine;

/// <summary>
/// Tracks thrust and orgasm counts during a sex simulation session.
/// Session persists across multiple rounds until player exits Simulation section.
/// </summary>
[Serializable]
public class SexSessionData
{
    // Thrust tracking per hole (for sensitive points calculation)
    public int pussyThrustCount = 0;
    public int buttholeThrustCount = 0;
    
    // === Orgasm tracking (Roleplay) ===
    // Roleplay orgasms happen via Roleplay Button actions
    public int roleplayPussyOrgasmCount = 0;    // "Roleplay Pussy" → +30 Sex Points each
    public int roleplayButtholeOrgasmCount = 0;  // "Roleplay Butthole" → +25 Sex Points each
    public int roleplayBlowjobOrgasmCount = 0;   // "Roleplay Blowjob" → +25 Sex Points each
    public int roleplayPaizuriOrgasmCount = 0;   // "Roleplay Paizuri" → +25 Sex Points each
    
    // === Orgasm tracking (Sex) ===
    // Sex orgasms happen during Pussy/Butthole sex in specific positions
    public int missionaryOrgasmCount = 0;   // +35 Sex Points each
    public int doggyOrgasmCount = 0;        // +40 Sex Points each
    public int cowgirlOrgasmCount = 0;      // +45 Sex Points each
    public int fullNelsonOrgasmCount = 0;   // +50 Sex Points each
    public int spooningOrgasmCount = 0;     // +55 Sex Points each
    public int matingPressOrgasmCount = 0;  // +60 Sex Points each
    
    // Operation counts per body part. Each operation adds points equal to the
    // body's current Sensitive Level.
    public int boobsOperationCount = 0;
    public int handOperationCount = 0;
    public int mouthOperationCount = 0;
    public int pussyOperationCount = 0;
    public int buttholeOperationCount = 0;
    
    // Other tracking
    public int cumInsideCount = 0;
    public int lovePointsChange = 0;  // Can be positive (reward) or negative (penalty)
    
    /// <summary>
    /// Reset all counters (called when entering Simulation section)
    /// </summary>
    public void Reset()
    {
        pussyThrustCount = 0;
        buttholeThrustCount = 0;
        
        roleplayPussyOrgasmCount = 0;
        roleplayButtholeOrgasmCount = 0;
        roleplayBlowjobOrgasmCount = 0;
        roleplayPaizuriOrgasmCount = 0;
        
        missionaryOrgasmCount = 0;
        doggyOrgasmCount = 0;
        cowgirlOrgasmCount = 0;
        fullNelsonOrgasmCount = 0;
        spooningOrgasmCount = 0;
        matingPressOrgasmCount = 0;
        
        boobsOperationCount = 0;
        handOperationCount = 0;
        mouthOperationCount = 0;
        pussyOperationCount = 0;
        buttholeOperationCount = 0;
        
        cumInsideCount = 0;
        lovePointsChange = 0;
        
        Debug.Log("[SexSessionData] Session tracking reset");
    }
    
    // === Orgasm Helpers ===
    
    public int GetTotalRoleplayOrgasms() =>
        roleplayPussyOrgasmCount +
        roleplayButtholeOrgasmCount +
        roleplayBlowjobOrgasmCount +
        roleplayPaizuriOrgasmCount;
    
    public int SexOrgasmCount
    {
        get
        {
            return missionaryOrgasmCount
                + doggyOrgasmCount
                + cowgirlOrgasmCount
                + fullNelsonOrgasmCount
                + spooningOrgasmCount
                + matingPressOrgasmCount;
        }
    }
    
    public int GetTotalOrgasms() => GetTotalRoleplayOrgasms() + SexOrgasmCount;

    public int GetTotalPenetrativeSexActions() => pussyThrustCount + buttholeThrustCount;

    public bool HasPenetrativeSex() => GetTotalPenetrativeSexActions() > 0;
    
    // === Sex Points Calculation ===
    
    /// <summary>
    /// Calculate sex points from Roleplay orgasms
    /// </summary>
    public int CalculateRoleplaySexPoints()
    {
        return roleplayPussyOrgasmCount * DefaultSettings.RoleplayPussySexPoints
             + roleplayButtholeOrgasmCount * DefaultSettings.RoleplayButtholeSexPoints
             + roleplayBlowjobOrgasmCount * DefaultSettings.RoleplayBlowjobSexPoints
             + roleplayPaizuriOrgasmCount * DefaultSettings.RoleplayPaizuriSexPoints;
    }
    
    /// <summary>
    /// Calculate sex points from Sex orgasms (per position)
    /// </summary>
    public int CalculateSexOrgasmSexPoints()
    {
        return missionaryOrgasmCount * DefaultSettings.MissionarySexPoints
             + doggyOrgasmCount * DefaultSettings.DoggySexPoints
             + cowgirlOrgasmCount * DefaultSettings.CowgirlSexPoints
             + fullNelsonOrgasmCount * DefaultSettings.FullNelsonSexPoints
             + spooningOrgasmCount * DefaultSettings.SpooningSexPoints
             + matingPressOrgasmCount * DefaultSettings.MatingPressSexPoints;
    }

    /// <summary>
    /// Calculate the Lewd Level bonus from penetrative thrusts.
    /// Each penetrative thrust awards bonus points based on Mai's current Lewd Level.
    /// </summary>
    public int CalculatePenetrativeSexBonusPoints(int maiLewdLevel)
    {
        return DefaultSettings.PenetrativeSexBonusPointsByLewdLevel.TryGetValue(maiLewdLevel, out int bonusPointsPerThrust)
            ? GetTotalPenetrativeSexActions() * bonusPointsPerThrust
            : 0;
    }
    
    /// <summary>
    /// Calculate total sex points from all orgasm types
    /// </summary>
    public int CalculateTotalSexPoints()
    {
        return CalculateRoleplaySexPoints() + CalculateSexOrgasmSexPoints();
    }

    /// <summary>
    /// Calculate total sex points including the Lewd Level bonus from penetrative thrusts.
    /// </summary>
    public int CalculateTotalSexPointsForLewdLevel(int maiLewdLevel)
    {
        return CalculateTotalSexPoints() + CalculatePenetrativeSexBonusPoints(maiLewdLevel);
    }
    
    // === Sensitive Points Calculation ===
    
    public int GetOperationCount(SensitiveBodyPart bodyPart)
    {
        return bodyPart switch
        {
            SensitiveBodyPart.Boobs => boobsOperationCount,
            SensitiveBodyPart.Mouth => mouthOperationCount,
            SensitiveBodyPart.Pussy => pussyOperationCount,
            SensitiveBodyPart.Butthole => buttholeOperationCount,
            _ => 0
        };
    }

    public int GetSensitivePoints(SensitiveBodyPart bodyPart, Target mai = null)
    {
        if (mai != null && mai.IsMaxLewdLevel())
        {
            return 0;
        }

        int sensitiveLevel = mai != null
            ? mai.GetSensitiveLevel(bodyPart)
            : DefaultSettings.DefaultSensitiveLevel;
        int pointsPerInteraction = DefaultSettings.GetSensitivePointsPerInteraction(sensitiveLevel);
        return GetOperationCount(bodyPart) * pointsPerInteraction;
    }

    public int GetBoobsSensitivePoints(Target mai = null) => GetSensitivePoints(SensitiveBodyPart.Boobs, mai);
    public int GetMouthSensitivePoints(Target mai = null) => GetSensitivePoints(SensitiveBodyPart.Mouth, mai);
    public int GetPussySensitivePoints(Target mai = null) => GetSensitivePoints(SensitiveBodyPart.Pussy, mai);
    public int GetButtholeSensitivePoints(Target mai = null) => GetSensitivePoints(SensitiveBodyPart.Butthole, mai);
    
    /// <summary>
    /// Calculate total sensitive points from the session.
    /// </summary>
    public int CalculateTotalSensitivePoints(Target mai = null)
    {
        return GetBoobsSensitivePoints(mai) + GetMouthSensitivePoints(mai) + GetPussySensitivePoints(mai) +
               GetButtholeSensitivePoints(mai);
    }
    
    // === Pregnancy ===
    
    /// <summary>
    /// Calculate pregnancy chance based on cum inside count (capped at 100%)
    /// </summary>
    public int GetPregnancyChance() => Mathf.Min(
        cumInsideCount * DefaultSettings.PregnancyChancePerCumInside,
        DefaultSettings.MaxPregnancyChance);
    
    /// <summary>
    /// Get summary for logging
    /// </summary>
    public string GetSummary(int maiLewdLevel, Target mai = null)
    {
        int penetrativeBonusPoints = CalculatePenetrativeSexBonusPoints(maiLewdLevel);

        return $"Session Summary:\n" +
               $"  Roleplay Orgasms: {GetTotalRoleplayOrgasms()} (Pussy:{roleplayPussyOrgasmCount}, Butthole:{roleplayButtholeOrgasmCount}, Blowjob:{roleplayBlowjobOrgasmCount}, Paizuri:{roleplayPaizuriOrgasmCount})\n" +
               $"  Sex Orgasms: {SexOrgasmCount} (Missionary:{missionaryOrgasmCount}, Doggy:{doggyOrgasmCount}, Cowgirl:{cowgirlOrgasmCount}, FullNelson:{fullNelsonOrgasmCount}, Spooning:{spooningOrgasmCount}, MatingPress:{matingPressOrgasmCount})\n" +
               $"  Total Sex Points: {CalculateTotalSexPointsForLewdLevel(maiLewdLevel)} (Roleplay:{CalculateRoleplaySexPoints()}, Sex:{CalculateSexOrgasmSexPoints()}, FuckMai:{penetrativeBonusPoints})\n" +
               $"  Sensitive Points: {CalculateTotalSensitivePoints(mai)}\n" +
               $"  Cum Inside: {cumInsideCount} times (Pregnancy Chance: {GetPregnancyChance()}%)\n" +
               $"  Love Points: {(lovePointsChange >= 0 ? "+" : "")}{lovePointsChange}";
    }
}
