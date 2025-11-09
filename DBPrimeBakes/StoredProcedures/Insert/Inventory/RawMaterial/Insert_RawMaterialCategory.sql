CREATE PROCEDURE [dbo].[Insert_RawMaterialCategory]
	@Id INT,
	@Name VARCHAR(50),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[RawMaterialCategory] ([Name], [Status])
		VALUES (@Name, @Status);
	END
	ELSE
	BEGIN
		UPDATE [dbo].[RawMaterialCategory]
		SET [Name] = @Name, [Status] = @Status
		WHERE [Id] = @Id;
	END
END