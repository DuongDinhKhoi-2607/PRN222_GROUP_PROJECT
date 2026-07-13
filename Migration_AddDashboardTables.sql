/*
  Migration Script: Add pro_upgrades and token_usage_logs tables
  Run this on your existing RAGChatbotDB database
*/

USE [RAGChatbotDB]
GO

-- ── Add columns to users table if not exist ──────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('users') AND name = 'available_tokens')
BEGIN
    ALTER TABLE [dbo].[users] ADD [available_tokens] INT NOT NULL DEFAULT 20;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('users') AND name = 'last_token_update_time')
BEGIN
    ALTER TABLE [dbo].[users] ADD [last_token_update_time] DATETIME2(7) NOT NULL DEFAULT GETDATE();
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('users') AND name = 'is_pro')
BEGIN
    ALTER TABLE [dbo].[users] ADD [is_pro] BIT NOT NULL DEFAULT 0;
END
GO

-- ── Create pro_upgrades table ────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'pro_upgrades')
BEGIN
    CREATE TABLE [dbo].[pro_upgrades](
        [id] [bigint] IDENTITY(1,1) NOT NULL,
        [user_id] [bigint] NOT NULL,
        [amount] [decimal](18,2) NOT NULL,
        [payment_method] [varchar](50) NOT NULL DEFAULT 'VNPay',
        [transaction_id] [nvarchar](255) NULL,
        [upgraded_at] [datetime2](7) NULL DEFAULT (getdate()),
    PRIMARY KEY CLUSTERED ([id] ASC)
    );

    CREATE NONCLUSTERED INDEX [IX_pro_upgrades_user_id] ON [dbo].[pro_upgrades] ([user_id] ASC);

    ALTER TABLE [dbo].[pro_upgrades] WITH CHECK ADD CONSTRAINT [FK_pro_upgrades_users]
        FOREIGN KEY([user_id]) REFERENCES [dbo].[users] ([id]);
    ALTER TABLE [dbo].[pro_upgrades] CHECK CONSTRAINT [FK_pro_upgrades_users];
END
GO

-- ── Create token_usage_logs table ────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'token_usage_logs')
BEGIN
    CREATE TABLE [dbo].[token_usage_logs](
        [id] [bigint] IDENTITY(1,1) NOT NULL,
        [user_id] [bigint] NOT NULL,
        [tokens_used] [int] NOT NULL,
        [action] [varchar](50) NOT NULL DEFAULT 'chat',
        [used_at] [datetime2](7) NULL DEFAULT (getdate()),
    PRIMARY KEY CLUSTERED ([id] ASC)
    );

    CREATE NONCLUSTERED INDEX [IX_token_usage_logs_user_id] ON [dbo].[token_usage_logs] ([user_id] ASC);
    CREATE NONCLUSTERED INDEX [IX_token_usage_logs_used_at] ON [dbo].[token_usage_logs] ([used_at] ASC);

    ALTER TABLE [dbo].[token_usage_logs] WITH CHECK ADD CONSTRAINT [FK_token_usage_logs_users]
        FOREIGN KEY([user_id]) REFERENCES [dbo].[users] ([id]);
    ALTER TABLE [dbo].[token_usage_logs] CHECK CONSTRAINT [FK_token_usage_logs_users];
END
GO

-- ── Seed sample data for demonstration ───────────────────────────────────────
-- Insert sample pro upgrades (only if table is empty)
IF NOT EXISTS (SELECT TOP 1 1 FROM [dbo].[pro_upgrades])
BEGIN
    -- Get student IDs (create temp students if needed for demo)
    DECLARE @studentId BIGINT;
    SELECT TOP 1 @studentId = id FROM [dbo].[users] WHERE role = 'student' AND is_active = 1;

    IF @studentId IS NOT NULL
    BEGIN
        -- Sample pro upgrades across different months
        INSERT INTO [dbo].[pro_upgrades] ([user_id], [amount], [payment_method], [upgraded_at])
        VALUES
            (@studentId, 99000, 'VNPay', DATEADD(MONTH, -5, GETDATE())),
            (@studentId, 99000, 'VNPay', DATEADD(MONTH, -4, GETDATE())),
            (@studentId, 99000, 'VNPay', DATEADD(MONTH, -3, GETDATE())),
            (@studentId, 99000, 'VNPay', DATEADD(MONTH, -2, GETDATE())),
            (@studentId, 99000, 'VNPay', DATEADD(MONTH, -1, GETDATE())),
            (@studentId, 99000, 'VNPay', GETDATE());

        -- Sample token usage logs
        INSERT INTO [dbo].[token_usage_logs] ([user_id], [tokens_used], [action], [used_at])
        VALUES
            (@studentId, 4, 'chat', DATEADD(MONTH, -5, GETDATE())),
            (@studentId, 4, 'chat', DATEADD(MONTH, -4, GETDATE())),
            (@studentId, 8, 'chat', DATEADD(MONTH, -3, GETDATE())),
            (@studentId, 12, 'chat', DATEADD(MONTH, -2, GETDATE())),
            (@studentId, 16, 'chat', DATEADD(MONTH, -1, GETDATE())),
            (@studentId, 20, 'chat', GETDATE()),
            (@studentId, 4, 'chat', DATEADD(DAY, -10, GETDATE())),
            (@studentId, 4, 'chat', DATEADD(DAY, -5, GETDATE())),
            (@studentId, 8, 'chat', DATEADD(DAY, -3, GETDATE())),
            (@studentId, 4, 'chat', DATEADD(DAY, -1, GETDATE()));
    END
END
GO

PRINT 'Migration completed: pro_upgrades and token_usage_logs tables created successfully!';
GO
