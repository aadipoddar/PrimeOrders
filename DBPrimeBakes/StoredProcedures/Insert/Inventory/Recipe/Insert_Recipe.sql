CREATE PROCEDURE [dbo].[Insert_Recipe]
	@Id INT OUTPUT,
	@ProductId INT,
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Recipe] ([ProductId], [Status])
		VALUES (@ProductId, @Status);

		SET @Id = SCOPE_IDENTITY();
	END
	ELSE
	BEGIN
		UPDATE [dbo].[Recipe]
		SET [ProductId] = @ProductId, 
			[Status] = @Status
		WHERE [Id] = @Id;
	END

	SELECT @Id AS Id;
END