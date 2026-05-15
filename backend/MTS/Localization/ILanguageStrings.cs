namespace MTS.Localization;

public abstract class ILanguageStrings
{
    // Auth messages
    public abstract string LoginSuccess { get; }
    public abstract string ClientOnline { get; }

    // Connection messages
    public abstract string ConnectSuccess { get; }
    public abstract string SaveChangesCompleted { get; }

    // Character selection messages
    public abstract string LimitCharacter { get; }
    public abstract string CharacterNotFound { get; }

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
    public abstract string NotEnoughMoney { get; }
    public abstract string IsIncomplete { get; }
    public abstract string ItemCountInvalid { get; }
    public abstract string CharacterLimitExceeded { get; }
}
