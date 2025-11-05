CREATE PROCEDURE [dbo].[Insert_PurchaseReturn]
	@Id INT OUTPUT,
	@TransactionNo VARCHAR(MAX),
	@CompanyId INT,
	@PartyId INT,
	@TransactionDateTime DATETIME,
	@FinancialYearId INT,
	@ItemsTotalAmount MONEY,
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
		INSERT INTO [dbo].[PurchaseReturn]
		(
			[TransactionNo],
			[CompanyId],
			[PartyId],
			[TransactionDateTime],
			[FinancialYearId],
			[ItemsTotalAmount],
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
			@ItemsTotalAmount,
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
		UPDATE [dbo].[PurchaseReturn]
		SET 
			[TransactionNo] = @TransactionNo,
			[CompanyId] = @CompanyId,
			[PartyId] = @PartyId,
			[TransactionDateTime] = @TransactionDateTime,
			[FinancialYearId] = @FinancialYearId,
			[ItemsTotalAmount] = @ItemsTotalAmount,
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