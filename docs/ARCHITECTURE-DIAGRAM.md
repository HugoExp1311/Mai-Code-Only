# Mai's Love Story - Architecture Diagram

## System Overview

```mermaid
graph TB
    subgraph "Core Managers (Singletons)"
        GM[GameManager<br/>Game State & Flow]
        DM[DialogueManager<br/>Dialogue System]
        UPM[UIPanelManager<br/>UI Orchestration]
        AM[AudioManager<br/>Audio Playback]
        LM[LocalizationManager<br/>Multi-Language]
        TM[TimeManager<br/>Time Progression]
        DataM[DataManager<br/>Data Persistence]
    end

    subgraph "EventBus System"
        EB[EventBus<br/>Type-Safe Events]
        GSE[GameStartEvent]
        PCE[PlaceChangedEvent]
        TCE[TimeChangedEvent]
        RCE[ResourceChangedEvent]
        SCE[StatsChangedEvent]
    end

    subgraph "UI System"
        subgraph "InGame Section"
            NP[NavigationPanel]
            DP[DialoguePanel]
            L2DP[Live2DPanel]
            PSP[PlaceSelectionPanel]
            ACP[ActionConfirmPopup]
            MP[MaiPanel]
            MPP[MaiProfilePanel]
            SP[SkillPanel]
            ShP[ShopPanel]
            IP[InventoryPanel]
        end
    end

    subgraph "Character System"
        Player[Player<br/>Stats & Resources]
        Mai[Mai<br/>Character State]
        CIM[CharacterInteractManager<br/>Interactions]
        SM[SkillManager<br/>Skill Progression]
    end

    subgraph "Data Systems"
        Inv[Inventory<br/>Item Management]
        ShopM[ShopManager<br/>Shop Transactions]
        ItemDB[ItemDatabase<br/>Item Registry]
    end

    subgraph "External Systems"
        L2D[Live2D Cubism SDK<br/>Character Animation]
        FMOD[FMOD<br/>Audio Middleware]
        Addr[Unity Addressables<br/>Asset Management]
        Loc[Unity Localization<br/>Language Tables]
    end

    GM --> EB
    DM --> EB
    TM --> EB
    Player --> EB
    Mai --> EB

    EB --> NP
    EB --> DP
    EB --> L2DP
    EB --> PSP
    EB --> MP

    UPM --> NP
    UPM --> DP
    UPM --> L2DP
    UPM --> PSP
    UPM --> ACP
    UPM --> MP
    UPM --> MPP
    UPM --> SP
    UPM --> ShP
    UPM --> IP

    DM --> DP
    AM --> FMOD
    LM --> Loc

    CIM --> Player
    CIM --> Mai
    SM --> Player

    ShopM --> Inv
    ShopM --> ItemDB
    Inv --> ItemDB

    L2DP --> L2D
    GM --> Addr

    style GM fill:#ff9999
    style DM fill:#ff9999
    style UPM fill:#ff9999
    style AM fill:#ff9999
    style LM fill:#ff9999
    style EB fill:#99ccff
    style L2D fill:#99ff99
    style FMOD fill:#99ff99
    style Addr fill:#99ff99
    style Loc fill:#99ff99
```

## Data Flow Diagram

```mermaid
sequenceDiagram
    participant Player
    participant GM as GameManager
    participant EB as EventBus
    participant UPM as UIPanelManager
    participant DM as DialogueManager
    participant DP as DialoguePanel
    participant AM as AudioManager

    Player->>GM: Start Game
    GM->>EB: Raise GameStartEvent
    EB->>UPM: Notify Panels
    UPM->>DP: Show DialoguePanel
    
    Player->>DP: Select Dialogue Choice
    DP->>DM: Process Choice
    DM->>AM: Play Sound Effect
    DM->>EB: Raise ResourceChangedEvent
    EB->>UPM: Update UI
    UPM->>DP: Refresh Display
```

## UI Panel Hierarchy

```mermaid
graph LR
    subgraph "GameSection: MainMenu"
        MM[MainMenuPanel]
        LS[LoadSavePanel]
        Set[SettingsPanel]
    end

    subgraph "GameSection: InGame"
        Nav[NavigationPanel]
        Dia[DialoguePanel]
        L2D[Live2DPanel]
        Place[PlaceSelectionPanel]
        Confirm[ActionConfirmPopup]
        Mai[MaiPanel]
        MaiP[MaiProfilePanel]
        Skill[SkillPanel]
        Shop[ShopPanel]
        Inv[InventoryPanel]
    end

    subgraph "GameSection: Minigame"
        MG1[MinigamePanel]
    end

    subgraph "GameSection: Simulation"
        Sim[SimulationPanel]
    end

    UPM[UIPanelManager] --> MM
    UPM --> LS
    UPM --> Set
    UPM --> Nav
    UPM --> Dia
    UPM --> L2D
    UPM --> Place
    UPM --> Confirm
    UPM --> Mai
    UPM --> MaiP
    UPM --> Skill
    UPM --> Shop
    UPM --> Inv
    UPM --> MG1
    UPM --> Sim

    style UPM fill:#ff9999
    style Nav fill:#99ccff
    style Dia fill:#99ccff
    style L2D fill:#99ccff
```

## Character System Architecture

```mermaid
classDiagram
    class ICharacter {
        <<interface>>
        +GetStats()
        +GetResources()
    }

    class Player {
        -BasicStats stats
        -BasicResource resources
        -Inventory inventory
        -SkillManager skills
        +PerformAction()
        +UseItem()
    }

    class Mai {
        -BasicStats stats
        -BasicResource resources
        -Live2DModel model
        +UpdateExpression()
        +UpdatePose()
    }

    class CharacterInteractManager {
        -Player player
        -Mai mai
        +HandleInteraction()
        +ProcessAction()
    }

    class SkillManager {
        -List~ISkill~ skills
        +UnlockSkill()
        +UpgradeSkill()
        +GetAvailableSkills()
    }

    class ISkill {
        <<interface>>
        +Execute()
        +CanUse()
    }

    class CommonSkillImpl {
        +Execute()
        +CanUse()
    }

    ICharacter <|.. Player
    ICharacter <|.. Mai
    Player --> SkillManager
    CharacterInteractManager --> Player
    CharacterInteractManager --> Mai
    SkillManager --> ISkill
    ISkill <|.. CommonSkillImpl
```

## Dialogue System Flow

```mermaid
flowchart TD
    Start[Start Dialogue] --> LoadCSV[Load CSV Data]
    LoadCSV --> ParseData[Parse Dialogue Data]
    ParseData --> CreateSequence[Create DialogueSequenceSO]
    CreateSequence --> ShowPanel[Show DialoguePanel]
    ShowPanel --> DisplayLine[Display Dialogue Line]
    DisplayLine --> HasChoices{Has Choices?}
    
    HasChoices -->|Yes| ShowChoices[Show Choice Buttons]
    HasChoices -->|No| WaitInput[Wait for Continue]
    
    ShowChoices --> PlayerChoice[Player Selects Choice]
    WaitInput --> Continue[Player Continues]
    
    PlayerChoice --> ProcessChoice[Process Choice]
    Continue --> NextLine{More Lines?}
    ProcessChoice --> NextLine
    
    NextLine -->|Yes| DisplayLine
    NextLine -->|No| GiveRewards[Give Rewards]
    
    GiveRewards --> RaiseEvents[Raise Events]
    RaiseEvents --> End[End Dialogue]
```

## Localization System

```mermaid
graph TB
    subgraph "Localization Tables"
        UITable[UI Table<br/>English/Vietnamese/Japanese]
        DialogueTable[Dialogue Table<br/>English/Vietnamese/Japanese]
        ItemTable[Item Table<br/>English/Vietnamese/Japanese]
    end

    subgraph "Localization Manager"
        LM[LocalizationManager]
        TR[TableRegistry]
        KH[KeyHelper]
    end

    subgraph "UI Components"
        LT[LocalizedText<br/>Component]
        TMP[TextMeshProUGUI]
    end

    UITable --> LM
    DialogueTable --> LM
    ItemTable --> LM
    
    LM --> TR
    LM --> KH
    
    LM --> LT
    LT --> TMP
    
    Player[Player Changes Language] --> LM
    LM --> RefreshAll[Refresh All Localized Components]
```

## Audio System Integration

```mermaid
graph LR
    subgraph "FMOD Studio"
        Events[FMOD Events]
        Banks[Audio Banks]
    end

    subgraph "Unity"
        AM[AudioManager]
        FE[FMODEvents<br/>Constants]
        BS[ButtonSound<br/>Component]
    end

    subgraph "Game Systems"
        UI[UI Panels]
        DM[DialogueManager]
        GM[GameManager]
    end

    Events --> Banks
    Banks --> AM
    FE --> AM
    
    UI --> BS
    BS --> AM
    
    DM --> AM
    GM --> AM
    
    AM --> Play[Play Sound]
```

## Asset Loading Flow

```mermaid
sequenceDiagram
    participant GM as GameManager
    participant Addr as Addressables
    participant Asset as Asset Bundle
    participant Scene as Scene/Prefab

    GM->>Addr: LoadAssetAsync<T>(key)
    Addr->>Asset: Locate Asset
    Asset->>Addr: Return Handle
    Addr->>GM: AsyncOperationHandle<T>
    
    GM->>GM: await handle.Task
    GM->>Scene: Instantiate/Use Asset
    
    Note over GM,Scene: Asset in use
    
    GM->>Addr: Release(handle)
    Addr->>Asset: Unload if no references
```

---

## Legend

- 🔴 **Red** - Core Singleton Managers
- 🔵 **Blue** - EventBus and Events
- 🟢 **Green** - External Systems (Live2D, FMOD, Unity)
- ⚪ **White** - UI Panels and Components

---

**Last Updated**: 2025-11-13  
**Related**: See [CCDK.md](../CCDK.md) for detailed documentation
