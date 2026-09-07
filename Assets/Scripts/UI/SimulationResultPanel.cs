using System.Collections.Generic;
using Base;
using Base.Character;
using Base.Character.Action;
using Base.Character.Stats;
using Base.Localization;
using Events;

using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class SimulationResultPanel : UIPanel
    {
        [Header("UI References")]
        [SerializeField] private LocalizedText headerLocalizedText;
        [SerializeField] private UnityEngine.UI.Image headerImage;
        [SerializeField] private Sprite positiveHeaderSprite;
        [SerializeField] private Sprite negativeHeaderSprite;
        [SerializeField] private Transform sexPointsContainer;
        [SerializeField] private Transform sensitivePointsContainer;
        [SerializeField] private Transform otherContainer;
        [SerializeField] private GameObject resultItemPrefab;
        [SerializeField] private Button nextDayButton;
        
        protected override void OnShow(object data)
        {
            base.OnShow(data);
            
            // Setup button listener
            if (nextDayButton != null)
            {
                nextDayButton.interactable = true;
                nextDayButton.onClick.AddListener(OnNextDayClicked);
            }
            
            // Display results if data is provided
            if (data is SexSessionData sessionData)
            {
                ShowResults(sessionData);
            }
        }
        
        protected override void OnHide()
        {
            base.OnHide();
            
            // Remove button listener
            if (nextDayButton != null)
            {
                nextDayButton.onClick.RemoveListener(OnNextDayClicked);
            }
        }
        
        public void ShowResults(SexSessionData sessionData)
        {
            // === Determine Mai's Satisfaction outcome ===
            Target mai = GameManager.Instance.DataManager.GetCurrentBoss() as Target;
            int maiLewdLevel = mai != null ? mai.GetLewdLevel() : DefaultSettings.DefaultLewdLevel;
            int totalOrgasms = sessionData.GetTotalOrgasms();
            int requiredOrgasms = DefaultSettings.SatisfactionOrgasmRequirements.TryGetValue(maiLewdLevel, out int req) ? req : 2;
            bool isPositiveOutcome = totalOrgasms >= requiredOrgasms;
            
            // Update header text and image based on outcome
            if (headerLocalizedText != null)
            {
                string headerKey = isPositiveOutcome ? "Simulation Result Header Positive" : "Simulation Result Header Negative";
                headerLocalizedText.SetLocalized(headerKey, LocalizationDomains.UI);
            }
            
            if (headerImage != null)
            {
                headerImage.sprite = isPositiveOutcome ? positiveHeaderSprite : negativeHeaderSprite;
            }
            
            // Clear existing items
            ClearContainer(sexPointsContainer);
            ClearContainer(sensitivePointsContainer);
            ClearContainer(otherContainer);
            
            // Track if containers have content
            bool hasSexPoints = false;
            bool hasSensitivePoints = false;
            bool hasOther = false;
            
            // === SEX POINTS SECTION ===
            
            // Orgasm (Roleplay) - combined count and points
            int roleplayOrgasms = sessionData.GetTotalRoleplayOrgasms();
            int roleplaySexPoints = sessionData.CalculateRoleplaySexPoints();
            if (roleplayOrgasms > 0)
            {
                CreateResultItem(sexPointsContainer, "Simulation Result Orgasm Roleplay", roleplaySexPoints);
                hasSexPoints = true;
            }
            
            // Orgasm (Sex) - combined count and points
            int sexOrgasms = sessionData.SexOrgasmCount;
            int sexOrgasmPoints = sessionData.CalculateSexOrgasmSexPoints();
            if (sexOrgasms > 0)
            {
                CreateResultItem(sexPointsContainer, "Simulation Result Orgasm Sex", sexOrgasmPoints);
                hasSexPoints = true;
            }

            // Fuck Mai - Lewd Level bonus from total Slow/Fast penetrative thrusts
            int penetrativeSexBonusPoints = sessionData.CalculatePenetrativeSexBonusPoints(maiLewdLevel);
            if (penetrativeSexBonusPoints > 0)
            {
                CreateResultItem(sexPointsContainer, "Simulation Result Fuck Mai", penetrativeSexBonusPoints);
                hasSexPoints = true;
            }
            
            // Total sex points
            int totalSexPoints = sessionData.CalculateTotalSexPointsForLewdLevel(maiLewdLevel);
            if (totalSexPoints > 0)
            {
                CreateResultItem(sexPointsContainer, "Simulation Result Total Points", totalSexPoints);
                hasSexPoints = true;
            }
            
            // === SENSITIVE POINTS SECTION ===
            
            // Boobs: operation count × current Boobs Sensitive Level
            if (sessionData.boobsOperationCount > 0)
            {
                CreateResultItem(sensitivePointsContainer, "Simulation Result Sensitive Boobs", sessionData.GetBoobsSensitivePoints(mai));
                hasSensitivePoints = true;
            }
            
            // Mouth: operation count × current Mouth Sensitive Level
            if (sessionData.mouthOperationCount > 0)
            {
                CreateResultItem(sensitivePointsContainer, "Simulation Result Sensitive Mouth", sessionData.GetMouthSensitivePoints(mai));
                hasSensitivePoints = true;
            }
            
            // Pussy: operation count × current Pussy Sensitive Level
            if (sessionData.pussyOperationCount > 0)
            {
                CreateResultItem(sensitivePointsContainer, "Simulation Result Sensitive Pussy", sessionData.GetPussySensitivePoints(mai));
                hasSensitivePoints = true;
            }
            
            // Butthole: operation count × current Butthole Sensitive Level
            if (sessionData.buttholeOperationCount > 0)
            {
                CreateResultItem(sensitivePointsContainer, "Simulation Result Sensitive Butthole", sessionData.GetButtholeSensitivePoints(mai));
                hasSensitivePoints = true;
            }
            
            // === OTHER SECTION ===
            
            // Cum Inside (No Condom) — show count and pregnancy chance
            if (sessionData.cumInsideCount > 0)
            {
                CreateResultItem(otherContainer, "Simulation Result Cum Inside", sessionData.cumInsideCount);
                hasOther = true;
                
                // Show pregnancy chance as separate line
                int pregnancyChance = sessionData.GetPregnancyChance();
                CreateResultItem(otherContainer, "Simulation Result Pregnancy Chance", pregnancyChance);
            }
            
            // Mai's Satisfaction — show love points change (positive or negative)
            // lovePointsChange was already computed in CalculateAndApplySessionRewards
            CreateResultItem(otherContainer, "Simulation Result Mai Satisfaction", sessionData.lovePointsChange);
            hasOther = true;
            
            // Enable/disable containers based on content
            if (sexPointsContainer != null)
            {
                sexPointsContainer.gameObject.SetActive(hasSexPoints);
            }
            
            if (sensitivePointsContainer != null)
            {
                sensitivePointsContainer.gameObject.SetActive(hasSensitivePoints);
            }
            
            if (otherContainer != null)
            {
                otherContainer.gameObject.SetActive(hasOther);
            }
        }
        
        private void CreateResultItem(Transform container, string labelKey, int value)
        {
            if (resultItemPrefab == null || container == null) return;
            
            GameObject itemObj = Instantiate(resultItemPrefab, container);
            SimulationResultItem item = itemObj.GetComponent<SimulationResultItem>();
            
            if (item != null)
            {
                item.Setup(labelKey, value);
            }
        }
        
        private void ClearContainer(Transform container)
        {
            if (container == null) return;
            
            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }
        }
        
        private void OnNextDayClicked()
        {
            if (nextDayButton != null)
            {
                // Keep the result panel visible until the fade covers it,
                // but prevent duplicate clicks while the transition is starting.
                nextDayButton.interactable = false;
            }
            
            // During the intro sequence, simulation exit should continue to Part3 CG,
            // not advance to the next day
            if (GameManager.Instance.IsPlayingIntroCG)
            {
                GameManager.Instance.OnSimulationFinished();
                return;
            }
            
            // Apply Next Day energy restore (+20 Energy)
            Player player = GameManager.Instance?.DataManager?.GetPlayer() as Player;
            if (player != null)
            {
                player.DoAction(new CharacterActions.ChangeStat(
                    RewardTarget.Player,
                    BasicStats.Stamina,
                    DefaultSettings.NextDayEnergyRestore
                ));
                Debug.Log($"[SimulationResult] Applied +{DefaultSettings.NextDayEnergyRestore} Energy for Next Day");
            }
            
            // Normal flow: advance to next day morning (this also resets daily actions)
            GameManager.Instance.AdvanceToNextDayMorning(7, 0);
            
            // Subscribe to section switch completion to fire GameStartEvent after transition
            UnityEngine.Events.UnityAction<GameSection, GameSection> onSwitchComplete = null;
            onSwitchComplete = (oldSection, newSection) =>
            {
                // Unsubscribe immediately
                UIPanelManager.Instance.OnSectionSwitched.RemoveListener(onSwitchComplete);
                
                // Fire GameStartEvent to reinitialize all UI components for the new day
                // This ensures backgrounds, buttons, and all UI elements are properly configured
                EventBus.EventBus<GameStartEvent>.Raise(new GameStartEvent 
                { 
                    Area = GameManager.Instance.Area, 
                    Time = GameManager.Instance.Time, 
                    Cycle = GameManager.Instance.Cycle 
                });
            };
            
            UIPanelManager.Instance.OnSectionSwitched.AddListener(onSwitchComplete);
            
            // Switch to InGame section; the result panel stays visible until the
            // section transition reaches its mid-point and hides Simulation panels.
            UIPanelManager.Instance.SwitchToSection(GameSection.InGame);
        }
    }
}
