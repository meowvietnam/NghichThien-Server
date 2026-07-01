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
    public override string NotEnoughNatural => "Not enough talent points for this operation!";
    public override string IsIncomplete => "Character creation process is incomplete!";
    public override string ItemCountInvalid => "Invalid number of items selected (must select exactly 1 item)!";
    public override string CharacterLimitExceeded => "Cannot create more characters, limit reached!";
    public override string NgoDaoNotExist => "This Ngo Dao does not exist!";
    public override string NgoDaoResourceNotExist => "Ngo Dao resource does not exist!";
    public override string HasLearnedNgoDao => "This Ngo Dao has already been learned!";
    public override string NotEnoughNgoDaoPoints => "Not enough Ngo Dao points!";
    public override string PreviousBranchRequiresAtLeastOneNgoDao => "Must learn at least one Ngo Dao from the previous branch!";
    public override string NotEnoughCamNgoPoints => "Not enough Cam Ngo points!";
    public override string ThanThongNotOwned => "This Than Thong is not owned!";
    public override string SlotCongPhapInvalid => "Cong Phap equipment slot is invalid!";
    public override string SlotCongPhongNotEmpty => "Cong Phap equipment slot is not empty!";
    public override string CongPhapNotOwned => "This Cong Phap is not owned!";
    public override string SlotCongPhapEmpty => "Cong Phap equipment slot is empty!";
    public override string NotEnoughMoney => "Not enough Linh Thach!";

    public override string MainCultivationRequiredForRetreat =>
        "Only the main cultivation technique can be used for closed-door cultivation!";

    public override string ExceedLimit => "Retreat duration exceeds the allowed limit!";
    public override string CongPhapNotFound => "Cong Phap does not exist!";
    public override string ThanThongNotFound => "Than Thong does not exist!";
    public override string CongPhapOwned => "This Cong Phap is already owned!";
    public override string ThanThongOwned => "This Than Thong is already owned!";
    public override string NotSupportItemType => "Item type is not supported!";
    public override string NotCompleteFirstMainQuest => "The first main quest must be completed!";
    public override string MonPhaiExisted => "A sect has already been joined!";
    public override string HackerMainQuest => "Main quest data is invalid!";
    public override string MainQuestResourceNotExist => "Main quest resource does not exist!";
    public override string HackerSideQuest => "Side quest data is invalid!";
    public override string HackerLevelEnemyCombat => "Enemy level in combat is invalid!";
    public override string HackerHP => "Post-combat HP is invalid!";
    public override string HackerLevelBossCombat => "Boss level in combat is invalid!";
    public override string HackerSkillCombat => "Skill used in combat is invalid!";
    public override string HackerEquipment => "Equipment data is invalid!";
    public override string HackerLevelNpc => "NPC level is invalid!";
    public override string NotEnoughCongPhap => "Not enough Cong Phap!";
    public override string NpcNotExist => "NPC does not exist!";
    public override string HackerSkillNotExistOnNpc => "Skill does not exist on this NPC!";
    public override string HackerTangQuaNPC => "NPC gift data is invalid!";
    public override string IapPurchaseAlreadyProcessed => "IAP purchase has already been processed!";
    public override string MoneyVipInvalid => "Invalid Tien Ngoc amount!";
    public override string ShopItemNotFound => "Item does not exist in the shop!";
    public override string ShopItemQuantityInvalid => "The shop does not have enough of this item!";
    public override string InventoryItemNotFound => "Item does not exist in the inventory!";
    public override string InventoryItemQuantityInvalid => "The inventory does not contain enough of this item!";
    public override string RetreatDurationInvalid => "Retreat duration must be at least 30 days!";
    public override string GiftItemQuantityInvalid => "Gift item quantity is invalid!";
}
