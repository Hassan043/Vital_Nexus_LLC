# Naming Conventions

Define shared naming and schema conventions for database objects.

This repository uses SQL Database Projects as the authoritative schema source for Azure SQL. The following conventions ensure consistent object names and deployable schema across all database projects.

## Object Naming
- Use `PascalCase` for table, view, and column names.
- Use singular nouns for table names (for example, `User`, not `Users`).
- Use schema-qualified object names when referencing objects across schemas.
- Avoid spaces and special characters in identifiers.

## Table and Column Conventions
- Primary key columns should use `Id` or `<Entity>NameId` when needed for clarity.
- Foreign keys should be named `<ReferencedEntity>Id`.
- Use descriptive column names, not abbreviations, unless the abbreviation is standard.
- Use `CreatedAt`, `UpdatedAt`, and `IsActive` for common audit/status columns.

## Keys and Constraints
- Use `PK_<TableName>` for primary key constraints.
- Use `FK_<TableName>_<ReferencedTable>` for foreign key constraints.
- Use `IX_<TableName>_<ColumnName>` for nonclustered index names.
- Use `UQ_<TableName>_<ColumnName>` for unique constraints.
- Use `DF_<TableName>_<ColumnName>` for default constraints.

## Programmability Objects
- Prefix stored procedures with `usp_` followed by the action and entity, for example `usp_GetPatientLabReports`.
- Prefix user-defined functions with `udf_`, for example `udf_CalculateAge`.
- Prefix security objects with `sec_`.
- Use clear, action-oriented names that describe the behavior of the procedure or function.

## Schema and Script Organization
- Keep shared database rules in `database/VitalNexus.Database.Shared`.
- Store static reference data scripts in `ReferenceData/` when appropriate.
- Organize SQL Database Project folders by object type, for example `Tables/`, `Views/`, `StoredProcedures/`, `Functions/`, and `PostDeployment/`.
- Keep database object creation scripts focused on schema definitions and avoid embedding application logic.

## Deployment and Project Consistency
- Use the shared conventions from `NamingConventions.md` in all SQL Database Projects.
- Do not rely on EF Core migrations for production schema deployment.
- Deploy schema changes only through DACPAC artifacts and `sqlpackage`.
