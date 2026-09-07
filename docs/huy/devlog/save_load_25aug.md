# Devlog — P1.1 Save / Load (25/08/2026)

> Task: `docs/huy/TaskTodo.md` — **P1.1 Save / Load** (GDD §3: Quick Save + Save (Number), nút Load Game ở Start Game menu).
> Trạng thái cuối (26/08): **hoàn tất cả 4 TODO cuối** — auto QuickSave sau Opening CG (TODO #2), UI Save Game in-game (TODO #3), dọn `Base/Save/` (TODO #4). Compile sạch, verify in-editor pass.

---

## 1. Tổng quan thay đổi

Trước thay đổi, `GameManager.LoadGame()` chỉ là `TODO` + `Debug.LogWarning`. Không có schema save, không có slot, mọi progression (Love/Lewd/Work/Skills/Inventory) mất khi thoát game.

Sau thay đổi:

| Khối | Vai trò |
|---|---|
| `Base/Persistence/SaveData.cs` (mới) | DTO schema — toàn bộ trạng thái game cần lưu |
| `Base/Persistence/SaveLoadService.cs` (mới) | Slot I/O qua `PlayerPrefs` (JSON) + slot metadata |
| `GameManager.cs` | `SaveGame(slot)` / `QuickSave()` / `LoadGame(slot)` / `LoadQuickSave()` / `CaptureSaveData()` / restore |
| `Player.cs` | `PlayerState` snapshot/restore (stats + skills + inventory) — idempotent |
| `Mai.cs` | `MaiState` snapshot/restore (Love/Libido/Lewd/Sensitive 4 bộ phận/Pregnancy) |
| `Inventory.cs` | `IInventory.ReplaceItems()` — replace toàn bộ (chống duplicate khi load) |
| `SkillManager.cs` / `ISkill.cs` | `SetSkillPointDirect(value)` (set tuyệt đối) + expose `Downgrade()` |

Các file `Assets/Scripts/Base/Save/*` là **skeleton mồ côi từ lần thử trước, không được reference** — đã xoá ngày 26/08 cùng `.meta` (TODO #4, hậu kiểm: 0 missing script trong scene/prefab, không còn type `Base.Save` nào).

---

## 2. Chi tiết từng file

### 2.1 `Base/Persistence/SaveData.cs` (mới)
DTO `[Serializable]` gồm:
- **Thời gian**: `time` (ISO-8601 "O"), `area`, `cycle`, `playerName`.
- **Player**: `stamina`, `maxStamina`, `charming`, `knowledge`, `money`, `skillPoint`, `skills` (list `SkillSaveData {skillType, level, unlocked}`), `inventory` (list `InventoryItemSaveData {id, amount, price, type, value}`).
- **Mai**: `love`, `libido`, `lewdLevel`, `pregnancyChance`, `sensitiveParts` (list `SensitivePartSaveData {bodyPart, points, level}` × 4 bộ phận).
- **Work**: `workLevel`, `workProgress`.
- **Daily state** (once-per-day gating): `lastEatingDate`, `lastSexDate`, `lastWorkingDate`, `lastExerciseDate`, `lastSleepDate`, `napCountToday`, `deepSleepCountToday`.
- **Unlock flags**: `unlockedSexScenes` (list tên `SexSceneType`).
- `schemaVersion` (meta) để migrate về sau.

### 2.2 `Base/Persistence/SaveLoadService.cs` (mới)
- Slot key: `KeyPrefix + slotId`, meta key riêng (`MetaKey + slotId`).
- `SlotCount = 3` slot thủ công; `QuickSaveSlotId = "Quick"` (Quick Save riêng, không đè slot thường — GDD §3).
- API: `HasSave(slotId)`, `HasAnySave()`, `Write(slotId, data, label)`, `Read(slotId)`, `Delete(slotId)`, `GetSlotKey/GetMetaKey`.
- `SaveSlotInfo { slotId, label, realTimeStamp, gameDay, schemaVersion }` cho UI list save.
- Serialize bằng `JsonUtility` → `PlayerPrefs`.

### 2.3 `GameManager.cs`
- **Save**: `SaveGame(int slot = 1)` và `QuickSave()` → `SaveLoadService.Write(slot, CaptureSaveData(), $"Day {DaysPlayed} - {Time:HH:mm}")`.
- **Load**: `LoadGame(int slot)`, `LoadQuickSave()`, `LoadGame(SaveData)` (typed); `LoadGame(object)` cũ giữ lại làm **back-compat wrapper** → gọi `LoadQuickSave()` (scene/UI cũ vẫn reference được).
- `CaptureSaveData()`: gom `player.CaptureState()` + `mai.CaptureState()` + Work Level/Progress + daily-state + `SexSceneUnlockService` unlock flags → DTO.
- Restore: set lại Player/Mai/Time/Area/Cycle/Work với clamp an toàn (`Mathf.Clamp` workLevel 1..MaxWorkLevel, parse date fail → fallback), raise `PlaceChangedEvent` + `TimeChangedEvent` để HUD refresh, rồi `SwitchToSection(GameSection.InGame)`.
- Restore unlock flags: list rỗng = save cũ → giữ flag hiện tại (không phá).

### 2.4 `Player.cs`
- `PlayerState` class: stats + `skills` (List`<SkillStateEntry>`) + `inventory` (List`<InventoryStateEntry>`).
- `CaptureState()` / `RestoreState(PlayerState)`:
  - Restore skill: set unlock flag, rồi **downgrade trước / upgrade sau** để đạt đúng level đã lưu (load save cũ hơn không bị kẹt level cao).
  - `SetSkillPointDirect(state.skillPoint)` gọi **sau cùng** — upgrade/downgrade không làm lệch point.
  - Inventory: build list `CommonItem` mới → `_inventory.ReplaceItems()` → load lại nhiều lần không nhân đôi đồ.

### 2.5 `Mai.cs`
- `MaiState`: `love`, `libido`, `lewdLevel`, `pregnancyChance`, + points/level của 4 bộ phận (Boobs/Mouth/Pussy/Butthole).
- `CaptureState()` / `RestoreState(MaiState)` set trực tiếp các `CommonStat` private field.

### 2.6 `Inventory.cs`
- Thêm `ReplaceItems(IEnumerable<IItem>)` vào `IInventory` + impl: clear list, add items amount > 0, `SaveInventory()`.

### 2.7 `SkillManager.cs` + `ISkill.cs`
- `SkillManager.SetSkillPointDirect(int value)` — set tuyệt đối (khác `IncreaseSkillPointDirect` là delta), thêm vào `ISkillManager` interface.

---

## 3. Cách verify (đã làm / cần làm tiếp)

Đã verify:
- Compile sạch trong Unity (không lỗi console).
- Round-trip test ở slot an toàn (slot 99, không đè Quick Save/slot thật) qua Unity `execute_code`.

Cần làm tiếp (TODO kế tiếp):
1. ~~**UI Load Game panel**~~ ✅ **Xong 26/08/2026** — xem §4b.
2. ~~**Auto Quick Save** ở mốc cốt truyện (sau Opening CG, sau mỗi Ending)~~ ✅ **Xong 26/08/2026** — xem §4c.
3. ~~Gọi `SaveGame` thủ công từ in-game menu (Setting panel hoặc nút riêng)~~ ✅ **Xong 26/08/2026** — xem §4d.
4. ~~Dọn folder `Base/Save/` (skeleton mồ côi, không reference)~~ ✅ **Xong 26/08/2026** — xem §4e.
5. Satisfaction counter / Event-Ending unlock flags chưa có trong schema (thuộc P1.2 — thêm field sau, có `schemaVersion` hỗ trợ migrate).

## 4. Ghi chú thiết kế
- Chọn `PlayerPrefs + JsonUtility` để đồng bộ pattern với `Inventory`/`SettingsManager` sẵn có; đổi sang file JSON trên disk sau khi có yêu cầu build standalone nhiều save.
- `SexSceneUnlockService` vẫn dùng PlayerPrefs làm runtime store; save slot chỉ snapshot/restore các flag này để **mỗi save slot tự chứa unlock của nó** (không leak giữa các save).
- `LoadGame(object)` giữ nguyên signature để scene reference cũ không break.

## 4b. Wire UI Load Game panel (26/08/2026)

TODO #1 ở §3 đã xong. Thay đổi:

| Khối | Vai trò |
|---|---|
| `Assets/Scripts/UI/LoadGamePanelController.cs` (mới) | Controller trên GO `Menu/Main Menu/Load Game` (cùng GO với `UIPanel` panelId `Load Game Popup`). Bind 6 row trong `Saves`, hiển thị slot meta, confirm load, nút Delete. |
| `SaveLoadService.SlotCount` 3 → 5 | UI có 6 row (1 Quick + 5 manual), service trước đây chỉ 3 → bump lên 5 khớp UI. |
| Scene `Game.unity` | Add `LoadGamePanelController` trên panel root; 6 row `UIPanelButtonAction.actionType` `Show` (rỗng, log lỗi khi click) → `Custom` (external code — convention như MaiPanel). |
| Localization (3 CSV + master) | Keys mới: `Start Load Game Slot Info` (Day {day} - {time}), `Start Load Game Delete Question/Yes`, `Notice Load Failed`, `Notice Save Slot Empty`. |

Hành vi:
- Row 0 → slot `Quick` (label key `Start Load Game Quick Save`), row n → slot `n-1` (label `Save {0}` với `{0}` = n-1). Slot rỗng giữ label mặc định, click → Notice "This save slot is empty!".
- Slot có data → label "Day X - HH:mm" (regex parse từ meta.label do `GameManager.SaveGame` ghi; fallback `meta.gameDay`/`realTimeStamp`), nút "×" Delete góc phải hiện. Click row → ConfirmPopup `Start Confirm Question/Yes/No` (đã có sẵn) → Yes = `LoadQuickSave()`/`LoadGame(n)` + hide panel; click × → ConfirmPopup delete → `SaveLoadService.Delete` + Refresh.
- Delete button tạo runtime (không serialize vào scene), font copy từ label TMP của row.
- Đã verify edit-mode: seed Quick + slot 4 → label Day/time đúng, Delete button visibility đúng theo hasSave, save/load scene sạch, console 0 lỗi.

## 4c. Auto Quick Save ở mốc cốt truyện (26/08/2026)

TODO #2 ở §3 đã xong. GDD §3: *"Auto Save Mỗi khi người chơi đã chơi đến 1 phân cảnh cụ thể đã unlock trong cốt truyện."*

- `GameManager.AutoQuickSave()` (mới): wrapper của `QuickSave()` + log mốc story (Day/time).
- 4 điểm gọi — mọi đường gameplay chính thức bắt đầu (intro kết thúc = mốc cốt truyện đầu tiên):
  | Vị trí | Trường hợp |
  |---|---|
  | Cuối `FinalizeIntroToCompany()` | Opening CG flow thường kết thúc → Company 7:30 |
  | Debug skip-intro path | test/dev (DebugHotkeys) |
  | Fallback `PlayIntroCG()` | load fail sequence `Intro` — game vẫn phải bắt đầu |
  | Fallback `PlayIntroPart4()` | load fail sequence Part4 Live2D — game vẫn phải bắt đầu |
- Ending (P1.2) chưa có flow — khi impl sẽ thêm `AutoQuickSave()` vào callback kết thúc Ending (đã note trong doc-comment hàm).

## 4d. UI Save Game thủ công in-game (26/08/2026)

TODO #3 ở §3 đã xong. Thay đổi:

| Khối | Vai trò |
|---|---|
| `Assets/Scripts/UI/SaveGamePanelController.cs` (mới) | Controller gắn lên popup `Save Game` trong scene `Game.unity` (layout clone từ Load Game popup của Main Menu). |
| Scene `Game.unity` | Gắn controller lên popup root; nút Save của Setting panel mở popup qua `UIPanelButtonAction`. |
| Localization (3 CSV + master) | Keys mới: `Start Save Game Overwrite Question/Yes`, `Notice Game Saved`, `Notice Save Failed`. |

Hành vi:
- Row 0 (Quick) **ẩn** — quick save chỉ là mốc story tự động; còn 5 slot số theo `SlotCount` (label `Save {0}` như Load Game popup).
- Slot trống → save ngay; slot có data → ConfirmPopup "Overwrite this save?" → `GameManager.SaveGame(slot)` (clamp 1..SlotCount).
- Sau save → `NoticeUI` "Game Saved"/"Save Failed" + refresh label slot `Day {n} - {HH:mm}` (regex parse từ meta.label như §4b, fallback `meta.gameDay`/`realTimeStamp`).
- Đóng popup → `UIPanel.RestoreUnderlyingPanels` tự hiện lại Setting panel (cơ chế stack sẵn có của `UIPanel.disableUnderlyingPanels`, không cần thêm code).
- Verify edit-mode: row Quick ẩn, seed data slot 2 → label `Day 2 - 21:05`, compile + console sạch.

## 4e. Dọn `Assets/Scripts/Base/Save/` (26/08/2026)

TODO #4 ở §3 đã xong:
- Trước khi xoá: grep GUID từng `.meta` trong `Base/Save/` — không xuất hiện ở scene/prefab/YAML nào.
- Xoá thư mục + `Assets/Scripts/Base/Save.meta`.
- Hậu kiểm trong editor: 0 missing-script component trên live scene lẫn 15 prefab, không còn type namespace `Base.Save` trong assembly nào, console sạch error/warning sau domain reload.

## 5. Hướng dẫn kiểm thử step by step ở Unity Editor

> Mục tiêu: verify P1.1 save/load đúng semantics — round-trip, idempotent khi load nhiều lần, isolation giữa các slot, back-compat `LoadGame(object)`, và restore UI/HUD.
> Nên chạy theo thứ tự; mỗi bước ghi kết quả Pass/Fail. Test xong **xoá slot 99** để không để lại rác.

### Bước 0 — Chuẩn bị
1. Mở project, vào scene `Assets/Scenes/Game.unity`.
2. Console (Window → General → Console): chọn *Clear on Recompile* + *Error Pause* để bắt lỗi sớm.
3. Đảm bảo compile sạch: **Assets → Refresh** (Ctrl+R), console không có error đỏ.
4. Bật **Debug hotkeys** (chỉ tồn tại trong Editor/Dev build): `Base/Debugging/DebugHotkeys.cs` tự bootstrap qua `RuntimeInitializeOnLoadMethod`, không cần wire scene:
   - **F10** — đang xem intro CG → skip thẳng vào InGame; đang trong dialogue (kể cả scene R18) → end dialogue ngay.
   - **F11** — unlock toàn bộ sex scene (test persistence flag mà không phải xem nội dung).
   - **F12** — round-trip test 1 phím: capture → write slot 99 → load → so JSON trước/sau → log PASS/FAIL (kèm diff JSON khi FAIL) → tự xoá slot 99.
   - `GameManager.CaptureSaveData()` đã đổi từ `private` → `public` để F12 gọi được.

### Bước 1 — Round-trip ở slot an toàn (slot 99)
Mở MCP Unity → *Execute Code* (hoặc kèm code tạm vào một script debug), chạy:

```csharp
var gm = GameManager.Instance;
var before = gm.CaptureSaveData();                 // snapshot hiện tại
SaveLoadService.Write("99", before, "test");       // ghi slot 99 (an toàn)

// Thay đổi trạng thái tuỳ ý (vd qua gameplay hoặc trực tiếp):
//   làm việc → +Progress, mua đồ, +Love...

gm.LoadGame(SaveLoadService.Read("99"));           // load lại
var after = gm.CaptureSaveData();
Debug.Log($"stamina {before.stamina}->{after.stamina} | money {before.money}->{after.money} | love {before.love}->{after.love}");
SaveLoadService.Delete("99");                      // dọn slot test
```

**Pass:** các chỉ số `after` khớp `before` (lưu ý: nếu có thay đổi giữa 2 lần capture, giá trị phải quay về `before`).

⚠️ **Đừng dùng `gm.SaveGame(99)` / `gm.LoadGame(99)`** — `GameManager` clamp slot về 1..3 nên "99" sẽ ghi đè **slot 3 thật**. Slot test phải đi thẳng `SaveLoadService` với id `"99"`.

**Nhanh hơn:** bấm **F9** 2 nhịp (nhịp 1: save + baseline → thay đổi trạng thái → nhịp 2: load + so sánh stats) và **F8** để xoá slot 99.

### Bước 2 — Idempotent: load 2 lần không nhân đôi đồ
1. Mua vài item ở Hiep Mart (vd 3 Milk).
2. `SaveGame(99)`.
3. `LoadGame(99)` → mở Inventory đếm Milk = 3.
4. `LoadGame(99)` lần nữa → mở Inventory đếm Milk lại.

**Pass:** vẫn đúng 3 Milk sau lần load thứ 2 (không thành 6/9). Tương tự verify SkillPoint và skill level không bị cộng dồn.

### Bước 3 — Isolation giữa các slot
1. Tạo save ở **slot 1** ở trạng thái A (vd ngày 2, 500 Money).
2. Tiếp tục chơi, mua đồ → save **slot 2** ở trạng thái B (vd ngày 2, 200 Money, có item).
3. `LoadGame(1)` → kiểm tra quay đúng trạng thái A (500 Money, **không** có item đã mua sau đó).
4. `LoadGame(2)` → kiểm tra đúng trạng thái B.

**Pass:** mỗi slot khôi phục đúng snapshot của chính nó; load slot 1 không xoá/mang item của slot 2.

### Bước 4 — Quick Save riêng biệt
1. `QuickSave()` → tiếp tục chơi thay đổi trạng thái.
2. `LoadQuickSave()` (hoặc gọi `LoadGame((object)null)` — back-compat wrapper cũng phải trỏ Quick Save).

**Pass:** trạng thái quay về thời điểm Quick Save; slot 1-3 (số) không bị đè bởi Quick Save (kiểm tra `SaveLoadService.HasSave("1")` meta label không đổi).

### Bước 5 — Restore unlock flags Sex Scene
1. Unlock một sex scene (vd qua `SexSceneUnlockService`).
2. `SaveGame(99)`.
3. Xoá PlayerPrefs flag đó (`PlayerPrefs.DeleteKey(...)` hoặc clear all) → xác nhận scene bị khoá lại.
4. `LoadGame(99)`.

**Pass:** scene vừa unlock ở bước 1 mở khoá trở lại (flag nằm trong save, không phụ thuộc PlayerPrefs runtime).
**Lưu ý:** save cũ (list `unlockedSexScenes` rỗng) → giữ flag hiện tại, không phá unlock đang có.

### Bước 6 — HUD refresh sau load
1. Chơi tới buổi tối (cycle Evening), chuyển Place qua Company.
2. `SaveGame(99)`.
3. Tiếp tục chơi (đổi place, đổi giờ) rồi `LoadGame(99)`.

**Pass:** HUD (Energy/Money/Time/Day), background Place, music theo Time khớp thời điểm save — không cần bấm nút nào để "kick" UI (verify `PlaceChangedEvent`/`TimeChangedEvent` được raise).

### Bước 7 — An toàn dữ liệu xấu / legacy
1. `PlayerPrefs.SetString(SaveLoadService.GetSlotKey("1"), "not-json")` → `LoadGame(1)`.
2. Sửa tay JSON slot: `workLevel = 99`, `time = "garbage"` → `LoadGame(1)`.

**Pass:** không exception đỏ; `workLevel` bị clamp về `MaxWorkLevel` (5), `time` fallback về mặc định (6:00 A.M ngày đầu).

### Bước 8 — Dọn dẹp sau test
```csharp
SaveLoadService.Delete("99");
PlayerPrefs.Save();
```

**Pass:** `SaveLoadService.HasSave("99") == false`.

### Checklist nhanh
- [ ] Round-trip slot 99 khớp stat
- [ ] Load 2 lần không duplicate inventory/skill
- [ ] Slot 1 vs 2 độc lập
- [ ] Quick Save không đè slot số
- [ ] `LoadGame((object)null)` back-compat trỏ Quick Save
- [ ] Unlock sex scene sống lại từ save
- [ ] HUD/Place/Music refresh tự động
- [ ] JSON hỏng / giá trị sai không crash (clamp + fallback)

