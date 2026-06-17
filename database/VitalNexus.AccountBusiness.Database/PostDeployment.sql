-- Post-deployment script for VitalNexus.AccountBusiness.Database
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE NormalizedEmail = N'DEMO@VITALNEXUS.LOCAL')
BEGIN
    INSERT INTO dbo.Users (Id, Email, NormalizedEmail, DisplayName, PasswordHash)
    VALUES (
        NEWID(),
        N'demo@vitalnexus.local',
        N'DEMO@VITALNEXUS.LOCAL',
        N'Demo User',
        N'$2a$10$iZbNEvNw0FvRpa9JhGSkQ.9V5de6QlEvctR4Eu0xOnXD4Gdta9a0K'
    );
END
GO
