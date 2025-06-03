CREATE PROCEDURE [dbo].[Load_User_By_Passcode]
	@Passcode smallint
AS
BEGIN
	SELECT * FROM [User] WHERE Passcode = @Passcode
END