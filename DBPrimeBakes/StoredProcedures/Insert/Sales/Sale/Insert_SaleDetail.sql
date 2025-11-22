CREATE PROCEDURE [dbo].[Insert_SaleDetail]
	@Id INT OUTPUT,
	@SaleId INT,
	@ProductId INT,
	@Quantity MONEY,
	@Rate MONEY,
	@BaseTotal MONEY,
	@DiscountPercent DECIMAL(5, 2),
	@DiscountAmount MONEY,
	@AfterDiscount MONEY,
	@CGSTPercent DECIMAL(5, 2),
	@CGSTAmount MONEY,
	@SGSTPercent DECIMAL(5, 2),
	@SGSTAmount MONEY,
	@IGSTPercent DECIMAL(5, 2),
	@IGSTAmount MONEY,
	@TotalTaxAmount MONEY,
	@InclusiveTax BIT,
	@Total MONEY,
	@NetRate MONEY,
	@Remarks VARCHAR(MAX),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[SaleDetail]
		(
			[SaleId],
			[ProductId],
			[Quantity],
			[Rate],
			[BaseTotal],
			[DiscountPercent],
			[DiscountAmount],
			[AfterDiscount],
			[CGSTPercent],
			[CGSTAmount],
			[SGSTPercent],
			[SGSTAmount],
			[IGSTPercent],
			[IGSTAmount],
			[TotalTaxAmount],
			[InclusiveTax],
			[Total],
			[NetRate],
			[Remarks],
			[Status]
		)
		VALUES
		(
			@SaleId,
			@ProductId,
			@Quantity,
			@Rate,
			@BaseTotal,
			@DiscountPercent,
			@DiscountAmount,
			@AfterDiscount,
			@CGSTPercent,
			@CGSTAmount,
			@SGSTPercent,
			@SGSTAmount,
			@IGSTPercent,
			@IGSTAmount,
			@TotalTaxAmount,
			@InclusiveTax,
			@Total,
			@NetRate,
			@Remarks,
			@Status
		)
		SET @Id = SCOPE_IDENTITY()
	END

	ELSE
	BEGIN
		UPDATE [dbo].[SaleDetail]
		SET
			[SaleId] = @SaleId,
			[ProductId] = @ProductId,
			[Quantity] = @Quantity,
			[Rate] = @Rate,
			[BaseTotal] = @BaseTotal,
			[DiscountPercent] = @DiscountPercent,
			[DiscountAmount] = @DiscountAmount,
			[AfterDiscount] = @AfterDiscount,
			[CGSTPercent] = @CGSTPercent,
			[CGSTAmount] = @CGSTAmount,
			[SGSTPercent] = @SGSTPercent,
			[SGSTAmount] = @SGSTAmount,
			[IGSTPercent] = @IGSTPercent,
			[IGSTAmount] = @IGSTAmount,
			[TotalTaxAmount] = @TotalTaxAmount,
			[InclusiveTax] = @InclusiveTax,
			[Total] = @Total,
			[NetRate] = @NetRate,
			[Remarks] = @Remarks,
			[Status] = @Status
		WHERE
			[Id] = @Id
	END

	SELECT @Id AS Id

END