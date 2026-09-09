# Tông Môn (Clan)

## Phạm vi đã triển khai

Server có 10 entity Clan, cấu hình EF/MySQL, migration `AddClans`, service xử lý nghiệp vụ, TCP handler và outbox worker. Unity có DTO tương ứng, API bất đồng bộ, cache thông tin Clan của nhân vật và sự kiện trên main thread. Chưa tạo panel/prefab UI.

Các lệnh: tạo, xin vào hoặc vào tự do, hủy đơn, duyệt/từ chối, rời, kick, đổi chức vụ, chuyển tông chủ, sửa cấu hình, cống hiến linh thạch, giải tán.

Các truy vấn: tìm theo ID/đầu tên, thông tin công khai/nội bộ, Clan của mình, thành viên, đơn chờ, đơn đã gửi, nhật ký quản trị, nhật ký tài sản. Tất cả danh sách có giới hạn và cursor.

`ClanRewardClaim` mới là nền tảng DB chống nhận thưởng trùng. Nhiệm vụ, nội dung phần thưởng, shop, nâng cấp, kho, chat và Tông Môn chiến chưa có API trong đợt này.

## Cài schema

Áp dụng migration trước khi chạy server có ClanHandler. Không có lệnh tự migrate lúc startup. Migration này tạo bảng mới; không sửa dữ liệu nhân vật hiện có. Snapshot EF đã cập nhật.

Từ thư mục `backend/MTS`, dùng môi trường và kết nối DB do người vận hành cấu hình:

```sh
dotnet ef database update AddClans
```

`AppDbContextFactory` hiện đọc `appsettings.{ASPNETCORE_ENVIRONMENT}.json`; đặt biến môi trường phù hợp với hệ thống đang triển khai. Không dùng `--offline-migrations` để cập nhật DB thật.

SQL nâng cấp từ `M014` được lưu ở `backend/MTS/Migrations/Scripts/AddClans.sql`. Tên Clan được giữ sau giải tán; migration Down xóa các bảng Clan và toàn bộ lịch sử trong đó.

Tạo script/migration mà không truy cập DB ứng dụng:

```sh
dotnet ef migrations script M014 AddClans -- --offline-migrations
```

## Cấu hình

Thêm section `Clan` vào cấu hình server nếu muốn đổi các mặc định sau:

```json
{
  "Clan": {
    "CreationCost": 0,
    "InitialCapacity": 30,
    "MaxCapacity": 200,
    "JoinCooldownHours": 24,
    "ApplicationLifetimeHours": 72,
    "MaxPendingApplications": 10,
    "DailyDonationLimit": 100000,
    "CalendarUtcOffsetHours": 7
  }
}
```

Đây là mặc định triển khai, chưa phải cân bằng gameplay đã duyệt. Cống hiến 1 linh thạch tăng 1 quỹ và 1 điểm cống hiến. Giới hạn ngày tính toàn nhân vật, không làm mới khi đổi Clan. `MaxCapacity` kiểm tra cấu hình; chưa có API nâng sức chứa. `MinLevel` dùng `Progression.Level` hiện có.

Tên chuẩn hóa bằng Unicode Form C, gộp khoảng trắng, chuyển chữ hoa; giữ dấu tiếng Việt. Unique trên `NormalizedName` với collation nhị phân. Tìm theo đầu tên; không dùng tìm chứa `%keyword%`. Từ khóa tối thiểu 2 ký tự, ngoại trừ ID số. Từ khóa rỗng trả danh sách Clan đang hoạt động.

Chỉ tông chủ được đổi chức vụ, chuyển quyền, giải tán. Phó tông chủ được duyệt đơn, kick người cấp dưới và sửa cấu hình. Trưởng lão được duyệt đơn. Tông chủ không thể rời khi chưa chuyển quyền. Chỉ giải tán khi còn một thành viên và quỹ bằng 0. Khi có quỹ cần triển khai chính sách chi/hoàn/hủy quỹ trước khi cho giải tán; hiện service chủ động trả `ClanHasFunds`.

Rời/kick làm mất điểm cống hiến của lần gia nhập hiện tại; lịch sử giao dịch vẫn giữ. Chức tông chủ chỉ được suy ra từ `Clan.LeaderCharId`; không lưu `Leader` trong `ClanMember.Role`.

## DB và tính nhất quán

- `Clan`: thông tin, quỹ, sức chứa, phiên bản và tông chủ.
- `ClanMember`: PK CharId đảm bảo tối đa một Clan; FK ClanId, vai trò, điểm.
- `ClanMembershipState`: cooldown tồn tại sau khi xóa thành viên.
- `ClanApplication`: unique (ClanId, CharId), giữ đơn gần nhất; lịch sử có trong audit.
- `ClanLedgerEntry`: các dòng biến động quỹ/điểm/linh thạch, cùng TransactionId = RequestId.
- `ClanMemberPeriodStats`: bộ đếm Donation theo ngày và ScopeId = 0.
- `ClanRewardClaim`: unique (CharId, RewardKey), chưa có API cấp thưởng.
- `ClanAuditLog`: nhật ký thao tác, chỉ quản lý được đọc qua API.
- `ClanCommandReceipt`: unique (ActorCharId, RequestId), hash nội dung, kết quả thành công.
- `ClanOutboxEvent`: thông báo thay đổi chờ phát, thử lại khi lỗi.

Lệnh thay đổi chạy trong transaction Serializable với concurrency token, unique constraint và kiểm tra quyền bên trong transaction. Mỗi lần thay đổi cập nhật Version của Clan. `ExpectedVersion > 0` cho phép UI từ chối thao tác từ snapshot cũ. Khi trả `ConflictRetry`, gửi lại cùng request; service sẽ xét lại điều kiện.

Cống hiến/phí tạo giữ `Client.CharacterLock` giống autosave và các handler gameplay. Chỉ cập nhật cột LinhThach trong DB, cập nhật RAM sau khi xác nhận commit. Không gọi SaveChanges của toàn bộ nhân vật trong transaction Clan. Receipt không lưu số dư để phát lại: retry trả số dư RAM hiện tại, tránh khôi phục một số dư cũ.

Nếu mất ACK sau COMMIT, service dùng locking read của receipt để xác định kết quả. Nếu DB tiếp tục không truy cập được, giữ khóa nhân vật và thử lại mỗi 5 giây; không cho autosave ghi số dư cũ trong lúc kết quả chưa rõ. Đây là đánh đổi nhất quán: nhân vật đó phải đợi DB phục hồi. Cần theo dõi log và thời gian giữ khóa.

Thiết kế tài sản này dựa trên kiến trúc hiện tại: một phiên sở hữu RAM của nhân vật, một tiến trình game server. Không cho nhiều node đồng thời quản lý cùng nhân vật hoặc có writer tiền ngoài CharacterLock. Trước khi scale nhiều node, cần ownership/fencing cho nhân vật và cơ chế truyền sự kiện liên node. Lock RAM không thay thế DB constraints.

## Realtime và TCP

Tag v1: `ClanCommand = 3600`, `ClanQuery = 3601`, `ClanChanged = 3602`. Request/response dùng JSON string qua framing Network hiện có. Hai bản `ClanProtocol.cs` ở server và Unity phải giống nhau. `ActorCharId` luôn lấy từ phiên đăng nhập, không lấy từ request.

Server giới hạn một Clan request đang xử lý trên mỗi kết nối, query cách nhau tối thiểu 200 ms, command 100 ms. Payload tối đa 16 KiB trước giải mã và 8192 ký tự sau giải mã. Kết quả danh sách tối đa 50 dòng, mặc định 20.

Outbox worker đọc tối đa 100 dòng mỗi 250 ms và gộp thông báo theo Clan. Registry định tuyến theo CharId đến handler online; không quét toàn bộ người chơi. Thông báo chỉ gồm EventId, ClanId, Version; không chứa quỹ, đơn xin vào hay dữ liệu riêng tư. Thành viên vừa bị kick cũng nhận invalidation.

`PublishedAt` nghĩa thông báo đã giao cho tầng TCP cục bộ, không phải client đã ACK. Tầng TCP hiện có queue gửi không giới hạn; Clan giảm lượng tin bằng gộp invalidation nhưng chưa thay đổi cơ chế backpressure chung. Unity tự nạp lại snapshot khi nhận invalidation hoặc reconnect. UI cũng có thể gọi RefreshAsync khi mở panel. Không dùng Version của invalidation như một event log liên tục để replay.

Chạy một outbox dispatcher cho một game server. Chưa dùng Redis và chưa có multi-node fanout. Receipt/ledger/audit không tự xóa; cần chính sách lưu trữ trước khi chạy dài hạn. Có thể dọn outbox đã PublishedAt sau thời gian giữ phù hợp vì client khôi phục bằng snapshot, không replay outbox.

## Gọi từ Unity

```csharp
using Network.Core;
using Network.Models.Clans;

var clans = Network.Client.Instance.CoreModule.GetHandler<ClanHandler>();
var search = await clans.SearchAsync("Thanh");
var mine = await clans.RefreshAsync();

var request = new ClanCommandRequest
{
    Command = ClanCommandType.Create,
    Name = "Thanh Vân Tông",
    JoinPolicy = 1 // 0: tự do, 1: duyệt đơn, 2: đóng tuyển
};
var created = await clans.ExecuteAsync(request);
// Nếu timeout, giữ nguyên request và gọi lại: RequestId đã được gán.

var donate = new ClanCommandRequest
{
    Command = ClanCommandType.Donate,
    ClanId = created.ClanId,
    Amount = 100
};
var donated = await clans.ExecuteAsync(donate);
```

UI subscribe `SnapshotUpdated` và `Invalidated`, unsubscribe khi panel bị hủy. Hai event chạy trên Unity main thread. `ExecuteAsync` cập nhật Stats.LinhThach phía client sau thành công; trạng thái Clan cập nhật qua refresh được gộp. UI cần xử lý `Success`/`Code`, không giả định có kết quả khi timeout. Kết quả tìm kiếm có thể lỗi thời; thao tác gia nhập luôn kiểm tra lại.

Các mã thường gặp: `AlreadyMember`, `ClanFull`, `JoinCooldown`, `ApplicationExpired`, `Forbidden`, `RoleHierarchy`, `InsufficientFunds`, `DailyLimit`, `NameTaken`, `StaleVersion`, `ConflictRetry`, `RequestIdReused`, `Busy`, `RateLimited`.

## Kiểm thử

Dùng MySQL tạm, tên DB mới bắt đầu bằng `mts_clan_test_`. Fixture không đọc appsettings; từ chối DB đã tồn tại; chạy toàn bộ migration và xóa DB test sau khi hoàn tất.

```sh
MTS_CLAN_TEST_CONNECTION='Server=127.0.0.1;Port=33367;User=root;Database=mts_clan_test_run;SslMode=None' \
  dotnet test backend/MTS.Tests/MTS.Tests.csproj
```

Khi không đặt biến trên, các integration test được skip. Các ca gồm tranh suất cuối, hai Clan duyệt một người, retry cống hiến, rollback sau debit, mất ACK commit, kiểm tra quyền, cooldown, giới hạn ngày, tên có wildcard, outbox và giải tán. Không dùng DB thật để chạy bộ test này.
