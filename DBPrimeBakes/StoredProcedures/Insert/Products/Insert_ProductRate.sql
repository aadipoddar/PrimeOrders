CREATE PROCEDURE [dbo].[Insert_ProductRate]
	@Id INT,
	@ProductId INT,
	@Rate MONEY,
	@LocationId INT,
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
	INSERT INTO [dbo].[ProductRate] ([ProductId], [Rate], [LocationId], [Status])
		VALUES (@ProductId, @Rate, @LocationId, @Status);
	END

	ELSE
	BEGIN
		UPDATE [dbo].[ProductRate]
		SET [ProductId] = @ProductId, 
			[Rate] = @Rate, 
			[LocationId] = @LocationId,
			[Status] = @Status
		WHERE [Id] = @Id;
	END
END