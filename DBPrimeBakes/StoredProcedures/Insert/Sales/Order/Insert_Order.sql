CREATE PROCEDURE [dbo].[Insert_Order]
	@Id INT OUTPUT,
	@TransactionNo VARCHAR(MAX),
	@CompanyId INT,
	@LocationId INT,
	@SaleId INT = NULL,
	@TransactionDateTime DATETIME,
	@FinancialYearId INT,
	@TotalItems INT,
	@TotalQuantity MONEY,
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
		INSERT INTO [dbo].[Order]
		(
			[TransactionNo],
			[CompanyId],
			[LocationId],
			[SaleId],
			[TransactionDateTime],
			[FinancialYearId],
			[TotalItems],
			[TotalQuantity],
			[Remarks],
			[CreatedBy],
			[CreatedAt],
			[CreatedFromPlatform],
			[Status]
		)
		VALUES
		(
			@TransactionNo,
			@CompanyId,
			@LocationId,
			@SaleId,
			@TransactionDateTime,
			@FinancialYearId,
			@TotalItems,
			@TotalQuantity,
			@Remarks,
			@CreatedBy,
			@CreatedAt,
			@CreatedFromPlatform,
			@Status
		)
		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[Order]
		SET
			TransactionNo = @TransactionNo,
			CompanyId = @CompanyId,
			LocationId = @LocationId,
			SaleId = @SaleId,
			TransactionDateTime = @TransactionDateTime,
			FinancialYearId = @FinancialYearId,
			TotalItems = @TotalItems,
			TotalQuantity = @TotalQuantity,
			Remarks = @Remarks,
			Status = @Status,
			LastModifiedBy = @LastModifiedBy,
			LastModifiedAt = @LastModifiedAt,
			LastModifiedFromPlatform = @LastModifiedFromPlatform
		WHERE Id = @Id;
	END

	SELECT @Id AS Id;
END