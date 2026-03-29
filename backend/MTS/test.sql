UPDATE `Characters`
SET `CamNgoSaves`    = @p0,
    `Colors`         = @p1,
    `CongPhapOwner`  = @p2,
    `CurrentEnergy`  = @p3,
    `DanCombatUsed`  = @p4,
    `IdCurrentNode`  = @p5,
    `NgoDaoSaves`    = @p6,
    `Skins`          = @p7,
    `ThanThongEquip` = @p8,
    `ThanThongOwner` = @p9
WHERE `Id` = @p10;
SELECT ROW_COUNT();
