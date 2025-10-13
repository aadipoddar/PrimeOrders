CREATE PROCEDURE [dbo].[Insert_Accounting]
	@Id INT OUTPUT,
	@TransactionNo VARCHAR(20),
	@VoucherId INT,
	@Remarks VARCHAR(500),
	@AccountingDate DATE,
	@FinancialYearId INT,
	@GeneratedModule VARCHAR(20),
	@UserId INT,
	@CreatedAt DATETIME,
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Accounting]
		(
			[TransactionNo],
			VoucherId,
			Remarks,
			AccountingDate,
			FinancialYearId,
			UserId,
			GeneratedModule,
			CreatedAt,
			Status
		) VALUES
		(
			@TransactionNo,
			@VoucherId,
			@Remarks,
			@AccountingDate,
			@FinancialYearId,
			@UserId,
			@GeneratedModule,
			@CreatedAt,
			@Status
		)

		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[Accounting]
		SET
			[TransactionNo] = @TransactionNo,
			VoucherId = @VoucherId,
			Remarks = @Remarks,
			AccountingDate = @AccountingDate,
			FinancialYearId = @FinancialYearId,
			UserId = @UserId,
			GeneratedModule = @GeneratedModule,
			CreatedAt = @CreatedAt,
			Status = @Status
		WHERE Id = @Id
	END

	SELECT @Id AS Id
END