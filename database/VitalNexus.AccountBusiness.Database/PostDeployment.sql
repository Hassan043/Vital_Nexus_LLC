
-- Example: create Users table if not exists (for demo purposes)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
	CREATE TABLE dbo.Users (
		Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
		Email NVARCHAR(256) NOT NULL,
		DisplayName NVARCHAR(200) NULL,
		CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
	);
END

-- Example seed data (only for dev/testing)
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = 'demo@vitalnexus.local')
BEGIN
	INSERT INTO dbo.Users (Id, Email, DisplayName)
	VALUES (NEWID(), 'demo@vitalnexus.local', 'Demo User');
END

GO
