# MAI'S LOVE STORY — GDD KỸ THUẬT (Agent-Oriented)

> **Mục đích:** Bản GDD chuẩn hóa, chắt lọc từ GDD ngôn ngữ tự nhiên (`docs/huy/gdd/*.md`, export ClickUp) + đối chiếu code thật (`docs/huy/summary.md`). Đây là "source of truth" về **thiết kế** để agent bóc tách task; khi cần "source of truth" về **code** thì đọc `Assets/Scripts/`.
> **Ngày biên soạn:** 2026-08-25. **Engine:** Unity 6000.3.22f1 + URP 17.3.
> **Nguồn gốc:** GDD chính thức tại ClickUp Doc `2kzka05c-198`. 4 file trong `docs/huy/gdd/` (In_Game, Sex_Scene, Story, Ending) là cùng 1 bản export **trùng nội dung** — đã gộp & khử trùng lặp ở đây.

> **Quy ước đánh dấu implement:**
> - ✅ **Có** — đã có trong code (theo summary.md, xác minh 2026-08-24).
> - 🟡 **Một phần** — có khung/liên quan nhưng chưa khớp đủ spec.
> - ❌ **Chưa** — chưa thấy trong code, cần dev mới.
> - ⚠️ **Lệch spec** — code hiện tại khác GDD (cần quyết định giữ cái nào).

---

## 1. Tổng quan & Core Loop

**Thể loại:** Visual Novel + Life-sim + Adult sim (18+). Nữ chính **Mai** dựng bằng Live2D. Người chơi vào vai **Player** (chồng của Mai).

**Vòng lặp chính (daily loop):**
```
Bắt đầu ngày (7:00 A.M, tại Home)
  → Di chuyển giữa 4 Place (Home / Company / Park / Hiep Mart) — mỗi lần tốn Time
  → Hoạt động theo Place & khung giờ:
      • Home: tương tác Mai (Gift/Talk/Sex/Eat), Sleep
      • Company: Work / Talk / Progress (thăng Work Level)
      • Park: Exercise / Talking
      • Hiep Mart: mua Goods & Gift
  → Buổi tối (khung giờ mở): Sex → Sex Scene Gameplay → Finish (thống kê Sex Points, Mai's Satisfaction)
  → Ngày mới
Tích lũy lâu dài → Event / Ending
```

**Trục tiến triển (progression axes):**
- **Player:** Energy/Max Energy, Knowledge, Charming, Money, Work Level, Sex Skill 1 & 2, Sex Scenes unlock.
- **Mai:** Love (Points → Level 1-5), Libido (5 trạng thái), Lewd Level (1-5), Sensitive Level từng bộ phận (1-10), Pregnancy rate.
- **Kết cục:** 4 Ending, quyết định bởi số lần đạt/trượt "Mai's Satisfaction" + Love Level + Lewd Level + Work Level.

---

## 2. Bản đồ GDD ↔ Code (Implementation Status)

| Hệ thống GDD | Trạng thái | File code chính | Ghi chú lệch |
|---|---|---|---|
| Game flow (MainMenu→Intro→InGame→Sim) | ✅ | `GameManager.cs` | Save/Load ❌ |
| Start Game menu (6 nút) | 🟡 | `UIManager`, `SettingPanel` | Load Game / Scene gallery ❌ |
| Time & Day cycle | 🟡 | `TimeManager.cs`, `DayTimeUI` | Cần verify khung giờ chi tiết |
| Place (4 khu) + time-lock | ✅ | `PlaceSelectionPanel`, `PlaceUI`, `EnumSettings.Area` | |
| Dialogue + Talk options | ✅ | `DialogueManager`, `DialoguePanel`, `DialogueSequenceSO` | Nội dung Love Level 2-4 GDD còn trống |
| CG opening | ✅ | `CGDialogueSequenceSO`, `CGPanel` | |
| Love Points/Level | 🟡 | `Mai.cs`, `CommonStat` | Verify ngưỡng level |
| Libido (5 trạng thái) | 🟡 | `Mai.cs`, `SensitivePointsDecayEvent` | Verify số ngày chuyển state |
| Lewd Level + Sensitive Level | 🟡 | `MaiBodyPanel`, `Base/Character/Stats` | Verify công thức |
| Player Sex Skills (2 nhóm) | 🟡 | `SkillManager`, `SkillUpgradeUI` | Map skill GDD ↔ 8 skill code |
| Sex Scene unlock (10 cảnh) | 🟡 | `SexSceneUnlockService`, `SexSceneDefinition` | Doggy & Standing chưa impl |
| Sex Scene Gameplay (bars, buttons) | ✅ | `SexSimulationManager`, `SexSessionData`, `UI/SexPosition/*` | |
| Position strategies | 🟡 | `SexPositionConfig*` | Doggy/Standing throw NotImplementedException |
| Sex Scene animations (Live2D) | ✅ | `Live2D/*AnimationEventReceiver`, StateBehaviour | Contract tests bảo vệ |
| Finish / Sex Points / Satisfaction | 🟡 | `SimulationResultPanel`, `SexSessionData` | Verify công thức điểm |
| Shop & Inventory | ✅ | `ShopPanel`, `ItemManager`, `Inventory` | |
| Mai Clothes (trang phục) | ❌ | — | GDD có, code chưa có panel riêng |
| Work Level / Progress | ❌ | — | Cần dev |
| Events (5-6 scripted) | ❌ | — | Cần dev |
| Endings (4) | ❌ | — | Cần dev |
| Pregnancy system | ❌ | — | Cần dev |
| Save/Load + Scene gallery | ❌ | `GameManager.LoadGame()` TODO | |
| Localization (EN/VI/JA) | ✅ | `Base/Localization/*` CSV | |
| Audio (FMOD) | ✅ | `AudioManager`, `FMODEvents` | |

> **Ưu tiên dev còn thiếu:** Save/Load, Work Progress, Pregnancy, Events, Endings, Scene Gallery, Mai Clothes, position còn thiếu (Doggy, Standing/scene chưa impl).
---

## 3. Start Game (Title Menu)

Giao diện title có **6 nút menu** + **1 nút thumbnail**.

- **Thumbnail:** mở link ngoài `https://x.com/HiepStudio`.
- **6 nút:**
  1. **New Game** — bắt đầu game mới (vào CG Opening).
  2. **Load Game** ❌ — mở danh sách Save. 2 loại: **Quick Save** (auto-save khi chạm phân cảnh cốt truyện unlock) và **Save (Number)** (save thủ công). Bấm 1 save → hỏi "Start from here? Yes/No".
  3. **Scene** ❌ — thư viện cảnh đã unlock. 2 loại: **Event** (5 event dự kiến) và **Ending** (4 ending dự kiến). Cảnh chưa unlock bị xám. Bấm cảnh → hỏi xác nhận.
  4. **Setting** ✅ — Music / Sound / Voice (volume), Screen (Full Screen/Window), Language (EN/VI/JA), Title (về Start Game).
  5. **Credit** ❌ — danh sách nhân sự, mỗi avatar dẫn link cá nhân (Hiep Studio, voice actress Ka Đê/Du Mộng/Hotra, developer...).
  6. **Quit** — thoát game.

**Localization title (EN/VN/JP):** Start from here?/Bắt đầu từ đây?/ここから始まりますか？ • Yes/Vâng/はい • No/Không/いいえ • Event/Sự kiện/イベント • Ending/Cảnh kết/エンディング.

---

## 4. In-Game HUD & Panels

### 4.1 HUD chỉ số (Energy / Money / Time)
- **Energy:** hiển thị `hiện tại / giới hạn`. Dùng cho mọi hoạt động. Mặc định max 100.
- **Money:** tiền tệ, mua vật phẩm.
- **Time:** gồm **Day** (số ngày trôi qua) + **Time A.M/P.M**. 6 A.M–6 P.M = sáng; 6 P.M–6 A.M = tối.

### 4.2 Các nút HUD
- **Mai Profile** → §6.
- **Mai Clothes** ❌ — kho trang phục Mai đã sở hữu; nút "Wear" để Mai mặc.
- **Status** (Player Skill/Profile) → §8.
- **Place** → §5.
- **Setting** — cài đặt in-game.
- **Inventory** — kho đồ; "Use" (Player dùng), "Give" (tặng Mai, chỉ khi đang action "Gift").

### 4.3 Action với Mai (tại Home)
Lúc đầu các nút Action **ẩn**; **bấm vào Mai** mới hiện. 4 action:
- **Gift** — tặng quà → +Love.
- **Talk** — nói chuyện → +Love (§7).
- **Sex** — làm tình → +Love, +Lewd.
- **Eat** — ăn cùng Mai → +Love, +Energy.

### 4.4 Khung giờ Mai xuất hiện ở Home
- **Xuất hiện:** 7:00–7:29 A.M và 6:00–11:00 P.M (mọi ngày).
- **Không xuất hiện:** 7:30 A.M–5:59 P.M và 11:01 P.M–6:59 A.M.
- **Cuối tuần (Sat/Sun):** Mai chỉ vắng 11:01 P.M–6:59 A.M (ban ngày cuối tuần vẫn ở nhà).
---

## 5. Place (Di chuyển)

4 Place: **Home, Company, Park, Hiep Mart**. SFX bấm riêng (`sound_home_place`, `sound_company_place`, `sound_park_place`, `sound_hiepmart_place`).

**Time cộng khi di chuyển:** Company/Home/Park +0:30, Hiep Mart +0:15.

**Background Home theo Time:** 6 A.M–6 P.M = Home Morning; 6 P.M–12 A.M = Home Evening; 12 A.M–6 A.M = Home Night. Qua 12:00 A.M nếu Time cộng tiếp → **Day +1**.

**Time-lock di chuyển:**
- **Company:** khóa 5:00 P.M–7:29 A.M. Msg: *"It's not my working hours at this time"*.
- **Park:** khóa 6:00 P.M–6:00 A.M. Msg: *"I don't wanna go out at this time"*.
- **Hiep Mart:** mở mọi giờ, không đổi background.
- Thông báo dùng `Board_Small` + SFX `sound_bad`.

**Music theo Time:** 6 A.M–6 P.M = `Music_Morning`; 6 P.M–6 A.M = `Music_Evening`.

---

## 6. Mai — Profile & Relationship Status

### 6.1 Profile
Thông số tĩnh (không đổi suốt game), trừ Relationship Status. Có nút **Body** → Mai Body Status (§6.5).

### 6.2 Love
- **Love Points:** tăng/giảm qua gameplay. Sàn = 0 (không âm).
- **Love Level (5 cấp):** tự lên cấp khi đủ points (không cần xác nhận).
  - LV1: mặc định • LV2: 100 • LV3: 200 • LV4: 300 • LV5: 400.
  - LV5 cao nhất: mọi thưởng/phạt Love Points sau đó = +0.
- Bar `Bar_Love_Level` (fill `Bar_Love_Level_2`).

### 6.3 Libido (trạng thái nứng)
5 trạng thái, tăng theo số ngày **không** làm tình:
| Trạng thái | JP | Điều kiện |
|---|---|---|
| Not interested / Không hứng thú | 興味無し | Mặc định |
| Normal / Bình thường | 普通 | Sau 1 ngày |
| Ready to have sex / Sẵn sàng chịch | セックス覚悟 | Sau 3 ngày |
| Horny / Nứng | ムラムラする | Sau 5 ngày |
| Craving for cock / Vắt cực khô | 力尽きるまでセックス | Sau 7 ngày |

- **Không thể** sex khi = Not interested / Normal.
- **Có thể** sex khi = Ready / Horny / Craving.
- Sau khi sex → reset về **Not interested**.
- Hình minh họa: Ready → `Emotion_Ready`; Craving → `Emotional_Horny`.

**Craving for cock — hội thoại ép sex:** sau bất kỳ đối thoại Talk nào, Mai hỏi *"Bae, it's been a long time... How about...?"* → 2 option:
- **"I'm tired"/"Tha anh":** −10 Love Points, Libido về **Ready to have sex**. (Chọn lần thứ 3 → Event "Siêu nứng", §10.)
- **"Sure, let's fuck"/"Chịch thôi":** vào Sex Scene Gameplay. Sau đó:
  - Time **trước** 9 P.M: về sinh hoạt bình thường, Time +2h, Energy −50.
  - Time **sau** 9 P.M: sang ngày mới 7 A.M, Energy −10.

### 6.4 Pregnancy rate ❌
Tỷ lệ có thai nếu sex **không Condom** + **Cum Inside** (mỗi lần Cum Inside +20%). Đạt **100%** → chuyển thẳng Ending.

### 6.5 Mai Body Status
Chỉ số 4 bộ phận: **Boobs, Mouth, Pussy, Butthole** + **Pregnancy Status**.

**Lewd Level (1-5)** — quyết định bởi Sensitive Points 4 bộ phận. Nâng cấp → **trừ hết** Sensitive Points hiện có (vd có 270 Pussy, cần 250 → còn 20). Hình `Body_Horny_Lv1..5`.

| Lewd Level | Pussy & Butthole | Boobs & Mouth |
|---|---|---|
| LV1 | mặc định | mặc định |
| LV2 | > 60 | > 40 |
| LV3 | > 250 | > 170 |
| LV4 | > 600 | > 300 |
| LV5 | > 1000 | > 500 |

- Nút Level Up (`Button_Mai_Body_Level_Up`). Thành công → đóng bảng; thất bại → thông báo lỗi.
- LV5 = max: 4 bộ phận hiển thị "Max".

**Sensitive Level từng bộ phận (1-10)** — mỗi thao tác +N (N=level). Nâng cấp → trừ Sensitive Points.
| Level | Cần | +/thao tác |
|---|---|---|
| 1 | mặc định | +1 |
| 2 | 20 | +2 |
| 3 | 50 | +3 |
| 4 | 100 | +4 |
| 5 | 150 | +5 |
| 6 | 220 | +6 |
| 7 | 300 | +7 |
| 8 | 370 | +8 |
| 9 | 480 | +9 |
| 10 | 600 | +10 |

- LV10 = max → nút Level Up biến mất.
- Nút "Profile" (xanh) → về Mai Profile.

- Quy ước: Day 21 = Sunday, Day 69 = Saturday.
---

## 7. Talk System (hội thoại với Mai)

**Cách hiển thị:** bấm nút "Talk" → Mai rời background, xuất hiện giữa màn hình. Bong bóng thoại: Mai bên **phải** (`Dialog_Mai.png`), Player bên **trái** (`Dialog_You.png`), chữ căn giữa bong bóng.
- Font Mai: 10pt, màu `#1f67c8`, viền trắng.
- Font Player: 10pt, màu `#000000`, viền trắng.
- Font Option: HP-Impact, 10pt, màu `#ffffff`.

**Menu Talk chính (3 option):**
1. **Ask her about her day** — tối đa **1 lần/ngày**. Random 1 trong nhiều câu trả lời (không phụ thuộc Love Level).
2. **Talk about a specific topic** — không giới hạn lượt/ngày. Hiện 3 sub-topic theo Love Level. Sau khi đã nhận thưởng cả 3 sub-topic, lần sau vẫn chọn được nhưng **không còn thưởng**.
3. **Nevermind** — đóng Talk, Mai về trạng thái ban đầu.

### 7.1 Talk about a specific topic — theo Love Level
| Love Level | 3 sub-topic | Trạng thái nội dung |
|---|---|---|
| LV1 | Mai's Working Hours / Mai's Job / Mai's Coworkers | ✅ đủ EN/VN/JP |
| LV2 | Món ăn thích / Chỗ du lịch thích / Bạn bè của Mai | 🟡 mới có tiêu đề, thoại trống |
| LV3 | Thời gian rảnh / Quà tặng thích / Tuổi thơ | 🟡 thoại trống |
| LV4 | Mong ước / Kỷ niệm / Tư thế làm tình thích | 🟡 thoại trống |
| LV5 | Hiển thị toàn bộ câu hỏi LV1-4 đã hỏi | — |

**LV1 reward mẫu (đã có thoại đầy đủ 3 ngôn ngữ):**
- Working Hours: +5 Knowledge, +2 Love.
- Mai's Job: +5 Knowledge, +10 Love.
- Mai's Coworkers: +5 Knowledge, +5 Love.

### 7.2 Ask her about her day — nhánh phân nhánh
Câu mở: *"How's your day anyway?"* Dự kiến 10 câu trả lời; hiện GDD có 5 (EN có 3, VN/JP có 5). Mỗi câu trả lời → sub-option với reward khác nhau, **có thể âm Love**:
- **#1 (fruit tea):** +15 Love → "Ask about colleague" −10 Love | "Ask about fruit tea" +10 Love +10 Charming.
- **#2 (tai nạn xe):** −15 Love → "Ask about motorbike" −20 Love | "Comfort her" +10 Love +10 Charming.
- **#3 (ngày bình thường):** +0 Love → "No more asking" +0 | "Watch a movie" +5 Love.
- **#4 (cà phê mèo):** +5 Love → "Nuôi chó" −10 Love | "Nuôi mèo" +20 Love +10 Charming.
- **#5 (deadline):** −10 Love → "Ngủ trước" −10 Love | "Phụ giúp Mai" +10 Love +10 Charming | "Pha đồ uống" +20 Love.

> Design pattern: mỗi câu trả lời là 1 tình huống, người chơi chọn phản ứng → reward Love/Charming (thưởng khi đồng cảm, phạt khi vô tâm). Charming ảnh hưởng chọn lời thoại (§8).

---

## 8. Player Stats & Skills (Status Panel)

### 8.1 Player Profile (3 chỉ số nâng cấp)
- **Stamina/Energy:** giới hạn Energy. Không max cứng, mặc định 100. Nâng qua Exercise.
- **Knowledge:** ảnh hưởng công việc. Mặc định 100. Nâng qua đọc sách/làm việc.
- **Charming:** ảnh hưởng lựa chọn lời thoại. Mặc định 100. Nâng qua Talking/cắt tóc.

> ⚠️ GDD dùng lẫn "Energy" và "Stamina" cho cùng chỉ số giới hạn — thống nhất trong code cần chọn 1 tên.

### 8.2 Sex Skill 1 — tỷ lệ Orgasm của Mai (dùng Sex Points nâng cấp)
| Skill | Ý nghĩa | Range cấp |
|---|---|---|
| **Magic Hand** | % Orgasm khi dùng Tay | 0.5%→2.5% (LV1-5) |
| **Hmmm … delicious!** | % Orgasm khi dùng Miệng/Lưỡi | 0.5%→2.5% (LV1-5) |
| **You like my dick, huh?** | % Orgasm khi Dick chơi Pussy | 0.5%→5% (LV1-10) |
| **You like anal, don't you?** | % Orgasm khi Dick chơi Butthole | 0.5%→5% (LV1-10) |

- Hand/Mouth: mỗi cấp +0.5% (5 cấp). Necessary Sex Points: 100/150/200/250.
- Dick Pussy/Butthole: mỗi cấp +0.5% (10 cấp). Necessary Sex Points: 200/300/400/500/600/700/800/900/1000.

### 8.3 Sex Skill 2 — cơ chế Cumming của Player
| Skill | Ý nghĩa | Range |
|---|---|---|
| **I'm bout to CUM** | % thanh Cumming tăng mỗi lần đút (càng cao cấp càng CHẬM ra) | 5%→1% (LV1-9). Necessary: 100..500 |
| **I need more bullet!** | Số lần Cum tối đa/đêm | x1→x10 (LV1-10). Necessary: 100..500 |
| **Long Night** | Giới hạn thanh Stamina/đêm | 100%→200% (LV1-11). Necessary: 100..600 |
| **Big Dick** | % Stamina tiêu hao mỗi lần đút (càng cao càng ÍT tốn) | 10%→1% (LV1-10). Necessary: 100..500 |

### 8.4 Sex Scene unlock (10 cảnh)
Mở khóa bằng Sex Points (bị **trừ** khi mở). Một số cần điều kiện thêm.
| Scene | Sex Points | Điều kiện thêm |
|---|---|---|
| Roleplay Pussy | 0 (mặc định) | — |
| Missionary | 0 (mặc định) | — |
| Roleplay Butthole | 50 | — |
| Blowjob | 50 | — |
| Paizuri | 50 | — |
| Doggy Style | 200 | Sensitive Pussy ≥3, Butthole ≥3 |
| Cowgirl | 300 | Sensitive Pussy ≥5, Lewd ≥2 |
| Spooning | 500 | Love >200, Lewd ≥2 |
| Mating Press | 700 | Sensitive Pussy ≥6, Butthole ≥6, Lewd ≥3 |
| Full Nelson | 1000 | Max Energy >300, Love >400, Lewd ≥4 |

> UI: bấm cảnh → "Fuck yeah!"/"Nah...". Đủ điều kiện → mở; thiếu → thông báo từ chối. Board `Board_SkillDescription`.
> ⚠️ Code: `SexPositionConfigFactory` hiện **throw NotImplementedException** cho DoggyStyle & Standing. Spooning/Mating Press/Full Nelson/Doggy cần dev thêm.

---

## 9. Hoạt động theo Place (chi tiết reward)

### 9.1 Home — Sex / Eat / Sleep
**Sex action** (nút Sex ở góc trái dưới):
- Chỉ hiện khung giờ **9 P.M–11 P.M**, mỗi ngày 1 lần.
- "Fuck yeah!" → vào Sex Scene, +25 Energy, +1 Day, Time reset 7 A.M.
- "I wanna sleep" → +50 Energy, +1 Day, Time reset 7 A.M.

**Eat action:**
- Chỉ hiện **7 P.M–9 P.M**, 1 lần/ngày. "Sure, why not?" → +5 Love, +40 Energy, +1h. "I'm not hungry" → đóng.

**Sleep action:**
- "Take a nap": +20 Energy, +2h. Tối đa 2 lần/ngày.
- "Sweet dream": +80 Energy, −2 Knowledge, skip sang hôm sau 6:30 A.M. Tối đa 1 lần/ngày. Chỉ dùng được **9 P.M–6 A.M**.
- Quá số lần → msg *"I've had enough sleep."/"Mình đã ngủ đủ rồi."*

### 9.2 Company — Work / Talk / Progress
Vào được: 7:30 A.M–4:59 P.M (T2-T6); 7:30 A.M–12:00 P.M (Thứ 7). Không vào: 5 P.M–7:29 A.M mọi ngày, cả ngày Chủ Nhật.

**Work (base, Level 1):**
- Work: +400 Money, +5 Knowledge, +10 Progress, −30 Energy, +8h.
- Work hard: +800 Money, +10 Knowledge, +20 Progress, −50 Energy, +8h.
- I'm lazy: đóng.

**Work Level (1-5) — nhân hệ số** (chỉ Money/Knowledge/Progress nhân, Energy & giờ giữ nguyên):
- LV1 x1 • LV2 x1.5 • LV3 x2 • LV4 x2.5 • LV5 x3. Kết quả .5 → làm tròn xuống.
- Công thức: +(400×L) Money, +(5×L) Knowledge, +(10×L) Progress.
- Next Level (Progress cần): L1→2: 150 • L2→3: 400 • L3→4: 750 • L4→5: 1000.
- Nút "Promote": đủ Progress → thăng cấp; thiếu → thông báo từ chối.

> ⚠️ GDD Ending yêu cầu **Work Level 10** nhưng Work Level chỉ định nghĩa tới LV5 — mâu thuẫn, cần user chốt.

**Talk (Company):** "Let's talk!" → +5 Charming, −10 Energy, +1h. 1 lần/ngày (riêng cho Company, tách biệt với Park).

### 9.3 Park — Exercise / Talking
Vào được: không trong 6 P.M–6 A.M.
- **Exercise:** "Okay..." +5 Max Energy, −20 Energy, +2h • "Try my best" +10 Max Energy, −40 Energy, +2h. 1 lần/ngày.
- **Talking:** "Let's talk!" +5 Charming, −10 Energy, +1h. 1 lần/ngày (riêng Park). → Tổng có thể Talk 2 lần/ngày (Park + Company).

### 9.4 Hiep Mart — Shop
- Bấm nhân vật nữ áo đỏ (Staff, Live2D) → thoại "Welcome to Hiep Mart!" biểu cảm "Smile", voice `voice_xinchao`. Mở menu mua hàng.
- 2 tab: **Goods** / **Gift**. Nút "Buy" → SFX `sound_buy`. Nút "Close" → Staff nói "Thank you..." + voice `voice_camon` (chỉ Close của Hiep Mart, các Close khác dùng `sound_close`).

**Goods:**
| Item | Tác dụng | Money |
|---|---|---|
| Milk | +5 Energy | 100 |
| Energy Drink | +10 Energy | 200 |
| Banh Mi | +20 Energy | 350 |
| Lunch | +50 Energy | 850 |
| Condom | Ngăn có thai | 50 |
| Rocket Drink | +50 Stamina | 250 |

**Gift:**
| Item | Tác dụng | Money | Dùng cho Event |
|---|---|---|---|
| Lipstick | +100 Love | 1500 | |
| Flower | +200 Love | 2800 | |
| Big Teddy Bear | +500 Love | 6000 | |
| Apron | +100 Love | 500 | |
| Bikini | +100 Love | 1000 | Event Đi Biển |
| Gym Outfit | +100 Love | 1500 | Event Tập Gym |
| Sexy Sleepwear | +100 Love | 2000 | Event Kỉ Niệm Ngày Cưới |

---

## 10. Sex Scene Gameplay (core simulation)

**Assets:** UI ở `Assets/Image/Bed Sex Scene/Bed Sex Scene UI`; animation ở `Assets/Image/Bed Sex Scene/(Scene)`.

**Vào scene:** hiển thị `(Scene) Start` → `(Scene) Standard - Loop`.

### 10.1 Các thanh (bars) & chỉ số HUD
| # | Tên | Ý nghĩa | Cơ chế tăng |
|---|---|---|---|
| 1 | **Insert/Roleplay Button** | Panel điều khiển hành động | §10.3 |
| 2 | **Orgasm bar (Mai)** | Độ sướng Mai. 3 màu Xanh→Vàng→Đỏ (`Mai_Bar_1/2/3`). 100%→1 Orgasm | Sex: +% theo skill "You like my dick/anal". Foreplay (Roleplay/Blowjob/Paizuri): +% theo skill Hand/Mouth. Sex Toy: +1.5%/animation. **Không** phụ thuộc Slow/Fast. 100% → `Squirting`→`Squirting - Loop` |
| 3 | **Cumming bar (Player)** | Tình trạng xuất tinh | Slow: +(%level/2); Fast: +%level. %level từ skill "I'm bout to CUM". 100% → Cum |
| 4 | **Stamina bar** | Thể lực/đêm | Tiêu hao theo skill "Big Dick". 0% → không thao tác tốn Stamina được |
| 5 | **Số lần Cum** | Số lần Cum còn lại/đêm | Cumming 100% → tiêu 1 lần. Hết → không tăng Cumming được |
| 6 | Inventory | Túi đồ Player | |
| 7 | Bong bóng thoại Mai | Mai nói với Player | |
| 8 | **Finish** | Kết thúc scene, sang ngày mới | §10.5 |

**Roleplay Action bar** (chỉ Roleplay Pussy/Butthole/Paizuri/Blowjob): thay thế Cumming bar. Chỉ animation tăng được Orgasm/Cumming mới +1%/lần. 100% → Roleplay Button biến mất, hiển thị animation "Start" → chỉ còn cách Finish để sang phần chịch Mai.

### 10.2 Insert Button flow (Missionary/Doggy/Cowgirl — kiểu penetration)
- Nút **Scene**: chọn cảnh khác (chỉ cảnh đã unlock; chưa unlock bị xám).
- Nút **Insert** → 2 lựa chọn **Pussy** / **Butthole** → `(Scene) Pussy/Butthole Insert` → `Insert - Loop`.
- Sau Insert: **Slow** (loop) / **Fast** (loop) / **Stop** (→ `Stop` → `Standard - Loop`, giảm tốn Stamina/tránh Cum sớm).
- Cumming 100% → nút **Outside** hiện **2 giây**:
  - Bấm Outside → `Cum Outside` → `Cum Outside - Loop` (bắn ra ngoài).
  - Không bấm → `Cum` → `Cum - Loop` (bắn vào trong = Cum Inside).
- Sau Cum: nút **Pull Out** → `Pull Out` → `Pull Out - Loop` → về UI ban đầu.

### 10.3 Roleplay Button flow (Roleplay Pussy/Butthole/Paizuri/Blowjob)
- Nút **cam đánh số**: animation riêng theo nhóm (vd Hand có 3 nút). Bấm → tự động chạy "Slow".
- Nút **hồng** (tốc độ): trái=Stop (`Button_SpeedStop`), giữa=Slow (`Button_SpeedSlow`), phải=Fast (`Button_SpeedFast`). Nếu animation không có "Fast" thì ẩn Slow+Fast.
- Roleplay Action bar 100% → Roleplay Button biến mất.

### 10.4 Quy ước đặt tên animation (quan trọng cho contract)
- **Level suffix:** `(Level 1-2)`, `(Level 3-4)`, `(Level 5)` theo Lewd Level của Mai. VD `Missionary Pussy Fast (Level 1-2)`.
- **CD suffix:** animation có "CD" chỉ chạy khi dùng **Condom**; không dùng → animation không-CD thay thế.
- **Loop:** tên chứa "Loop", "Slow", "Fast" đều là animation lặp đến khi có tương tác tiếp theo.

### 10.5 Finish — thống kê & Sex Points
Bấm Finish → "Yes/No". Yes → bảng thống kê Sex Points (dùng nâng Sex Skill; đồng thời +Sensitive Points cho Mai → ảnh hưởng Lewd Level):
1. **Orgasm (Roleplay):** Roleplay Pussy +30, Roleplay Butthole +25 Sex Points/lần.
2. **Orgasm (Sex):** Missionary +35, Doggy +40, Cowgirl +45, Full Nelson +50, Spooning +55, Mating Press +60 Sex Points/lần.
3. **Fuck Mai:** mỗi lần chịch (Slow/Fast) +1 (Lewd 1-2) hoặc +2 (Lewd 3-5) Sex Points.
4-7. **Boobs/Mouth/Pussy/Butthole:** mỗi thao tác +Sensitive Points theo Sensitive Level bộ phận.
8. **Cum Inside (No Condom):** mỗi lần +20% Pregnancy rate.
9. **Next Day:** sang ngày mới 7 A.M, +20 Energy.

**Mai's Satisfaction** (theo Lewd Level, cần đủ số Orgasm/đêm):
| Lewd Level | Orgasm cần | Đạt → +Love | Trượt → −Love |
|---|---|---|---|
| 1 | ≥2 | +10 | −5 |
| 2 | ≥4 | +20 | −12 |
| 3 | ≥6 | +30 | −18 |
| 4 | ≥8 | +40 | −24 |
| 5 | ≥10 | +50 | −30 |

> **Đây là biến quyết định Ending** — đếm số lần đạt/trượt Satisfaction (§12).

### 10.6 Từng Scene — animation state machine (tóm tắt)
- **Missionary** (Pussy/Butthole, có CD): Start→Standard-Loop; Waiting sau 5s idle; Insert→Slow/Fast→Stop; Squirting khi Orgasm max; Cum/Cum Outside/Pull Out. ✅
- **Cowgirl** (chỉ Pussy, không Anal, có CD): Waiting sau **10s** idle → tự Insert→Slow (Mai tự đút, lặp 2 lần → chuyển Slow theo Lewd + CD). ✅
- **Doggy Style** (Pussy/Butthole, có CD): Waiting 5s. ❌ chưa impl trong code.
- **Roleplay Pussy:** nhóm Hand (Clit Finger/2 Finger/Moc Cua), Tongue (Clit/Lick/Insert), Sex Toy (Hitachi/Egg Vib/Cucumber/Didlo). Egg Vib có chuỗi Insert Start→After→Work→Cumming. ✅
- **Roleplay Butthole:** Hand (Finger1/2/Massage/Grab Both), Tongue (Lick/Insert), Toy (Pen/Egg/Cucumber). Egg Insert Loop: mỗi anim +1.5% Orgasm +2% Roleplay Action. ✅
- **Paizuri:** Boob(1-4), Hand(1-3), Tongue(1-2). Cumming Mai / Cumming Dick 1-3 theo nhóm. ✅
- **Blowjob:** Handjob(1-2), Blowjob(1-3), Tongue(Kiss Dick/Move1/Move2). Cum Blowjob/Cum Face/Cum Handjob theo nhóm. ✅

---

## 11. Story — Opening (CG)

Assets: `Assets/Image/CG/Opening Scene`. Quy ước GDD: chữ nghiêng = dẫn truyện; nghiêng-mở-ngoặc = thiết lập (VD `(+2 Love points)`, `(Màn hình đen)`); "Option:" = dừng chờ lựa chọn.

**Tóm tắt Opening:** Player về nhà, ăn tối cùng Mai (vợ). Có 2 điểm lựa chọn:
1. Khi Mai đút đồ ăn: **"Surprise/Bất ngờ"** (+2 Love) vs **"What is that?/Em làm gì vậy?"** (−5 Love).
2. Sáng hôm sau trước khi đi làm: **"Kiss Mai/Hôn Mai"** (+10 Love) vs **"Say Goodbye/Chào tạm biệt"** (−5 Love).

**Flow:** Ăn tối → tâm sự (kỷ niệm cấp 3, cưới 9 tháng) → `(Màn hình đen)` → **gameplay Missionary** (sex đầu game) → cảnh nằm cạnh Mai → Mai đề cập chuyện sinh con (Player hứa sẽ ổn định tài chính) → `(Màn hình đen, sáng)` → Day 1, 7:00 A.M, Home → lựa chọn kiss/goodbye → `(Inventory +Lunch)` → Day 1, 7:30 A.M, Company.
- Có đủ 3 ngôn ngữ EN/VN/JP trong GDD.
- Sprite CG: `Opening_1` … `Opening_37`.

---

## 12. Events (scripted) ❌

5-6 event, unlock theo điều kiện. Hiện GDD phần lớn mới có **điều kiện + tóm tắt kịch bản**, thoại chi tiết chưa viết.

| Event | Điều kiện | Ghi chú |
|---|---|---|
| **Lần đầu chơi Anal** | Love LV≥2, Sensitive Butthole >100 | |
| **Tập Gym** | Love LV≥2, Max Energy >200, đã tặng Gym Outfit | |
| **Đi Biển** | Love LV≥3, Lewd ≥2, đã tặng Bikini | |
| **Họp lớp** | Love LV≥3, Lewd ≥4 | |
| **Kỉ Niệm Ngày Cưới** | Love LV≥5, Lewd ≥5, đã tặng Sexy Sleepwear | |
| **Siêu nứng** | Chọn "I'm tired" lần thứ 3 khi Libido=Craving, Lewd ≥3 | Thoại Mai còn trống |
| **Ngồi Lên Mặt** | Sensitive Butthole >800, Lewd ≥3, Love LV≥3 | Cảnh Face Sitting |

> Scene Event trong menu Scene (§3) hiển thị 5 event → cần chốt danh sách event chính thức đưa vào gallery.

---

## 13. Endings (4) ❌

Kích hoạt bởi số lần **đạt/trượt Mai's Satisfaction** cộng dồn + Love Level + (một số) Work Level.

| Ending | Điều kiện | Tóm tắt |
|---|---|---|
| **1. Divorce** | Trượt Satisfaction lần thứ **20**, Love LV 1/2/3 | Mai ly dị. |
| **2. Cheating** | Trượt Satisfaction lần thứ **20**, Love LV 4/5 | Mai ngoại tình. |
| **3. Happy Marriage** | Đạt Satisfaction lần thứ **30**, Lewd 1/2/3, Love LV5, Work LV10 | Mua nhà nhỏ, sống hạnh phúc có con. |
| **4. Happy Marriage?** | Đạt Satisfaction lần thứ **40**, Lewd 5, Love LV5, Work LV10 | Sinh con, tiệc đầy tháng, Mai muốn đứa thứ 2. |

> **Ngoài ra:** Pregnancy rate = 100% → cũng chuyển thẳng Ending (§6.4) — cần chốt Ending nào ứng với pregnancy.
> ⚠️ Endings 3&4 yêu cầu **Work Level 10** nhưng Work Level max = 5 (§9.2). Cần user chốt.

---

## 14. Open Questions (cần user quyết định)

Đánh dấu chỗ GDD mâu thuẫn / thiếu, để bóc task chính xác:

1. **Work Level max:** GDD Work định nghĩa LV1-5, nhưng Ending yêu cầu **LV10**. Chốt max mấy cấp?
2. **Energy vs Stamina:** GDD dùng lẫn 2 tên cho cùng "giới hạn Energy". Thống nhất tên nào trong code?
3. **Pregnancy → Ending:** 100% pregnancy chuyển tới Ending nào? Có Ending "sinh con ngoài ý muốn" riêng không?
4. **Talk content LV2-4:** thoại 3 sub-topic mỗi level còn trống. Ai viết? Có localize 3 ngôn ngữ?
5. **Ask her about her day:** GDD hứa 10 câu, mới có 5 (và EN chỉ 3). Bổ sung đủ không?
6. **Sex Scene chưa impl:** Doggy Style, Spooning, Mating Press, Full Nelson (code chỉ có Missionary/Cowgirl/4 Roleplay). Ưu tiên cảnh nào trước?
7. **Skill mapping:** GDD định nghĩa Sex Skill 1 (4 skill) + Sex Skill 2 (4 skill) = 8, cần map với 8 skill trong `SkillManager`/`DefaultSettings` (Cooking/Cleaning/Massage...?) — có thể là 2 hệ khác nhau.
8. **Sex Points vs SkillPoint:** GDD dùng "Sex Points" nâng Sex Skill; code có `SkillPoint` (PlayerPrefs). Cùng 1 resource hay tách?
9. **Save/Load & Scene Gallery:** chưa có; cần thiết kế schema save (day/time/stats/unlock flags).
10. **Mai Clothes:** panel trang phục chưa có trong code; Gift outfit hiện chỉ +Love — có gắn với hệ mặc đồ không?

---

## Phụ lục A — Bảng giải mã link "Private (ClickUp)"

Các ID ClickUp xuất hiện trong GDD gốc, ánh xạ sang section:
| ID | Nội dung |
|---|---|
| `-1118` | New Game flow (CG Opening) |
| `-338` | Mai Profile / Relationship / Body Status |
| `-318` | Place |
| `-358` / `-378` | Player Skill / Profile |
| `-398` | Sex Skill 1 |
| `-418` | Sex Skill 2 |
| `-298` | Talk / Action with Mai |
| `-458` | Sex Scene Gameplay |
| `-578` | Gift |
| `-598` | Talk |
| `-618` | Sex action |
| `-638` | Eat |
| `-1218` | Hiep Mart items |
| `-1398` | Insert/Roleplay Button panel |
| `-1418` | Orgasm bar |
| `-1438` | Cumming bar |
| `-1458` | Stamina bar |
| `-1478` | Số lần Cum |
| `-1498` | Finish / Satisfaction |
| `-1598/-1618/-1638` | Roleplay Pussy/Butthole/Paizuri |
| `-1658` | Roleplay Action bar |
| `-1678` | Roleplay Button (cam đánh số) |
| `-1698` | Blowjob |
| `-1938` | Ask her about her day |
| `-1998` | Libido / Relationship Status |
| `-2018` | Mai Body Status |
| `-1018/-1038/-1778/-1838` | Talk topic theo Love Level 1-4 |

## Phụ lục B — Bảng thuật ngữ chuẩn hóa

| Thuật ngữ | Định nghĩa ngắn |
|---|---|
| **Love Points / Level** | Điểm/cấp tình cảm Mai (LV1-5). |
| **Libido** | Trạng thái ham muốn Mai (5 mức theo ngày không sex). |
| **Lewd Level** | Cấp dâm (1-5) theo Sensitive Points 4 bộ phận. |
| **Sensitive Level / Points** | Cấp nhạy cảm từng bộ phận (1-10) & điểm tích lũy. |
| **Sex Points** | Điểm sau mỗi đêm, nâng Sex Skill / unlock Scene. |
| **Orgasm bar** | Thanh khoái cảm Mai; đầy → 1 Orgasm. |
| **Cumming bar** | Thanh xuất tinh Player. |
| **Stamina bar** | Thể lực Player/đêm. |
| **Mai's Satisfaction** | Điều kiện thỏa mãn/đêm (Orgasm ≥ ngưỡng theo Lewd). |
| **Work Level / Progress** | Cấp & tiến độ công việc (nhân thưởng). |
| **Pregnancy rate** | Tỷ lệ có thai (Cum Inside no-condom +20%/lần). |

