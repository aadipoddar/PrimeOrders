﻿CREATE PROCEDURE [dbo].[Insert_SaleDetail]
	@Id INT,
	@SaleId INT,
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
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[SaleDetail]
		(
			SaleId,
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
			Status
		) VALUES
		(
			@SaleId,
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
			@Status
		);
		
		SET @Id = SCOPE_IDENTITY();
	END
	ELSE
	BEGIN
		UPDATE [dbo].[SaleDetail]
		SET
			SaleId = @SaleId,
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
			Status = @Status
		WHERE Id = @Id;
	END
	SELECT @Id AS Id;
END;