CREATE PROCEDURE [dbo].[Insert_SaleReturn]
	@Id INT OUTPUT,
	@TransactionNo VARCHAR(MAX),
	@CompanyId INT,
	@LocationId INT,
	@PartyId INT,
	@CustomerId INT,
	@TransactionDateTime DATETIME,
	@FinancialYearId INT,
	@ItemsTotalAmount MONEY,
	@OtherChargesPercent DECIMAL(5, 2),
	@OtherChargesAmount MONEY,
	@DiscountPercent DECIMAL(5, 2),
	@DiscountAmount MONEY,
	@RoundOffAmount MONEY,
	@TotalAmount MONEY,
	@Cash MONEY,
	@Card MONEY,
	@UPI MONEY,
	@Credit MONEY,
	@Remarks VARCHAR(MAX),
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
		INSERT INTO [dbo].[SaleReturn]
		(
			[TransactionNo],
			[CompanyId],
			[LocationId],
			[PartyId],
			[CustomerId],
			[TransactionDateTime],
			[FinancialYearId],
			[ItemsTotalAmount],
			[OtherChargesPercent],
			[OtherChargesAmount],
			[DiscountPercent],
			[DiscountAmount],
			[RoundOffAmount],
			[TotalAmount],
			[Cash],
			[Card],
			[UPI],
			[Credit],
			[Remarks],
			[CreatedBy],
			[CreatedFromPlatform],
			[Status]
		)
		VALUES
		(
			@TransactionNo,
			@CompanyId,
			@LocationId,
			@PartyId,
			@CustomerId,
			@TransactionDateTime,
			@FinancialYearId,
			@ItemsTotalAmount,
			@OtherChargesPercent,
			@OtherChargesAmount,
			@DiscountPercent,
			@DiscountAmount,
			@RoundOffAmount,
			@TotalAmount,
			@Cash,
			@Card,
			@UPI,
			@Credit,
			@Remarks,
			@CreatedBy,
			@CreatedFromPlatform,
			@Status
		)
		SET @Id = SCOPE_IDENTITY()
	END

	ELSE
	BEGIN
		UPDATE [dbo].[SaleReturn]
		SET 
			[TransactionNo] = @TransactionNo,
			[CompanyId] = @CompanyId,
			[PartyId] = @PartyId,
			[CustomerId] = @CustomerId,
			[TransactionDateTime] = @TransactionDateTime,
			[FinancialYearId] = @FinancialYearId,
			[ItemsTotalAmount] = @ItemsTotalAmount,
			[OtherChargesPercent] = @OtherChargesPercent,
			[OtherChargesAmount] = @OtherChargesAmount,
			[DiscountPercent] = @DiscountPercent,
			[DiscountAmount] = @DiscountAmount,
			[RoundOffAmount] = @RoundOffAmount,
			[TotalAmount] = @TotalAmount,
			[Cash] = @Cash,
			[Card] = @Card,
			[UPI] = @UPI,
			[Credit] = @Credit,
			[Remarks] = @Remarks,
			[Status] = @Status,
			[LastModifiedBy] = @LastModifiedBy,
			[LastModifiedAt] = @LastModifiedAt,
			[LastModifiedFromPlatform] = @LastModifiedFromPlatform
		WHERE [Id] = @Id
	END

	SELECT @Id AS Id
END