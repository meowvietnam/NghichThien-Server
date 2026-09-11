# Kho Clan dùng điểm cống hiến

Kho Clan là kho động theo từng Clan, lưu trong DB. Mặc định Clan mới không có vật phẩm nào. Thành viên có quyền có thể đưa vật phẩm xếp chồng của chính mình vào kho, đặt giá bằng điểm cống hiến. Thành viên Clan mua bằng `ClanMember.ContribPoints`; số lượng trong kho giảm theo số mua, hết thì dòng bán bị xóa. Chủ sở hữu không được mua vật phẩm do chính mình thêm vào kho. Mỗi thành viên có giới hạn số lượng vật phẩm được thêm vào kho trong một ngày.

## Quyền

Leader mặc định có:

- `AddShopItem`: thêm vật phẩm từ túi của mình vào kho.
- `RemoveShopItem`: xóa vật phẩm mình đã đưa vào kho, vật phẩm được trả lại túi của chính mình.

Người không có quyền không thể thêm hoặc xóa. Khi xóa, ngoài quyền `RemoveShopItem`, server còn kiểm tra `OwnerCharId == CharId` để chỉ chủ nhân vật phẩm được thu hồi dòng bán đó.

## Dữ liệu DB

`ClanShopItems`:

| Field | Ý nghĩa |
|---|---|
| Id | ID dòng bán |
| ClanId | Clan sở hữu kho |
| OwnerCharId | Nhân vật đã gửi vật phẩm vào kho |
| ItemId | ID vật phẩm |
| ItemType | Loại vật phẩm |
| Count | Số lượng còn bán |
| Price | Giá điểm cống hiến cho 1 vật phẩm |
| CreatedAt | Thời điểm thêm vào kho |


`ClanMembers` dùng thêm quota ngày cho thao tác thêm vật phẩm:

| Field | Ý nghĩa |
|---|---|
| ShopItemsAddedToday | Tổng số lượng vật phẩm thành viên đã đưa vào kho trong ngày hiện tại |
| LastShopItemAddedAt | Ngày gần nhất quota thêm kho được ghi nhận, lưu theo UTC date |

Quota tính theo tổng `Item.Count` đã gửi vào kho trong ngày, không tính theo số request hay số dòng bán. Ví dụ giới hạn 100/ngày, gửi 30 linh thảo rồi gửi tiếp 70 linh đan thì hết quota. Xóa vật phẩm khỏi kho hoặc vật phẩm đã được mua không hoàn lại quota ngày.

Chỉ hỗ trợ vật phẩm xếp chồng: Herb, LinhDan, ThanThong, CongPhap, Recipe, Material. Trang bị và vật phẩm có thuộc tính riêng chưa hỗ trợ.

## Giao thức MemoryPack

| Tag | Request | Response / event |
|---|---|---|
| 3644 GetClanShopRequest | rỗng | GetClanShopResult: Success, Message, Items, ContribPoints, MaxQuantityPerRequest, MaxItemsAddedPerDay, ShopItemsAddedToday |
| 3648 BuyClanShopRequest | ShopItemId, Quantity | BuyClanShopResult: Success, Message, ShopItemId, Quantity, Cost, ContribPoints, InventoryItem |
| 3652 ClanMemberPointsChanged | Server push | ClanId, CharId, ContribPoints |
| 3656 AddClanShopItemRequest | Item, Price | AddClanShopItemResult: Success, Message, ShopItem, InventoryItem, MaxItemsAddedPerDay, ShopItemsAddedToday |
| 3660 RemoveClanShopItemRequest | ShopItemId | RemoveClanShopItemResult: Success, Message, ShopItemId, InventoryItem |
| 3664 ClanShopItemAdded | Server push | ShopItem |
| 3668 ClanShopItemRemoved | Server push | ClanId, ShopItemId |
| 3672 ClanShopItemChanged | Server push | ShopItem |

`InventoryItem.Count` trong response là tổng số lượng hiện có trong túi sau thao tác, không phải delta. Nếu `InventoryItem = null` sau khi thêm vào kho nghĩa là vật phẩm đó trong túi đã hết.

## Luồng chính

1. Mở kho: client gửi `GetClanShopRequest`, hiển thị `Items` và điểm hiện có.
2. Thêm vật phẩm: client gửi `AddClanShopItemRequest` với `Item.Id`, `Item.Type`, `Item.Count`, `Price`. Server kiểm tra quyền, membership, loại vật phẩm, giá, số lượng, vật phẩm trong túi và quota thêm vật phẩm trong ngày. Thành công thì trừ túi, tăng `ShopItemsAddedToday`, thêm DB/RAM, trả `ShopItem` cùng quota mới, rồi push `ClanShopItemAdded` cho cả Clan.
3. Mua: client gửi `BuyClanShopRequest`. `Quantity` là số vật phẩm muốn mua. Server từ chối nếu người mua là `OwnerCharId` của dòng bán; nếu hợp lệ thì lấy giá và item từ DB/RAM, trừ điểm cá nhân, cộng vật phẩm vào túi, giảm `Count` trong kho. Nếu còn hàng thì push `ClanShopItemChanged`; nếu hết hàng thì push `ClanShopItemRemoved`. Đồng thời push `ClanMemberPointsChanged`.
4. Xóa vật phẩm: client gửi `RemoveClanShopItemRequest`. Server kiểm tra quyền và chủ sở hữu dòng bán, xóa DB/RAM, trả vật phẩm về túi, rồi push `ClanShopItemRemoved`.
5. Thất bại: response `Success=false`, client hiển thị `Message` và không tự thay đổi túi/kho/điểm.

Request chưa có khóa idempotency: mỗi request hợp lệ là một giao dịch mới. Khi mất response, client nên sync lại Clan và túi đồ trước khi gửi lại thao tác. Client có thể hiển thị số còn được thêm trong ngày bằng `MaxItemsAddedPerDay - ShopItemsAddedToday`.

## Lưu dữ liệu và đồng thời

Handler giữ `CharacterLock`; manager dùng khóa Clan chung với cống hiến, kick và rời Clan. Các thao tác kho chỉ ghi DB cho `ClanShopItems`, `ClanMember.ContribPoints` và quota ngày khi thêm vật phẩm. Inventory của người chơi chỉ cập nhật RAM và `Tracking.Items`; DB túi đồ sẽ do cơ chế save nhân vật hiện có xử lý sau.

Kho lưu bằng bảng `ClanShopItems`, không dùng loop autosave Clan. `GetShop` đọc từ `reference.Clan.ShopItems`, nên thành viên đang watch Clan nhận thay đổi realtime qua event nhỏ.

Khi thành viên rời/bị kick, các vật phẩm do thành viên đó gửi vào kho được trả về túi trong DB trước khi xóa membership. Khi Clan bị giải tán, toàn bộ vật phẩm còn trong kho được trả về chủ sở hữu trong DB trước khi xóa Clan.

## Cấu hình

`Program.cs` đọc cấu hình kho Clan từ appsettings/environment:

| Key | Mặc định | Ý nghĩa |
|---|---:|---|
| ClanShop:MaxQuantityPerRequest | 100 | Số lượng tối đa được mua/thêm trong một request |
| ClanShop:MaxItemsAddedPerDay | 100 | Tổng số lượng vật phẩm mỗi thành viên được thêm vào kho trong một ngày; <= 0 khóa thao tác thêm vật phẩm |
