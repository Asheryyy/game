# Chung cư cũ – Tòa B

Game 3D góc nhìn thứ nhất thể loại **kinh dị + trinh thám**, bối cảnh một khu chung cư cũ ở Việt Nam. Bạn vào vai một người trẻ vừa ra trường, thuê phòng 303 tòa B. Tiền nước cao bất thường, có tiếng gõ tường lúc nửa đêm, có bóng ma ngoài hành lang, và phòng 302 bên cạnh được cho là bỏ hoang… Hãy tìm ra sự thật.

- Engine: **Unity 6 (6000.2.7f2)**, Universal Render Pipeline
- Ngôn ngữ trong game: tiếng Việt
- Có **3 kết thúc** (1 Bad End, 2 Good End)

---

## 1. Cần cài trước

| Phần mềm | Ghi chú |
|---|---|
| [Git](https://git-scm.com/) | |
| [Git LFS](https://git-lfs.com/) | **Bắt buộc.** Model, texture, âm thanh được lưu bằng Git LFS |
| [Unity Hub](https://unity.com/download) + **Unity 6000.2.7f2** | Nên dùng đúng bản này. Bản khác vẫn mở được nhưng dễ lỗi vặt |

> ⚠️ Nếu clone khi **chưa cài Git LFS**, các file `.fbx`, `.png`, `.wav`… chỉ là file text trỏ tới LFS → mở game lên sẽ mất hết model và texture.

## 2. Tải project về

```bash
git lfs install          # chỉ cần chạy 1 lần trên mỗi máy
git clone https://github.com/Asheryyy/game.git
cd game
git lfs pull             # chắc chắn đã tải đủ file lớn
```

Repo khá nặng (khoảng 1 GB file LFS), nên lần clone đầu sẽ mất một lúc.

**Lỡ clone trước khi cài LFS?** Cài Git LFS rồi chạy trong thư mục project:

```bash
git lfs install
git lfs pull
```

## 3. Mở trong Unity

1. Mở **Unity Hub** → **Add** → **Add project from disk** → chọn thư mục `game` vừa clone.
2. Chọn bản Unity **6000.2.7f2** rồi mở.
3. **Lần mở đầu tiên sẽ lâu** (có thể 10–30 phút), vì Unity phải import lại toàn bộ asset. Thư mục `Library` không được đưa lên git, đây là chuyện bình thường.
4. Nếu Unity báo lỗi package, vào **Window → Package Manager** và đợi nó tải xong các package (Cinemachine, Timeline, Input System, URP…).

## 4. Chơi thử

1. Mở scene **`Assets/Scenes/MainMenu.unity`**.
2. Bấm **Play** ▶.
3. Chọn **CHƠI MỚI** để bắt đầu từ đầu, hoặc **TIẾP TỤC** nếu đã có file lưu. Ngoài ra có **CHỌN CHƯƠNG** và **CÀI ĐẶT** (âm lượng, chất lượng, độ phân giải…).

> Toàn bộ gameplay nằm trong `Assets/Scenes/SampleScene.unity`. Mở thẳng scene này cũng chơi được, nhưng nên vào từ MainMenu để chạy đúng phần mở đầu.

**Build ra file chạy:** **File → Build Profiles** (hoặc Build Settings) → kiểm tra thứ tự scene là `MainMenu` rồi `SampleScene` → **Build**.

## 5. Điều khiển

| Phím | Chức năng |
|---|---|
| **W A S D** | Di chuyển |
| **Chuột** | Nhìn xung quanh |
| **Shift** | Chạy |
| **C** | Ngồi / cúi người |
| **Space** | Nhảy |
| **E** | Tương tác: xem đồ vật, đọc giấy, mở cửa, nói chuyện, giao hàng… (có chỗ phải **giữ E** hoặc **bấm E liên tục**) |
| **1 / 2 / 3** hoặc chuột | Chọn khi có bảng lựa chọn |
| **Esc** | Tạm dừng / lưu game / cài đặt |

Game **tự lưu** mỗi khi nhận nhiệm vụ mới. Có thể lưu tay trong menu tạm dừng, trừ lúc đang hội thoại hoặc đang bị truy đuổi.

## 6. Cấu trúc thư mục chính

```
Assets/
├── Scenes/            MainMenu.unity, SampleScene.unity (toàn bộ gameplay)
├── *.cs               Script game: Quest, Dialogue, Clue, Save, Cutscene, ChoiceEvent, ChaserEnemy...
├── Editor/            Tool trong menu Tools/ để dựng cutscene, Good End, animator...
├── Cutscene_*.playable  Timeline các cutscene (BadEnd1, BadEnd2, ChayVeNha, GoodEnd)
├── AssetChungCu/      Model khu chung cư, hành lang, đồng hồ nước (export từ Blender)
├── NV*/, 3giangho/, nhanvatchinh/   Nhân vật (NPC, giang hồ, nhân vật chính)
├── AnimationSource/   Animation (đánh nhau, ngã, tập võ...)
└── Audio/             Âm thanh tự làm (gõ tường, bước chân, tiếng hét...)
BlenderSource/         File nguồn export từ Blender (.fbx, texture). Unity không dùng thư mục này
```

- Thoại, nhiệm vụ, manh mối được điền trong **Inspector** của `SampleScene`, không nằm trong code.
- Các tool trong menu **Tools/** chỉ dùng khi chỉnh sửa project. Người chơi không cần đụng tới.

## 7. Những thứ KHÔNG có trong repo

| File | Lý do |
|---|---|
| `BlenderSource/*.blend`, `*.blend1` | File gốc Blender nặng 336 MB, vượt giới hạn 100 MB/file của GitHub. Game chỉ dùng file `.fbx` đã export nên **không ảnh hưởng gì tới game**. Muốn sửa model gốc thì xin file `.blend` từ tác giả |
| `Library/`, `Temp/`, `Logs/`, `UserSettings/` | Unity tự tạo lại khi mở project |
| `Assets/_Recovery/` | Scene Unity tự khôi phục sau khi crash, không phải scene của game |

## 8. Gặp lỗi?

| Hiện tượng | Cách sửa |
|---|---|
| Model trắng/hồng, texture mất, Console báo file `.fbx` lỗi | Chưa tải LFS → chạy `git lfs install` rồi `git lfs pull` |
| Vật thể màu **hồng** | Sai render pipeline: kiểm tra **Edit → Project Settings → Graphics** đang dùng URP, hoặc mở đúng bản Unity 6000.2.7f2 |
| Chữ tiếng Việt bị ô vuông | Font TMP chưa import xong. Đợi Unity import xong rồi mở lại scene |
| Bấm Play không điều khiển được | Hãy vào từ scene `MainMenu`. Kiểm tra **Project Settings → Player → Active Input Handling** là *Input System Package* hoặc *Both* |
| "TIẾP TỤC" vào sai chỗ | File lưu cũ không khớp bản mới. Chọn **CHƠI MỚI**, hoặc xóa file `save.json` trong `%USERPROFILE%\AppData\LocalLow\Asheryyy\ChungCuCu\` (Windows) |

---

<details>
<summary><b>⚠️ Spoiler: các kết thúc</b></summary>

- **Bad End:** tin nhầm người, hoặc bị bọn giang hồ bắt lại.
- **Good End – Khống chế:** đem theo con dao tìm được trong phòng 302 và đánh thắng tên giang hồ.
- **Good End – "Im lặng là vàng":** để ý những mảnh giấy người lạ để lại sau khi bạn bị đánh ngất, và chọn **tin tưởng** người đó.

</details>
