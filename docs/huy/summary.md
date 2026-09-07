# MAI'S LOVE STORY — TỔNG HỢP CÔNG NGHỆ & CODEBASE

> Tài liệu onboarding (Huy) — xác minh trực tiếp từ code ngày 2026-08-23.
> Source of truth: `Assets/Scripts/` (176 file runtime) + `Assets/Editor/` (31 file) + `Assets/Resources/`.
> Khi docs cũ (`docs/*`, `docs/ai-context/*`, viết 10–11/2025) mâu thuẫn với code → **tin code**.

---

## 1. Công nghệ ĐANG DÙNG THẬT ✅

| Công nghệ | Bằng chứng trong code | Vai trò |
|---|---|---|
| **Unity 6000.3.22f1** + URP 17.3 | ProjectSettings, `Packages/manifest.json` | Engine |
| **C# / uGUI + TextMesh Pro** | 25+ file dùng TMPro | UI (dùng `UIPanel`/`Button`, KHÔNG dùng UI Toolkit) |
| **Live2D Cubism SDK 5-r.5** | `Assets/Live2D/Cubism/`, 10+ script Cubism | Nhân vật Mai: biểu cảm, motion, animation sex scene |
| **FMOD** | `Assets/Plugins/FMOD/`, 4 file dùng `FMODUnity` (`AudioManager`, `FMODEvents`, `SexSimulationSfxPlayer`, `ShopInteraction`) | Toàn bộ audio — qua `RuntimeManager`, không dùng AudioSource |
| **Custom CSV Localization** | `Base/Localization/` + CSV ở `Assets/Resources/Localization/{UI\|Dialogues}/{en-US,vi-VN,ja-JP}.csv` | 3 ngôn ngữ, tự viết, KHÔNG dùng bảng Unity Localization |
| **Custom EventBus** | `EventBus/` (3 file core) + `Events/` (11 event) | Giao tiếp giữa các hệ thống (type-safe, generic) |
| **ScriptableObject** | 5 class (mục 4) | Dữ liệu author trong Editor |
| **Unity Input System** | `CursorManager`, `DialoguePanel`, `MaiPanel`, `ShopInteraction` | Click/chuột |
| **Unity Test Framework** | 10 file test trong `Assets/Editor/Tests/` | EditMode contract tests |

## 2. Công nghệ ĐÃ ADD NHƯNG KHÔNG DÙNG (docs ghi dư) ⚠️

| Package | Thực trạng |
|---|---|
| **Addressables 2.9.1** | ❌ 0 dòng code dùng. `Base/AddressableKeys.cs` đánh dấu `[Obsolete]` ("Use Resources.Load or FMOD instead"). Game load asset bằng `Resources.Load` (vd `GameManager` load `Sequences/Intro/Intro`) |
| **Unity Localization package** | ⚠️ Nửa vời: vài file còn `using UnityEngine.Localization` (tàn dư) nhưng hệ thật là CSV custom qua `LocalizationManager` |
| **Cinemachine, ProBuilder, Timeline, Visual Effect Graph, Visual Scripting, AI Inference/Navigation, multiplayer.center** | ❌ Không dùng (template mặc định) |
| **Unity Analytics, Unity Collab** | ❌ Mặc định, không dùng |

## 3. Bản đồ code (cấu trúc thực tế — khác docs cũ)

```
Assets/Scripts/                     (176 file runtime)
├── GameManager.cs                  # Game flow: MainMenu → CG Intro (4 phần) → InGame → Simulation
├── DialogueManager.cs              # State machine hội thoại, CSV dialogue, branching, rewards
├── TimeManager.cs                  # Thời gian in-game, day cycle
├── UIManager.cs
├── Base/
│   ├── Character/                  # Player, Target (Mai), Stats, Skills (8 skill), Action, Inventory
│   ├── Dialogue System/            # DialogueSequenceSO + node/line/choice/reward data
│   ├── CG/                         # CGDialogueSequenceSO (CG = hình nền tĩnh, không Live2D)
│   ├── Localization/               # CSV localization (LocalizationManager, KeyHelper, config...)
│   ├── Audio/                      # FMOD wrapper
│   ├── Settings/                   # Enums: Language, Area, DayCycle, ScreenSettings
│   ├── SexScenes/, Inventory/, Event System/, UI/Popup/
├── EventBus/                       # 3 file core: EventBus, EventBinding, IEvent
├── Events/                         # 11 event struct
├── Managers/                       # SexSimulationManager, SexSessionData, ItemManager
├── UI/                             # ~50 panel/component + SexPosition/ (7 position config)
├── Live2D/                         # Live2DMotionController + animation-event receivers
├── Shop/, Inventory/
Assets/Editor/                      # DialogueImporter, DebugToolsWindow, Localization tools, Tests/
Assets/Resources/                   # Sequences/ (.asset), Shop Item/, Localization CSV, Dialogues.csv
Assets/Scenes/Game.unity            # CHỈ 1 scene duy nhất
```

**8 hệ thống chính:**

| Hệ thống | File gốc | Skill doc |
|---|---|---|
| 🎮 Game flow | `GameManager.cs` | `.agents/skills/game-systems/` |
| 💬 Dialogue | `DialogueManager.cs`, `Assets/Editor/DialogueImporter.cs` | `skills/dialogue-system/` |
| 👤 Nhân vật & stats | `Base/Character/` | `skills/character-stats/` |
| 🔞 Sex simulation | `Managers/SexSimulationManager.cs`, `UI/SexPosition/` | `skills/sex-simulation/` |
| 🖥️ UI | `UI/UIPanelManager.cs` | `skills/ui-system/` |
| 🌍 i18n | `Base/Localization/` | `skills/dialogue-system/dialogue-localization.md` |
| 🔊 Audio | `Base/Audio/` | `skills/technical-systems/fmod-audio.md` |
| 🎬 Live2D | `Scripts/Live2D/` | `skills/technical-systems/live2d-animation.md` |

**Lỗ hổng chức năng đáng chú ý (chưa implement):**
- 🚫 **Save/Load hoàn toàn chưa có** — `GameManager.LoadGame()` là placeholder TODO (chỉ Money/SkillPoint sống sót qua `PlayerPrefs` nhờ `CommonResource`)
- 🚫 Doggy & Standing positions (`SexPositionConfigFactory` throw NotImplementedException)
- Chỉ có 1 scene duy nhất


## 4. Chi tiết danh sách script theo features

> Kiểm ngày 2026-08-24: **176 file runtime** (`Assets/Scripts/`) + **31 file editor** (`Assets/Editor/`) — list bên dưới phủ đủ từng file.
> Quy ước: 🟢 = singleton MonoBehaviour gắn trong scene `Game.unity` • SO = ScriptableObject • file trong thư mục con `Editor/` chỉ chạy trong Unity Editor.

### Bản đồ nhanh: feature → nơi chứa

| # | Feature | Nơi chứa chính | Số file |
|---|---|---|---|
| 4.1 | Core & game flow | `Scripts/*.cs` (root) + `Base/` core | 17 |
| 4.2 | EventBus & Events | `EventBus/`, `Events/` | 14 |
| 4.3 | Dialogue system | `Base/Dialogue System/`, `UI/DialoguePanel.cs` | 7 |
| 4.4 | CG (cinematic intro) | `Base/CG/`, `UI/CGPanel.cs` | 5 |
| 4.5 | Character: stats / skills / actions | `Base/Character/` | 15 |
| 4.6 | Sex simulation | `Base/SexScenes/`, `Managers/` (sex), `UI/SexPosition/`, `UI/Visibility/`, sex panels | 20 |
| 4.7 | Live2D (motion, animation, render) | `Scripts/Live2D/`, `UI/Live2DPanel.cs` | 25 |
| 4.8 | UI framework (panel / tab / popup infra) | `UI/` infra, `Base/UI/Popup/`, `UI/Editor/` | 31 |
| 4.9 | UI screens in-game | `UI/*Panel.cs` (Mai, Place, Skill, Settings...) | 14 |
| 4.10 | Shop & Inventory | `Shop/`, `Inventory/`, `Base/Inventory/`, `Managers/ItemManager.cs` | 11 |
| 4.11 | Localization (CSV custom) | `Base/Localization/` | 14 |
| 4.12 | Audio (FMOD) | `Base/Audio/` | 3 |
| 4.13 | Editor tools | `Assets/Editor/` (trừ Tests) | 21 |
| 4.14 | Tests (EditMode) | `Assets/Editor/Tests/` | 10 |

**Tổng: 207 file = 176 (trong `Assets/Scripts/`) + 31 (trong `Assets/Editor/`)** ✅
> Ghi chú: một số file nằm trong `Assets/Scripts/` nhưng là editor-only (thư mục con `Editor/`, hoặc `#if UNITY_EDITOR`) — được đánh dấu 🔧 trong bảng.

### 4.1 Core & game flow 🎮

Entry point vòng đời game: MainMenu → CG Intro → InGame (đi lại trong nhà, nói chuyện với Mai) → Simulation (sex sim). Mọi hệ thống bám vào `GameManager.Instance`.

| File | Loại | Chức năng |
|---|---|---|
| `GameManager.cs` | 🟢 Mono singleton | Trạng thái game tổng: giữ `Player`/`Target` (Mai), đổi section (MainMenu/Intro/InGame/Simulation), chuyển Area bằng event, `LoadGame()` hiện là placeholder (chưa có save) |
| `DialogueManager.cs` | 🟢 Mono singleton | State machine hội thoại: chạy `DialogueSequenceSO`/`CGDialogueSequenceSO`, typewriter, branching, cộng reward stat khi xong node |
| `TimeManager.cs` | class tĩnh | Logic thời gian in-game: khung giờ Mai ở nhà (sáng/tối/cuối tuần) — data để game quyết định gặp Mai ở đâu |
| `UIManager.cs` | 🟢 Mono singleton | Điều phối UI tổng theo `GameStartEvent`/time change; đổi ngôn ngữ UI |
| `CursorManager.cs` | 🟢 Mono singleton | Đổi texture chuột (chuột thường / khi hover clickable), dùng Input System; có flag `isExternalControl` để hệ thống khác tạm chiếm |
| `Base/DataManager.cs` | SO | SO giữ tham chiếu `IPlayer` + `ITarget` (Mai) — tạo qua menu `Mai's Love Story/Data Manager` |
| `Base/DefaultSettings.cs` | static | Mọi hằng số author: stat/resource mặc định, danh sách shop item, skill point cost, khung giờ daily action |
| `Base/Basic.cs` | interfaces | `IBasic<T>`/`IResource<T>`/`IStats<T>` — contract chung cho stat & resource |
| `Base/Utilities.cs` | helper | `KeyValuePair<TKey,TValue>` serialize được (dùng cho time-based backgrounds) |
| `Base/Settings/EnumSettings.cs` | enums | `Language`, `ScreenSettings`, `Area` (Home, Shop, Park...), `DayCycle`, `SettingsType`, `Gender`... |
| `Base/Settings/SettingsManager.cs` | class | Giữ volume Music/Sound/Voice + setting ngôn ngữ, màn hình |
| `Base/Logger/FileLogger.cs` | static | Ghi log ra file `Logs/` (chủ yếu cho DialogueImporter) |
| `Base/AddressableKeys.cs` | ⚠️ obsolete | Placeholder Addressables, đã đánh dấu `[Obsolete]` — không dùng |
| `Base/IsExternalInit.cs` | shim | Cho phép dùng `record` (C# 9) trên Unity cũ |
| `AnimationType.cs` | enum | Liệt kê mọi biểu cảm Live2D của Mai (`MaiBlush`, `MaiHorny`, `MaiTalk`...) — tên tương ứng trigger animator |
| `AdjustInnerHandlePivot.cs` | Mono, `[ExecuteAlways]` | UI helper: chỉnh pivot handle khi scroll (ScrollRect.onValueChanged) |
| `RectTransformContextMenu.cs` | Editor-only | Menu CONTEXT RectTransform: tính anchors tương đối Canvas |

### 4.2 EventBus & Events 🔌

Giao tiếp loose-coupled giữa các hệ thống. `EventBus<TEvent>` là static generic — register `EventBinding<TEvent>`, publish bằng `EventBus<T>.Raise()`.

| File | Chức năng |
|---|---|
| `EventBus/EventBus.cs` | Bus static generic (HashSet bindings) |
| `EventBus/EventBinding.cs` | Wrapper đăng ký/hủy đăng ký handler |
| `EventBus/IEvent.cs` | Marker interface cho mọi event struct |
| `Events/GameStartEvent.cs` | Bắt đầu game mới (kèm Area + thời gian) |
| `Events/TimeChangedEvent.cs` | Thời gian in-game trôi → UI ngày/giờ cập nhật, Mai đổi chỗ |
| `Events/PlaceChangedEvent.cs` | Player đổi khu vực |
| `Events/PlaceUnavailableEvent.cs` | Khu vực không mở khung giờ đó → `NoticeUI` hiện thông báo |
| `Events/StatsChangedEvent.cs` | Stat đổi → mọi panel số liệu tự refresh |
| `Events/ResourceChangedEvent.cs` | Money/resource đổi |
| `Events/SensitivePointsDecayEvent.cs` | Decay sensitive points 1 lần/đêm (thay 5 StatsChangedEvent) |
| `Events/TransitionRequestEvent.cs` / `TransitionMidPointEvent.cs` / `TransitionCompleteEvent.cs` | Chuỗi fade màn hình: request → điểm đen giữa (setup area mới) → hoàn tất |
| `Events/AreaTransitionReadyEvent.cs` | Area mới sẵn sàng để hiển thị sau fade |

### 4.3 Dialogue system 💬

Hội thoại thường (kèm Live2D biểu cảm). Data = `DialogueSequenceSO` (.asset) được import từ CSV bởi `Assets/Editor/DialogueImporter.cs`; text lưu bằng localization key, không hard-code.

| File | Chức năng |
|---|---|
| `Base/Dialogue System/DialogueSequenceSO.cs` | SO chứa cả cây hội thoại: nodes, StartNodeID, validate graph; enum `SequenceType` (Linear / RandomOutcome) |
| `Base/Dialogue System/DialogueNodeData.cs` | 1 node: list lines, choices, `rewardsOnNodeCompletion` |
| `Base/Dialogue System/DialogueLine.cs` | 1 dòng thoại: speaker (Player/Mai), localizationKey, animation/emotion trigger |
| `Base/Dialogue System/DialogueChoiceData.cs` | 1 lựa chọn: localizationKey + node đến |
| `Base/Dialogue System/DialogueReward.cs` | Reward gắn node: target + stat + amount |
| `Base/Dialogue System/IDialogueUI.cs` | Contract UI mà DialogueManager gọi (show panel, typewriter, fade...) — `DialoguePanel` implement |
| `UI/DialoguePanel.cs` | UI hội thoại thật: text TMP, tên người nói, nút next, click-by-InputSystem, fade panel |

### 4.4 CG sequences (cinematic, ảnh tĩnh) 🖼️

Biến thể dialogue cho intro/tutorial: giống hệt dialogue thường nhưng mỗi dòng kèm Sprite nền, KHÔNG điều khiển Live2D.

| File | Chức năng |
|---|---|
| `Base/CG/CGDialogueSequenceSO.cs` | SO chứa chuỗi CG dialogue (menu `Mai's Love Story/CG Dialogue Sequence`) |
| `Base/CG/CGDialogueNodeData.cs` | Node CG: lines, choices, `nextNodeID` override từ CSV |
| `Base/CG/CGDialogueLine.cs` | Line CG = `DialogueLine` + sprite nền |
| `Base/CG/CGDialogueChoiceData.cs` | Choice CG, tham chiếu sang CGDialogueSequenceSO khác |
| `UI/CGPanel.cs` | Panel hiển thị sprite CG theo sự kiện của DialogueManager |

### 4.5 Character: stats, skills, actions 👤

Toàn bộ model nhân vật là **pure C# class** (không phải MonoBehaviour) — chỉ có `CharacterInteractManager` là Mono để nhận click. Player & Mai implement `ICharacter`.

**Core & interactions:**

| File | Chức năng |
|---|---|
| `Base/Character/ICharacter.cs` | Contract nhân vật: `GetName`, `Talk`, `GetGender`, `DoAction(CharacterActions)` |
| `Base/Character/Player.cs` | Player: stamina/resource, inventory, skills; phát `StatsChangedEvent` khi đổi |
| `Base/Character/Mai.cs` | `Target` (Mai): love/libido/sensitive points, nhận quà, trả `Talk()` theo mood |
| `Base/Character/CharacterInteractManager.cs` | 🟢 singleton: giữ sequence hội thoại mặc định khi click Mai (`defaultTalkSequence`) |

**Stats (`Stats/`):**

| File | Chức năng |
|---|---|
| `BasicStats.cs` | enum tên stat (Stamina, Knowledge, Charming, Love, Libido, sensitive points...) |
| `BasicResource.cs` | enum tên resource (Money, SkillPoint...) |
| `CommonStat.cs` | Generic stat có max value (`CommonStat<int>`) |
| `CommonResource.cs` | Resource persist bằng **PlayerPrefs** (key `BasicResource_{name}`) — thứ duy nhất "sống sót" sau khi tắt game |
| `RewardTarget.cs` | enum Player / Mai — dùng trong dialogue reward & speaker |

**Skills (`Skills/`)** — 8 skill (Cooking, Cleaning, Massage... xem `DefaultSettings`):

| File | Chức năng |
|---|---|
| `ISkill.cs` | Contract skill: level, value, unlock, upgrade/downgrade, point cost |
| `CommonSkillImpl.cs` | Implement mặc định của `ISkill` |
| `SkillManager.cs` | Dictionary SkillType → ISkill, trừ SkillPoint khi upgrade |

**Actions (`Action/`)** — mọi hành động là C# record:

| File | Chức năng |
|---|---|
| `CharacterActions.cs` | Abstract record: `BuyItem`, `UseItem`, `SkillLevelUp`, `Sleep`, `Eating`, `Sex`... — `Player.DoAction` switch xử lý |
| `PlayerDailyAction.cs` | enum hành động theo ngày (Sleep, Eating, Sex, Working, Talking, Exercise, Progress) |
| `DailyActionMetadata.cs` | Metadata tập trung: khung giờ + area cho từng daily action |

### 4.6 Sex simulation 🔞

Flow: UI bấm nút → `SexSimulationManager` tiêu stamina, cộng cum bar → đạt ngưỡng → orgasm → `SexSessionData` ghi nhận → thoát → `SimulationResultPanel` tổng kết. Mỗi position là một **strategy class** (`SexPositionConfig`).

| File | Chức năng |
|---|---|
| `Managers/SexSimulationManager.cs` | 🟢 singleton: stamina consumption, cum bar, orgasm tracking, điều phối position hiện tại |
| `Managers/SexSessionData.cs` | Đếm thrust/orgasm từng hole trong 1 session (persist giữa các round) |
| `Base/SexScenes/SexSceneDefinition.cs` | enum `SexSceneType` + definition data từng scene |
| `Base/SexScenes/SexSceneUnlockService.cs` | Điều kiện unlock từng scene theo stat; lưu unlock bằng PlayerPrefs (`SexSceneUnlocked_*`) |
| `UI/SexPosition/SexPositionConfig.cs` | Base class strategy: luật hiện button theo `SimulationState` |
| `UI/SexPosition/SexPositionTypes.cs` | enum `SimulationState` (Idle/Selecting/Active) + enum position |
| `UI/SexPosition/SexPositionConfigFactory.cs` | Factory tạo config; ⚠️ DoggyStyle & Standing **throw NotImplementedException** |
| `UI/SexPosition/MissionaryPositionConfig.cs` | Chiến lược Missionary (13 animator parameters) |
| `UI/SexPosition/CowgirlPositionConfig.cs` | Chiến lược Cowgirl (vaginal-only, loop-entry signal từ animator) |
| `UI/SexPosition/RoleplayPussyPositionConfig.cs` | Chiến lược Roleplay Pussy (toy Hitachi/Egg Vib) |
| `UI/SexPosition/RoleplayButtholePositionConfig.cs` | Chiến lược Roleplay Butthole (egg mechanic) |
| `UI/SexPosition/RoleplayBlowjobPositionConfig.cs` | Chiến lược Blowjob (blend tree + 3 tongue loop) |
| `UI/SexPosition/RoleplayPaizuriPositionConfig.cs` | Chiến lược Paizuri (9 action, lock theo category) |
| `UI/SimulationNavigationPanel.cs` | Panel điều khiển trong sex sim: nút action theo position, SFX cue, skill gating |
| `UI/SimulationResultPanel.cs` | Kết quả sau session: stat thay đổi, số orgasm... |
| `UI/SimulationResultItem.cs` | 1 dòng kết quả (label + value) |
| `UI/SexSceneUnlockPanelController.cs` | Panel điều kiện unlock sex scene |
| `UI/Visibility/SexButtonVisibility.cs` | Hiện nút Sex 21:00–23:00, 1 lần/ngày |
| `UI/Visibility/EatingButtonVisibility.cs` | Hiện nút Eating 19:00–21:00, 1 lần/ngày |
| `UI/Visibility/SleepButtonVisibility.cs` | Nút Sleep luôn hiện |

### 4.7 Live2D 🎬

Điều khiển model Cubism (Mai + Staff): motion, animation event từ sex sim clip, và render pass URP lọc theo camera.

**Runtime core:**

| File | Chức năng |
|---|---|
| `Live2D/Live2DMotionController.cs` | Phát motion Cubism theo state (map từ `AnimationType`) |
| `UI/Live2DPanel.cs` | Panel trung tâm quản lý MỌI model Live2D trong hierarchy (show/hide theo section) |
| `Live2D/CameraFilteredCubismRenderPassFeature.cs` | URP render pass feature: render Cubism cho mọi camera **trừ** camera bị đánh dấu exclude |
| `Live2D/CubismCameraRenderExclusion.cs` | Marker gắn camera để skip Cubism pass (vd camera UI) |

**Sex sim animation bridge** (nhận event từ animation clip Live2D → gọi gameplay):

| File | Chức năng |
|---|---|
| `Live2D/MissionaryAnimationEventReceiver.cs` | Missionary: chuyển animation event → gameplay, toggle `Fast` param |
| `Live2D/CowgirlAnimationEventReceiver.cs` | Cowgirl: guarded callbacks trên motion clip |
| `Live2D/CowgirlStateEntryBehaviour.cs` | StateMachineBehaviour báo boundary vào loop Cowgirl |
| `Live2D/RoleplayBlowjobAnimationEventReceiver.cs` | Blowjob loop events (reject khi đang chuyển state) |
| `Live2D/RoleplayBlowjobStartStateBehaviour.cs` | → `SexSimulationManager.NotifyRoleplayStartEntered()` |
| `Live2D/RoleplayBlowjobAfterCummingStateBehaviour.cs` | → `NotifyRoleplayAfterCummingEntered()` |
| `Live2D/RoleplayButtholeAnimationEventReceiver.cs` | Butthole loop events |
| `Live2D/RoleplayButtholeStartStateBehaviour.cs` | Butthole start boundary |
| `Live2D/RoleplayButtholeEggLoopStateBehaviour.cs` | Trạng thái egg loop (butthole) |
| `Live2D/RoleplayButtholeCumming2StateBehaviour.cs` | Cumming variant butthole |
| `Live2D/RoleplayPaizuriAnimationEventReceiver.cs` | Paizuri loop events |
| `Live2D/RoleplayPaizuriStartStateBehaviour.cs` | Paizuri start boundary |
| `Live2D/RoleplayPaizuriAfterCummingStateBehaviour.cs` | After-cumming paizuri |
| `Live2D/RoleplayPussyAnimationEventReceiver.cs` | Pussy loop events |
| `Live2D/RoleplayPussyStartStateBehaviour.cs` | Pussy start boundary |
| `Live2D/RoleplayPussyEggWorkStateBehaviour.cs` | Egg vib hoạt động (pussy) |
| `Live2D/RoleplayPussyAfterCummingStateBehaviour.cs` | After-cumming pussy |
| `Live2D/SexSimulationSfxPlayer.cs` | Phát SFX FMOD theo cue (Slow/Fast/CumInside/Squirt) map từ position |
| `Live2D/SexSimulationSfxPreviewContext.cs` | Marker cho preview animation đơn lập (test bench) — không cần sim thật |

**Editor:**

| File | Chức năng |
|---|---|
| `Live2D/Editor/Live2DMotionControllerEditor.cs` | 🔧 Inspector riêng cho MotionController (dropdown animator params) |
| `Live2D/Editor/Live2DTestBenchWindow.cs` | 🔧 EditorWindow test animation Live2D đơn lập + SFX preview |

### 4.8 UI framework 🖥️

Panel system tự viết: `UIPanelManager` quản lý stack + sorting order; `UIPanel` là base của mọi panel; popup/tab là cơ chế generic tái dùng.

**Core panel system:**

| File | Chức năng |
|---|---|
| `UI/UIPanelManager.cs` | 🟢 singleton: lifecycle panel, z-order tự động, show/hide panel |
| `UI/UIPanel.cs` | Base panel: show/hide + event, sorting order, background click |
| `UI/UIPanelWithBackground.cs` | Panel có background đổi theo Area/DayCycle |
| `UI/UIPanelBackgroundController.cs` | ⚠️ DEPRECATED — dùng `UIPanelWithBackground` thay thế |
| `UI/RuntimePanelConfig.cs` | Struct config panel theo scenario (title, background, behavior type) |
| `UI/TransitionPanel.cs` | Fade in/out màn hình khi chuyển area/section (nghe `TransitionRequestEvent`) |
| `UI/Effect/InvertedMaskImage.cs` | Image mask alpha ngược (soft mask) cho con |
| `UI/Effect/MaskedFillImage.cs` | Image render qua alpha ngược của mask cha |

**Popup system:**

| File | Chức năng |
|---|---|
| `Base/UI/Popup/PopupState.cs` | Data 1 state popup: header, icon, 3 nút |
| `Base/UI/Popup/PopupButtonState.cs` | Data 1 nút popup: text + action |
| `Base/UI/Popup/PopupButtonAction.cs` | Action record popup: `ProgressTime`, cộng stat... |
| `Base/UI/Popup/ActionConfirmPopupDatabase.cs` | Static DB map action (Sleep/Eating/Sex) → state machine popup |
| `UI/ActionConfirmPopup.cs` | UI popup xác nhận hành động hàng ngày |
| `UI/ActionConfirmPopupData.cs` | Data truyền vào popup (icon, header, stat change) |
| `UI/ConfirmPopup.cs` | Popup Yes/No đơn giản (vd "Return to Title?") |
| `UI/NoticeUI.cs` | Toast notification tự đóng; nghe `PlaceUnavailableEvent` |
| `UI/FloatingStatPopupController.cs` | Popup số bay (+2 Love) khi stat đổi |
| `UI/FloatingStatPopupItem.cs` | 1 item text bay (fade, đổi màu) |

**Tab system:**

| File | Chức năng |
|---|---|
| `UI/TabManager.cs` | Tab trong 1 panel cha, switch bằng `ITabStateHandler` |
| `UI/TabComponent.cs` | Marker tabId cho GameObject |
| `UI/ScrollViewTabManager.cs` | Tab cho scroll sections |
| `UI/ScrollViewTabButton.cs` | Nút chuyển section |
| `UI/TabStateHandlers/ITabStateHandler.cs` | Contract xử lý visual state nút tab |
| `UI/TabStateHandlers/AnimatorTabStateHandler.cs` | Handler bằng Animator bool params |

**Editor (UI):**

| File | Chức năng |
|---|---|
| `UI/Editor/UIPanelEditor.cs` | 🔧 Inspector UIPanel (ẩn field theo behavior type) |
| `UI/Editor/UIPanelManagerEditor.cs` | 🔧 Inspector UIPanelManager |
| `UI/Editor/TabManagerEditor.cs` | 🔧 Dropdown tab ID cho TabManager |
| `UI/Editor/ScrollViewTabManagerEditor.cs` | 🔧 Dropdown section cho ScrollViewTabManager |
| `UI/Editor/PostProcessors/ButtonActionPostProcessor.cs` | 🔧 Tự gắn `UIPanelButtonAction` khi thêm Button |
| `UI/Editor/PostProcessors/UIMigrationPreferences.cs` | 🔧 Shared prefs cho post processors |
| `UI/Editor/PostProcessors/UIPanel2ToUIPanelPostProcessor.cs` | 🔧 ⚠️ OBSOLETE — migration UIPanel2→UIPanel đã xong |

### 4.9 UI screens in-game 🗺️

Panel người chơi thấy khi chơi — tất cả kế thừa `UIPanel`.

| File | Chức năng |
|---|---|
| `UI/InGameNavigationPanel.cs` | Thanh navigation: hiện stat player + thời gian |
| `UI/MaiPanel.cs` | Panel Mai: tự show/hide theo Area + khung giờ (dùng `TimeManager`) |
| `UI/MaiBodyPanel.cs` | Hiện sensitive points từng vùng cơ thể Mai |
| `UI/MaiProfilePanel.cs` | Hồ sơ Mai: Love, Libido (localized smart strings) |
| `UI/SkillPanel.cs` | Panel stat player: Energy/Knowledge/Charming + skill point |
| `UI/SkillUpgradeUI.cs` | UI nâng cấp skill bằng SkillPoint |
| `UI/PlaceSelectionPanel.cs` | Chọn Area để di chuyển; disable nút theo khung giờ |
| `UI/PlaceUI.cs` | 1 area button: background đổi theo DayCycle |
| `UI/SettingPanel.cs` | Settings: volume, ngôn ngữ, fullscreen |
| `UI/DayTimeUI.cs` | HUD ngày/thứ/giờ (nghe `TimeChangedEvent`) |
| `UI/ShopInteraction.cs` | Click Live2D Staff (Cubism raycast) → mở/đóng shop + SFX |
| `UI/Actions/UIPanelButtonAction.cs` | Component gắn Button: khai báo action (show/hide panel, daily action, time progress) |
| `UI/Actions/Editor/UIPanelButtonActionEditor.cs` | 🔧 Inspector auto-fill Hide action theo parent panel |
| `UI/BaseItemUI.cs` | Base hiển thị item (icon/tên/mô tả) cho Shop+Inventory |

### 4.10 Shop & Inventory 🛒

| File | Chức năng |
|---|---|
| `Managers/ItemManager.cs` | 🟢 singleton: mọi dữ liệu item (shop + inventory), sprite map |
| `Shop/ShopItemData.cs` | SO visual 1 item shop (menu `Shop/Item Visual`) |
| `Shop/ShopItemUI.cs` | UI 1 item trong shop: giá, nút mua (trừ Money, thêm Inventory) |
| `Shop/ShopPanel.cs` | Panel shop: tabs Goods/Gifts, refresh sau mua |
| `Shop/Editor/ShopItemDataEditor.cs` | 🔧 Inspector dropdown chọn item từ `DefaultSettings.ShopItems` |
| `Base/Inventory/Inventory.cs` | List item của player, persist PlayerPrefs key `PlayerInventory` |
| `Base/Inventory/Item/IItem.cs` | Contract item + enum `ItemType` (Energy, Gift, Toy...) |
| `Base/Inventory/Item/CommonItem.cs` | Implement item thường (giá, số lượng, value) |
| `Base/Inventory/Item/ItemDatabase.cs` | ⚠️ File rỗng (chưa implement) |
| `Inventory/InventoryPanel.cs` | Panel inventory (lấy data từ ItemManager) |
| `Inventory/InventoryItemUI.cs` | UI 1 item inventory + nút dùng/tặng |

### 4.11 Localization 🌍

Tự viết 100%: CSV theo domain (`UI`, `Dialogues`) x 3 ngôn ngữ trong `Assets/Resources/Localization/{Domain}/{locale}.csv`. KHÔNG dùng Unity Localization package.

| File | Chức năng |
|---|---|
| `Base/Localization/LocalizationManager.cs` | 🟢 singleton: load CSV theo locale, lookup key, đổi ngôn ngữ runtime |
| `Base/Localization/LocalizedText.cs` | Component gắn TMP_Text: tự dịch theo key, đăng ký với manager |
| `Base/Localization/LocalizationConfig.cs` | SO config hệ thống (MissingKeyBehavior...) |
| `Base/Localization/LocalizationDomains.cs` | Hằng số domain: `UI`, `Dialogues` |
| `Base/Localization/LocalizationCache.cs` | Cache string đã dịch |
| `Base/Localization/StringPool.cs` | Pool string giảm GC |
| `Base/Localization/TemplateEngine.cs` | Template `{var}` substitution, multi-line list |
| `Base/Localization/KeyHelper.cs` | Quy ước sinh key (`Item {Name} Desc`, `Action {X}`...) |
| `Base/Localization/TableRegistry.cs` | Stub tương thích code editor cũ (no-op) |
| `Base/Localization/TMProExtensions.cs` | Extension fluent API cho TMP |
| `Base/Localization/Data/LocalizationSettings.cs` | enum `LocalizationMode` (Static/Dynamic) |
| `Base/Localization/Data/PendingOperation.cs` | Operation chờ domain CSV sẵn sàng |
| `Base/Localization/Data/PerformanceStats.cs` | Thống kê perf hệ thống |
| `Base/Localization/Tests/PerformanceMonitoringTest.cs` | 🔧 Test component editor-only đo perf |

### 4.12 Audio (FMOD) 🔊

Mọi âm thanh đi qua FMOD `RuntimeManager` — không dùng AudioSource.

| File | Chức năng |
|---|---|
| `Base/Audio/AudioManager.cs` | 🟢 singleton: play/stop FMOD event, music crossfade, volume theo SettingsManager |
| `Base/Audio/FMODEvents.cs` | Component giữ `EventReference` (Music, Button SFX, sex SFX...) để kéo thả trong Inspector |
| `Base/Audio/ButtonSound.cs` | Gắn Button: tự phát SFX click/back khi bấm |

### 4.13 Editor tools 🔧 (`Assets/Editor/`)

Chỉ chạy trong Unity Editor — KHÔNG vào build.

**Dialogue & CG authoring:**

| File | Chức năng |
|---|---|
| `DialogueImporter.cs` | ⭐ Tool chính: import CSV (`Assets/Resources/Dialogues.csv`) → tạo/cập nhật `DialogueSequenceSO` + `CGDialogueSequenceSO`, log qua `FileLogger` |
| `DialogueSequenceSOEditor.cs` | Inspector riêng cho dialogue SO (xem/sửa node, validate) |
| `CGDialogueSequenceSOEditor.cs` | Inspector riêng cho CG dialogue SO |

**UI tooling:**

| File | Chức năng |
|---|---|
| `ButtonMigrationValidator.cs` | Quét scene kiểm tra Button nào thiếu/thừa `UIPanelButtonAction` |
| `ButtonOnClickMapper.cs` | Map/tạo OnClick UnityEvent tự động cho Button |
| `UIPanelAnalyzer.cs` | EditorWindow: liệt kê mọi UIPanel + quan hệ trong scene |
| `TMP_RestoreDefaultFont.cs` | Khôi phục font mặc định cho TMP bị mất font |

**Localization tooling** (namespace `Base.Localization.Editor`):

| File | Chức năng |
|---|---|
| `Localization/CSVExporter.cs` | Import/export CSV locale (Key,Text) theo domain |
| `Localization/LocalizationEditorWindow.cs` | Cửa sổ chính quản lý localization trong scene |
| `Localization/LocalizedTextInspector.cs` | Inspector riêng cho `LocalizedText` |
| `Localization/LocalizedTextPostProcessor.cs` | Tự gắn/quản lý `LocalizedText` khi TMP thay đổi |
| `Localization/LocalizedTextEditorPreview.cs` | Preview text đã dịch ngay trong Editor |
| `Localization/SceneTextScanner.cs` | Quét TMP chưa localize trong scene |
| `Localization/HotReloadTools.cs` | Reload CSV khi đang chạy (`Ctrl+Shift+L`) |
| `Localization/DebugTools.cs` | Debug localization (dump key thiếu...) |
| `Localization/RuntimeCsvLocalizationEditorUtility.cs` | Helper đọc CSV runtime dùng chung cho các tool |
| `Localization/ImportShopItemsLocalization.cs` | Import CSV shop item legacy (Key,EN,VI,JA) → UI locale CSV |
| `Localization/ImportStatChangeLocalization.cs` | Import template stat change → UI locale CSV |
| `Localization/ImportStatChangeLocalizationRunner.cs` | Runner tự động gọi import trên (`[InitializeOnLoad]`) |
| `Localization/VerifyStatChangeLocalization.cs` | Verify key stat change tồn tại đủ 3 ngôn ngữ |

**Debug:**

| File | Chức năng |
|---|---|
| `DebugToolsWindow.cs` | `Tools/Debug Tools`: cộng tiền, set stat, nhảy time... để test nhanh |

### 4.14 Tests (EditMode) ✅ (`Assets/Editor/Tests/`)

Contract tests bằng Unity Test Framework + NUnit — bảo vệ "hợp đồng" giữa animation clip Live2D và code gameplay (tên event, parameter, skill gating).

| File | Kiểm tra |
|---|---|
| `MissionaryContractTests.cs` | Missionary: 13 animator params, animation events đúng tên/đúng vị trí |
| `CowgirlContractTests.cs` | Cowgirl: signal + receiver contract |
| `RoleplayPussyContractTests.cs` | Roleplay Pussy: state/param/event contract |
| `RoleplayButtholeContractTests.cs` | Butthole: egg loop + event contract |
| `RoleplayBlowjobContractTests.cs` | Blowjob: blend tree + tongue loop contract |
| `RoleplayPaizuriContractTests.cs` | Paizuri: category lock contract |
| `SexSimulationSfxContractTests.cs` | SFX cue mapping đúng FMOD event theo position |
| `SexSceneUnlockServiceTests.cs` | Unlock service: điều kiện stat + PlayerPrefs persistence |
| `Live2DCoreDistributionTests.cs` | Cubism core files phân bố đúng (không thiếu/thừa) |
| `Live2DRenderingConfigurationTests.cs` | URP: `CameraFilteredCubismRenderPassFeature` gắn đúng renderer, camera exclusion đúng |

---

## 5. Flow tổng kết nhanh (đọc code từ đâu)

```
Game.unity (1 scene)
└─ GameManager ──► GameStartEvent ──► UIManager, Live2DPanel, DayTimeUI...
   ├─ InGame: click Mai → CharacterInteractManager → DialogueManager → DialoguePanel + Live2DMotionController (AnimationType)
   ├─ Sex: SexButtonVisibility (21-23h) → SexSimulationManager + SexPositionConfig* → Live2D animation events → SexSessionData → SimulationResultPanel
   ├─ Shop: ShopInteraction (click Staff) → ShopPanel → ItemManager → Inventory
   └─ Daily: UIPanelButtonAction → ActionConfirmPopup → Player.DoAction(record) → stats/resource đổi → StatsChangedEvent → UI refresh
```

> Mẹo: muốn biết 1 feature hoạt động, tìm **Manager singleton** của nó trước (`*.Instance`), rồi theo **EventBus** xem ai phát/nghe event liên quan.






 

