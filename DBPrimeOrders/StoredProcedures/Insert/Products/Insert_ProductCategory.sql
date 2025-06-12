CREATE PROCEDURE [dbo].[Insert_ProductCategory]
	@Id INT,
	@Name VARCHAR(50),
	@LocationId INT,
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[ProductCategory] ([Name], [LocationId], [Status])
		VALUES (@Name, @LocationId, @Status);
	END

	ELSE
	BEGIN
		UPDATE [dbo].[ProductCategory]
		SET [Name] = @Name, [LocationId] = @LocationId, [Status] = @Status
		WHERE [Id] = @Id;
	END

END