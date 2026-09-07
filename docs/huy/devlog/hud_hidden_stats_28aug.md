# Devlog — 7 hidden stats lên HUD Navigation + sửa bug "frozen value texts" (28/08/2026)

> Task: thêm 7 hidden stats vào HUD `Menu/In Game/Navigation` theo plan **Option B** — Skill Point, Charming, Knowledge, **Day X** counter, Day Cycle (Morning/Evening/Night), Work Level, Work Progress. Trong lúc làm phát hiện & sửa kèm **bug có sẵn**: 3 value text (Money/Stamina/Time) bị `excludeFromLocalization=1` từ tool migration cũ → `SetLocalized()` no-op → giá trị HUD đóng băng.
> Trạng thái cuối (28/08): **runtime verification PASS** — play mode: cả 10 hàng hiển thị live (Money '900', SP/Charming/Knowledge '0', Day '1', Cycle 'Morning'→'Night', Work Level 'Lv 1', Work Progress '0/150' / max 'Max'/'Lv 5'), switch ngôn ngữ EN↔VI↔JA re-render đúng toàn bộ label + value. Screenshot: `Assets/Screenshots/screenshot-20260828-001902.png`.

---

## 1. Vấn đề

1. 7 hidden stats chỉ xem được gián tiếp (skill panel, work confirm popup...) — HUD chính không có.
2. **Bug có sẵn (phát hiện khi audit trước khi clone row)**: value text của Money/Stamina/Time (`Menu/In Game/Navigation/*/Text (TMP)` child thứ 2) có `excludeFromLocalization: 1` + key rác kiểu `UI_236900`, `UI_Stamina_Text (TMP)_Text` — dấu vết tool capture-text cũ. Vì `LocalizedText.SetLocalized()` early-return khi excluded, mọi `UpdateAllUI()` không ghi được text → HUD hiển thị mãi giá trị đóng cứng trong scene (Money '236900' trong khi live '900'; Time '24:00 A.M' — chuỗi code không bao giờ sinh ra được).
3. CSV thiếu key cho 7 label mới + key giá trị (`UI Value Level`, cycle keys).

## 2. Tổng quan thay đổi

| Khối | Thay đổi |
|---|---|
| `Assets/Scripts/UI/InGameNavigationPanel.cs` | +7 `[SerializeField] LocalizedText` (skillPointText, charmingText, knowledgeText, workLevelText, workProgressText, dayText, cycleText) với `[Header]`; +5 method update (`UpdateDayAndCycle`, `UpdateSkillPoints`, `UpdateCharming`, `UpdateKnowledge`, `UpdateWorkStats`); `HandleResourceChanged` → full `UpdateAllUI()` (Money AND SkillPoint cùng đi qua ResourceChangedEvent); `HandleTimeChanged` + `UpdateDayAndCycle()` + `UpdateWorkStats()`; `UpdateAllUI()`/`OnLanguageChanged()` mở rộng |
| `Assets/Resources/Localization/UI/{en-US,vi-VN,ja-JP}.csv` | +11 key mỗi locale (§3) |
| `Assets/Resources/UI Table_Localization.csv` | +11 dòng master tương ứng |
| `Menu/In Game/Navigation` (scene) | Clone row Money ×7 (x 0.215–0.403, stack dọc y 0.449–0.890), set key label, value `exclude=0`, wire 7 field panel |
| Scene (bug fix) | 3 value text Money/Stamina/Time: `excludeFromLocalization=0`, xoá key rác |

Không đụng prefab (Navigation là **NotAPrefab** — sửa scene an toàn, không override).

## 3. Key CSV thêm mới (11 key × 3 locale)

| Key | en-US | vi-VN | ja-JP |
|---|---|---|---|
| `In Game Skill Point` | Skill Point | Điểm Kĩ Năng | スキルポイント |
| `In Game Charming` | Charming | Quyến Rũ | 魅力 |
| `In Game Knowledge` | Knowledge | Kiến Thức | 知識 |
| `In Game Day` | Day | Ngày | 日 |
| `In Game Cycle` | Cycle | Buổi | 時間帯 |
| `In Game Cycle Morning` | Morning | Sáng | 朝 |
| `In Game Cycle Evening` | Evening | Chiều | 夕方 |
| `In Game Cycle Night` | Night | Tối | 夜 |
| `In Game Work Level` | Work Level | Cấp Độ Công Việc | 仕事レベル |
| `In Game Work Progress` | Work Progress | Tiến Độ Công Việc | 仕事進捗 |
| `UI Value Level` | Lv {amount} | Lv {amount} | Lv {amount} |

Value dùng lại key có sẵn: `UI Value Integer` (Skill Point/Charming/Knowledge/Day — chỉ số), `UI Value Ratio` (Work Progress "0/150"), `UI Value Max` (max-level "Max"). Ban đầu định thêm `UI Value Day` ("Day {amount}") nhưng row đã có label "Day" → đổi sang `SetInteger(dayText, day)` tránh "Day Day 3".


## 4. Layout 7 row mới (clone từ Money row)

Template: `Image` sprite `Bar_Money`, 2 con `Text (TMP)` (label 32pt Center + value auto-size 0–72 Left). Anchors stretch x 0.215–0.403 (thẳng cột với Money), mỗi row cao 0.063:

| Row | y anchor | Label key | Panel field | Data source |
|---|---|---|---|---|
| Skill Point | 0.827–0.890 | `In Game Skill Point` | skillPointText | `Player.GetSkillPoint()` |
| Charming | 0.764–0.827 | `In Game Charming` | charmingText | `Player.GetCharming()` |
| Knowledge | 0.701–0.764 | `In Game Knowledge` | knowledgeText | `Player.GetKnowledge()` |
| Day | 0.638–0.701 | `In Game Day` | dayText | `GameManager.DaysPlayed` |
| Cycle | 0.575–0.638 | `In Game Cycle` | cycleText | `currentCycle` → 3 key Morning/Evening/Night |
| Work Level | 0.512–0.575 | `In Game Work Level` | workLevelText | `GameManager.GetWorkLevel()` qua `UI Value Level` |
| Work Progress | 0.449–0.512 | `In Game Work Progress` | workProgressText | `GetWorkProgress()`/`GetNextLevelRequirement()`; req ≤ 0 → `UI Value Max` |

Band này (y 0.449–0.890, x 0.215–0.403) đã audit trống: không đè Mai Profile (x 0.008–0.076), Mai Clothes, hay các button phải (x ≥ 0.903). Cả 14 LocalizedText mới (7 label + 7 value): `componentId` duy nhất theo path, `excludeFromLocalization=0`, `tableName=UI`; value để key rỗng (code set runtime qua `SetLocalized`).

## 5. Bug "frozen value texts" — chẩn đoán & fix

- **Bằng chứng**: HEAD scene đã có sẵn `excludeFromLocalization: 1` + key `UI_236900` trên value text Money (không phải do working tree). Play mode: corrupt text 3 value → invoke `UpdateAllUI()` → text vẫn corrupt (SetLocalized no-op). Time text '24:00 A.M' là stale data mà code không thể sinh ra (midnight branch chỉ cho '12:xx').
- **Fix**: set `excludeFromLocalization=0` + clear key cho 3 value text này (giữ nguyên ref panel). Sau fix play mode: Money '900' live, Time '07:30 AM' + 'Monday' live.
- **Lesson**: mọi `LocalizedText` được code drive qua `SetLocalized` **phải** có `exclude=0`; khi clone row phải override flag này (clone kế thừa serialized state).

## 6. Verify — runtime (PASS, 28/08)

Kỹ thuật: play mode + reflection invoke handlers (`UpdateAllUI`, `HandleGameStart`, `HandleTimeChanged`), snapshot `TMP_Text.text` từng row:

1. **Boot**: Money '900' (live, không còn 236900), SP/Charm/Knowledge '0', WL 'Lv 1', WP '0/150'. Day/Cycle/Time rỗng (đúng — chưa có GameStartEvent, `SetEmpty` fallback).
2. **GameStart(Morning)**: Day '1', Cycle 'Morning', Time '07:30 AM' + 'Monday'.
3. **TimeChanged(Night)**: Cycle → 'Night' ngay.
4. **Ngôn ngữ**: EN→VI: Money 'Tiền', SP 'Điểm Kĩ Năng', Cycle 'Buổi'/'Tối', WL 'Cấp Độ Công Việc'... EN→JA: '時間帯'/'夜', '仕事レベル'... JA→EN round-trip OK.
5. **Max-level path**: set workLevel=5 (nextReq=-1) → WL 'Lv 5', WP 'Max'; restore OK.
6. Console sạch (chỉ 2 warning có sẵn: ShopInteraction CubismRaycaster, LocalizationManager cleanup — không liên quan).
7. Scene saved sau edit (isDirty=false), exit play không mất gì.

Ghi chú quy trình: MCP WebSocket bridge từng wedge trong 1 play session (SendJsonAsync exception loop trong Editor.log) — stop play + retry là recover; mọi verify ở trên chạy ở session play thứ 2.

## 7. Còn lại / follow-up

- 65 `excludeFromLocalization: 1` khác vẫn tồn tại trong scene — phần lớn là text được code drive trực tiếp hoặc fixture; chỉ 3 value HUD này là bị sai. Nếu thấy text HUD nào "không chịu đổi" → check flag này trước.
- `UI Value Day` key KHÔNG thêm (không dùng nữa).
- Screenshot evidence: `Assets/Screenshots/screenshot-20260828-001902.png`.

### Checklist nhanh

- [x] C# compile sạch, 7 field + 5 method mới trong assembly
- [x] 11 key × 3 locale + master table (BOM/CRLF giữ nguyên)
- [x] 7 row scene + wire 7 ref, không NULL
- [x] Bug fix 3 frozen value text có sẵn
- [x] Runtime verify PASS (giá trị, cycle change, max-level, 3 ngôn ngữ)
- [x] Scene saved, devlog
