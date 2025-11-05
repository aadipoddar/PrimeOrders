CREATE PROCEDURE [dbo].[Load_CurrentDateTime]
AS
BEGIN
    SELECT CAST((GETDATE() AT TIME ZONE 'UTC') AT TIME ZONE 'India Standard Time' AS datetime2) AS CurrentDateTime;
END