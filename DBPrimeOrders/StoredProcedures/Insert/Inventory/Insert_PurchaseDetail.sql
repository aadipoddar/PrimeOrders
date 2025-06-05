CREATE PROCEDURE [dbo].[Insert_PurchaseDetail]
	@Id INT,
	@PurchaseId INT,
	@RawMaterialId INT,
	@Quantity DECIMAL(7, 3),
	@Rate MONEY,
	@BaseTotal MONEY,
	@DiscPercent DECIMAL(5, 2),
	@DiscAmount MONEY,
	@AfterDiscount MONEY,
	@CGStPercent DECIMAL(5, 2),
	@CGStAmount MONEY,
	@SGSTPercent DECIMAL(5, 2),
	@SGStAmount MONEY,
	@IGSTPercent DECIMAL(5, 2),
	@IGSTAmount MONEY,
	@Total MONEY,
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
			Rate,
			BaseTotal,
			DiscPercent,
			DiscAmount,
			AfterDiscount,
			CGStPercent,
			CGStAmount,
			SGSTPercent,
			SGStAmount,
			IGSTPercent,
			IGSTAmount,
			Total,
			Status
		) VALUES
		(
			@PurchaseId,
			@RawMaterialId,
			@Quantity,
			@Rate,
			@BaseTotal,
			@DiscPercent,
			@DiscAmount,
			@AfterDiscount,
			@CGStPercent,
			@CGStAmount,
			@SGSTPercent,
			@SGStAmount,
			@IGSTPercent,
			@IGSTAmount,
			@Total,
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
			Rate = @Rate,
			BaseTotal = @BaseTotal,
			DiscPercent = @DiscPercent,
			DiscAmount = @DiscAmount,
			AfterDiscount = @AfterDiscount,
			CGStPercent = @CGStPercent,
			CGStAmount = @CGStAmount,
			SGSTPercent = @SGSTPercent,
			SGStAmount = @SGStAmount,
			IGSTPercent = @IGSTPercent,
			IGSTAmount = @IGSTAmount,
			Total = @Total,
			Status = @Status
		WHERE Id = @Id;
	END
END