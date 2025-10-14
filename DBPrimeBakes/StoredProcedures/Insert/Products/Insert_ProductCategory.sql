CREATE PROCEDURE [dbo].[Insert_ProductCategory]
	@Id INT,
	@Name VARCHAR(50),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[ProductCategory] ([Name], [Status])
		VALUES (@Name, @Status);
	END

	ELSE
	BEGIN
		UPDATE [dbo].[ProductCategory]
		SET [Name] = @Name, [Status] = @Status
		WHERE [Id] = @Id;
	END

END