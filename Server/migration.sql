IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [AspNetRoles] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ContactForms] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(max) NOT NULL,
    [FormType] int NOT NULL,
    [Category] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [Email] nvarchar(max) NOT NULL,
    [ClientIP] nvarchar(max) NOT NULL,
    [SubmissionTimestamp] datetime2 NOT NULL,
    [RequestType] int NULL,
    [UrgencyLevel] int NULL,
    [PreferredDate] datetime2 NULL,
    [PriorityLevel] int NULL,
    [Location] nvarchar(max) NOT NULL,
    [ConcernCategory] int NULL,
    [SuggestionCategory] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [IsRead] bit NOT NULL,
    [IsResolved] bit NOT NULL,
    [Status] int NOT NULL,
    [IsArchived] bit NOT NULL,
    [ArchivedDate] datetime2 NULL,
    CONSTRAINT [PK_ContactForms] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [EventPlans] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [ImageUrl] nvarchar(max) NOT NULL,
    [EventDate] datetime2 NOT NULL,
    [ExpiryDate] datetime2 NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedBy] nvarchar(max) NOT NULL,
    [UserRole] nvarchar(50) NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [Location] nvarchar(200) NOT NULL,
    [EventType] nvarchar(50) NOT NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [MaxAttendees] int NULL,
    [ImageFileName] nvarchar(255) NOT NULL,
    [ImageContentType] nvarchar(100) NOT NULL,
    CONSTRAINT [PK_EventPlans] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Notifications] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NULL,
    [Title] nvarchar(200) NOT NULL,
    [Message] nvarchar(1000) NOT NULL,
    [Type] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [IsRead] bit NOT NULL DEFAULT CAST(0 AS bit),
    [ReferenceId] nvarchar(100) NOT NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Staff] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Position] nvarchar(100) NOT NULL,
    [Bio] nvarchar(500) NULL,
    [ImageUrl] nvarchar(max) NULL,
    [FacebookUrl] nvarchar(max) NULL,
    [InstagramUrl] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETDATE()),
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_Staff] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [SubmissionTrackings] (
    [Id] int NOT NULL IDENTITY,
    [ClientIP] nvarchar(max) NOT NULL,
    [SubmissionTime] datetime2 NOT NULL,
    [IsBanned] bit NOT NULL,
    [BanEndTime] datetime2 NULL,
    CONSTRAINT [PK_SubmissionTrackings] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [Role] nvarchar(50) NOT NULL DEFAULT N'Client',
    [IsEmailVerified] bit NOT NULL DEFAULT CAST(0 AS bit),
    [EmailVerificationToken] nvarchar(100) NULL,
    [EmailVerificationTokenExpiry] datetime2 NULL,
    [LastEmailSentTime] datetime2 NULL,
    [PasswordResetToken] nvarchar(max) NULL,
    [PasswordResetTokenExpiry] datetime2 NULL,
    [LastPasswordResetEmailSentTime] datetime2 NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [LastLoginAt] datetime2 NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] int NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] int NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserRoles] (
    [UserId] int NOT NULL,
    [RoleId] int NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserTokens] (
    [UserId] int NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Bills] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [Description] nvarchar(200) NOT NULL,
    [BillType] nvarchar(50) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [BillDate] datetime2 NOT NULL,
    [DueDate] datetime2 NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [IsPaid] bit NOT NULL,
    [PaymentDate] datetime2 NULL,
    [CubicMeter] decimal(18,2) NULL,
    [PreviousCubicMeter] decimal(18,2) NULL,
    [PricePerCubicMeter] decimal(18,2) NULL,
    [ORNumber] nvarchar(50) NULL,
    CONSTRAINT [PK_Bills] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Bills_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [BoardMessages] (
    [MessageId] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [Content] nvarchar(1000) NOT NULL,
    [Timestamp] datetime2 NOT NULL,
    [IsRead] bit NOT NULL DEFAULT CAST(0 AS bit),
    [IsDelivered] bit NOT NULL,
    [Priority] nvarchar(max) NOT NULL,
    [Type] nvarchar(50) NOT NULL,
    CONSTRAINT [PK_BoardMessages] PRIMARY KEY ([MessageId]),
    CONSTRAINT [FK_BoardMessages_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [CubicMeterPriceSettings] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [PricePerCubicMeter] decimal(18,2) NOT NULL,
    [LastUpdated] datetime2 NOT NULL,
    CONSTRAINT [PK_CubicMeterPriceSettings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CubicMeterPriceSettings_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [CubicMeterReadings] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [Reading] decimal(18,2) NOT NULL,
    [PricePerCubicMeter] decimal(18,2) NOT NULL,
    [ReadingDate] datetime2 NOT NULL,
    CONSTRAINT [PK_CubicMeterReadings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CubicMeterReadings_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Submissions] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [Subject] nvarchar(200) NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(2000) NOT NULL,
    [Type] nvarchar(50) NOT NULL,
    [Priority] nvarchar(50) NOT NULL,
    [Status] int NOT NULL,
    [Category] nvarchar(100) NOT NULL,
    [Location] nvarchar(200) NOT NULL,
    [PreferredDate] datetime2 NULL,
    [SubmittedDate] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [LastUpdated] datetime2 NULL,
    [Attachments] nvarchar(1000) NOT NULL,
    CONSTRAINT [PK_Submissions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Submissions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [UserDetails] (
    [UserId] int NOT NULL,
    [Gender] nvarchar(max) NULL,
    [Nationality] nvarchar(max) NULL,
    [DateOfBirth] datetime2 NULL,
    [PhotoFileName] nvarchar(max) NULL,
    [PhotoUrl] nvarchar(max) NULL,
    [PhotoContentType] nvarchar(max) NULL,
    [FullName] nvarchar(max) NULL,
    [Address] nvarchar(max) NULL,
    [MaritalStatus] nvarchar(max) NULL,
    [ContactNumber] nvarchar(11) NULL,
    CONSTRAINT [PK_UserDetails] PRIMARY KEY ([UserId]),
    CONSTRAINT [FK_UserDetails_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [WaterMeterReadings] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [Reading] decimal(18,2) NOT NULL,
    [ReadingDate] datetime2 NOT NULL,
    CONSTRAINT [PK_WaterMeterReadings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WaterMeterReadings_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [MessageReads] (
    [MessageId] int NOT NULL,
    [UserId] int NOT NULL,
    [IsRead] bit NOT NULL DEFAULT CAST(0 AS bit),
    CONSTRAINT [PK_MessageReads] PRIMARY KEY ([MessageId], [UserId]),
    CONSTRAINT [FK_MessageReads_BoardMessages_MessageId] FOREIGN KEY ([MessageId]) REFERENCES [BoardMessages] ([MessageId]) ON DELETE CASCADE,
    CONSTRAINT [FK_MessageReads_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
);
GO

CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
GO

CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
GO

CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
GO

CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
GO

CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
GO

CREATE INDEX [IX_Bills_UserId] ON [Bills] ([UserId]);
GO

CREATE INDEX [IX_BoardMessages_UserId] ON [BoardMessages] ([UserId]);
GO

CREATE INDEX [IX_CubicMeterPriceSettings_UserId] ON [CubicMeterPriceSettings] ([UserId]);
GO

CREATE INDEX [IX_CubicMeterReadings_UserId] ON [CubicMeterReadings] ([UserId]);
GO

CREATE INDEX [IX_EventPlans_EventDate] ON [EventPlans] ([EventDate]);
GO

CREATE INDEX [IX_EventPlans_ExpiryDate] ON [EventPlans] ([ExpiryDate]);
GO

CREATE INDEX [IX_EventPlans_Status] ON [EventPlans] ([Status]);
GO

CREATE INDEX [IX_MessageReads_UserId] ON [MessageReads] ([UserId]);
GO

CREATE INDEX [IX_Submissions_UserId] ON [Submissions] ([UserId]);
GO

CREATE INDEX [EmailIndex] ON [Users] ([NormalizedEmail]);
GO

CREATE UNIQUE INDEX [UserNameIndex] ON [Users] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
GO

CREATE INDEX [IX_WaterMeterReadings_UserId] ON [WaterMeterReadings] ([UserId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250405234349_InitialCreate', N'7.0.20');
GO

COMMIT;
GO

