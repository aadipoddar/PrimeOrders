CREATE PROCEDURE [dbo].[Update_Settings]
	@Key VARCHAR(50),
	@Value VARCHAR(MAX),
	@Description VARCHAR(250)
AS
BEGIN

	UPDATE Settings
	SET [Value] = @Value
	WHERE [Key] = @Key

END