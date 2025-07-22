-- RefreshTokens tablosu oluþturulmasý
CREATE TABLE [dbo].[RefreshTokens] (
    [Id] [uniqueidentifier] NOT NULL PRIMARY KEY DEFAULT (NEWID()),
    [UserId] [nvarchar](50) NOT NULL,
    [TokenHash] [nvarchar](256) NOT NULL,
    [ExpiresAt] [datetime2] NOT NULL,
    [CreatedAt] [datetime2] NOT NULL DEFAULT (GETUTCDATE()),
    [IsRevoked] [bit] NOT NULL DEFAULT (0),
    [RevokedAt] [datetime2] NULL,
    [ReplacedByToken] [nvarchar](256) NULL
);
-- Index oluþturulmasý
CREATE INDEX [IX_RefreshTokens_UserId] ON [dbo].[RefreshTokens] ([UserId]);
CREATE INDEX [IX_RefreshTokens_TokenHash] ON [dbo].[RefreshTokens] ([TokenHash]);
CREATE INDEX [IX_RefreshTokens_ExpiresAt] ON [dbo].[RefreshTokens] ([ExpiresAt]); 