USE [PolyCafePOS];
GO

IF COL_LENGTH('dbo.Employees', 'Email') IS NULL
BEGIN
    ALTER TABLE [dbo].[Employees]
    ADD [Email] NVARCHAR(255) NULL;
END
GO

UPDATE [dbo].[Employees]
SET [Email] = CONCAT([Username], '@polycafe.local')
WHERE [Email] IS NULL OR LTRIM(RTRIM([Email])) = '';
GO
