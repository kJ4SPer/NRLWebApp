-- PSEUDOCODE / PLAN (detailed)
-- 1. This file must work without SQL Server parser errors in Visual Studio while still providing
--    a MySQL variant for users running MySQL/MariaDB.
-- 2. Provide a SQL Server (T-SQL) section that:
--    a. Checks whether the database exists using DB_ID.
--    b. Creates the database if absent.
--    c. Checks whether the server-level login exists in sys.server_principals.
--    d. Creates the login with a secure password if absent.
--    e. Switches to the created database.
--    f. Checks whether the database user exists in sys.database_principals.
--    g. Creates the database user mapped to the server login and grants an appropriate role (db_owner here).
-- 3. Separate the MySQL/MariaDB variant in a commented block so editors targeting SQL Server won't flag MySQL syntax.
-- 4. Keep credentials and sensitive values clearly marked so they can be replaced by environment-specific secrets.
-- 5. Use GO batch separators for T-SQL compatibility in tools that understand them.
--
-- NOTES:
-- - This file favors SQL Server syntax (to avoid SQL80001 parsing errors in Visual Studio).
-- - The original MySQL/MariaDB commands are preserved below in a commented block for users who will run the script in MySQL.
-- - Replace 'YourPassword' with a secure password or use a secret mechanism for deployments.

-- ============================
-- SQL Server (T-SQL) variant
-- ============================
IF DB_ID(N'NRLWebApp') IS NULL
BEGIN
    CREATE DATABASE [NRLWebApp];
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'appuser')
BEGIN
    -- Replace the password with a secure value before running in production
    CREATE LOGIN [appuser] WITH PASSWORD = N'YourPassword';
END
GO

USE [NRLWebApp];
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'appuser')
BEGIN
    CREATE USER [appuser] FOR LOGIN [appuser];
    -- Grant ownership for convenience; tighten permissions as appropriate for your app
    ALTER ROLE [db_owner] ADD MEMBER [appuser];
END
GO

-- ============================
-- MySQL / MariaDB variant (COMMENTED OUT)
-- Run this section only in a MySQL/MariaDB client; keep it commented for SQL Server tooling.
-- ============================

/*
CREATE DATABASE IF NOT EXISTS `NRLWebApp` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

CREATE USER IF NOT EXISTS 'appuser'@'%' IDENTIFIED BY 'YourPassword';
GRANT ALL PRIVILEGES ON `NRLWebApp`.* TO 'appuser'@'%';
FLUSH PRIVILEGES;
*/