namespace MTS.Network;

public enum MessageTag
{
    Connect = 21,
    ConnectResult = 22,
    CheckVersion = 28,
    CheckVersionResult = 29,
    Reconnect = 30,
    ReconnectResult = 32,
    LoginDeveloper = 35,
    LoginDeveloperResult = 38,
    GetCharacters = 39,
    GetCharactersResult = 40,
    CanCreateCharacter = 41,
    CanCreateCharacterResult = 42,
    BackToSelectChar = 48,
    BackToSelectCharOk = 49,
    SetCharName = 56,
    SetCharNameResult = 58,
    SetSkins = 61,
    SetSkinsResult = 68,
    ApplyStats = 89,
    ApplyStatsResult = 90,
    SelectCharacter = 91,
    SelectCharacterResult = 92,
    MoveToMap = 93,
    MoveToMapResult = 94,
    GetPlayerOnlineThisNode = 101,
    GetPlayerOnlineThisNodeResult = 102,
    GetMails = 105,
    GetMailsResult = 106,
    ReadMail = 107,
    MarkMailRead = 108,
    RemoveMail = 109,
    RemoveMailId = 110,
    RemoveAllMails = 111,
    RemoveAllMailsResult = 112,
    ClaimAllMails = 113,
    ClaimAllMailsResult = 114,
    ClaimMail = 115,
    ClaimMailResult = 116,
    NewMail = 119,
    GetInventory = 195,
    GetInventoryResult = 196,
    EquipEquipment = 197,
    EquipEquipmentResult = 198,
    GetLuyenDanConfigRequest = 200,
    LuyenDan = 201,
    LuyenDanResult = 202,


    AddItems = 209,
    AddItemsResult = 210,
    SetInfinityEnergy = 220,
    SetNgoDaoDiem = 230,
    SetNgoDaoDiemResult = 231,
    SetCamNgo = 232,
    SetCamNgoResult = 233,
    AddThanThong = 234,
    AddThanThongResult = 235,
    AddCongPhap = 236,
    AddCongPhapResult = 237,
    SetMoneyVip = 238,
    SetMoneyVipResult = 239,
    SetMoney = 240,
    SetMoneyResult = 241,
    SetExpRequest = 243,
    TestRequest = 245,

    ChangeScene = 256,
    ChangeSceneResult = 257,
    LuyenKhi = 310,
    LuyenKhiResult = 311,
    SetEquipmentName = 320,
    SetEquipmentNameResult = 321,

    LearnNgoDao = 332,
    LearnNgoDaoResult = 333,

    EquipThanThong = 340,
    EquipThanThongResult = 341,
    UnequipThanThong = 342,
    UnequipThanThongResult = 343,

    EquipCongPhap = 352,
    EquipCongPhapResult = 353,
    UnequipCongPhap = 354,
    UnequipCongPhapResult = 355,

    TuLuyen = 370,
    TuLuyenResult = 371,

    LinhNgo = 382,
    LinhNgoResult = 383,
    DotPhaCongPhap = 384,
    DotPhaCongPhapResult = 385,
    UseDanRequest = 386,

    JoinMonPhai = 392,
    JoinMonPhaiResult = 393,

    CompleteMainQuest = 401,
    CompleteMainQuestResult = 402,
    CompleteSideQuest = 403,
    CompleteSideQuestResult = 404,
    GetDailyQuestRequest = 406,
    DoneDailyQuestRequest = 407,
    ReloadDailyQuest = 408,
    ReloadDailyQuestResult = 409,

    ResultCombat = 413,
    ResultCombatResult = 414,

    LoadCharCombat = 415,
    LoadCharCombatResult = 416,
    LoadCharCombatArena = 417,
    LoadCharCombatArenaResult = 418,
    LoadRankRequest = 419,
    ClaimRankRewardRequest = 420,

    WatchAdsForEnergy = 450,
    WatchAdsForEnergyResult = 451,

    GetProductsRequest = 511,
    IapPurchase = 520,
    PurchaseIapResult = 521,
    ExchangeMoneyVipRequest = 522,

    ThinhGiaoKyNang = 610,
    ThinhGiaoKyNangResult = 611,
    TangQuaNpc = 612,
    TangQuaNpcResult = 613,
    Gacha = 614,
    GachaResult = 615,


    LoadShopRequest = 635,

    ReloadShop = 636,
    ReloadShopResult = 637,


    BuyAndSellRequest = 647,


    LevelUpdated = 783,
    SpecialLevelUpRequest = 789,

    GetAdsMoneyVipConfigRequest = 843,

    WatchAdsMoneyVip = 854,
    WatchAdsMoneyVipResult = 855,

    ActiveAdsMoneyVip = 856,
    WatchAdsCombat = 858,
    WatchAdsCombatResult = 859,
    WatchAds = 861,

    WatchAdsSideQuest = 872,
    WatchAdsSideQuestResult = 873,

    GetRankRequest = 887,
    CompleteAds = 912,
    CloseAds = 913,
    LoadAdsFail = 914,
    ResetArenaRank = 923,


    LoginRequest = 934,

    LogoutRequest = 948,
    PlayRequest = 959,
    DeleteAccountRequest = 969,


    InGameLogout = 987,
    InGameLogoutResult = 988,
    BackToMainMenuRequest = 992,
    Ping = 1029,
    Pong = 1030,
    SyncCharacterData = 1062,
    SyncCharacterDataResult = 1063,

    ExperienceReward = 1089,
    DailyReset = 1123,

    ConvertElementalSkillRequest = 1232,

    JoinAuction = 1234,
    JoinAuctionResult = 1235,
    GetAuctionData = 1256,
    GetAuctionDataResult = 1268,
    AuctionBid = 1291,
    AuctionBidResult = 1292,
    ReplaceAuctionPlayer = 1304,
    GetAuctionInfo = 1315,
    GetAuctionInfoResult = 1316,
    HighestBidChanged = 1331,
    RefundAuctionBid = 1339,
    HostChat = 1346,
    AuctionReward = 1357,
    NewAuctionSession = 1362,

    GetAuctionInfo2Request = 1366,

    Notify = 1595,
    AdminNotify = 1597,

    RegisterRequest = 1634,
    GuestRegisterRequest = 1683,
    ResetNgoDaoRequest = 1752,
    CreateNewAuctionSessionRequest = 1812,
    RemoveLastAuctionSessionRequest = 1816,
    CanBuyProductRequest = 1921,
    GetTrungSinhConfigRequest = 1933,
    ToTrungSinhRequest = 1954,
    TrungSinhRequest = 1996,
    
    GetMonsterKillRequest = 2009,
    ClaimMonsterKillRewardRequest = 2032,
}