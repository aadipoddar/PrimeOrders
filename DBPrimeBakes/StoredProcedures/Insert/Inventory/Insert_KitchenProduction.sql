CREATE PROCEDURE [dbo].[Insert_KitchenProduction]
	@Id INT OUTPUT,
	@KitchenId INT,
	@LocationId INT,
	@UserId INT,
	@TransactionNo VARCHAR(20),
	@ProductionDate DATETIME,
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[KitchenProduction] (KitchenId, LocationId, UserId, TransactionNo, ProductionDate, Status)
		VALUES (@KitchenId, @LocationId, @UserId, @TransactionNo, @ProductionDate, @Status);

		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[KitchenProduction]
		SET KitchenId = @KitchenId,
			LocationId = @LocationId,
			UserId = @UserId,
			TransactionNo = @TransactionNo,
			ProductionDate = @ProductionDate,
			Status = @Status
		WHERE Id = @Id;
	END

	SELECT @Id AS Id;
END