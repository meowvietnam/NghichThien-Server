START TRANSACTION;
CREATE TABLE `ClanCommandReceipts` (
    `ActorCharId` int NOT NULL,
    `RequestId` char(36) COLLATE ascii_general_ci NOT NULL,
    `CommandType` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `PayloadHash` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `ResultPayload` json NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_ClanCommandReceipts` PRIMARY KEY (`ActorCharId`, `RequestId`),
    CONSTRAINT `FK_ClanCommandReceipts_Characters_ActorCharId` FOREIGN KEY (`ActorCharId`) REFERENCES `Characters` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `ClanMemberPeriodStats` (
    `CharId` int NOT NULL,
    `ScopeId` int NOT NULL,
    `PeriodId` int NOT NULL,
    `CounterType` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    `Value` bigint NOT NULL,
    `Version` bigint NOT NULL,
    CONSTRAINT `PK_ClanMemberPeriodStats` PRIMARY KEY (`CharId`, `ScopeId`, `PeriodId`, `CounterType`),
    CONSTRAINT `FK_ClanMemberPeriodStats_Characters_CharId` FOREIGN KEY (`CharId`) REFERENCES `Characters` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `Clans` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `NormalizedName` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL,
    `ImageUrl` varchar(512) CHARACTER SET utf8mb4 NULL,
    `EmblemId` int NOT NULL,
    `Description` varchar(512) CHARACTER SET utf8mb4 NOT NULL,
    `Announcement` varchar(1024) CHARACTER SET utf8mb4 NOT NULL,
    `LeaderCharId` int NOT NULL,
    `Level` int NOT NULL,
    `Experience` bigint NOT NULL,
    `Funds` bigint NOT NULL,
    `MemberCount` int NOT NULL,
    `MaxMembers` int NOT NULL,
    `JoinPolicy` tinyint unsigned NOT NULL,
    `MinLevel` int NOT NULL,
    `Status` tinyint unsigned NOT NULL,
    `Version` bigint NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NOT NULL,
    `DissolvedAt` datetime(6) NULL,
    CONSTRAINT `PK_Clans` PRIMARY KEY (`Id`),
    CONSTRAINT `CK_Clan_Funds` CHECK (`Funds` >= 0),
    CONSTRAINT `CK_Clan_Members` CHECK (`MemberCount` >= 0 AND `MemberCount` <= `MaxMembers`),
    CONSTRAINT `FK_Clans_Characters_LeaderCharId` FOREIGN KEY (`LeaderCharId`) REFERENCES `Characters` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `ClanApplications` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `ClanId` int NOT NULL,
    `CharId` int NOT NULL,
    `Message` varchar(256) CHARACTER SET utf8mb4 NOT NULL,
    `Status` tinyint unsigned NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `ExpiresAt` datetime(6) NOT NULL,
    `ReviewedByCharId` int NULL,
    `ReviewedAt` datetime(6) NULL,
    `Version` bigint NOT NULL,
    CONSTRAINT `PK_ClanApplications` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ClanApplications_Characters_CharId` FOREIGN KEY (`CharId`) REFERENCES `Characters` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_ClanApplications_Characters_ReviewedByCharId` FOREIGN KEY (`ReviewedByCharId`) REFERENCES `Characters` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_ClanApplications_Clans_ClanId` FOREIGN KEY (`ClanId`) REFERENCES `Clans` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `ClanAuditLogs` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `ClanId` int NOT NULL,
    `ActorCharId` int NOT NULL,
    `TargetCharId` int NULL,
    `Action` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `Data` json NOT NULL,
    `RequestId` char(36) COLLATE ascii_general_ci NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_ClanAuditLogs` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ClanAuditLogs_Clans_ClanId` FOREIGN KEY (`ClanId`) REFERENCES `Clans` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `ClanLedgerEntries` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `TransactionId` char(36) COLLATE ascii_general_ci NOT NULL,
    `ClanId` int NOT NULL,
    `ActorCharId` int NOT NULL,
    `AccountType` tinyint unsigned NOT NULL,
    `Delta` bigint NOT NULL,
    `BalanceAfter` bigint NOT NULL,
    `Reason` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `RequestId` char(36) COLLATE ascii_general_ci NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_ClanLedgerEntries` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ClanLedgerEntries_Characters_ActorCharId` FOREIGN KEY (`ActorCharId`) REFERENCES `Characters` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_ClanLedgerEntries_Clans_ClanId` FOREIGN KEY (`ClanId`) REFERENCES `Clans` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `ClanMembers` (
    `CharId` int NOT NULL,
    `ClanId` int NOT NULL,
    `Role` tinyint unsigned NOT NULL,
    `TotalContribution` bigint NOT NULL,
    `AvailableContribution` bigint NOT NULL,
    `JoinedAt` datetime(6) NOT NULL,
    `Version` bigint NOT NULL,
    CONSTRAINT `PK_ClanMembers` PRIMARY KEY (`CharId`),
    CONSTRAINT `CK_ClanMember_Contribution` CHECK (`TotalContribution` >= 0 AND `AvailableContribution` >= 0),
    CONSTRAINT `CK_ClanMember_Role` CHECK (`Role` >= 0 AND `Role` <= 2),
    CONSTRAINT `FK_ClanMembers_Characters_CharId` FOREIGN KEY (`CharId`) REFERENCES `Characters` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_ClanMembers_Clans_ClanId` FOREIGN KEY (`ClanId`) REFERENCES `Clans` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `ClanMembershipStates` (
    `CharId` int NOT NULL,
    `LastClanId` int NULL,
    `LeftAt` datetime(6) NULL,
    `NextJoinAt` datetime(6) NULL,
    `LeaveReason` tinyint unsigned NULL,
    `Version` bigint NOT NULL,
    CONSTRAINT `PK_ClanMembershipStates` PRIMARY KEY (`CharId`),
    CONSTRAINT `FK_ClanMembershipStates_Characters_CharId` FOREIGN KEY (`CharId`) REFERENCES `Characters` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_ClanMembershipStates_Clans_LastClanId` FOREIGN KEY (`LastClanId`) REFERENCES `Clans` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `ClanOutboxEvents` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `EventId` char(36) COLLATE ascii_general_ci NOT NULL,
    `ClanId` int NOT NULL,
    `TargetCharId` int NULL,
    `ClanVersion` bigint NOT NULL,
    `EventType` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `PublishedAt` datetime(6) NULL,
    `RetryCount` int NOT NULL,
    `NextAttemptAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_ClanOutboxEvents` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ClanOutboxEvents_Clans_ClanId` FOREIGN KEY (`ClanId`) REFERENCES `Clans` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `ClanRewardClaims` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `CharId` int NOT NULL,
    `RewardKey` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
    `SourceClanId` int NOT NULL,
    `RequestId` char(36) COLLATE ascii_general_ci NOT NULL,
    `ClaimedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_ClanRewardClaims` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ClanRewardClaims_Characters_CharId` FOREIGN KEY (`CharId`) REFERENCES `Characters` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_ClanRewardClaims_Clans_SourceClanId` FOREIGN KEY (`SourceClanId`) REFERENCES `Clans` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_ClanApplications_CharId_Status` ON `ClanApplications` (`CharId`, `Status`);

CREATE UNIQUE INDEX `IX_ClanApplications_ClanId_CharId` ON `ClanApplications` (`ClanId`, `CharId`);

CREATE INDEX `IX_ClanApplications_ClanId_Status_CreatedAt_Id` ON `ClanApplications` (`ClanId`, `Status`, `CreatedAt`, `Id`);

CREATE INDEX `IX_ClanApplications_ReviewedByCharId` ON `ClanApplications` (`ReviewedByCharId`);

CREATE INDEX `IX_ClanAuditLogs_ClanId_Id` ON `ClanAuditLogs` (`ClanId`, `Id`);

CREATE INDEX `IX_ClanLedgerEntries_ActorCharId` ON `ClanLedgerEntries` (`ActorCharId`);

CREATE INDEX `IX_ClanLedgerEntries_ClanId_Id` ON `ClanLedgerEntries` (`ClanId`, `Id`);

CREATE INDEX `IX_ClanLedgerEntries_TransactionId` ON `ClanLedgerEntries` (`TransactionId`);

CREATE INDEX `IX_ClanMembers_ClanId_CharId` ON `ClanMembers` (`ClanId`, `CharId`);

CREATE INDEX `IX_ClanMembershipStates_LastClanId` ON `ClanMembershipStates` (`LastClanId`);

CREATE INDEX `IX_ClanOutboxEvents_ClanId` ON `ClanOutboxEvents` (`ClanId`);

CREATE UNIQUE INDEX `IX_ClanOutboxEvents_EventId` ON `ClanOutboxEvents` (`EventId`);

CREATE INDEX `IX_ClanOutboxEvents_PublishedAt_NextAttemptAt_Id` ON `ClanOutboxEvents` (`PublishedAt`, `NextAttemptAt`, `Id`);

CREATE UNIQUE INDEX `IX_ClanRewardClaims_CharId_RewardKey` ON `ClanRewardClaims` (`CharId`, `RewardKey`);

CREATE INDEX `IX_ClanRewardClaims_SourceClanId` ON `ClanRewardClaims` (`SourceClanId`);

CREATE INDEX `IX_Clans_LeaderCharId` ON `Clans` (`LeaderCharId`);

CREATE UNIQUE INDEX `IX_Clans_NormalizedName` ON `Clans` (`NormalizedName`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260909085245_AddClans', '9.0.3');

COMMIT;

