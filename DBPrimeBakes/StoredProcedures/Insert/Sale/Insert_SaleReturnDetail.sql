CREATE PROCEDURE [dbo].[Insert_SaleReturnDetail]
	@Id INT,
	@SaleReturnId INT,
	@ProductId INT,
	@Quantity DECIMAL(7, 3),
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
		INSERT INTO [dbo].[SaleReturnDetail]
		(
			SaleReturnId,
			ProductId,
			Quantity,
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
			@SaleReturnId,
			@ProductId,
			@Quantity,
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
		UPDATE [dbo].[SaleReturnDetail]
		SET
			SaleReturnId = @SaleReturnId,
			ProductId = @ProductId,
			Quantity = @Quantity,
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
END;