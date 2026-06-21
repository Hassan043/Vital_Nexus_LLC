-- Post-deployment script for VitalNexus.AccountBusiness.Database
:r .\ReferenceData\ApplicationRoles.sql

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = 'demo@vitalnexus.local')
BEGIN
    INSERT INTO dbo.Users (Id, EntraObjectId, Email, DisplayName)
    VALUES (NEWID(), NEWID(), 'demo@vitalnexus.local', 'Demo User');
END
GO
