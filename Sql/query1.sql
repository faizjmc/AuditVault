
  USE AuditVaultDb;
  
SELECT TOP (1000) [MigrationId] ,[ProductVersion] FROM [AuditVaultDb].[dbo].[__EFMigrationsHistory]



SELECT * FROM Tenants;
SELECT * FROM Users;

SELECT * FROM [AuditVaultDb].[dbo].[AuditLogs] order by CreatedAt desc
