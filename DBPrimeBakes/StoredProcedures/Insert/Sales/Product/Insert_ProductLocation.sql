CREATE PROCEDURE [dbo].[Insert_ProductLocation]
	@Id INT OUTPUT,
	@ProductId INT,
	@Rate MONEY,
	@LocationId INT,
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[ProductLocation] ([ProductId], [Rate], [LocationId], [Status])
		VALUES (@ProductId, @Rate, @LocationId, @Status);

		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[ProductLocation]
		SET [ProductId] = @ProductId, 
			[Rate] = @Rate, 
			[LocationId] = @LocationId,
			[Status] = @Status
		WHERE [Id] = @Id;
	END

	SELECT @Id AS 'Id';
END