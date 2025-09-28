CREATE PROCEDURE [dbo].[Insert_AccountingDetails]
	@Id INT,
	@AccountingId INT,
	@LedgerId INT,
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
			Debit,
			Credit,
			Remarks,
			Status
		) VALUES
		(
			@AccountingId,
			@LedgerId,
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
			Debit = @Debit,
			Credit = @Credit,
			Remarks = @Remarks,
			Status = @Status
		WHERE Id = @Id
	END
END