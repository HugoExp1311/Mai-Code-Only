# MAI'S LOVE STORY — TASK TODO (Ưu tiên hoàn thiện Gameflow)

> **Mục đích:** Danh sách task xếp theo ưu tiên để hoàn thiện một gameflow đóng kín (New Game → Daily Loop → Sex Scene → Tích lũy → Ending), bóc từ `docs/huy/gdd_technical.md`.
> **Ngày biên soạn:** 2026-08-25. **Nguồn:** GDD §2 (status map), §14 (Open Questions), + verify code trực tiếp 2026-08-25.
> **Quy ước:** ✅ đã xong • 🟡 một phần • ❌ chưa có • ⚠️ lệch spec cần chốt.

---

## P0 — Chốt quyết định thiết kế (blocker cho mọi task dưới)

Không viết code mới trước khi chốt các mục ⚠️ trong GDD §14:

- [ ] **P0.1** ⚠️ Work Level max: LV5 (§9.2) hay LV10 (Ending 3&4 §13)? → ảnh hưởng `DefaultSettings.MaxWorkLevel`, bảng multiplier, requirements.
- [ ] **P0.2** ⚠️ Thống nhất tên **Energy vs Stamina** trong code (GDD dùng lẫn — code hiện đang dùng `BasicStats.Stamina`).
- [ ] **P0.3** ⚠️ Pregnancy 100% → chuyển sang Ending nào? (§6.4 + §13).
- [ ] **P0.4** ⚠️ Sex Points (GDD) vs `SkillPoint` (code, PlayerPrefs): gộp hay tách?
- [ ] **P0.5** ⚠️ Skill mapping: 8 skill GDD (Sex Skill 1+2) ↔ 8 skill trong `SkillManager`/`DefaultSettings`.
- [ ] **P0.6** Chốt danh sách Event chính thức đưa vào Scene gallery (§12 ghi 5-7 event, gallery §3 ghi 5).

> Kết quả P0 nên update ngược lại `gdd_technical.md` §14.

---

## P1 — Phần xương sống của gameflow (phải có để "chơi hết game" được)

### P1.1 Save / Load 🟡 — **ưu tiên cao nhất trong P1**
Lý do: không có save thì mọi progression (Love/Lewd/Work/Satisfaction counter) đều mất khi thoát → Ending không thể đạt được thực tế.
- [x] Thiết kế save schema: Day/Time, Player stats + Money, Work Level/Progress, Mai (Love Points/Level, Libido, Lewd, Sensitive 4 bộ phận, Pregnancy rate), Satisfaction đạt/trượt counter, Sex Scene unlock flags, Event/Ending unlock flags, Inventory.
- [x] Impl `GameManager.SaveGame()` / hoàn thành `GameManager.LoadGame()` (hiện TODO).
- [x] **Quick Save** (auto-save khi chạm mốc cốt truyện) + **Save (Number)** (thủ công) — GDD §3 nút 2. *(26/08: `GameManager.AutoQuickSave()` hook sau Opening CG/skip-intro/fallback; save thủ công in-game qua `SaveGamePanelController` (Setting → Save popup, overwrite confirm); skeleton mồ côi `Base/Save/` đã xoá. Chi tiết: `docs/huy/devlog/save_load_25aug.md` mục 4.)*
- [x] UI **Load Game** ở Start Game menu + xác nhận "Start from here? Yes/No". *(26/08: wire xong panel qua `LoadGamePanelController` — slot meta Day/time, confirm load, nút Delete có xác nhận; `SlotCount` 3→5 khớp UI.)*

### P1.2 Satisfaction counter + Ending (4) ❌
Lý do: kết thúc gameflow — không có Ending thì không có đích đến.
- [ ] Đếm số lần **đạt/trượt Mai's Satisfaction** (tính mỗi đêm sau Finish, ngưỡng theo Lewd §10.5) — persist qua save (P1.1).
- [ ] Điều kiện kích hoạt 4 Ending (Divorce/Cheating/Happy Marriage/Happy Marriage?) theo §13 + nhánh pregnancy (chờ P0.3).
- [ ] Ending presentation: CG/thoại kết (dùng hệ `CGDialogueSequenceSO`/`CGPanel` sẵn có) → quay về Start Game.
- [ ] Unlock Ending vào Scene gallery (§3 nút 3).

### P1.3 Pregnancy system ❌ (phụ thuộc P0.3)
- [ ] Tăng +20% mỗi lần Cum Inside không Condom; hiện Pregnancy Status trong Mai Body Status (§6.5).
- [ ] 100% → chuyển thẳng Ending tương ứng.
- [ ] Logic Condom (đã có item trong Shop) ngăn tăng rate khi dùng.

### P1.4 Hoàn thiện night-loop → day-loop
- [ ] 🟡 Verify công thức **Sex Points / Satisfaction / Sensitive Points** ở `SimulationResultPanel` + `SexSessionData` khớp §10.5 (7 dòng thống kê + bảng Satisfaction theo Lewd).
- [ ] 🟡 Verify khung giờ chi tiết: Sex 9-11 P.M 1 lần/ngày, Eat 7-9 P.M, Company T2-T7, Park ban ngày, Mai xuất hiện ở Home theo §4.4 (so với `DefaultSettings` action availability + `UIPanelButtonAction.IsDailyActionAvailable`).


---

## P2 — Lấp lỗ hổng daily-loop (chơi mỗi ngày đủ nội dung)

### P2.1 Talk content Love LV2-4 🟡
- [ ] Viết thoại 3 sub-topic × 3 level (LV2 món ăn/du lịch/bạn bè, LV3 rảnh/quà/tuổi thơ, LV4 mong ước/kỷ niệm/tư thế) × 3 ngôn ngữ (EN/VN/JP) — hiện chỉ có tiêu đề, thoại trống.
- [ ] Bonus: "Ask her about her day" đủ 10 câu (đang có 5, EN chỉ 3) + nhánh reward âm/dương theo §7.2.

### P2.2 Libido / Craving hội thoại ép sex 🟡
- [ ] Verify số ngày chuyển 5 trạng thái Libido trong `Mai.cs` (1/3/5/7 ngày không sex) + reset về "Not interested" sau sex (§6.3).
- [ ] Impl hội thoại ép sex khi Craving ("Bae, it's been a long time...") sau Talk: "I'm tired" −10 Love (đếm số lần → Event Siêu nứng) | "Sure, let's fuck" → Sex Scene + time/energy rule §6.3 (trước 9 P.M: +2h −50 Energy; sau 9 P.M: sang ngày mới 7 A.M −10 Energy).
- [ ] Chặn Sex khi Libido = Not interested/Normal (§6.3).

### P2.3 Sex Scene còn thiếu 🟡❌
- [ ] **Doggy Style** (Pussy/Butthole, có CD): fix `SexPositionConfigFactory` đang throw `NotImplementedException` + animation state machine + contract test (pattern `MissionaryContractTests`). Unlock 200 pts + Sensitive Pussy/Butthole ≥3 (§8.4).
- [ ] **Standing** (đang throw): GDD §8.4 không liệt kê — chốt giữ hay loại khỏi scope (update GDD nếu bỏ).
- [ ] **Spooning (500) / Mating Press (700) / Full Nelson (1000)** ❌: thêm position config + unlock condition + animation + contract test. Ưu tiên theo cost unlock thấp → cao.
- [ ] 🟡 Verify ngưỡng unlock Love/Lewd/Sensitive khớp §8.4 trong `SexSceneUnlockService`/`SexSceneDefinition`.

### P2.4 Mai Clothes panel ❌
- [ ] HUD button §4.2 → panel kho trang phục sở hữu (mặc định + Apron/Bikini/Gym Outfit/Sexy Sleepwear), nút "Wear" đổi outfit Mai (Live2D).
- [ ] Gắn Gift outfit: mua ở Hiep Mart → vào kho clothes thay vì chỉ +Love (GDD Q10 — mặc định đề xuất: cả +Love lẫn vào kho).

---

## P3 — Hoàn thiện meta / replay (sau khi gameflow chạy kín)

- [ ] **Events (5-7 scripted)** ❌ §12: điều kiện unlock (Love/Max Energy/Gift item/Lewd/Sensitive) + thoại (phần lớn chưa viết — cần content team) + vào Scene gallery tab Event. Ưu tiên: Lần đầu Anal → Tập Gym → Đi Biển → Họp lớp → Kỉ niệm cưới → Siêu nứng → Ngồi lên mặt.
- [ ] **Scene gallery** ❌ (Start Game nút "Scene"): list Event + Ending đã unlock, cảnh chưa unlock xám, xác nhận trước khi xem lại.
- [ ] **Credit** ❌ (Start Game nút 5): danh sách nhân sự + avatar link cá nhân.
- [ ] 🟡 Verify Love Level ngưỡng (100/200/300/400) + LV5 = mọi thưởng/phạt Love = 0 (§6.2).
- [ ] 🟡 Verify công thức Lewd Level & Sensitive Level/Points §6.5 trong `MaiBodyPanel`/`Base/Character/Stats`.
- [ ] 🟡 Map skill GDD ↔ `SkillManager` (chờ P0.5) + verify necessary points §8.2-8.3.

---

## Ghi chú verify 2026-08-25 (quan trọng — điều chỉnh ưu tiên GDD §2)

**Work Level / Progress thực tế ĐÃ IMPLEMENT gần đủ** (GDD §2 ghi ❌ là outdated):
- `GameManager.cs`: `workLevel`/`workProgress`, `GetWorkLevel()`, `GetWorkProgress()`, `GetNextLevelRequirement()`, `AddWorkProgress()`, `TryPromoteWorkLevel()`, `HasWorkedToday()`/`MarkWorkingUsed()`.
- `Base/DefaultSettings.cs`: `MaxWorkLevel = 5`, base rewards (400 Money / 5 Knowledge / 10 Progress / −30 Energy / 8h), multipliers x1→x3, requirements 150/400/750/1000, `GetWorkRewards()`, `CanPromoteWorkLevel()`.
- `UI/ActionConfirmPopup.cs`: Progress layout riêng (Work Level / Points / Next Level / nút Promote + Notice Success/Fail), `ApplyWorkRewardsBasedOnLevel()` (Work hard ×2 money/knowledge/progress, stamina −50).
- `UI/Actions/UIPanelButtonAction.cs`: `PlayerDailyAction.Working/Progress` gating theo Area Company + once-per-day.
- Localization đủ 3 ngôn ngữ (`Action Working`, `Action Progress`, `Action Confirm Progress *`, Promote Success/Fail) trong `Assets/Resources/Localization/UI/{en-US,vi-VN,ja-JP}.csv`.
- **Còn thiếu cho Work Progress:** verify trong Game.unity nút Working/Progress ở Company đã wire `dailyAction` đúng; kiểm tra chồng lặp reward giữa stat-change config trong `ActionConfirmPopupDatabase` và `ApplyWorkRewardsBasedOnLevel()` (tránh cộng 2 lần); Work Level chưa nằm trong save. → Việc còn lại là **verify/tinh chỉnh**, không phải dev từ đầu.

Thứ tự ưu tiên gốc trong GDD §2 ("Save/Load, Work Progress, Pregnancy, Events, Endings, Scene Gallery, Mai Clothes, position còn thiếu") đã được sắp lại trong file này theo thực tế code: Save/Load + Endings + Pregnancy giữ nguyên là khối lớn nhất; Work Progress hạ xuống chỉ còn verify; Events/Scene gallery dời sau P2 vì phụ thuộc content.