-- Post-deployment script for VitalNexus.AccountBusiness.Database
:r .\ReferenceData\ApplicationRoles.sql

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = 'demo@vitalnexus.local')
BEGIN
    DECLARE @DemoCustomerId UNIQUEIDENTIFIER = NEWID();

    INSERT INTO dbo.Customers (Id, Name)
    VALUES (@DemoCustomerId, 'Demo Customer');

    INSERT INTO dbo.Users (Id, EntraObjectId, CustomerId, Email, DisplayName)
    VALUES (NEWID(), NEWID(), @DemoCustomerId, 'demo@vitalnexus.local', 'Demo User');
END
GO
