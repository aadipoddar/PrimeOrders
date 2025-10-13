CREATE PROCEDURE [dbo].[Insert_AccountingDetails]
	@Id INT,
	@AccountingId INT,
	@LedgerId INT,
	@ReferenceType VARCHAR(20) = NULL,
	@ReferenceId INT = NULL,
	@Debit MONEY,
	@Credit MONEY,
	@Remarks VARCHAR(250),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[AccountingDetails]
		(
			AccountingId,
			LedgerId,
			ReferenceType,
			ReferenceId,
			Debit,
			Credit,
			Remarks,
			Status
		) VALUES
		(
			@AccountingId,
			@LedgerId,
			@ReferenceType,
			@ReferenceId,
			@Debit,
			@Credit,
			@Remarks,
			@Status
		)
	END

	ELSE
	BEGIN
		UPDATE [dbo].[AccountingDetails]
		SET
			AccountingId = @AccountingId,
			LedgerId = @LedgerId,
			ReferenceType = @ReferenceType,
			ReferenceId = @ReferenceId,
			Debit = @Debit,
			Credit = @Credit,
			Remarks = @Remarks,
			Status = @Status
		WHERE Id = @Id
	END
END