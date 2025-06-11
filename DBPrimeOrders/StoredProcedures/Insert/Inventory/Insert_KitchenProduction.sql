CREATE PROCEDURE [dbo].[Insert_KitchenProduction]
	@Id INT OUTPUT,
	@KitchenId INT,
	@LocationId INT,
	@UserId INT,
	@ProductionDate DATETIME,
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[KitchenProduction] (KitchenId, LocationId, UserId, Status)
		VALUES (@KitchenId, @LocationId, @UserId, @Status);

		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[KitchenProduction]
		SET KitchenId = @KitchenId,
			LocationId = @LocationId,
			UserId = @UserId,
			Status = @Status
		WHERE Id = @Id;
	END

	SELECT @Id AS Id;
END