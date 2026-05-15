namespace MTS.Localization;

public class EnglishStrings : ILanguageStrings
{
    public override string LoginSuccess => "Login successful!";
    public override string ClientOnline => "Account is already logged in elsewhere!";
    public override string ConnectSuccess => "Connected to server successfully!";
    public override string SaveChangesCompleted => "Save changes completed!";
    public override string LimitCharacter => "Character limit reached!";
    public override string CharacterNotFound => "Character not found!";
    public override string SkinGenderInvalid => "Skin gender is invalid!";
    public override string SkinCategoryMissing => "Not all skin categories selected!";
    public override string SkinOptionInvalid => "Skin option does not exist!";
    public override string SkinColorMissing => "Not all skin colors selected!";
    public override string NameInvalid => "Character name is invalid!";
    public override string NameLengthInvalid => "Character name must be between 5 and 20 characters!";
    public override string NameSpecialCharInvalid => "Character name cannot contain special characters!";
    public override string NameExisted => "Character name already exists!";
    public override string StepInvalid => "Character creation step does not exist or is invalid!";
    public override string GroupInvalid => "Selection group is invalid!";
    public override string GroupExclusiveViolation => "Conflicting selection groups detected!";
    public override string ElementalInvalid => "Elemental root selection is invalid!";
    public override string ElementalCountMismatch => "Number of selected elemental roots does not match the requirement!";
    public override string ElementalCountInvalid => "Number of selected elemental roots is invalid!";
    public override string ElementalOptionInvalid => "Elemental root option does not exist!";
    public override string ElementalStatInvalid => "Elemental root stat is invalid!";
    public override string SelectionInvalid => "Selection is invalid!";
    public override string StatOptionInvalid => "Stat option does not exist!";
    public override string NotEnoughMoney => "Not enough talent points for this operation!";
    public override string IsIncomplete => "Character creation process is incomplete!";
    public override string ItemCountInvalid => "Invalid number of items selected (must select exactly 1 item)!";
    public override string CharacterLimitExceeded => "Cannot create more characters, limit reached!";
}
