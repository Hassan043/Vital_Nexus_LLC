-- Post-deployment script for VitalNexus.AccountBusiness.Database
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = 'demo@vitalnexus.local')
BEGIN
    INSERT INTO dbo.Users (Id, Email, DisplayName)
    VALUES (NEWID(), 'demo@vitalnexus.local', 'Demo User');
END
GO
