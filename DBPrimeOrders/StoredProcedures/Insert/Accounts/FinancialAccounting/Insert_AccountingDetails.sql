CREATE PROCEDURE [dbo].[Insert_AccountingDetails]
	@Id INT,
	@AccountingId INT,
	@Type CHAR(1),
	@LedgerId INT,
	@Amount MONEY,
	@Remarks VARCHAR(250),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[AccountingDetails]
		(
			AccountingId,
			Type,
			LedgerId,
			Amount,
			Remarks,
			Status
		) VALUES
		(
			@AccountingId,
			@Type,
			@LedgerId,
			@Amount,
			@Remarks,
			@Status
		)
	END

	ELSE
	BEGIN
		UPDATE [dbo].[AccountingDetails]
		SET
			AccountingId = @AccountingId,
			Type = @Type,
			LedgerId = @LedgerId,
			Amount = @Amount,
			Remarks = @Remarks,
			Status = @Status
		WHERE Id = @Id
	END
END