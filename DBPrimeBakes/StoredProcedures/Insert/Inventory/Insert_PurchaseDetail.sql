CREATE PROCEDURE [dbo].[Insert_PurchaseDetail]
	@Id INT,
	@PurchaseId INT,
	@RawMaterialId INT,
	@Quantity MONEY,
	@MeasurementUnit VARCHAR(10),
	@Rate MONEY,
	@BaseTotal MONEY,
	@DiscPercent DECIMAL(5, 2),
	@DiscAmount MONEY,
	@AfterDiscount MONEY,
	@CGSTPercent DECIMAL(5, 2),
	@CGSTAmount MONEY,
	@SGSTPercent DECIMAL(5, 2),
	@SGSTAmount MONEY,
	@IGSTPercent DECIMAL(5, 2),
	@IGSTAmount MONEY,
	@Total MONEY,
	@NetRate MONEY,
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[PurchaseDetail]
		(
			PurchaseId,
			RawMaterialId,
			Quantity,
			MeasurementUnit,
			Rate,
			BaseTotal,
			DiscPercent,
			DiscAmount,
			AfterDiscount,
			CGSTPercent,
			CGSTAmount,
			SGSTPercent,
			SGSTAmount,
			IGSTPercent,
			IGSTAmount,
			Total,
			NetRate,
			Status
		) VALUES
		(
			@PurchaseId,
			@RawMaterialId,
			@Quantity,
			@MeasurementUnit,
			@Rate,
			@BaseTotal,
			@DiscPercent,
			@DiscAmount,
			@AfterDiscount,
			@CGSTPercent,
			@CGSTAmount,
			@SGSTPercent,
			@SGSTAmount,
			@IGSTPercent,
			@IGSTAmount,
			@Total,
			@NetRate,
			@Status
		);
	END

	ELSE
	BEGIN
		UPDATE [dbo].[PurchaseDetail]
		SET
			PurchaseId = @PurchaseId,
			RawMaterialId = @RawMaterialId,
			Quantity = @Quantity,
			MeasurementUnit = @MeasurementUnit,
			Rate = @Rate,
			BaseTotal = @BaseTotal,
			DiscPercent = @DiscPercent,
			DiscAmount = @DiscAmount,
			AfterDiscount = @AfterDiscount,
			CGSTPercent = @CGSTPercent,
			CGSTAmount = @CGSTAmount,
			SGSTPercent = @SGSTPercent,
			SGSTAmount = @SGSTAmount,
			IGSTPercent = @IGSTPercent,
			IGSTAmount = @IGSTAmount,
			Total = @Total,
			NetRate = @NetRate,
			Status = @Status
		WHERE Id = @Id;
	END
END