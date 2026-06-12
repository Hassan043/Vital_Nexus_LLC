-- Placeholder stored procedures for Patients database access.
CREATE PROCEDURE [dbo].[usp_GetActivePatients]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [Id],
        [AnonymousPatientIdentifier],
        [CreatedAt],
        [UpdatedAt]
    FROM [dbo].[Patient]
    WHERE [IsActive] = 1;
END
