CREATE PROCEDURE [dbo].[Insert_Purchase]
	@Id INT OUTPUT,
	@TransactionNo VARCHAR(MAX),
	@CompanyId INT,
	@PartyId INT,
	@TransactionDateTime DATETIME,
	@FinancialYearId INT,
	@TotalItems INT,
	@TotalQuantity MONEY,
	@BaseTotal MONEY,
	@ItemDiscountAmount MONEY,
	@TotalAfterItemDiscount MONEY,
	@TotalInclusiveTaxAmount MONEY,
	@TotalExtraTaxAmount MONEY,
	@TotalAfterTax MONEY,
	@OtherChargesPercent DECIMAL(5, 2),
	@OtherChargesAmount MONEY,
	@CashDiscountPercent DECIMAL(5, 2),
	@CashDiscountAmount MONEY,
	@RoundOffAmount MONEY,
	@TotalAmount MONEY,
	@Remarks VARCHAR(MAX),
	@DocumentUrl VARCHAR(MAX) = NULL,
	@CreatedBy INT,
	@CreatedAt DATETIME,
	@CreatedFromPlatform VARCHAR(MAX),
	@Status BIT,
	@LastModifiedBy INT,
	@LastModifiedAt DATETIME,
	@LastModifiedFromPlatform VARCHAR(MAX)
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Purchase]
		(
			[TransactionNo],
			[CompanyId],
			[PartyId],
			[TransactionDateTime],
			[FinancialYearId],
			[TotalItems],
			[TotalQuantity],
			[BaseTotal],
			[ItemDiscountAmount],
			[TotalAfterItemDiscount],
			[TotalInclusiveTaxAmount],
			[TotalExtraTaxAmount],
			[TotalAfterTax],
			[OtherChargesPercent],
			[OtherChargesAmount],
			[CashDiscountPercent],
			[CashDiscountAmount],
			[RoundOffAmount],
			[TotalAmount],
			[Remarks],
			[DocumentUrl],
			[CreatedBy],
			[CreatedFromPlatform],
			[Status]
		)
		VALUES
		(
			@TransactionNo,
			@CompanyId,
			@PartyId,
			@TransactionDateTime,
			@FinancialYearId,
			@TotalItems,
			@TotalQuantity,
			@BaseTotal,
			@ItemDiscountAmount,
			@TotalAfterItemDiscount,
			@TotalInclusiveTaxAmount,
			@TotalExtraTaxAmount,
			@TotalAfterTax,
			@OtherChargesPercent,
			@OtherChargesAmount,
			@CashDiscountPercent,
			@CashDiscountAmount,
			@RoundOffAmount,
			@TotalAmount,
			@Remarks,
			@DocumentUrl,
			@CreatedBy,
			@CreatedFromPlatform,
			@Status
		)
		SET @Id = SCOPE_IDENTITY()
	END

	ELSE
	BEGIN
		UPDATE [dbo].[Purchase]
		SET 
			[TransactionNo] = @TransactionNo,
			[CompanyId] = @CompanyId,
			[PartyId] = @PartyId,
			[TransactionDateTime] = @TransactionDateTime,
			[FinancialYearId] = @FinancialYearId,
			[TotalItems] = @TotalItems,
			[TotalQuantity] = @TotalQuantity,
			[BaseTotal] = @BaseTotal,
			[ItemDiscountAmount] = @ItemDiscountAmount,
			[TotalAfterItemDiscount] = @TotalAfterItemDiscount,
			[TotalInclusiveTaxAmount] = @TotalInclusiveTaxAmount,
			[TotalExtraTaxAmount] = @TotalExtraTaxAmount,
			[TotalAfterTax] = @TotalAfterTax,
			[OtherChargesPercent] = @OtherChargesPercent,
			[OtherChargesAmount] = @OtherChargesAmount,
			[CashDiscountPercent] = @CashDiscountPercent,
			[CashDiscountAmount] = @CashDiscountAmount,
			[RoundOffAmount] = @RoundOffAmount,
			[TotalAmount] = @TotalAmount,
			[Remarks] = @Remarks,
			[DocumentUrl] = @DocumentUrl,
			[Status] = @Status,
			[LastModifiedBy] = @LastModifiedBy,
			[LastModifiedAt] = @LastModifiedAt,
			[LastModifiedFromPlatform] = @LastModifiedFromPlatform
		WHERE [Id] = @Id
	END

	SELECT @Id AS Id
END