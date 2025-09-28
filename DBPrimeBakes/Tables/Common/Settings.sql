CREATE TABLE [dbo].[Settings]
(
    [Key] VARCHAR(50) NOT NULL UNIQUE, 
    [Value] VARCHAR(MAX) NOT NULL, 
    [Description] VARCHAR(250) NOT NULL, 
)