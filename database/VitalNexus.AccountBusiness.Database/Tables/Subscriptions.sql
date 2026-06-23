-- One subscription per customer (demo fields only; Stripe billing deferred).
CREATE TABLE [dbo].[Subscriptions]
(
    [CustomerId] UNIQUEIDENTIFIER NOT NULL,
    [PlanTierId] INT NOT NULL,
    [Status] NVARCHAR(32) NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_Subscriptions_CreatedAt] DEFAULT SYSUTCDATETIME(),
    [ActivatedAt] DATETIME2 NULL,
    CONSTRAINT [PK_Subscriptions] PRIMARY KEY ([CustomerId]),
    CONSTRAINT [FK_Subscriptions_Customers] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id]),
    CONSTRAINT [FK_Subscriptions_PlanTiers] FOREIGN KEY ([PlanTierId]) REFERENCES [dbo].[PlanTiers] ([Id])
);
GO
