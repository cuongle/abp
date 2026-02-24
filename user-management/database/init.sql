CREATE DATABASE UserManagementDb;
GO

USE UserManagementDb;
GO

CREATE TABLE dbo.Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserName NVARCHAR(256) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(256) NOT NULL,
    PasswordSalt NVARCHAR(256) NOT NULL,
    Role NVARCHAR(32) NOT NULL DEFAULT 'User',
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

-- Password for admin is: Admin@123 (generated with backend PasswordHasher)
INSERT INTO dbo.Users(UserName, PasswordHash, PasswordSalt, Role)
VALUES
('admin@local', '9xe4f6JqA34eX8YfWdAm74x8mC9eo+3Tdz2Ng9N42YQ=', 'v7NnBCF4vNye6mciVIf8SQ==', 'Admin');
GO
