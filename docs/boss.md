# Boss thế giới

Mỗi ngày được chọn tạo **một boss duy nhất** lúc `StartTime`, chọn ngẫu nhiên từ
`GameConfig.BossDatas`. Khi `KeepUntilNextSpawn` là `false`, Boss chỉ tồn tại đến
`EndTime`; ngoài khoảng giờ không có boss. Khi là `true`, `EndTime` được bỏ qua:
phiên Boss tồn tại cho đến khi tạo được phiên kế tiếp theo lịch và được thay thế.
NodeId được chọn ngẫu nhiên từ node thường, loại mọi thành/cứ điểm
(`Stronghold`) và node đang có gia tộc/tông môn trong database. NodeId được lưu
cùng phiên boss. Phiên bị đóng khi chưa tiêu diệt Boss sẽ không phát thưởng.

Lịch cấu hình trong mục `Boss` của `appsettings.Development.json` hoặc
`appsettings.Production.json` (giờ Việt Nam, UTC+7):

```json
"Boss": {
  "DaysOfWeek": ["Monday", "Wednesday", "Friday"],
  "StartTime": "19:00:00",
  "EndTime": "20:00:00",
  "KeepUntilNextSpawn": true
}
```

Mặc định cả bảy ngày, 12:00–13:00 và `KeepUntilNextSpawn: false`.
`DaysOfWeek` dùng tên thứ tiếng Anh của `DayOfWeek`; mảng rỗng sẽ không spawn boss.
Khi có giờ kết thúc, `EndTime` sớm hơn `StartTime` nghĩa là khung giờ kéo qua
nửa đêm và vẫn thuộc ngày bắt đầu. Mốc kết thúc không còn được tính là trong phiên.
Khi `KeepUntilNextSpawn: true`, Boss được khôi phục sau restart theo mốc spawn gần
nhất, kể cả đã qua ngày đó. Boss đã chết không hồi sinh cho đến mốc tiếp theo.
Thay đổi cấu hình cần khởi động lại server.

Khi boss chết, mỗi nhân vật từng tham gia nhận thư gồm 1.000 Tiên Ngọc và một đan lô.
Tỉ lệ ban đầu: Tam phẩm 96%, Tứ phẩm 2%, Ngũ phẩm 1,5%, Lục phẩm 0,5%.
Mỗi 0,1% HP tối đa thực gây ra cộng 0,1 điểm phần trăm cho **từng** phẩm
Tứ, Ngũ, Lục và trừ 0,3 điểm từ Tam. Tăng theo từng mốc đủ 0,1% HP;
khi Tam về 0% thì dừng, giữ tổng đúng 100%.
Người kết liễu nhận thêm lò Lục phẩm và 5.000 Tiên Ngọc trong cùng thư.
Lò được chọn ngẫu nhiên trong các mẫu của phẩm tương ứng và có độ bền 100.
Lần đầu bắt đầu đánh boss trong mỗi ngày Việt Nam (UTC+7), người chơi nhận ngay
100 Tiên Ngọc, kể cả boss chưa chết.
Danh sách nhận thư lấy từ lịch sử lượt đánh của cả phiên, không lấy từ người
đang trong trận hoặc danh sách online. `BossStore.SendRewardMails` lưu thư vào
DB cho mọi người; chỉ thông báo `NewMail` cần kiểm tra người chơi online.
Phần thưởng chung và đặc biệt được chốt lúc spawn; lò theo sát thương được quay khi boss chết.

## Dữ liệu và database

1. Trong Unity, điền `BossDatas[].Character` bằng `CharacterSnapshot` (ID boss, tên,
   cấp và các chỉ số; `SinhMenh` phải lớn hơn 0). Chạy `Export` trên asset
   `BossResources`; thao tác này tự đồng bộ HP và các danh sách trống. Copy
   `Assets/Exports/BossResources.bin` sang `backend/MTS/Data/BossResources.bin`.
   Sau thay đổi từ CharacterWrapper sang CharacterSnapshot, cần export lại file này.
   Server nạp vào `GameConfig.BossDatas`, project tự copy file khi build/publish.
   Nếu chưa có file hoặc danh sách rỗng, service ghi cảnh báo và không mở boss.
2. Apply migration `20260921100144_AddBossProgress`, `20260921150009_AddBossNodeId`,
   `20260922100000_AddBossDamage` và `20260922110000_AddDailyBossReward` qua workflow database hiện có trước khi bật server.
   Các migration thêm phiên boss, NodeId, tổng sát thương và mốc nhận thưởng ngày.
3. Chạy server. Server khôi phục Boss của phiên hiện tại (hoặc tạo nếu chưa tồn tại).
   Boss đã chết không hồi sinh khi restart.

HP, số lượt và tổng sát thương thực gây ra theo từng nhân vật, dữ liệu boss và phần thưởng được lưu theo từng mốc spawn.
Mỗi lần Ready hợp lệ tốn một lượt, tối đa 3 lượt/nhân vật/boss. Ready gửi lặp
không tốn thêm. Thoát vào lại hoặc restart server không reset lượt.
Người Ready trước lúc boss chết được nhận thưởng, kể cả đã thoát hoặc offline.
Nếu boss chết/hết giờ lúc client đang tải scene, lượt chưa Ready không bị tính
và không được tính là tham gia.

Kết liễu và toàn bộ thư được commit trong cùng một `SaveChanges` (transaction của EF).
Chỉ sau khi lưu thành công mới đổi HP trên manager và báo kết thúc cho client.
Khi lưu lỗi, HP/lượt trong manager không đổi. Triển khai theo mô hình một game server
đang vận hành một boss; không có điều phối boss giữa nhiều tiến trình server.

## Giao thức client

- Khi mở bản đồ, client hỏi Boss đang hoạt động bằng `GetBossStatus` với NodeId=0
  và cập nhật mỗi 5 giây. Mô hình Spine của boss được đặt tại node spawn, dùng
  cùng `SkinId` với popup/trận đánh và tự biến mất khi boss chết hoặc đổi phiên.
- Sau khi nhân vật đến node, client gửi `GetBossStatus` (3845, int NodeId).
  Server xác nhận vị trí nhân vật rồi trả `BossStatus` (3846): mã phiên, NodeId,
  snapshot boss, SkinId, HP, số người đang đánh, lượt còn lại, sát thương cá nhân
  tỉ lệ từng phẩm và lịch ngày/giờ xuất hiện theo cấu hình server.
- Client mở `Resources/BossInfoPopup.prefab`: hình boss, tên/cảnh giới, HP,
  số người đang đánh, lượt còn lại, icon/tỉ lệ phần thưởng, lịch Boss, nút Tham gia và Đóng.
  Thưởng lần đầu mỗi ngày có icon và dòng chữ lớn ngay trên nút Tham gia; khi có lỗi, dòng này nhường chỗ cho thông báo.
  Popup dùng lại hình nền
  PrepareCombatPopup và kiểu nút PopupConfirmAction. Poll mỗi 2 giây khi mở.
  Đóng popup không mất lượt; click lại node hiện tại để mở lại.
- `JoinBoss` (3847, JoinBossRequest) gửi SpawnId + NodeId. Server chỉ nhận trong
  AllMaps, kiểm tra đúng vị trí/phiên, boss còn sống, HP nhân vật và số lượt.
  `JoinBossResult` (3848) mang snapshot nhân vật và boss khi tham gia thành công.
  Không cho vào boss bằng `ChangeScene("Boss")` để tránh bỏ qua kiểm tra vị trí.
- Client chuẩn bị dữ liệu và tải scene Combat cục bộ. CombatManager.Start mới
  chuyển sang BossState và gửi Ready để bảo đảm các đối tượng chiến đấu đã sẵn sàng.
- Số đang đánh chỉ tính người đã Ready và còn kết nối. Người chỉ xem popup/tải
  scene không được tính. Danh sách lịch sử tham gia để nhận thưởng không thay đổi.
- Client gửi `ReadyFightingBoss`. Server ghi nhận lượt, gửi `BossHp` (int, tag 3844)
  để bù lượng HP mất trong lúc tải scene, rồi `SyncStartTurn`.
- `SyncDame` kiểm tra phiên người gửi, ID người gửi/đích, số hit (1–100),
  damage và damage hộ tráo (0–1.000.000/hit); cộng bằng `long` tránh overflow.
  Server cộng tổng sát thương thực làm giảm HP boss theo nhân vật; phần overkill không tính.
  Bảng sát thương được lưu sau mỗi đòn hợp lệ và đưa vào `CreateDamageRewards` khi boss chết.
  Server tin phép tính damage của client, không mô phỏng lại skill hoặc chống replay packet.
- `BossEnded` (bool, tag 3843): `true` là boss chết, `false` là hết giờ hoặc bị phiên mới thay thế.
- `ResultCombatBoss` trả `ResultCombatResult` với phần thưởng rỗng. Chỉ cập nhật
  chỉ số hợp lệ, không cộng tiền, vật phẩm, ngộ đạo hoặc thưởng combat thường.
  Kết quả gửi lặp chỉ nhận lại phản hồi đã lưu, không xử lý lại; SyncDame sau kết quả không còn hợp lệ.
- `ResultCombat` thường không chấp nhận trận Boss, tránh đi nhầm nhánh phát thưởng cũ.
- Mất kết nối ngắn giữ nguyên lượt; reconnect đồng bộ HP/trạng thái. Logout,
  đăng nhập nơi khác, rời state hoặc hết thời hạn reconnect dọn phiên đang đánh.

## Kiểm tra

```sh
dotnet run --project backend/MTS.BossTests/MTS.BossTests.csproj
```

Các kiểm tra không kết nối database: lịch UTC+7, ba lượt, Ready lặp, giả mạo ID,
damage âm/quá giới hạn, nhiều người cùng kết liễu, người đã rời trận vẫn được
đưa vào danh sách thưởng, kết quả không phát thưởng, lỗi lưu, khôi phục tiến độ,
hết giờ, HP snapshot và nội dung thư thưởng cho người kết liễu. Cần kiểm tra thêm migration/mail với database thử nghiệm
và chạy luồng combat trong Unity trước khi phát hành.

## Prefab và kiểm tra Unity

`Tools/Boss/Build Boss Info Popup` dựng lại prefab từ các asset UI hiện có.
Hình boss lấy từ `BossResources.BossVisuals` theo SkinId (prefab SkeletonGraphic);
nếu chưa setup thì dùng CharacterVisualControl với ngoại hình từ snapshot.
Popup tạo thêm khu vực phần thưởng khi mở để hiển thị icon Furnace và Tiên Ngọc
từ các `Resources` hiện có; không cần dựng lại prefab để áp dụng khu vực này.
