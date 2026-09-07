# Devlog — Gift giving & generic item usage wiring (02/09/2026)

> Task: triển khai wiring cho `OnGiveClicked` và `OnUseClicked` trong `InventoryItemUI.cs` — hai stub `Debug.LogWarning` được thay bằng call thật qua `DataManager.OnUseItem`.
> Trạng thái cuối (02/09): **compile sạch, 0 error / 0 warning** trong Unity console. Script validation pass. Không sửa logic core (`DataManager`, `Player`, `Mai`) — chỉ nối UI layer vào API đã sẵn có.

---

## 1. Vấn đề

`InventoryItemUI.cs` có hai handler `OnGiveClicked` (nút "Give" — tặng quà cho Mai) và `OnUseClicked` (nút "Use" — dùng item) đều là stub:

```csharp
// OnGiveClicked — cũ
Debug.LogWarning($"[InventoryItemUI] Gift giving not yet implemented for: {item.Identifier()}");

// OnUseClicked — cũ (nhánh non-condom)
Debug.LogWarning($"[InventoryItemUI] Item usage not yet implemented for: {item.Identifier()}");
```

`DataManager.OnUseItem(IItem, Action<UIAction>)` đã tồn tại và làm đúng việc — route `UseItem` action đến Player, rồi nếu `item.IsForGift()` (ItemType == Love) thì route thêm `ReceiveItem` đến boss hiện tại (Mai). Nhưng UI chưa gọi tới — bấm nút Give/Use chỉ in warning, không có hiệu ứng thật.

Nhánh condom trong `OnUseClicked` đã được implement từ trước (route qua `SexSimulationManager.TryActivateCowgirlCondom` + `Player.DoAction(UseItem)`) — không đụng.

---

## 2. Thay đổi

### 2.1 `Assets/Scripts/Inventory/InventoryItemUI.cs`

**`OnGiveClicked`** — chuyển từ `void` sang `async void`, thay stub bằng:

```csharp
private async void OnGiveClicked()
{
    if (item == null || item.Amount() <= 0) return;

    if (GameManager.Instance == null || GameManager.Instance.DataManager == null) return;

    await GameManager.Instance.DataManager.OnUseItem(item, null);
    UpdateAmount();

    // Play gift sound
    if (AudioManager.Instance != null && FMODEvents.Instance != null)
    {
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.OnNotice);
    }
}
```

**`OnUseClicked`** (nhánh non-condom) — thay stub bằng:

```csharp
if (GameManager.Instance == null || GameManager.Instance.DataManager == null) return;

await GameManager.Instance.DataManager.OnUseItem(item, null);
UpdateAmount();

PlayUseSound();
```


### 2.2 Không sửa file khác

| File | Lý do không sửa |
|---|---|
| `DataManager.cs` | `OnUseItem` đã đúng: Player.UseItem → nếu `IsForGift()` thì Boss.ReceiveItem. Không cần đổi. |
| `CommonItem.cs` / `IItem.cs` | `IsForGift()` trả `ItemType == Love` — đúng cho 7 gift item (Lipstick, Flower, Teddy Bear, Apron, Bikini, Gym Outfit, Sexy Sleepwear). |
| `CharacterActions.cs` | `UseItem` / `ReceiveItem` record đã có. |
| `Mai.cs` | `ReceiveGift(IItem)` handler đã có (line 92, dispatch ở line 270). |

---

## 3. Flow dữ liệu

```
UI: bấm "Give" hoặc "Use"
 └─ InventoryItemUI.OnGiveClicked / OnUseClicked
     └─ DataManager.OnUseItem(item, null)        // async Task
         ├─ Player.DoAction(UseItem)              // áp dụng effect lên Player (energy, stamina, v.v.)
         └─ if (item.IsForGift())                 // ItemType == Love
             └─ Boss.DoAction(ReceiveItem)        // Mai.ReceiveGift → +Love, +Lewd, v.v.
     └─ UpdateAmount()                            // refresh số lượng trên UI
     └─ PlayUseSound() / gift sound               // feedback âm thanh
```

**Lưu ý:** `OnUseItem` truyền `null` cho `Action<UIAction> onUpdateUI` — không có callback UI phụ. `UpdateAmount()` được gọi trực tiếp sau await, đọc lại `item.Amount()` đã được `Player.DoAction` giảm.

---

## 4. Edge cases & guard clauses

| Trường hợp | Xử lý |
|---|---|
| `item == null` hoặc `amount <= 0` | Return sớm — không call API. |
| `GameManager.Instance == null` | Return — tránh NRE khi gọi ngoài gameplay (debug, boot). |
| `DataManager == null` | Return — DataManager là `ScriptableObject.CreateInstance` trong `StartGame()` / debug path; nếu chưa init thì không nên dùng. |
| Condom item trong `OnUseClicked` | Nhánh riêng: check `SexSimulationManager.CanActivateCowgirlCondom()` → `Player.DoAction(UseItem)` → `TryActivateCowgirlCondom()` → `UpdateAmount` + `PlayUseSound`. **Không** qua `DataManager.OnUseItem`. |
| `async void` fire-and-forget | `OnGiveClicked` / `OnUseClicked` là UI event handler (UnityEvent bind) — `async void` là pattern bắt buộc. Exception trong await sẽ vào Unity unhandled handler; không có try-catch vì `OnUseItem` nội bộ đã không throw. |

---

## 5. Verify

| Kiểm tra | Kết quả |
|---|---|
| Unity script validation (`validate_script` standard) | ✅ 0 errors, 0 warnings |
| Unity console (`read_console` errors+warnings) | ✅ 0 entries |
| Compilation state (`execute_code`) | ✅ `ready` (not compiling) |
| GitNexus `impact OnUseItem --direction upstream` | ⚠️ **UNKNOWN** — 0 callers resolved by graph (xem §6) |
| GitNexus `detect_changes --scope all` | Risk: **critical** — nhưng bao gồm toàn bộ dirty tree (17 files, 33 symbols), không chỉ task này (xem §6) |

### 5.1 Tại sao impact analysis trả UNKNOWN?

GitNexus graph không resolve được caller của `DataManager.OnUseItem` vì:
- Call site là `await GameManager.Instance.DataManager.OnUseItem(item, null)` — truy cập qua property chain (`GameManager.Instance.DataManager`), index không tạo edge cho pattern này.
- Text search xác nhận **2 caller thật** trong `InventoryItemUI.cs` (line 123 cho Give, line 167 cho Use) — đây là toàn bộ caller mới được thêm.

### 5.2 Tại sao detect_changes báo critical?

`detect_changes --scope all` quét toàn bộ working tree đang dirty, bao gồm:
- `AGENTS.md` (GitNexus section update)
- `DebugHotkeys.cs` (auto-save debug)
- `SaveLoadService.cs` (auto-slot)
- Nhiều file khác không liên quan đến task này

Risk **critical** đến từ tổng số file thay đổi trong tree, không phải từ riêng wiring `InventoryItemUI`. Thay đổi của task này (1 file, 2 method) isolated trong UI layer.

---

## 6. Cảnh báo có sẵn (KHÔNG sinh ra từ task này)

- ~~`CharacterInteractManager.cs:67` vẫn có TODO: *"Implement gift UI, item selection, then call `_currentTargetBoss.DoAction(new CharacterActions.ReceiveItem(...))`"*~~ → **Đã fix một phần (02/09, see §9)**: null-retry pattern cho `_currentTargetBoss` đã được thêm vào `InitiateGift`/`InitiateSex`/`InitiateFood`. Toàn bộ gift UI / item selection qua path này vẫn là TODO.
- `FMODEvents.Instance.OnNotice` dùng cho cả gift sound lẫn use sound — chưa có sound riêng cho gift. Có thể refine sau.

---

## 7. Localization

Các key đã có sẵn trong `Assets/Resources/Localization/UI/{en-US,ja-JP,vi-VN}.csv`:

| Key | en-US |
|---|---|
| `Inventory Give` | Give |
| `Inventory Use` | Use |
| `Inventory Item Amount` | x{amount} |
| `In Game Inventory` | Inventory |

Không thêm key mới — nút Give/Use đã bind từ trước, chỉ logic handler là thiếu.

---

## 8. Checklist

- [x] `OnGiveClicked` — stub → `await DataManager.OnUseItem(item, null)` + `UpdateAmount()`
- [x] `OnUseClicked` (non-condom) — stub → `await DataManager.OnUseItem(item, null)` + `UpdateAmount()` + `PlayUseSound()`
- [x] Null-guard `GameManager.Instance` + `DataManager` trên cả hai handler
- [x] Nhánh condom trong `OnUseClicked` không bị regress
- [x] Script validation: 0 errors, 0 warnings
- [x] Unity console: 0 errors, 0 warnings
- [x] Devlog tạo

---

## 9. Follow-up fix — CharacterInteractManager null-retry (02/09)

### Vấn đề

`CharacterInteractManager.Start()` cố gán `_currentTargetBoss = DataManager.GetCurrentBoss()`, nhưng nếu `DataManager` chưa sẵn sàng lúc `Start()` (timing issue — `DataManager` là `ScriptableObject.CreateInstance` trong `StartGame()`), `_currentTargetBoss` stays null. Khi user bấm Gift button (`MaiPanel.OnGiftButtonClick` → `CIM.InitiateGift`), method chỉ check `if (_currentTargetBoss == null)` rồi log error `"CIM: No target boss for gift."` mà không thử re-fetch → **fix không chạy, gift path hỏng**.

`InitiateTalk()` đã có retry pattern (line 31-34): re-fetch `_currentTargetBoss` tại call-time nếu null. Nhưng `InitiateGift()`, `InitiateSex()`, `InitiateFood()` thiếu pattern này → cùng bug.

### Thay đổi

**File:** `Assets/Scripts/Base/Character/CharacterInteractManager.cs`

Thêm null-retry block vào đầu `InitiateGift()`, `InitiateSex()`, `InitiateFood()` — copy đúng pattern từ `InitiateTalk()`:

```csharp
// Try to get the target boss if not already assigned (fallback for timing issues)
if (_currentTargetBoss == null && GameManager.Instance?.DataManager != null)
{
    _currentTargetBoss = GameManager.Instance.DataManager.GetCurrentBoss();
}
```

### GitNexus verification

| Kiểm tra | Kết quả |
|---|---|
| `impact InitiateGift --direction upstream` | ✅ **LOW** risk, 1 direct caller (`MaiPanel.OnGiftButtonClick`), 0 processes affected. `epistemic: exact`. |
| `detect_changes --scope all` | ⚠️ **critical** — nhưng là tổng dirty tree (15 files, 38 symbols, 43 processes). Processes affected đều từ file khác (DebugHotkeys, SaveLoadService, GameManager, v.v.) — không phải từ CIM changes. |

### Không sửa

| File | Lý do |
|---|---|
| `MaiPanel.cs` | `OnGiftButtonClick` đã gọi `CIM.InitiateGift()` đúng — không cần đổi. |
| `DataManager.cs` | `GetCurrentBoss()` đã trả `ITarget` đúng — không cần đổi. |
| `InventoryItemUI.cs` | Inventory gift path đã được fix ở §2 — không đụng. |

### Còn TODO

`InitiateGift()` vẫn chỉ `Debug.Log` sau khi `_currentTargetBoss` đã được resolve — chưa có gift UI / item selection / `_currentTargetBoss.DoAction(ReceiveItem)`. Null-retry fix chỉ đảm bảo **không còn error "No target boss for gift"**; bản thân gift interaction qua MaiPanel path vẫn là placeholder.

Inventory path (`InventoryItemUI.OnGiveClicked` → `DataManager.OnUseItem`) đã hoàn chỉnh từ §2 và là path chính để tặng quà.
