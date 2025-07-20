CREATE PROCEDURE [dbo].[Insert_Accounting]
	@Id INT OUTPUT,
	@ReferenceNo VARCHAR(20),
	@VoucherId INT,
	@Remarks VARCHAR(500),
	@AccountingDate DATE,
	@FinancialYearId INT,
	@Generated BIT,
	@UserId INT,
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Accounting]
		(
			ReferenceNo,
			VoucherId,
			Remarks,
			AccountingDate,
			FinancialYearId,
			UserId,
			Generated,
			Status
		) VALUES
		(
			@ReferenceNo,
			@VoucherId,
			@Remarks,
			@AccountingDate,
			@FinancialYearId,
			@UserId,
			@Generated,
			@Status
		)

		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[Accounting]
		SET
			ReferenceNo = @ReferenceNo,
			VoucherId = @VoucherId,
			Remarks = @Remarks,
			AccountingDate = @AccountingDate,
			FinancialYearId = @FinancialYearId,
			UserId = @UserId,
			Generated = @Generated,
			Status = @Status
		WHERE Id = @Id
	END

	SELECT @Id AS Id
END