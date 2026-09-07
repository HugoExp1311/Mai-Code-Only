# Devlog — Setting panel localization + Save/Load popup in-game (27/08/2026)

> Task: tiếp nối **P1.1 Save / Load** (devlog `save_load_25aug.md`) — hoàn tất lớp **localization** cho panel Setting in-game và 2 popup Save/Load Game trong section In Game, sửa sorting order, bind `LocalizedText`, thêm key CSV còn thiếu cho đủ 3 locale.
> Trạng thái cuối (27/08): **runtime verification PASS** — playtest EN trong In Game: Setting tab hoá tiếng, Load Game hiện slot "Day X - HH:mm", Save Game confirm overwrite + notice "Game saved!". Screenshot bằng chứng: `Assets/Screenshots/playtest_setting_load_en.png`.

---

## 1. Vấn đề

Sau khi xong phần "xương" Save/Load (25/08), playtest trong section **In Game** lộ ra:

1. Panel `Menu/In Game/Setting` (bản tabbed) có nhiều text **hard-code tiếng Anh**, không có key localization: tiêu đề, 3 tab **Audio / Display / System**, 2 nút **Save Game / Load Game**.
2. Popup **Load Game** chỉ tồn tại ở Main Menu (`Menu/Main Menu/Load Game`) — mở từ In Game thì không có panel đúng section.
3. Chưa có popup **Save Game** cho In Game; các chuỗi UI mới (`Day {day} - {time}`, câu hỏi xoá/overwrite, notice save/load...) chưa có trong CSV.
4. Sorting order các popup trong cụm `Menu` chưa nhất quán — popup mở sau có thể vẽ đè sai lớp.

Lưu ý bẫy: scene có **2 GameObject cùng tên `Setting`** — `Menu/Main Menu/Setting` (legacy, slider dọc, không tab) và `Menu/In Game/Setting` (tabbed). `GameObject.Find("Setting")` trả về bản legacy → mọi thao tác phải tìm theo **path đầy đủ**.

---

## 2. Tổng quan thay đổi

| Khối | Thay đổi |
|---|---|
| `Assets/Resources/Localization/UI/{en-US,ja-JP,vi-VN}.csv` | Thêm 14 key mới mỗi locale (bảng §3) |
| `Assets/Resources/UI Table_Localization.csv` | Thêm dòng tương ứng vào bảng master (cột Key,EN,VI,JA) |
| `Menu/In Game/Setting` (scene) | Bind 17 `LocalizedText` cho toàn bộ text trong panel (bảng §4) |
| `Menu/In Game/Load Game` (scene) | **Clone** của `Menu/Main Menu/Load Game`, section `In Game`, giữ nguyên `panelId="Load Game Popup"` — resolve theo section (bảng §5) |
| `Menu/In Game/Save Game` (scene) | Popup mới, `panelId="Save Game Popup"`, section `In Game`, clone layout "Saves" từ popup Load |
| Canvas `sortingOrder` | Audit + chỉnh lại toàn cụm popup (bảng §5) |
| `Assets/Scripts/UI/LoadGamePanelController.cs` | Dùng bộ key mới: `Start Load Game Slot Info`, delete question/yes, `Notice Load Failed`, `Notice Save Slot Empty` |
| `Assets/Scripts/UI/SaveGamePanelController.cs` | Dùng bộ key mới: `Start Save Game Title`, overwrite question/yes, `Notice Game Saved`, `Notice Save Failed` |

Không sửa logic save/load của `GameManager` / `SaveLoadService` — phần đó đã chốt ngày 25/08.

---

## 3. Key CSV thêm mới (14 key × 3 locale)

Diff thật lấy từ git (`Assets/Resources/Localization/UI/*.csv`), cùng bộ dòng được thêm vào `UI Table_Localization.csv`:

### 3.1 Nhóm Setting panel

| Key | en-US | ja-JP | vi-VN |
|---|---|---|---|
| `Setting Load Game` | Load Game | ロードゲーム | Tải Game |
| `Setting Save Game` | Save Game | セーブゲーム | Lưu Game |
| `Setting Tab Audio` | Audio | サウンド | Âm thanh |
| `Setting Tab Display` | Display | ディスプレイ | Hiển thị |
| `Setting Tab System` | System | システム | Hệ thống |

### 3.2 Nhóm Save/Load popup

| Key | en-US | ja-JP | vi-VN |
|---|---|---|---|
| `Start Load Game Slot Info` | Day {day} - {time} | {day}日 - {time} | Ngày {day} - {time} |
| `Start Load Game Delete Question` | Delete this save? | このセーブを削除しますか？ | Xóa lưu game này? |
| `Start Load Game Delete Yes` | Delete | 削除 | Xóa |
| `Start Save Game Title` | Save Game | セーブゲーム | Lưu Game |
| `Start Save Game Overwrite Question` | Overwrite this save? | このセーブを上書きしますか？ | Ghi đè lưu game này? |
| `Start Save Game Overwrite Yes` | Overwrite | 上書き | Ghi đè |

### 3.3 Nhóm Notice

| Key | en-US | ja-JP | vi-VN |
|---|---|---|---|
| `Notice Game Saved` | Game saved! | ゲームを保存しました！ | Đã lưu game! |
| `Notice Save Failed` | Failed to save the game! | セーブに失敗しました！ | Không lưu được game! |
| `Notice Load Failed` | Failed to load this save! | セーブのロードに失敗しました！ | Không tải được lưu game này! |
| `Notice Save Slot Empty` | This save slot is empty! | このセーブスロットは空です！ | Ô lưu game này trống! |

Các key cũ (`Setting Title`, `Setting Music/Sound/Voice`, `Setting Screen...`, `Setting Language`, `Setting Return`, `Start Load Game Save No`...) đã có sẵn, không đụng. `Start Confirm No` được dùng chung làm nút "No/Cancel" cho cả confirm xoá lẫn overwrite.

---

## 4. Bind `LocalizedText` trên `Menu/In Game/Setting`

17 binding (đếm live từ editor). Cơ chế: `LocalizedText.OnEnable()` tự apply key; đổi ngôn ngữ được refresh qua event `LocalizationManager.LanguageChanged` (SettingPanel subscribe trong `OnShow`, unsubscribe trong `OnHide`).

| Vị trí | Key |
|---|---|
| `NameTMP` (tiêu đề) | `Setting Title` |
| 3 tab buttons | `Setting Tab Audio` / `Setting Tab Display` / `Setting Tab System` |
| Nhóm Audio | `Setting Music`, `Setting Sound`, `Setting Voice` |
| Nhóm Display | `Setting Screen`, `Setting Screen Full Screen`, `Setting Screen Window` |
| Nhóm Language | `Setting Language`, `UI_English`, `UI_Tiếng Việt_Text (TMP)_Text`, `UI_日本語_Text (TMP)_Text` |
| Nút Return | `Setting Return` |
| Nút Save/Load | `Setting Save Game`, `Setting Load Game` |

Bản legacy `Menu/Main Menu/Setting` giữ 12 binding cũ (không có tab / save / load) — không thay đổi, vì nó thuộc section Main Menu và chỉ cần bộ key cũ.

---

## 5. Danh tính panel & sorting order (audit live từ scene, edit mode)

| Path | Component | panelId | section | behavior | sortingOrder |
|---|---|---|---|---|---|
| `Menu/Main Menu/Load Game` | `UIPanel` | `Load Game Popup` | Main Menu | Popup | 2 |
| `Menu/Main Menu/Setting` | `SettingPanel` | `Setting Popup` | Main Menu | Popup | 3 |
| `Menu/In Game/Setting` | `SettingPanel` | `Setting Popup` | In Game | Popup | 4 |
| `Menu/In Game/Load Game` | `UIPanel` | `Load Game Popup` | In Game | Popup | 6 |
| `Menu/In Game/Save Game` | `UIPanel` | `Save Game Popup` | In Game | Popup | 7 |

(`SettingPanel` là subclass của `UIPanel` — cùng cơ chế register/show/hide.)

### Vì sao clone trùng `panelId` mà vẫn đúng?

`UIPanelManager` được thiết kế cho duplicate ID giữa các section:

- `OrganizePanelsBySection()` quét **tất cả** panel bằng `FindObjectsByType<UIPanel>(Include inactive)` — comment trong code nói rõ để xử lý "multiple panels share the same ID (in different sections)".
- `ShowPanel(id)` / `HidePanel(id)` **ưu tiên panel thuộc section hiện tại** trước khi fallback — nên ở In Game, `ShowPanel("Load Game Popup")` luôn mở bản In Game, không mở nhầm bản Main Menu.
- Boot: `OrganizePanelsDelayed()` (chờ 3 frame cho panel tự register) → `DisableAllPanelsImmediate()` → `SwitchToSection(initialSection=MainMenu)`; sau đó `PanelWatchdog()` chạy thường trực để bắt panel bị bật ngoài ý muốn.

### Vì sao serialize `activeSelf=True` trên mọi popup mà không sao?

Tất cả popup trong scene (kể cả 5 panel trên) đang serialize `activeSelf=True` — đây là **trạng thái đã commit** (`Game.unity` isDirty=false ở edit mode), không phải residue của play mode. Runtime hiển thị do `UIPanelManager` toàn quyền quản lý: boot sequence tắt hết trước khi bật section đầu. Đây là convention chung của scene, **không cần sửa scene**.

---

## 6. Verify — runtime (đã PASS, playtest 27/08)

Kịch bản: vào In Game (section `InGame`), ngôn ngữ EN.

1. **Setting**: mở từ in-game menu → tiêu đề "Setting", 3 tab "Audio / Display / System", nút "Save Game" / "Load Game" đều render từ key. Đổi ngôn ngữ trong panel → text đổi ngay (không cần mở lại panel).
2. **Load Game**: bấm `Setting Load Game` → popup Load Game (order 6, trên Setting) hiện Quick Save + Save 1..3; slot có dữ liệu hiện "Day X - HH:mm", slot trống giữ label "Save {n}"; bấm slot trống → notice "This save slot is empty!"; bấm slot đầy → confirm load; nút xoá → confirm "Delete this save?".
3. **Save Game**: bấm `Setting Save Game` → popup Save Game (order 7) — slot trống save ngay + notice "Game saved!"; slot đầy → confirm "Overwrite this save?".
4. **Z-order**: Setting(4) → Load(6) → Save(7) vẽ đúng lớp; Confirm Popup đè lên trên cùng khi xuất hiện.
5. Console không có lỗi localization mới (chỉ còn warning có sẵn — §7).

Bằng chứng: `Assets/Screenshots/playtest_setting_load_en.png` (giữ lại làm evidence, kèm `.meta`).

---

## 7. Cảnh báo có sẵn (KHÔNG sinh ra từ task này)

- Localization missing key: `UI_Love Level_Text (TMP)_Text`, `Lewd Level Text` — HUD level, chưa có key trong CSV.
- `ShopInteraction`: `CubismRaycaster` chưa gán reference.

Ghi nhận để xử lý task riêng, không block task này.

---

## 8. Dọn dẹp

- Xoá toàn bộ file `tmp_*.txt` dùng để trích xuất ground truth trong lúc điều tra (`tmp_gitdiff_csv`, `tmp_gitdiff_uitable`, `tmp_gitstatus`, `tmp_scenediffstat`, `tmp_canvas_orders`, `tmp_panelbtn`, `tmp_panelmgr`, `tmp_register`, `tmp_code_lang`, `tmp_scene_ctx`, `tmp_csvinfo`, `tmp_csvedit_report`, `tmp_csvstate`, `tmp_tabs_report`, `tmp_langkeys*`, `tmp_showpanels`).
- Giữ `Assets/Screenshots/playtest_setting_load_en.png` + `.meta` làm evidence cho devlog.

### Checklist nhanh

- [x] 14 key mới × 3 locale (CSV + UI Table)
- [x] 17 `LocalizedText` bind trên `Menu/In Game/Setting`
- [x] Load Game Popup clone cho In Game, section-aware duplicate ID
- [x] Save Game Popup in-game + overwrite confirm
- [x] Sorting order 2/3/4/6/7 nhất quán
- [x] Runtime verify PASS (EN playtest, screenshot)
- [x] Dọn tmp files

