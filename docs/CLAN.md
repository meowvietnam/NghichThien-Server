# Tông Môn

## Phạm vi

Tông Môn là tổ chức xã hội, một nhân vật chỉ thuộc một Tông Môn. Server là nguồn dữ liệu duy nhất cho thành viên, vai trò, quỹ và Tiên Ngọc; client gửi lệnh có `RequestId` và hiển thị snapshot.

Đã triển khai: tạo/tìm, đơn xin vào, vai trò, cống hiến, ledger, outbox/reconnect; thanh toán tạo Clan bằng Tiên Ngọc hoặc quyền PayOS; lưu node thành và lọc quyền xem; mở khóa/học/chọn truyền thừa. Có handler webhook PayOS qua SDK xác minh chữ ký. UI prefab, icon thành AllMaps và combat adapter chưa triển khai; hiệu ứng truyền thừa chưa tác động vào trận.

## Tạo Tông Môn và thành

Người tạo gửi tên, biểu tượng, chính sách tuyển và `CastleNodeId`. Server chuẩn hóa tên, kiểm tra node tồn tại trong `maps.bin`, xác nhận người tạo chưa thuộc Clan, sau đó tạo Clan và thành viên đầu tiên trong một transaction.

`CastleNodeId` là node AllMaps của thành. Nhiều Clan có thể chọn cùng node; client chỉ tạo icon thành khi snapshot nói người xem là thành viên. Snapshot công khai không được trả node thành.

## Thanh toán

| Phương thức | Luồng |
|---|---|
| Tiên Ngọc | Trừ `CharacterData.MoneyVip` cùng transaction tạo Clan; giá `CreationMoneyVipCost`. |
| PayOS | Tạo `ClanCreationPayment`, mở checkout URL; webhook đã xác minh đánh dấu `Paid`; lệnh tạo Clan tiêu quyền đó thành `Consumed`. |

Không cấp Clan từ return URL. Chỉ webhook qua SDK PayOS được xác minh chữ ký mới được xử lý; kiểm tra `code == "00"`, VND, `orderCode`, amount, `paymentLinkId` và reference. Webhook lặp phải idempotent. Quyền `Paid` không mất khi tên trùng hay người chơi đổi ý.

## Cống hiến

`DonationUnitMoneyVip = n` là giá server cấu hình.

1. Mỗi ngày có một lượt miễn phí: `FreeDonation=true`, `DonationCount=1`; không trừ Tiên Ngọc, cộng `n` vào `JadeFunds`, tổng cống hiến và điểm dùng được.
2. Sau lượt đó có hai nút trả phí: x1 hoặc x10. x1 trừ `n`; x10 trừ `10 × n`; cả hai cộng số tương ứng vào quỹ và điểm.

Không có giới hạn lượt trả phí ngày. Server cho phép trả phí trước hoặc sau lượt miễn phí. `ClanMemberPeriodStats` ghi số lượt miễn phí/trả phí; lượt miễn phí có scope nhân vật, nên đổi Clan không reset. Ngày tính theo `CalendarUtcOffsetHours`. Trường legacy `Amount` khác 0 bị từ chối; server tự tính giá.

## Truyền thừa

Truyền thừa được thiết kế là hiệu ứng nội bộ Clan được chốt khi vào trận. Các bước 1–3 đã có; bước 4–5 là phần tích hợp gameplay còn thiếu:

1. Quản lý mở khóa trong `ClanInheritance`, trừ `JadeFunds`.
2. Thành viên học trong `ClanMemberInheritance`, trừ `AvailableContribution`.
3. Học xong tự chọn truyền thừa đó; thành viên có thể chọn lại một truyền thừa đã học bằng `ActiveInheritanceId`, hoặc gửi `InheritanceId=0` để bỏ chọn. Rời/bị kick khỏi Clan xóa dữ liệu đã học của lần thành viên này.
4. Khi tạo `CharacterSnapshot`, server đọc lựa chọn hiện tại và ghi effect vào snapshot.
5. Client áp đúng một lần sau `CharacterCombat.Init()` và trước turn đầu; đổi lựa chọn giữa trận không tác động trận đang chạy.

Effect v1:

| Effect | Áp dụng |
|---|---|
| InitialElementalEnergy | `HapThuLinhKhi(element, value)` |
| InitialHoTrao | `AddBuffAndDebuffCount(EBuffAndDeBuff.HoTrao, value)` |

Chỉ nhận danh sách effect server cấu hình. Không nhận buff/skill tùy ý từ client.

## Bảng DB

`Clans`, `ClanMembers`, `ClanMembershipStates`, `ClanApplications`, `ClanLedgerEntries`, `ClanMemberPeriodStats`, `ClanCommandReceipts`, `ClanOutboxEvents`, `ClanCreationPayments`, `ClanInheritances`, `ClanMemberInheritances`.

`ClanCommandReceipts` có khóa `(ActorCharId, RequestId)` để chống gửi lặp. Tiền/quỹ/receipt phải commit trong cùng transaction. Event outbox chỉ mang `EventId`, `ClanId`, `Version`; client tải snapshot mới sau event hoặc reconnect.

## Cấu hình mẫu

```json
{
  "Clan": {
    "CreationMoneyVipCost": 500,
    "CreationPayOsVnd": 20000,
    "DonationUnitMoneyVip": 5,
    "InitialCapacity": 30,
    "MaxCapacity": 200,
    "CalendarUtcOffsetHours": 7,
    "Inheritances": [
      { "Id": 1, "Name": "Kim Khí Khai Trận", "Effect": 1, "Element": 1, "Value": 2, "UnlockCost": 500, "LearnCost": 50, "MinClanLevel": 1 }
    ]
  }
}
```

Giá bằng 0 phải trả `CreationPriceNotConfigured` hoặc `DonationPriceNotConfigured`, không được miễn phí ngầm.

## Migration và checklist trước production

```sh
dotnet ef database update AddClans
dotnet ef database update ClanEconomyCastleInheritance
```

Trước khi bật: cấu hình PayOS và kiểm tra webhook trong môi trường thử nghiệm; hoàn thiện combat adapter snapshot và UI icon thành. Không coi dữ liệu truyền thừa đã học là bằng chứng hiệu ứng đã chạy trong trận.

## API client và đồng bộ

Hai phía phải cùng cập nhật protocol v2. Tag TCP: `ClanCommand=3600`, `ClanQuery=3601`, `ClanChanged=3602`, `ClanPreparePayment=3603`.

- `PreparePaymentAsync(requestId)`: giữ nguyên GUID khi timeout; trả `Payments[0]` gồm mã đơn, số tiền, URL, trạng thái và hạn thanh toán. Tối đa 3 đơn pending chưa hết hạn mỗi nhân vật. Giá được lưu lúc tạo đơn, thay cấu hình không đổi đơn cũ.
- `QueryAsync(Payments)`: đọc đơn của chính nhân vật kể cả chưa có Clan; phân trang theo mã đơn giảm dần. Không dùng return URL làm bằng chứng đã trả tiền.
- `ExecuteAsync(Create)`: `CreationPaymentMethod=0` dùng Tiên Ngọc; `=1` yêu cầu `PaymentOrderId` thuộc nhân vật và đã Paid. Quyền được tiêu cùng transaction tạo Clan, thất bại không mất quyền.
- `ExecuteAsync(Donate)`: `FreeDonation=true, DonationCount=1` hoặc trả phí với `DonationCount=1/10`. Giữ GUID khi gửi lại cùng thao tác; thao tác mới phải GUID mới.
- `UnlockInheritance`, `LearnInheritance`, `SelectInheritance` nhận `ClanId`, `InheritanceId`; `QueryAsync(Inheritances)` trả danh mục cấu hình và trạng thái mở/học, chỉ thành viên được đọc.
- `Mine` trả node thành, quỹ Tiên Ngọc, giá mỗi lượt, lượt miễn phí còn hay hết và truyền thừa được chọn. `Detail` công khai không trả node/quỹ/thông báo nội bộ.

Response thành công có `MoneyVip` hiện tại của nhân vật; Unity áp trên main thread trước khi gửi request Clan tiếp theo. Replay không khôi phục số dư cũ. Client làm mới snapshot khi có invalidation hoặc reconnect. Outbox hiện truyền trong một process server; chạy nhiều instance cần cơ chế phát event liên process.

## Lỗi nghiệp vụ và kiểm thử

Giá 0 tắt phương thức tương ứng; cấu hình âm hoặc truyền thừa không hợp lệ bị từ chối. Các mã lỗi chính: `InvalidCastleNode`, `InvalidPaymentMethod`, `PaidCreationRequired`, `PaymentLinkExpired`, `InvalidPayment`, `InsufficientFunds`, `FreeDonationUsed`, `InvalidDonationCount`, `ClanHasFunds`, `InheritanceLocked`, `InheritanceNotLearned`, `InsufficientClanFunds`, `InsufficientContribution`.

Test tích hợp ở `backend/MTS.Tests/Clans/ClanIntegrationTests.cs`. Chỉ chạy trên DB mới tên `mts_clan_test_*` qua biến `MTS_CLAN_TEST_CONNECTION`; fixture tự migrate và xóa DB sau test. Phủ transaction/replay, mất ACK commit, autosave, tranh chấp slot/thành viên, quyền riêng tư, miễn phí/x10, PayOS lặp/đến trễ/tiêu quyền và vòng đời truyền thừa. Gateway PayOS được giả lập; test này không thực hiện thanh toán thực hoặc kiểm chứng tích hợp mạng PayOS production.
