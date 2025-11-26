CREATE TABLE [dbo].[Settings]
(
    [Key] VARCHAR(50) NOT NULL UNIQUE, 
    [Value] VARCHAR(MAX) NOT NULL, 
    [Description] VARCHAR(MAX) NOT NULL, 
)