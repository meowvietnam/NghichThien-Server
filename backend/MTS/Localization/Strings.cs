namespace MTS.Localization;

public static class Strings
{
    private static ILanguageStrings _current = new VietnameseStrings();

    public static Language CurrentLanguage
    {
        get => _current is VietnameseStrings ? Language.Vietnamese : Language.English;
        set => _current = value == Language.Vietnamese ? new VietnameseStrings() : new EnglishStrings();
    }

    public static string LoginSuccess => _current.LoginSuccess;
    public static string ClientOnline => _current.ClientOnline;
    public static string ConnectSuccess => _current.ConnectSuccess;
    public static string SaveChangesCompleted => _current.SaveChangesCompleted;
    public static string LimitCharacter => _current.LimitCharacter;
    public static string CharacterNotFound => _current.CharacterNotFound;
    public static string SkinGenderInvalid => _current.SkinGenderInvalid;
    public static string SkinCategoryMissing => _current.SkinCategoryMissing;
    public static string SkinOptionInvalid => _current.SkinOptionInvalid;
    public static string SkinColorMissing => _current.SkinColorMissing;
    public static string NameInvalid => _current.NameInvalid;
    public static string NameLengthInvalid => _current.NameLengthInvalid;
    public static string NameSpecialCharInvalid => _current.NameSpecialCharInvalid;
    public static string NameExisted => _current.NameExisted;
    public static string StepInvalid => _current.StepInvalid;
    public static string GroupInvalid => _current.GroupInvalid;
    public static string GroupExclusiveViolation => _current.GroupExclusiveViolation;
    public static string ElementalInvalid => _current.ElementalInvalid;
    public static string ElementalCountMismatch => _current.ElementalCountMismatch;
    public static string ElementalCountInvalid => _current.ElementalCountInvalid;
    public static string ElementalOptionInvalid => _current.ElementalOptionInvalid;
    public static string ElementalStatInvalid => _current.ElementalStatInvalid;
    public static string SelectionInvalid => _current.SelectionInvalid;
    public static string StatOptionInvalid => _current.StatOptionInvalid;
    public static string NotEnoughNatural => _current.NotEnoughNatural;
    public static string IsIncomplete => _current.IsIncomplete;
    public static string ItemCountInvalid => _current.ItemCountInvalid;
    public static string CharacterLimitExceeded => _current.CharacterLimitExceeded;
    public static string NgoDaoNotExist => _current.NgoDaoNotExist;
    public static string NgoDaoResourceNotExist => _current.NgoDaoResourceNotExist;
    public static string HasLearnedNgoDao => _current.HasLearnedNgoDao;
    public static string NotEnoughNgoDaoPoints => _current.NotEnoughNgoDaoPoints;
    public static string PreviousBranchRequiresAtLeastOneNgoDao => _current.PreviousBranchRequiresAtLeastOneNgoDao;
    public static string NotEnoughCamNgoPoints => _current.NotEnoughCamNgoPoints;
    public static string ThanThongNotOwned => _current.ThanThongNotOwned;
    public static string SlotCongPhapInvalid => _current.SlotCongPhapInvalid;
    public static string SlotCongPhongNotEmpty => _current.SlotCongPhongNotEmpty;
    public static string CongPhapNotOwned => _current.CongPhapNotOwned;
    public static string SlotCongPhapEmpty => _current.SlotCongPhapEmpty;
    public static string NotEnoughMoney => _current.NotEnoughMoney;
    public static string MainCultivationRequiredForRetreat => _current.MainCultivationRequiredForRetreat;
    public static string ExceedLimit => _current.ExceedLimit;
    public static string CongPhapNotFound => _current.CongPhapNotFound;
    public static string ThanThongNotFound => _current.ThanThongNotFound;
    public static string CongPhapOwned => _current.CongPhapOwned;
    public static string ThanThongOwned => _current.ThanThongOwned;
    public static string NotSupportItemType => _current.NotSupportItemType;
}
