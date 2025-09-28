CREATE PROCEDURE [dbo].[Load_Settings_By_Key]
	@Key VARCHAR(50)
AS
BEGIN

	SELECT *
	FROM Settings
	WHERE [Key] = @Key

END
