CREATE PROCEDURE [dbo].[Insert_SaleReturn]
	@Id INT OUTPUT,
	@TransactionNo VARCHAR(20),
	@Remarks VARCHAR(250),
	@UserId INT,
	@LocationId INT,
	@ReturnDateTime DATETIME,
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[SaleReturn] 
			([TransactionNo], [Remarks], [UserId], [LocationId], [ReturnDateTime], [Status])
		VALUES 
			(@TransactionNo, @Remarks, @UserId, @LocationId, @ReturnDateTime, @Status);
		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[SaleReturn]
		SET
			[TransactionNo] = @TransactionNo,
			[Remarks] = @Remarks,
			[UserId] = @UserId,
			[LocationId] = @LocationId,
			[ReturnDateTime] = @ReturnDateTime,
			[Status] = @Status
		WHERE Id = @Id;
	END
	SELECT @Id AS Id;
END