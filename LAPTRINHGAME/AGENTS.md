# 🐱🐭 CATandMOUSE - HỒ SƠ KIẾN TRÚC DỰ ÁN & KIM CHỈ NAM PHÁT TRIỂN (AGENTS.md)

Tài liệu này được lập ra nhằm lưu trữ trọn vẹn kiến trúc hệ thống, quy chuẩn kỹ thuật và định hướng thiết kế của dự án game **CATandMOUSE** (Thể loại Trốn tìm / Ngụy trang - Prop Hunt 2D). Bất kỳ AI Agent hoặc Developer nào tiếp quản dự án đều **BẮT BUỘC** đọc kỹ và tuân thủ tuyệt đối các nguyên tắc trong tài liệu này.

---

## 1. TỔNG QUAN DỰ ÁN (PROJECT OVERVIEW)

* **Tên Game**: `CATandMOUSE`
* **Thể loại**: 2D Top-down Stealth, Prop Hunt, Party Game (Retro-Modern Indie Style).
* **Cảm hứng nghệ thuật**: *Hotline Miami*, *Enter the Gungeon*, *Garry's Mod Prop Hunt*.
* **Nền tảng mục tiêu**: PC / Mac (Unity 6 - Phiên bản `6000.6.4f1` hoặc mới hơn).
* **Render Pipeline**: URP 2D (Universal Render Pipeline 2D).
* **Mục tiêu cốt lõi**:
  * Trò chơi 2 người chơi đối kháng trên cùng bàn phím (Local Split Input) trong Assignment 1, sẵn sàng module hóa sang Multiplayer Network (Netcode for GameObjects / Mirror) trong Assignment 2.
  * Chuột (Mouse) phải ẩn náu, biến hình ngụy trang vào các đồ vật nhà kho để sống sót trong 300 giây.
  * Mèo (Cat) phải tìm kiếm và tát trúng Chuột trước khi hết giờ. Nếu tát nhầm đồ thật sẽ bị choáng (Stun).

---

## 2. BỐI CẢNH CÔNG NGHỆ & THƯ VIỆN CỐT LÕI (CORE TECH STACK)

| Thư viện / Công nghệ | Namespace | Mục đích & Quy định sử dụng |
| :--- | :--- | :--- |
| **UniTask** | `Cysharp.Threading.Tasks` | **BẮT BUỘC 100%**: Tất cả Timer, Cooldown, Delay, Async State Loops phải dùng `async UniTaskVoid` hoặc `async UniTask`. **TUYỆT ĐỐI KHÔNG DÙNG Coroutine (`IEnumerator`)**. Mọi tác vụ bất đồng bộ phải truyền `CancellationToken` thông qua `this.GetCancellationTokenOnDestroy()`. |
| **DOTween** | `DG.Tweening` | **BẮT BUỘC**: Dùng cho toàn bộ hiệu ứng nảy co giãn (`DOPunchScale`), rung chấn màn hình & choáng (`DOShakePosition`), hiệu ứng khói phóng to/thu nhỏ khi biến hình. |
| **Cinemachine** | `Unity.Cinemachine` | Dùng `CinemachineCamera` 2D cận cảnh (`OrthographicSize = 3.8f`), theo dõi nhân vật với `Lookahead Time = 0.15f`, `Damping = (0.5f, 0.5f, 0f)` qua `CinemachinePositionComposer`, kết hợp kẹp viền bản đồ `CinemachineConfiner2D` không để lộ viền đen vô cực. Hỗ trợ phím **TAB** đổi mục tiêu theo dõi Mèo ⇄ Chuột. |
| **URP 2D Lighting** | `UnityEngine.Rendering.Universal` | Hệ thống chiếu sáng 2D đa nguồn: `Light2D` (Global Light, Point Light 2D, Freeform Light 2D) kết hợp đổ bóng thời gian thực `ShadowCaster2D`. |
| **URP Volume** | `UnityEngine.Rendering` | Hiệu ứng hậu kỳ điện ảnh: `Bloom`, `Vignette`, `ColorAdjustments` (Post Exposure, Contrast). |

---

## 3. KIẾN TRÚC MÃ NGUỒN CỐT LÕI (CORE SCRIPTS)

### 3.1. `PropItem.cs` ([`Assets/Scripts/PropItem.cs`](file:///c:/Users/ADMIN/Downloads/LAPTRINHGAME/CATandMOUSE/Assets/Scripts/PropItem.cs))
* **Trách nhiệm**: Quản lý thuộc tính và trạng thái của đồ vật trên bản đồ.
* **Biến chính**:
  * `string propId`: Định danh phân loại (Pallet, Crates, Barrels, Toolbox,...).
  * `Sprite propSprite`: Hình ảnh đại diện của vật thể.
  * `Color propColor`: Màu sắc của vật thể (mặc định trắng cho Pixel Art).
  * `Vector2 customColliderSize`: Kích thước Collider tương ứng để Chuột mượn khi biến hình.
  * `bool isFake`: `false` = đồ thật của màn chơi; `true` = Chuột đang ngụy trang thành.
* **Hành vi**:
  * `OnHitByCat()`: Gọi khi Mèo tấn công trúng. Dùng DOTween `transform.DOPunchScale(Vector3.one * 0.25f, 0.25f, 10, 1f)` tạo hiệu ứng va đập vật lý nảy bật sống động.

### 3.2. `MouseController.cs` ([`Assets/Scripts/MouseController.cs`](file:///c:/Users/ADMIN/Downloads/LAPTRINHGAME/CATandMOUSE/Assets/Scripts/MouseController.cs))
* **Trách nhiệm**: Điều khiển Chuột, cơ chế ẩn nấp và ngụy trang.
* **Phím điều khiển**:
  * Di chuyển: Phím **W - A - S - D**.
  * Ngụy trang / Hoàn nguyên: Phím **Space**.
* **Cơ chế hoạt động**:
  * Tự động quét tìm `PropItem` gần nhất trong bán kính tương tác (`disguiseRadius = 2.0f`).
  * Khi bấm `Space`:
    * Nếu đang là Chuột: Biến hình thành Prop gần nhất (thay đổi `SpriteRenderer.sprite`, đổi kích thước `BoxCollider2D.size`, co giãn Scale `transform.DOScale`). Kích hoạt cooldown 1.5s bằng `UniTask.Delay(..., cancellationToken)`.
    * Nếu đang ngụy trang: Hoàn nguyên trở lại hình dạng Chuột gốc (`originalSprite`, scale nhỏ `0.8f`).
  * Có cờ `isDisguised` để thông báo trạng thái.
  * Xử lý trạng thái khi bị Mèo bắt (`OnCaught()`): Tắt collider, phát nổ tween nhỏ, ngắt điều khiển.

### 3.3. `CatController.cs` ([`Assets/Scripts/CatController.cs`](file:///c:/Users/ADMIN/Downloads/LAPTRINHGAME/CATandMOUSE/Assets/Scripts/CatController.cs))
* **Trách nhiệm**: Điều khiển Mèo, cơ chế tấn công và trừng phạt khi tát nhầm.
* **Phím điều khiển**:
  * Di chuyển: **4 Phím Mũi Tên (Up, Down, Left, Right)**.
  * Tấn công (Cào/Tát): Phím **J** hoặc **Click Chuột Trái**.
* **Cơ chế hoạt động**:
  * Khi nhấn tấn công: Bật animation vỗ móng vuốt, quét va chạm `Physics2D.OverlapCircleAll` tại điểm tấn công trước mặt (`attackRange = 1.2f`).
  * **Trường hợp 1 - Tát trúng Chuột (`MouseController` hoặc `PropItem.isFake == true`)**:
    * Kích hoạt chiến thắng cho Mèo qua `GameManager.Instance.OnCatWon()`.
  * **Trường hợp 2 - Tát trúng Đồ thật (`PropItem.isFake == false`)**:
    * Gọi `prop.OnHitByCat()`.
    * **Phạt Mèo bị Stun 1.5s**: Khóa di chuyển bằng cờ `isStunned`, kích hoạt hiệu ứng rung lắc chóng mặt bằng DOTween `spriteRenderer.transform.DOShakePosition(1.5f, strength: 0.3f, vibrato: 20)`, đếm ngược bằng `UniTask.Delay(1500, cancellationToken)`.
  * **Trường hợp 3 - Tát vào không khí**: Cooldown nhẹ 0.5s bằng UniTask.

### 3.4. `GameManager.cs` ([`Assets/Scripts/GameManager.cs`](file:///c:/Users/ADMIN/Downloads/LAPTRINHGAME/CATandMOUSE/Assets/Scripts/GameManager.cs))
* **Trách nhiệm**: Quản lý vòng đời trận đấu (Game Lifecycle), đếm ngược và trạng thái thắng/thua.
* **Vòng lặp chính**:
  * Trạng thái trận đấu (`GameState`): `Preparing` -> `Playing` -> `CatWon` / `MouseWon`.
  * Đếm ngược 300 giây thời gian thực bằng `UniTask.Delay(1000, cancellationToken: destroyCts.Token)`.
  * Nếu hết 300 giây mà Chuột chưa bị bắt -> Chuột thắng (`OnMouseWon()`).
* **Fallback UI**: Tích hợp sẵn `OnGUI()` tự động hiển thị:
  * HUD thanh thời gian đếm ngược (Timer Bar).
  * Thông báo hướng dẫn phím bấm cho người chơi.
  * Màn hình Game Over Popup kèm nút "Chơi lại" (Restart) tự động nạp lại màn chơi.

### 3.5. `CatAIController.cs` ([`Assets/Scripts/CatAIController.cs`](file:///c:/Users/ADMIN/Downloads/LAPTRINHGAME/CATandMOUSE/Assets/Scripts/CatAIController.cs))
* **Trách nhiệm**: Trí tuệ nhân tạo điều khiển Mèo cho chế độ Chơi Đơn Offline (Player vs Bot).
* **Máy trạng thái hành vi (Finite State Machine)**:
  * **Patrol (Tuần tra)**: Di chuyển tuần tự qua mạng lưới 18 Waypoints trải dài khắp 5 phân khu nhà kho, đảm bảo không bỏ sót bất kỳ góc phòng nào.
  * **Investigate (Điều tra)**: Khi tuần tra ngang qua đồ vật khả nghi hoặc có biến động, Bot dừng lại ngửi và quan sát trong 1.5 - 2.5s.
  * **Chase (Rượt đuổi)**: Phát hiện Chuột di chuyển trong bán kính 4.5m hoặc Chuột chưa biến hình trong bán kính 6.0m. Nếu Chuột ngụy trang đứng yên gần đó, mức độ nghi ngờ (suspicion) tăng dần theo thời gian quan sát.
  * **Attack (Tấn công)**: Khi áp sát Chuột trong cự ly `<= 1.2m`, Bot lập tức tung đòn cào tát vồ mồi (`TriggerAttack`).

### 3.6. `LanNetworkManager.cs` ([`Assets/Scripts/LanNetworkManager.cs`](file:///c:/Users/ADMIN/Downloads/LAPTRINHGAME/CATandMOUSE/Assets/Scripts/LanNetworkManager.cs))
* **Trách nhiệm**: Kết nối Multiplayer 2 máy tính qua mạng LAN / Wi-Fi nội bộ bằng giao thức C# TCP Socket thuần túy (`TcpListener` / `TcpClient`), loại bỏ 100% phụ thuộc vào các package mạng phức tạp bên ngoài.
* **Cơ chế hoạt động**:
  * **Máy 1 (Host - Đóng vai Mèo)**: Lắng nghe trên cổng `7777`, tự động phát hiện IP máy trong mạng nội bộ (`192.168.x.x`). Camera cố định khóa chặt theo dõi Mèo. Chuột trên máy Host được điều khiển từ xa qua mạng (`isNetworkRemote = true`).
  * **Máy 2 (Client - Đóng vai Chuột)**: Nhập IP máy Host để kết nối. Camera cố định khóa chặt theo dõi Chuột. Mèo trên máy Client được điều khiển từ xa qua mạng.
  * **Bảo mật tầm nhìn Prop Hunt**: Đảm bảo 2 màn hình hoàn toàn độc lập, người chơi Mèo không thể nhìn lén màn hình vị trí ngụy trang của Chuột.
  * **Đồng bộ hóa 20Hz (50ms tick)**: Đồng bộ mượt mà tọa độ, hướng quay, trạng thái ngụy trang/hoàn nguyên, hành động cào tát, trạng thái choáng (Stun), thời gian đếm ngược và kết quả thắng/thua.

### 3.7. `GameModeManager.cs` ([`Assets/Scripts/GameModeManager.cs`](file:///c:/Users/ADMIN/Downloads/LAPTRINHGAME/CATandMOUSE/Assets/Scripts/GameModeManager.cs))
* **Trách nhiệm**: Menu chọn chế độ chơi phong cách Neon Retro Hotline Miami khi khởi động game:
  * **[Nút 1] CHƠI ĐƠN OFFLINE (VS BOT MÈO)**: Người chơi vào vai Chuột, tự động kích hoạt `CatAIController` để Bot Mèo tuần tra săn lùng.
  * **[Nút 2] CHƠI ONLINE / MẠNG LAN (2 MÁY)**: Mở bảng thiết lập Host (Làm Mèo) hoặc Client (Làm Chuột), hiển thị IP nội bộ và ô nhập IP kết nối.
  * **[Nút 3] CHƠI 2 NGƯỜI CỤC BỘ (1 BÀN PHÍM)**: Chế độ cổ điển 2 người chung phím (WASD cho Chuột, Phím mũi tên cho Mèo).

### 3.8. `ColliderGizmoVisualizer.cs` ([`Assets/Scripts/ColliderGizmoVisualizer.cs`](file:///c:/Users/ADMIN/Downloads/LAPTRINHGAME/CATandMOUSE/Assets/Scripts/ColliderGizmoVisualizer.cs))
* **Trách nhiệm**: Hỗ trợ nhà phát triển debug trực quan trong Unity Scene View:
  * Vẽ khung dây đỏ (`Gizmos.color = Color.red`) cho toàn bộ tường vật lý BoxCollider2D.
  * Vẽ khung dây xanh lục cho Triggers.
  * Vẽ khung dây vàng cho nhân vật.
  * Giúp phát hiện ngay lập tức mọi vật cản vô hình hoặc khe hẹp không đạt chuẩn.

### 3.9. `UIManager.cs` ([`Assets/Scripts/UIManager.cs`](file:///c:/Users/ADMIN/Downloads/LAPTRINHGAME/CATandMOUSE/Assets/Scripts/UIManager.cs))
* **Trách nhiệm**: Singleton quản trị và điều phối toàn bộ hệ thống giao diện UGUI phong cách Hoạt hình Chunky Among Us / Brawl Stars 3D.
* **Cơ chế hoạt động**:
  * Đọc dữ liệu thời gian thực từ `GameManager`, `MouseController`, và `CatController` để truyền tới các View tương ứng (`HUDView`, `VictoryModal`, `ModeMenu`).
  * Khởi tạo font chữ hệ thống dự phòng thông minh (`LegacyRuntime.ttf` / `Arial.ttf`).
  * Ẩn toàn bộ giao diện OnGUI cũ khi hệ thống UGUI hoạt động.

### 3.10. `InGameHUDView.cs` ([`Assets/Scripts/InGameHUDView.cs`](file:///c:/Users/ADMIN/Downloads/LAPTRINHGAME/CATandMOUSE/Assets/Scripts/InGameHUDView.cs))
* **Trách nhiệm**: Điều khiển HUD trong trận đấu phong cách đồ họa nổi khối tương phản cao:
  * **Đồng hồ đếm ngược (Top Center)**: Đặt trong khung viên thuốc viền đen dày nổi bật (`UI_AmongUs_Timer_Pill.png`), hiển thị chữ số `MM:SS` to rõ trắng viền đen. Khi thời gian còn `<= 60 giây`, chữ số tự động chuyển sang đỏ Neon (`#EF4444`) và kích hoạt vòng lặp phóng to thu nhỏ nhịp nhàng qua DOTween (`DOScale(1.15f, 0.5f).SetLoops(-1, LoopType.Yoyo)`).
  * **Nút bánh răng Cài đặt [⚙] (Top Left)**: Thay thế hoàn toàn bảng chữ hướng dẫn dài dòng cũ bằng nút bánh răng tròn 3D gọn gàng. Khi click sẽ mở popup hộp thoại `Settings_Popup` phong cách Among Us hướng dẫn chi tiết phím bấm.
  * **2 Nút kỹ năng to gấp đôi (Bottom Right)**:
    - **Kỹ năng Chuột [SPACE]**: Kích thước 140x140 cực lớn, viền đen dày 6px, nền tròn Cyan 3D, lớp phủ tròn 360 độ (Radial 360 Fill) hiển thị hồi chiêu 1.5s, nảy `DOPunchScale` khi sẵn sàng.
    - **Kỹ năng Mèo [J]**: Kích thước 140x140 cực lớn, viền đen dày 6px, nền tròn Đỏ tươi 3D, hiển thị trạng thái "TÁT / BẮT", thời gian Choáng Stun `1.5s` kèm hiệu ứng cảnh báo.

### 3.11. `VictoryModalView.cs` ([`Assets/Scripts/VictoryModalView.cs`](file:///c:/Users/ADMIN/Downloads/LAPTRINHGAME/CATandMOUSE/Assets/Scripts/VictoryModalView.cs))
* **Trách nhiệm**: Popup thông báo kết quả trận đấu phong cách Among Us:
  * Màn mờ đen phủ toàn màn hình (Dimmer Fade-in qua `DOFade(0.88f)`).
  * Khung hộp thoại bo tròn `UI_AmongUs_Panel.png` có **viền trắng dày 8px** + viền đen ngoài 4px, trượt nảy đàn hồi (`Ease.OutBack`, `DOScale(1.0f)`).
  * Tiêu đề to rõ: `👑 CATS VICTORY!` (Vàng Chanh `#FFE600`, viền kép 3.5px và bóng đổ 6px) hoặc `🧀 MICE ESCAPED!` (Cyber Cyan `#00D2D3`).
  * 2 Nút bấm 3D nổi khối: `▶ CHƠI LẠI` (Nút Vàng Cam Among Us) và `🏠 VỀ MENU CHÍNH` (Nút Xanh Cyan Among Us).

### 3.12. `GameModeMenuView.cs` ([`Assets/Scripts/GameModeMenuView.cs`](file:///c:/Users/ADMIN/Downloads/LAPTRINHGAME/CATandMOUSE/Assets/Scripts/GameModeMenuView.cs))
* **Trách nhiệm**: Menu chọn chế độ chơi và Cài đặt / Chọn Map phong cách Cyber-Casual 2026:
  * Khung nền: Thẻ kính mờ Frosted Glass Panel (`UI_Frosted_Glass_Card.png`) màu xanh navy đậm (`#0F172A`), viền bo tròn góc lớn có đường viền dạ quang trắng/cyan phát sáng.
  * Tiêu đề game: **"CAT & MOUSE"** viết hoa toàn bộ, font chữ cực dày, màu Vàng Chanh rực rỡ (`#FFE600`), viền đen kép dày 3.5px và bóng đổ đen underlay (0, -7px).
  * 3 Nút bấm dạng viên thuốc 3D bóng bẩy (320x140) chia 2 tầng màu kèm viền đen dày 6px:
    - `▶ CHƠI ĐƠN (VS BOT MÈO)`: Nút Vàng Cam (`#FFAA00` / `#D47A00`).
    - `🌐 CHƠI ONLINE / LAN`: Nút Xanh Cyan (`#00D2D3` / `#009999`).
    - `⚙ CÀI ĐẶT & CHỌN MAP`: Nút Tím Pastel (`#A855F7` / `#7E22CE`).
  * **Bảng Cài đặt & Chọn Map**: Cho phép chuyển đổi linh hoạt `🏢 MAP: PENTHOUSE HIỆN ĐẠI` ⇄ `📦 MAP: NHÀ KHO CŨ`, kèm nút chơi 2 người cục bộ.
  * Bảng thiết lập LAN Lobby phong cách Among Us Panel hiển thị IP máy, ô nhập IP Host, nút Tạo Host và nút Vào vai Chuột.

### 3.13. `UIButtonJuice.cs` ([`Assets/Scripts/UIButtonJuice.cs`](file:///c:/Users/ADMIN/Downloads/LAPTRINHGAME/CATandMOUSE/Assets/Scripts/UIButtonJuice.cs))
* **Trách nhiệm**: Hiệu ứng nút bấm đồ chơi xúc giác (Tactile Chunky Button Juice) phong cách cơ học thật:
  * Rê chuột vào (Hover): Phóng to nhẹ `1.08x` (`Ease.OutQuad`).
  * Nhấn chuột xuống (Press): Nút **thụt lún 4-5 pixel theo chiều dọc** (`anchoredPosition += (0, -5px)`) và co nhẹ `0.96x` tạo cảm giác phím cơ đồ chơi thật.
  * Nhả chuột (Release): Nút bật nảy tưng tưng trở lại vị trí gốc qua DOTween **`Ease.OutBounce`**.
  * Thiết lập `SetUpdate(true)` đảm bảo hoạt động mượt mà kể cả khi game bị pause.

### 3.14. `MapManager.cs` ([`Assets/Scripts/MapManager.cs`](file:///c:/Users/ADMIN/Downloads/LAPTRINHGAME/CATandMOUSE/Assets/Scripts/MapManager.cs))
* **Trách nhiệm**: Singleton quản trị và chuyển đổi luân phiên giữa 2 bản đồ:
  * `MapType.Penthouse` (Penthouse Hiện Đại) và `MapType.Warehouse` (Nhà Kho Cũ).
  * Bật/tắt các Container tương ứng (`Arena_Penthouse_Container` và `Arena_Warehouse_Container`).
  * Cập nhật tức thời lộ trình tuần tra Waypoint cho Bot Mèo (`CatAIController.SetMapWaypoints()`).
  * Lưu trữ cấu hình lựa chọn qua `PlayerPrefs` để ghi nhớ cho các trận đấu kế tiếp.
  * Tự động đặt lại vị trí xuất phát tối ưu cho Mèo và Chuột theo từng bản đồ.

### 3.15. `PenthouseArenaBuilder.cs` ([`Assets/Editor/PenthouseArenaBuilder.cs`](file:///c:/Users/ADMIN/Downloads/LAPTRINHGAME/CATandMOUSE/Assets/Editor/PenthouseArenaBuilder.cs))
* **Trách nhiệm**: Trình dựng tự động bản đồ Penthouse Hiện Đại 16:9:
  * Backdrop 32x18 units từ `Penthouse_Level_Map.jpg`.
  * Hệ thống Colliders 4 tường bao và vách ngăn phòng bên trong với các lối thông phòng mở rộng từ **2.2m đến 8.0m** (không bao giờ cấn mép).
  * Rải 11 đạo cụ nội thất hiện đại (`PropItem`) rải đều khắp các phòng: Sofa, Bàn cẩm thạch, Cây Monstera, Robot hút bụi, Ghế Scandinavian, Đèn ngủ, Ghế bar.
  * Tích hợp Gizmos xanh lá cây bán trong suốt debug trực quan.

---

## 4. HỆ THỐNG ĐẠO CỤ & CƠ CHẾ NGỤY TRANG (PROPS ASSET PACK)

Dự án sở hữu bộ đạo cụ công nghiệp 16-bit độc quyền tại `Assets/Resources/GeneratedSprites/Warehouse_Props_Pack.png` được tự động tách thành 8 loại Props chuẩn:

| Đạo cụ (Prop ID) | File Sprite | Kích thước Pixel | Đặc tính chiến thuật ngụy trang |
| :--- | :--- | :--- | :--- |
| **`Pixel_Prop_Pallet`** | `Pixel_Prop_Pallet.png` | 340 x 257 | Kệ gỗ dẹt, dễ nằm lẫn vào sàn gạch khu vực hành lang. |
| **`Pixel_Prop_Crates_Stack`** | `Pixel_Prop_Crates_Stack.png` | 491 x 355 | Chồng thùng gỗ chữ X lớn, cản tầm nhìn tốt nhưng kích thước lớn. |
| **`Pixel_Prop_Oil_Barrels`** | `Pixel_Prop_Oil_Barrels.png` | 422 x 319 | Cụm thùng phuy rỉ sét xanh đỏ, hòa hợp với khu vực ẩm thấp. |
| **`Pixel_Prop_Toolbox_Closed`**| `Pixel_Prop_Toolbox_Closed.png`| 205 x 131 | Hộp dụng cụ đóng, kích thước siêu nhỏ, rất khó bị phát hiện. |
| **`Pixel_Prop_Toolbox_Open`** | `Pixel_Prop_Toolbox_Open.png` | 237 x 217 | Hộp dụng cụ mở có cờ lê, chi tiết phức tạp đánh lừa thị giác. |
| **`Pixel_Prop_Gears`** | `Pixel_Prop_Gears.png` | 299 x 220 | Cụm bánh răng cơ khí, thường đặt cạnh máy móc khu xưởng. |
| **`Pixel_Prop_Safety_Cones`** | `Pixel_Prop_Safety_Cones.png` | 243 x 225 | Cọc tiêu vàng cảnh báo, đặt ở các khúc ngoặt băng chuyền. |
| **`Pixel_Prop_Wrench_Tools`** | `Pixel_Prop_Wrench_Tools.png` | 281 x 205 | Giá cờ lê mỏ lết, đặt áp sát chân tường phòng điều khiển. |

> *Ngoài ra còn có 3 Props cơ sở: `Pixel_Prop_Crate.png`, `Pixel_Prop_Barrel_Blue.png`, `Pixel_Prop_Barrel_Yellow.png`.*

---

## 5. ĐẤU TRƯỜNG NHÀ KHO HOTLINE MIAMI 16:9 & HỆ THỐNG VẬT LÝ

Bản đồ hoàn chỉnh được chuẩn hóa theo tỷ lệ 16:9 (Quy chuẩn kích thước bản đồ: Rộng 32 units, Cao 18 units, Tâm tại (0,0)).

### 5.1. Phân khu chức năng liên thông:
1. **Sảnh phân loại hàng hóa (Sorting Area)**: Trung tâm bản đồ, sàn gạch vàng in chữ `SORTING AREA`, có 3 băng chuyền lớn cản trở đường chạy thẳng.
2. **Kho cơ khí & Máy móc nặng (Heavy Storage - Bên trái)**: Không gian chật hẹp, nhiều góc khuất và máy ép cơ khí.
3. **Phòng điều hành & Máy chủ (Control Room - Góc trên trái)**: Dàn máy tính hacker phát sáng xanh ngọc Cyber-Cyan.
4. **Phòng quản lý (Office Room - Góc trên phải)**: Thảm len, bàn làm việc và các ống tản nhiệt.
5. **Phòng giải lao (Break Room - Góc dưới phải)**: Máy bán nước, bàn ăn và tủ cá nhân kim loại.
6. **Vũng rò rỉ hóa chất (Toxic Puddle - Hành lang giữa)**: Vệt chất lỏng xanh lục phát huỳnh quang, mở toang thẳng xuống Sorting Area.

### 5.2. Hệ thống Colliders vật lý chuẩn xác 100% (`Environment_Colliders`):
* **Tường bao ngoài 4 góc (Perimeter Walls - Bao trọn map 32x18)**: 
  - `Wall_Outer_Top`: Center `(0, 8.8f)`, Size `(32f, 1.2f)`.
  - `Wall_Outer_Bottom`: Center `(0, -8.8f)`, Size `(32f, 1.2f)`.
  - `Wall_Outer_Left`: Center `(-15.8f, 0)`, Size `(1.2f, 18f)`.
  - `Wall_Outer_Right`: Center `(15.8f, 0)`, Size `(1.2f, 18f)`.
* **Vách ngăn phòng bên trong (Đã chừa sẵn 7 lối thông cửa)**:
  - **A. Vách ngăn Heavy Storage vs Sảnh trung tâm**:
    - Đoạn trên: `Partition_Storage_Upper` (Center `(-6.8f, 6.0f)`, Size `(0.8f, 4.5f)`).
    - **[LỐI THÔNG CỬA 1]**: Mở toang từ Y = 2.5f đến Y = 4.0f (rộng 1.5 units).
    - Đoạn giữa: `Partition_Storage_Middle` (Center `(-6.8f, 1.0f)`, Size `(0.8f, 2.5f)`).
    - **[LỐI THÔNG CỬA 2]**: Mở toang từ Y = -0.5f đến Y = -2.0f (rộng 1.5 units).
    - Đoạn dưới: `Partition_Storage_Lower` (Center `(-6.8f, -5.5f)`, Size `(0.8f, 5.0f)`).
  - **B. Vách phòng Control Room (Góc trên trái)**:
    - Tường ngang đáy: `Partition_ControlRoom_Bottom` (Center `(-2.0f, 3.8f)`, Size `(4.5f, 0.8f)`).
    - **[LỐI THÔNG CỬA 3]**: Mép trái phòng tại X = -4.5f mở toang rộng 2.55 units.
    - Tường dọc ngăn với kho hóa chất: `Partition_ControlRoom_Right` (Center `(0.5f, 6.2f)`, Size `(0.8f, 4.0f)`).
  - **C. Kho hóa chất (Toxic Area)**:
    - **[LỐI THÔNG CỬA 4]**: Tuyệt đối không đặt tường ngang đáy khu (Y = 3.5f đến 4.5f, X từ -1f đến +3f), mở toang thông suốt xuống Sorting Area.
  - **D. Vách phòng Office (Góc trên phải)**:
    - Tường dọc trái: `Partition_Office_Left` (Center `(3.5f, 6.2f)`, Size `(0.8f, 4.0f)`).
    - Tường ngang đáy (trái cửa): `Partition_Office_Bottom_Left` (Center `(4.5f, 3.8f)`, Size `(1.8f, 0.8f)`).
    - **[LỐI THÔNG CỬA 5]**: Cửa ra vào tại X = 5.8f mở toang rộng 2.5 units.
    - Tường ngang đáy (phải cửa): `Partition_Office_Bottom_Right` (Center `(7.2f, 3.8f)`, Size `(1.2f, 0.8f)`).
    - Tường dọc phải: `Partition_Office_Right` (Center `(8.0f, 6.2f)`, Size `(0.8f, 4.0f)`).
  - **E. Vách phòng Break Room (Góc dưới phải)**:
    - Tường ngang nóc: `Partition_BreakRoom_Top` (Center `(11.5f, -0.8f)`, Size `(7.5f, 0.8f)`).
    - Tường dọc ngăn sảnh: `Partition_BreakRoom_Left` (Center `(7.8f, -5.5f)`, Size `(0.8f, 6.0f)`).
    - **[LỐI THÔNG CỬA 7]**: Khoảng hở góc trên trái căng tin tại `(X = 7.8f, Y = -1.5f)` mở toang rộng 2.5 units.
* **Đổ bóng 2D**: Toàn bộ Collider đều tích hợp `ShadowCaster2D` với `castsShadows = true`, `selfShadows = false`.
* **Công cụ Gizmo Debug trực quan**: Tích hợp `OnDrawGizmos()` và `ColliderGizmoVisualizer` tự động vẽ khối hộp màu xanh lá cây bán trong suốt (`new Color(0f, 1f, 0f, 0.4f)`) kết hợp khung dây cho toàn bộ Collider trong Scene View.

### 5.3. Quy chuẩn Tỷ lệ Kích thước Chuẩn Studio (Metric Units) & Lưu thông Màn chơi:
* **Mèo (Cat)**: Chiều dài cơ thể cố định = `1.0 unit`, `CircleCollider2D.radius = 0.35f`. Đảm bảo di chuyển lọt qua mọi cửa phòng và khe hẹp giữa các băng chuyền.
* **Chuột (Mouse)**: Chiều rộng cơ thể cố định = `0.6 unit`, `CircleCollider2D.radius = 0.2f`. Kích thước nhỏ gọn, tự động khóa cứng kích thước chống mọi hiện tượng phình to khi biến hình (Disguise/UnDisguise).
* **Props (Đạo cụ)**: Tỷ lệ chuẩn hóa theo mét:
  - Thùng hàng gỗ (`Pixel_Prop_Crates_Stack`): `1.1 units`
  - Pallet gỗ (`Pixel_Prop_Pallet`): `1.1 units`
  - Thùng phuy (`Pixel_Prop_Oil_Barrels`): `0.9 units`
  - Bánh răng (`Pixel_Prop_Gears`): `0.9 units`
  - Hộp dụng cụ (`Pixel_Prop_Toolbox_Closed`, `Toolbox_Open`): `0.8 units`
  - Giá cờ lê (`Pixel_Prop_Wrench_Tools`): `0.8 units`
  - Cọc tiêu (`Pixel_Prop_Safety_Cones`): `0.7 units`
* **Nguyên tắc Spawn Props**: 100% Props được bố trí áp sát mép tường và các góc chết (Y = ±6.6f hoặc X = ±12.4f). **TUYỆT ĐỐI KHÔNG** spawn đồ vật chắn giữa cửa ra vào hoặc tim đường hành lang để chừa toàn bộ lối tuần tra thông suốt cho Mèo.

---

## 6. HỆ THỐNG ÁNH SÁNG ĐIỆN ẢNH URP 2D & POST-PROCESSING

### 6.1. Chiếu sáng đa sắc (URP 2D Light2D):
* **`Global_Light_2D`**: Ánh sáng ban đêm xanh thẫm mờ ảo (Màu `#1F2738`, Cường độ `0.45f`).
* **`Light_Sorting_Ceiling`**: Đèn chùm vàng ấm trung tâm (Màu `#FFD97D`, Cường độ `1.5f`, Bán kính `9.5m`, Bật `ShadowsEnabled = true`).
* **`Light_Control_Room_Cyan`**: Ánh sáng xanh ngọc từ màn hình vi tính (Màu `#1AF2D8`, Cường độ `1.4f`, Bán kính `6.0m`).
* **`Light_Workshop_Amber`**: Đèn sợi đốt kho máy bên trái (Màu `#F4A261`, Cường độ `1.2f`, Bán kính `7.0m`).
* **`Light_BreakRoom_Warm`**: Đèn ấm phòng giải lao (Màu `#FFEBB0`, Cường độ `1.3f`, Bán kính `6.5m`).
* **`Light_Toxic_Puddle`**: Ánh sáng huỳnh quang xanh lá vũng hóa chất (Màu `#33FF66`, Cường độ `0.9f`, Bán kính `4.0m`).

### 6.2. Hậu kỳ điện ảnh (Volume Post-Processing Profile):
Được lưu trữ tại: `Assets/Settings/Cinematic_PostProcessing_Profile.asset`
* **Bloom**: Threshold = 0.9f, Intensity = 1.3f (Tạo độ tỏa sáng huyền ảo cho đèn vàng và màn hình máy tính).
* **Vignette**: Color = Black, Intensity = 0.35f, Smoothness = 0.4f (Làm tối 4 góc màn hình tạo chiều sâu điện ảnh).
* **Color Adjustments**: Post Exposure = +0.25f, Contrast = +18f (Đẩy tương phản đậm đà phong cách phim hoạt hình/retro).

---

## 7. HỆ THỐNG CÔNG CỤ EDITOR TỰ ĐỘNG HÓA 1-CLICK

Tất cả công cụ setup cũ đã được gom gọn tinh giản vào 1 file duy nhất với đúng 1 menu trên Unity Editor: `Tools -> CATandMOUSE`:

| Menu Item | File Script | Chức năng chính |
| :--- | :--- | :--- |
| **`🚀 Apply HD Camera & Fix All Colliders`** | [`HotlineArenaBuilder.cs`](file:///c:/Users/ADMIN/Downloads/LAPTRINHGAME/CATandMOUSE/Assets/Editor/HotlineArenaBuilder.cs) | **[DUY NHẤT]** Tự động chuẩn hóa HD Textures 4K Uncompressed, dựng toàn bộ đấu trường Hotline Miami 16:9 hoàn chỉnh với Colliders phòng (cửa rộng >= 3.0u), 12 Props nép tường, 2 Nhân vật chuẩn mét (Cat 1.0u, Mouse 0.6u), Ánh sáng 2D đa sắc, Post-Processing, Cinemachine cận cảnh 3.8f (Lookahead 0.15s, Damping 0.5s, Confiner 2D), phím TAB đổi camera, GameManager, GameModeManager, CatAIController, LanNetworkManager, ColliderGizmoVisualizer và **Hệ thống Modern Casual UI Canvas 2026 hoàn chỉnh** (`ModernUIBuilder.cs`, `ProceduralUISpriteGenerator.cs`). |

---

## 8. CẤU TRÚC THƯ MỤC DỰ ÁN & DANH MỤC TÀI NGUYÊN

```text
c:\Users\ADMIN\Downloads\LAPTRINHGAME\CATandMOUSE\
├── Assets\
│   ├── Editor\                                    # Công cụ tự động hóa Editor duy nhất
│   │   ├── HotlineArenaBuilder.cs                 # Master Builder cho Map Hotline Miami 16:9 & HD Textures
│   │   ├── ModernUIBuilder.cs                     # Builder kiến tạo toàn bộ UGUI Hierarchy & Canvas
│   │   └── ProceduralUISpriteGenerator.cs         # Generator sinh các Sprites Among Us 3D bằng SDF
│   ├── Resources\
│   │   └── GeneratedSprites\                      # Kho tài nguyên Sprite game hoàn chỉnh
│   │       ├── UI\                                # Bộ Sprite UI Among Us / Brawl Stars 3D
│   │       │   ├── UI_AmongUs_Button_Yellow.png   # Nút bấm viên thuốc 3D Vàng Cam (Solo Bot)
│   │       │   ├── UI_AmongUs_Button_Cyan.png     # Nút bấm viên thuốc 3D Xanh Cyan (LAN Host/Client)
│   │       │   ├── UI_AmongUs_Button_Red.png      # Nút bấm viên thuốc 3D Đỏ Tươi (Local 2P)
│   │       │   ├── UI_AmongUs_Panel.png           # Hộp thoại bo tròn viền trắng 8px nổi bật
│   │       │   ├── UI_AmongUs_Circle_Cyan.png     # Nút tròn 3D Cyan to 140px cho Chuột [SPACE]
│   │       │   ├── UI_AmongUs_Circle_Red.png      # Nút tròn 3D Đỏ to 140px cho Mèo [J]
│   │       │   ├── UI_AmongUs_Gear.png            # Icon bánh răng Cài đặt [⚙] gọn gàng
│   │       │   ├── UI_AmongUs_Timer_Pill.png      # Khung đếm ngược viên thuốc viền đen dày
│   │       │   └── UI_Circle_Fill.png             # Lớp phủ trắng tròn cho cooldown radial 360
│   │       ├── Warehouse_Full_Level_Map.png       # Bản đồ đấu trường tổng hợp 16:9 (1376x768)
│   │       ├── Warehouse_Props_Pack.png           # Master Sheet 8 loại Props công nghiệp
│   │       ├── Cat_Mouse_Character_Sheet.png      # Master Sheet 5 hàng hoạt ảnh Mèo & Chuột
│   │       ├── Warehouse_Tileset_Sheet.png        # Master Sheet gạch sàn & tường nhà kho
│   │       ├── Pixel_Cat_TopDown.png              # Sprite Mèo nhìn từ trên xuống
│   │       ├── Pixel_Cat_Stalking.png             # Sprite Mèo rình mồi
│   │       ├── Pixel_Cat_Attack.png               # Sprite Mèo vồ cào
│   │       ├── Pixel_Mouse_TopDown.png            # Sprite Chuột nhìn từ trên xuống
│   │       ├── Pixel_Mouse_Running.png            # Sprite Chuột chạy nhanh
│   │       ├── Pixel_Prop_Pallet.png              # Đạo cụ Pallet gỗ
│   │       ├── Pixel_Prop_Crates_Stack.png        # Đạo cụ Chồng thùng gỗ chữ X
│   │       ├── Pixel_Prop_Oil_Barrels.png         # Đạo cụ Cụm thùng phuy rỉ sét
│   │       ├── Pixel_Prop_Toolbox_Closed.png      # Đạo cụ Hộp đồ nghề đóng
│   │       ├── Pixel_Prop_Toolbox_Open.png        # Đạo cụ Hộp đồ nghề mở
│   │       ├── Pixel_Prop_Gears.png               # Đạo cụ Cụm bánh răng cơ khí
│   │       ├── Pixel_Prop_Safety_Cones.png        # Đạo cụ Cọc tiêu cảnh báo vàng
│   │       ├── Pixel_Prop_Wrench_Tools.png        # Đạo cụ Bộ cờ lê mỏ lết
│   │       ├── Pixel_Floor_Orange.png             # Sàn gạch caro cam
│   │       ├── Pixel_Floor_Hazard.png             # Tấm kim loại viền sọc cảnh báo
│   │       └── Pixel_Wall_Modular.png             # Khối tường bê tông công xưởng
│   ├── Scripts\                                   # Mã nguồn Gameplay & UI Logic
│   │   ├── PropItem.cs                            # Định danh đồ vật, nảy DOPunchScale
│   │   ├── MouseController.cs                     # Di chuyển WASD, biến hình Space
│   │   ├── CatController.cs                       # Di chuyển Mũi tên, tát J/Click, Stun 1.5s
│   │   ├── CatAIController.cs                     # Bot Mèo AI tuần tra 18 waypoint, rượt đuổi, cào tát
│   │   ├── LanNetworkManager.cs                   # Mạng LAN Socket TCP thuần túy 2 máy độc lập màn hình
│   │   ├── GameModeManager.cs                     # Quản lý chế độ chơi: Đơn vs Bot, LAN 2 máy, Cục bộ 2 người
│   │   ├── ColliderGizmoVisualizer.cs             # Debug Gizmo khung dây đỏ/xanh/vàng trong Scene View
│   │   ├── GameManager.cs                         # Singleton, đếm ngược 300s UniTask, gọi Victory Modal
│   │   ├── UIManager.cs                           # Singleton điều phối UGUI, đồng bộ HUD & Popup
│   │   ├── InGameHUDView.cs                       # HUD đếm ngược pulsing <60s, cooldown Space/J, thẻ Controls
│   │   ├── VictoryModalView.cs                    # Popup chiến thắng Dimmer fade + Banner Ease.OutBack + 2 Nút 3D
│   │   ├── GameModeMenuView.cs                    # Menu chọn chế độ chơi UGUI hiện đại + LAN setup lobby
│   │   └── UIButtonJuice.cs                       # Hiệu ứng tactile nảy co giãn cho mọi button (Hover 1.08x, Press 0.95x)
│   └── Settings\
│       └── Cinematic_PostProcessing_Profile.asset # Profile Bloom, Vignette, Color Adjustments
└── AGENTS.md                                      # TÀI LIỆU NÀY
```

---

## 9. QUY CHUẨN LẬP TRÌNH & NGUYÊN TẮC BẤT DI BẤT DỊCH (CODING CONVENTIONS)

1. **Khử hoàn toàn Coroutine**:
   * Tuyệt đối không viết `IEnumerator`, `StartCoroutine`, `WaitForSeconds`.
   * Luôn dùng `await UniTask.Delay(ms, cancellationToken: token)` hoặc `UniTask.Yield()`.
2. **Quản lý Hủy Token (`CancellationToken`)**:
   * Khi dùng UniTask trong MonoBehaviour, luôn truyền `this.GetCancellationTokenOnDestroy()` để ngắt tác vụ ngầm khi đổi màn hoặc GameObject bị hủy.
3. **Chuẩn tương thích Unity 6**:
   * Không dùng `rigidbody.velocity` (đã lỗi thời), phải dùng `rigidbody.linearVelocity`.
   * `ShadowCaster2D.shapePath` trong Unity 6 là thuộc tính Read-Only. Không gán thủ công `shapePath`, chỉ cần thêm component `AddComponent<ShadowCaster2D>()` và bật `castsShadows = true`, Unity 6 sẽ tự động nội suy bóng đổ từ `Collider2D` hoặc `SpriteRenderer`.
4. **Phân biệt điều khiển các chế độ chơi**:
   * Solo vs Bot: Người chơi Chuột (W, A, S, D + Space), Bot Mèo tự động tuần tra & săn lùng.
   * Online LAN: Máy Host (Mèo - Phím mũi tên / J), Máy Client (Chuột - WASD / Space). Độc lập góc nhìn Camera 100%.
   * Local 2P: Player 1 (Mouse - WASD + Space), Player 2 (Cat - Phím mũi tên + J).
5. **Đồ họa Pixel Art**:
   * Mọi Sprite 16-bit nhập vào đều phải được cài đặt `filterMode = FilterMode.Point` và `textureCompression = TextureImporterCompression.Uncompressed` để bảo đảm độ sắc nét tuyệt đối.
6. **Chuẩn tương thích Unity 6+ cho API tìm kiếm Object**:
   * Không dùng `FindFirstObjectByType<T>()` (đã lỗi thời vì phụ thuộc thứ tự instance ID), bắt buộc dùng `FindAnyObjectByType<T>()`.
   * Không truyền enum `FindObjectsSortMode` vào `FindObjectsByType<T>()` (đã bị khai tử), bắt buộc gọi trực tiếp `FindObjectsByType<T>()`.

---

## 10. LỘ TRÌNH PHÁT TRIỂN TIẾP THEO (ROADMAP - ASSIGNMENT 2 & EXTENSIONS)

1. **Hệ thống Hoạt ảnh (Character Animator)**:
   * Tạo `AnimatorController` sử dụng các frame trích xuất từ `Cat_Mouse_Character_Sheet.png`:
     * Cat: `Walk_BlendTree` (4 hướng), `Stalk_State`, `Swipe_Attack_Trigger`.
     * Mouse: `Creep_BlendTree`, `Sprint_State`, `Disguise_Puff_Animation`.
2. **Hệ thống Âm thanh Động lực (Dynamic Audio System)**:
   * Âm thanh bước chân Mèo rón rén, tiếng móng vuốt cào gió, tiếng va đập đồ vật gỗ/kim loại rỗng.
   * Nhạc nền Synthwave hồi hộp phong cách *Hotline Miami* tăng dần nhịp độ ở 60 giây cuối cùng.
3. **Mở rộng Multiplayer lên Internet (Dedicated Server / Mirror / Netcode)**:
   * Nâng cấp từ Socket TCP LAN nội bộ sang Relay/Lobby cho phép 2 người chơi qua Internet không cần chung Wi-Fi.
4. **Tính năng Bổ sung Chế độ Chơi**:
   * Mini-map radar định vị tiếng động.
   * Skill phụ cho Chuột: Tạo bóng ma (Decoy) hoặc rải bẫy chuối làm trượt chân Mèo.
