-- SQL Server (T-SQL) initialization. Run in SSMS/sqlcmd against a SQL Server instance.

-- Create database if missing
IF (SELECT COUNT(*) FROM sys.databases WHERE name = N'NRLWebApp') = 0
BEGIN
    CREATE DATABASE [NRLWebApp];
END
GO

USE [NRLWebApp];
GO

-- Create server login if missing (ensure the password meets your SQL Server policy)
IF (SELECT COUNT(*) FROM sys.server_principals WHERE name = N'appuser') = 0
BEGIN
    CREATE LOGIN [appuser] WITH PASSWORD = N'YourPassword';
END
GO

-- Create database user and grant role if missing
IF (SELECT COUNT(*) FROM sys.database_principals WHERE name = N'appuser') = 0
BEGIN
    CREATE USER [appuser] FOR LOGIN [appuser];
    EXEC sp_addrolemember N'db_owner', N'appuser';
END
GO