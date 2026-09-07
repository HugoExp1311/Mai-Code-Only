# Devlog — Save/Load UX: chặn load in-game, verify overwrite & delete slot (28/08/2026)

> Task: tiếp nối **P1.1 Save / Load** (devlog `save_load_25aug.md`, `setting_panel_save_load_27aug.md`) — chặn load save giữa chừng khi đang chơi (in-game), xác nhận logic overwrite-confirm và delete-slot còn nguyên.
> Trạng thái cuối (28/08): **compile sạch (0 lỗi/warning mới), verify edit-mode PASS** — bản In Game của Load Game Popup grey-out toàn bộ slot button (không click được), delete button vẫn hoạt động; bản Main Menu giữ nguyên load + delete. Overwrite confirm (Save popup) và delete confirm (Load popup) đã verify bằng code review + cấu trúc confirm popup đúng section.

---

## 1. Điều tra hiện trạng

Trước thay đổi, popup `Load Game Popup` tồn tại ở **2 section** (thiết kế từ 27/08):

| Panel | Path | Section | Controller |
|---|---|---|---|
| `Load Game Popup` | `Menu/Main Menu/Load Game` | MainMenu | `LoadGamePanelController` |
| `Load Game Popup` | `Menu/In Game/Load Game` | InGame | `LoadGamePanelController` |

Cả hai dùng chung controller, bấm slot đầy → confirm "Start from here?" → `GameManager.LoadGame()/LoadQuickSave()`. GDD không cho phép load giữa run (load chỉ ở Start Game menu), nên bản In Game phải bị chặn.

Các điểm đã verify trong lúc điều tra:

1. **Row slot không tự chuyển section khi load**: `UIPanelButtonAction` trên các row để `actionType = Custom` — chỉ phát âm thanh, click handler thật do `LoadGamePanelController` đăng ký (`targetSection = MainMenu` trên đó là field thừa của `SwitchSection` action, không dùng cho `Show/Custom` — **không sửa scene**, tránh nhiễu diff).
2. **Confirm/Notice popup có đủ theo section**: `Confirm Popup` có 3 bản (MainMenu / InGame / Simulation), `Notice Popup` ở `Menu/In Game/Notice`. `UIPanelManager.ShowPanel()` ưu tiên panel thuộc section hiện tại → flow confirm/delete của cả 2 popup Save/Load resolve đúng bản.
3. **Nút Save Game / Load Game trong Setting** (`UIPanelButtonAction` action `Show`) KHÔNG dùng `targetSection` — `ShowPanel()` tự ưu tiên section hiện tại, nên in-game mở đúng bản in-game (đây là lý do playtest 27/08 PASS dù inspector hiện `targetSection=MainMenu`).
4. **Overwrite confirm** (`SaveGamePanelController.HandleSlotClicked`): slot trống → save ngay + notice `Notice Game Saved`; slot đầy → `Confirm Popup` với key `Start Save Game Overwrite Question/Yes` (3 locale có đủ). Code nguyên vẹn từ 27/08, không đụng.
5. **Delete slot** (`LoadGamePanelController.HandleDeleteClicked`): confirm key `Start Load Game Delete Question/Yes` → `SaveLoadService.Delete()` xoá cả key save + key meta + `PlayerPrefs.Save()` → `Refresh()`. Logic đúng, không đụng.

## 2. Thay đổi

**Chỉ 1 file code:** `Assets/Scripts/UI/LoadGamePanelController.cs`

1. `Awake()` — derive `allowLoad` từ section của panel:
   ```csharp
   allowLoad = panel == null || panel.GameSection == GameSection.MainMenu;
   ```
2. `Refresh()` — set `row.SlotButton.interactable = allowLoad` trên mỗi row. Button dùng `Transition.ColorTint` nên tự động grey-out với `disabledColor` (RGBA 0.784, 0.784, 0.784, 0.502), không cần đổi màu tay.
3. `HandleSlotClicked()` / `BeginLoad()` — guard sớm `if (!allowLoad) return;` phòng thủ kép (belt-and-braces) dù button đã không interactable.

Không đụng: `SaveGamePanelController.cs`, `GameManager`, `SaveLoadService`, scene, CSV (không cần key mới; delete flow giữ nguyên message cũ).
