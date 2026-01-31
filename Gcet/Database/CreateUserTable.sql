IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'Gcet')
BEGIN
    CREATE DATABASE [Gcet];
END
GO

USE [Gcet];
GO

IF OBJECT_ID('dbo.[User]', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.[User];
END
GO

CREATE TABLE dbo.[User]
(
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Username NVARCHAR(100) NOT NULL UNIQUE,
    Email NVARCHAR(150) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    Role NVARCHAR(50) NOT NULL CONSTRAINT DF_User_Role DEFAULT ('User'),
    IsActive BIT NOT NULL CONSTRAINT DF_User_IsActive DEFAULT (1),
    CreatedAt DATETIME NOT NULL CONSTRAINT DF_User_CreatedAt DEFAULT (GETDATE()),
    UpdatedAt DATETIME NULL,
    LastLoginAt DATETIME NULL,
    FailedLoginAttempts INT NOT NULL CONSTRAINT DF_User_FailedLoginAttempts DEFAULT (0),
    LockoutEnd DATETIME NULL,
    PasswordChangedAt DATETIME NULL,
    CONSTRAINT CK_User_Role CHECK (Role IN ('Admin', 'User')),
    CONSTRAINT CK_User_FailedLoginAttempts CHECK (FailedLoginAttempts >= 0)
);
GO

CREATE NONCLUSTERED INDEX IX_User_Username ON dbo.[User] (Username);
CREATE NONCLUSTERED INDEX IX_User_Email ON dbo.[User] (Email);
CREATE NONCLUSTERED INDEX IX_User_Role ON dbo.[User] (Role);
CREATE NONCLUSTERED INDEX IX_User_IsActive ON dbo.[User] (IsActive);
GO

INSERT INTO dbo.[User] (Username, Email, PasswordHash, Role, IsActive, CreatedAt, PasswordChangedAt)
VALUES
(
    'admin',
    'admin@gcet.local',
    'AQAAAAIAAYagAAAAEJRgmNGGmE85fwh1se3j/UOHyGtFlcLcAue+h07qwe94BHeaZSb5qXtL4bpMz4cunw==', -- Password: Admin@123
    'Admin',
    1,
    GETDATE(),
    GETDATE()
);
GO
