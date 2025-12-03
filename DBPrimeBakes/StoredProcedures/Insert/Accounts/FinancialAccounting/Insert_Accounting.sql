CREATE PROCEDURE [dbo].[Insert_Accounting]
	@Id INT OUTPUT,
	@TransactionNo VARCHAR(100),
	@CompanyId INT,
	@VoucherId INT,
	@ReferenceId INT,
	@ReferenceNo VARCHAR(MAX),
	@TransactionDateTime DATETIME,
	@FinancialYearId INT,
	@TotalDebitLedgers INT,
	@TotalCreditLedgers INT,
	@TotalDebitAmount MONEY,
	@TotalCreditAmount MONEY,
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
		INSERT INTO [dbo].[Accounting]
		(
			TransactionNo,
			CompanyId,
			VoucherId,
			ReferenceId,
			ReferenceNo,
			TransactionDateTime,
			FinancialYearId,
			TotalDebitLedgers,
			TotalCreditLedgers,
			TotalDebitAmount,
			TotalCreditAmount,
			Remarks,
			CreatedBy,
			CreatedAt,
			CreatedFromPlatform,
			Status
		) VALUES
		(
			@TransactionNo,
			@CompanyId,
			@VoucherId,
			@ReferenceId,
			@ReferenceNo,
			@TransactionDateTime,
			@FinancialYearId,
			@TotalDebitLedgers,
			@TotalCreditLedgers,
			@TotalDebitAmount,
			@TotalCreditAmount,
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
		UPDATE [dbo].[Accounting]
		SET
			TransactionNo = @TransactionNo,
			CompanyId = @CompanyId,
			VoucherId = @VoucherId,
			ReferenceId = @ReferenceId,
			ReferenceNo = @ReferenceNo,
			TransactionDateTime = @TransactionDateTime,
			FinancialYearId = @FinancialYearId,
			TotalDebitLedgers = @TotalDebitLedgers,
			TotalCreditLedgers = @TotalCreditLedgers,
			TotalDebitAmount = @TotalDebitAmount,
			TotalCreditAmount = @TotalCreditAmount,
			Remarks = @Remarks,
			Status = @Status,
			LastModifiedBy = @LastModifiedBy,
			LastModifiedAt = @LastModifiedAt,
			LastModifiedFromPlatform = @LastModifiedFromPlatform
		WHERE Id = @Id
	END

	SELECT @Id AS Id
END