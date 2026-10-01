using MTS.Exceptions;

namespace MTS.IAP;

/// <summary>
/// Khoá tạm thời việc nạp theo từng cửa hàng. Chặn ở bước kiểm tra trước khi mua (CanBuyProduct) nên người chơi
/// không bị trừ tiền. Giao dịch đã thanh toán trước đó (receipt gửi lên để xác thực) vẫn được xử lý bình thường
/// để không mất tiền của người chơi. Bỏ store khỏi danh sách để mở lại.
/// </summary>
public static class IapStorePolicy
{
    private static readonly HashSet<Store> DisabledStores = [Store.AppStore];

    public const string DisabledMessage = "Tạm thời chưa hỗ trợ nạp qua App Store, vui lòng thử lại sau.";

    public static bool IsDisabled(Store store) => DisabledStores.Contains(store);

    public static void EnsurePurchasable(Store store)
    {
        if (IsDisabled(store)) throw new InvalidException(DisabledMessage);
    }
}
