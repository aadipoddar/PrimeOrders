CREATE PROCEDURE [dbo].[Insert_KitchenIssueDetail]
	@Id INT OUTPUT,
	@MasterId INT,
	@RawMaterialId INT,
	@Quantity MONEY,
	@UnitOfMeasurement VARCHAR(20),
	@Rate MONEY,
	@Total MONEY,
	@Remarks VARCHAR(MAX),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[KitchenIssueDetail]
		(
			MasterId,
			RawMaterialId,
			Quantity,
			UnitOfMeasurement,
			Rate,
			Total,
			Remarks,
			Status
		)
		VALUES
		(
			@MasterId,
			@RawMaterialId,
			@Quantity,
			@UnitOfMeasurement,
			@Rate,
			@Total,
			@Remarks,
			@Status
		);

		SET @Id = SCOPE_IDENTITY();
	END
	ELSE
	BEGIN
		UPDATE [dbo].[KitchenIssueDetail]
		SET MasterId = @MasterId,
			RawMaterialId = @RawMaterialId,
			Quantity = @Quantity,
			UnitOfMeasurement = @UnitOfMeasurement,
			Rate = @Rate,
			Total = @Total,
			Remarks = @Remarks,
			Status = @Status
		WHERE Id = @Id;
	END

	SELECT @Id AS Id;
END