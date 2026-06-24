-- Demo plan tiers with server-authoritative pricing (Stripe mapping deferred to F6.T3.x).
IF NOT EXISTS (SELECT 1 FROM dbo.PlanTiers WHERE Id = 1)
BEGIN
    INSERT INTO dbo.PlanTiers (Id, Name, Description, MonthlyPriceCents, PatientCapMax, IsActive)
    VALUES (1, N'Starter', N'Demo starter plan for new customers.', 9900, 250, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.PlanTiers WHERE Id = 2)
BEGIN
    INSERT INTO dbo.PlanTiers (Id, Name, Description, MonthlyPriceCents, PatientCapMax, IsActive)
    VALUES (2, N'Professional', N'Demo professional plan with multiple clinics.', 24900, 1000, 1);
END
GO
