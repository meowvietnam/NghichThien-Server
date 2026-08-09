namespace MTS.Localization;

public abstract class ILanguageStrings
{
    // Auth messages
    public abstract string LoginSuccess { get; }
    public abstract string GoogleLoginInvalid { get; }
    public abstract string GoogleAccountNotFound { get; }
    public abstract string AdminSecretKeyInvalid { get; }
    public abstract string AdminGoogleAccountNotFound { get; }
    public abstract string NotLoggedIn { get; }
    public abstract string DeleteAccountFailed { get; }
    public abstract string ClientOnline { get; }
    public abstract string ReconnectClientNotFound { get; }
    public abstract string UsernameRequired { get; }
    public abstract string UsernameTooShort { get; }
    public abstract string UsernameTooLong { get; }
    public abstract string UsernameContainsWhitespace { get; }
    public abstract string PasswordRequired { get; }
    public abstract string PasswordTooShort { get; }
    public abstract string PasswordTooLong { get; }
    public abstract string UsernameAlreadyExists { get; }
    public abstract string RegisterSuccess { get; }
    public abstract string UserDataInvalid { get; }
    public abstract string GuestAccountNotFound { get; }
    public abstract string UsernameOrPasswordInvalid { get; }
    public abstract string LoginWithAccountSuccess { get; }
    public abstract string GuestRegisterLimitReached { get; }
    public abstract string GuestAccountAlias { get; }

    // Connection messages
    public abstract string ConnectSuccess { get; }
    public abstract string SaveChangesCompleted { get; }

    // Character selection messages
    public abstract string LimitCharacter { get; }
    public abstract string CharacterNotFound { get; }
    public abstract string CharacterNotSelected { get; }

    // Character creation validation messages
    public abstract string SkinGenderInvalid { get; }
    public abstract string SkinCategoryMissing { get; }
    public abstract string SkinOptionInvalid { get; }
    public abstract string SkinColorMissing { get; }
    public abstract string NameInvalid { get; }
    public abstract string NameLengthInvalid { get; }
    public abstract string NameSpecialCharInvalid { get; }
    public abstract string NameExisted { get; }
    public abstract string StepInvalid { get; }
    public abstract string GroupInvalid { get; }
    public abstract string GroupExclusiveViolation { get; }
    public abstract string ElementalInvalid { get; }
    public abstract string ElementalCountMismatch { get; }
    public abstract string ElementalCountInvalid { get; }
    public abstract string ElementalOptionInvalid { get; }
    public abstract string ElementalStatInvalid { get; }
    public abstract string SelectionInvalid { get; }
    public abstract string StatOptionInvalid { get; }
    public abstract string NotEnoughNatural { get; }
    public abstract string IsIncomplete { get; }
    public abstract string ItemCountInvalid { get; }
    public abstract string CharacterLimitExceeded { get; }

    // Ngo Dao / Than Thong messages
    public abstract string NgoDaoNotExist { get; }
    public abstract string NgoDaoResourceNotExist { get; }
    public abstract string HasLearnedNgoDao { get; }
    public abstract string NotEnoughNgoDaoPoints { get; }
    public abstract string PreviousBranchRequiresAtLeastOneNgoDao { get; }
    public abstract string NotEnoughCamNgoPoints { get; }
    public abstract string ThanThongNotOwned { get; }

    // Cong Phap equip messages
    public abstract string SlotCongPhapInvalid { get; }
    public abstract string SlotCongPhapLevelRequired { get; }
    public abstract string SlotCongPhongNotEmpty { get; }
    public abstract string CongPhapNotOwned { get; }
    public abstract string SlotCongPhapEmpty { get; }
    public abstract string NotEnoughMoney { get;  }
    public abstract string MainCultivationRequiredForRetreat { get; }
    public abstract string ExceedLimit { get; }
    public abstract string CongPhapNotFound { get; }
    public abstract string CongPhapMaxLevelReached { get; }
    public abstract string NotEnoughThoNguyen { get; }
    public abstract string ThanThongNotFound { get; }
    public abstract string CongPhapOwned { get; }
    public abstract string ThanThongOwned { get; }
    public abstract string NotSupportItemType { get; }
    public abstract string ConvertElementalSkillItemCountInvalid { get; }
    public abstract string ConvertElementalSkillTypeSkillInvalid { get; }
    public abstract string ConvertElementalSkillItemTypeInvalid { get; }
    public abstract string ConvertElementalSkillItemQuantityInvalid { get; }
    public abstract string ConvertElementalSkillResourceNotFound { get; }
    public abstract string ConvertElementalSkillResultNotFound { get; }
    public abstract string ConvertElementalSkillSuccess { get; }

    // Quest / Mon Phai messages
    public abstract string NotCompleteFirstMainQuest { get; }
    public abstract string MonPhaiExisted { get; }
    public abstract string HackerMainQuest { get; }
    public abstract string MainQuestResourceNotExist { get; }
    public abstract string HackerSideQuest { get; }
    public abstract string MainQuestRewardAlreadyClaimed { get; }
    public abstract string MainQuestNotCompleted { get; }
    public abstract string DailyQuestRewardAlreadyClaimed { get; }
    public abstract string DailyQuestNotFound { get; }
    public abstract string SideQuestAdRewardNotAvailable { get; }
    public abstract string SideQuestAdRewardAlreadyClaimed { get; }

    // Combat validation messages
    public abstract string HackerLevelEnemyCombat { get; }
    public abstract string HackerHP { get; }
    public abstract string CombatAlreadyInProgress { get; }
    public abstract string CombatSessionNotFound { get; }
    public abstract string CombatSessionInvalid { get; }
    public abstract string CombatAdRewardNotAvailable { get; }
    public abstract string HackerLevelBossCombat { get; }
    public abstract string HackerSkillCombat { get; }

    // Equipment messages
    public abstract string HackerEquipment { get; }
    public abstract string HackerLevelNpc { get; }
    public abstract string NotEnoughCongPhap { get; }
    public abstract string CongPhapEquippedCannotBreakthrough { get; }
    public abstract string NpcNotExist { get; }
    public abstract string NpcGiftValueInvalid { get; }
    public abstract string HackerSkillNotExistOnNpc { get; }
    public abstract string NpcSkillRequestInvalid { get; }
    public abstract string HackerTangQuaNPC { get; }

    // IAP messages
    public abstract string IapPurchaseAlreadyProcessed { get; }
    public abstract string IapStoreNotSupported { get; }
    public abstract string IapProductNotFound { get; }
    public abstract string PaymentOrderNotFound { get; }
    public abstract string PaymentAmountInvalid { get; }
    public abstract string PaymentOrderStatusInvalid { get; }
    public abstract string MoneyVipInvalid { get; }
    public abstract string NotEnoughMoneyVip { get; }

    // Shop messages
    public abstract string ShopTypeInvalid { get; }
    public abstract string ShopMonPhaiInvalid { get; }
    public abstract string ShopItemNotFound { get; }
    public abstract string ShopItemQuantityInvalid { get; }
    public abstract string InventoryItemNotFound { get; }
    public abstract string InventoryItemQuantityInvalid { get; }

    // Character action messages
    public abstract string RetreatDurationInvalid { get; }
    public abstract string TamCanhBelowMinimum { get; }
    public abstract string GiftItemQuantityInvalid { get; }
    public abstract string GiftcodeInvalid { get; }
    public abstract string GiftcodeOutOfUses { get; }
    public abstract string GiftcodeExpired { get; }
    public abstract string GiftcodeAlreadyUsed { get; }
    public abstract string GiftcodeUnavailable { get; }
    public abstract string GiftcodeClaimSuccess { get; }
    public abstract string GiftcodeMailTitle { get; }
    public abstract string GiftcodeMailContent { get; }

    // Linh Dan messages
    public abstract string LinhDanQuantityInvalid { get; }
    public abstract string LinhDanNotFound { get; }
    public abstract string LinhDanCombatBuffInvalid { get; }
    public abstract string DanCombatLimitExceeded { get; }
    public abstract string SpecialLevelUpNotEnoughExp { get; }
    public abstract string SpecialLevelUpMaxLevelReached { get; }
    public abstract string SpecialLevelUpNotRequired { get; }
    public abstract string SpecialLevelUpItemQuantityInvalid { get; }
    public abstract string SpecialLevelUpItemNotFound { get; }

    // Arena messages
    public abstract string ArenaRewardNotFound { get; }
    public abstract string ArenaRewardAlreadyClaimed { get; }
    public abstract string ArenaRankNotReached { get; }

    // Ads messages
    public abstract string AdsMoneyVipLimitReached { get; }

    // Auction messages
    public abstract string AuctionNotOpen { get; }
    public abstract string AuctionServiceNotReady { get; }
    public abstract string AuctionRewardReceived { get; }
    public abstract string AuctionBidRefunded { get; }
    public abstract string AuctionSessionEnded { get; }
    public abstract string AuctionBidAmountInvalid { get; }
    public abstract string AuctionInfoContent { get; }
    public abstract string AuctionAlreadyJoined { get; }
    public abstract string AuctionHostSessionEnded { get; }
    public abstract string AuctionHostBidPlaced { get; }
    public abstract string AuctionHostPlayerJoined { get; }
    public abstract string[] AuctionHostRandomChats { get; }
}
