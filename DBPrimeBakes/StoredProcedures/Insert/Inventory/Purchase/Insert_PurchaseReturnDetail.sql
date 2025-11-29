CREATE PROCEDURE [dbo].[Insert_PurchaseReturnDetail]
	@Id INT OUTPUT,
	@MasterId INT,
	@RawMaterialId INT,
	@Quantity MONEY,
	@UnitOfMeasurement VARCHAR(20),
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
		INSERT INTO [dbo].[PurchaseReturnDetail]
		(
			[MasterId],
			[RawMaterialId],
			[Quantity],
			[UnitOfMeasurement],
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
			@MasterId,
			@RawMaterialId,
			@Quantity,
			@UnitOfMeasurement,
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
		UPDATE [dbo].[PurchaseReturnDetail]
		SET
			[MasterId] = @MasterId,
			[RawMaterialId] = @RawMaterialId,
			[Quantity] = @Quantity,
			[UnitOfMeasurement] = @UnitOfMeasurement,
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