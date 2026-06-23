-- Seed plan tiers for demo subscriptions.
IF NOT EXISTS (SELECT 1 FROM dbo.PlanTiers WHERE Id = 1)
BEGIN
    INSERT INTO dbo.PlanTiers (Id, Name, Description, IsActive)
    VALUES (1, 'Starter', 'Demo starter plan for new customers.', 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.PlanTiers WHERE Id = 2)
BEGIN
    INSERT INTO dbo.PlanTiers (Id, Name, Description, IsActive)
    VALUES (2, 'Professional', 'Demo professional plan with multiple clinics.', 1);
END
GO
