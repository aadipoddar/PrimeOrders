CREATE PROCEDURE [dbo].[Insert_KitchenIssueDetail]
	@Id INT,
	@KitchenIssueId INT,
	@RawMaterialId INT,
	@MeasurementUnit VARCHAR(10),
	@Quantity DECIMAL(7, 3),
	@Rate MONEY,
	@Total MONEY,
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[KitchenIssueDetail] (KitchenIssueId, RawMaterialId, MeasurementUnit, Quantity, Rate, Total, Status)
		VALUES (@KitchenIssueId, @RawMaterialId, @MeasurementUnit, @Quantity, @Rate, @Total, @Status);
	END
	ELSE
	BEGIN
		UPDATE [dbo].[KitchenIssueDetail]
		SET KitchenIssueId = @KitchenIssueId,
			RawMaterialId = @RawMaterialId,
			MeasurementUnit = @MeasurementUnit,
			Quantity = @Quantity,
			Rate = @Rate,
			Total = @Total,
			Status = @Status
		WHERE Id = @Id;
	END
END