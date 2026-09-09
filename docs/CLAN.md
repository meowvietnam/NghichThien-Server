# Tông Môn

## Phạm vi

Tông Môn là tổ chức xã hội, một nhân vật chỉ thuộc một Tông Môn. Server là nguồn dữ liệu duy nhất cho thành viên, vai trò, quỹ và Tiên Ngọc; client gửi lệnh có `RequestId` và hiển thị snapshot.

Nền tảng đã có: tạo/tìm, đơn xin vào, vai trò, cống hiến, ledger, outbox/reconnect và migration `AddClans`. Nhánh mở rộng này thêm schema và luật cho Tiên Ngọc, thành, PayOS và truyền thừa. UI prefab, PayOS webhook production, và combat adapter chưa bật production.

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

Không có giới hạn lượt trả phí ngày. `ClanMemberPeriodStats` chỉ chống dùng lại lượt miễn phí. Giá trong request client bị bỏ qua.

## Truyền thừa

Truyền thừa là hiệu ứng nội bộ Clan được chốt khi vào trận.

1. Quản lý mở khóa trong `ClanInheritance`, trừ `JadeFunds`.
2. Thành viên học trong `ClanMemberInheritance`, trừ `AvailableContribution`.
3. Thành viên chọn một truyền thừa đã học bằng `ActiveInheritanceId`.
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

Trước khi bật: hoàn thiện webhook PayOS, combat adapter snapshot, UI icon thành; kiểm thử webhook lặp/đến trễ, x10 thiếu Tiên Ngọc, double tap lượt miễn phí, kick trước cống hiến, đổi truyền thừa trong trận, reconnect và autosave sau khi trừ `MoneyVip`.
