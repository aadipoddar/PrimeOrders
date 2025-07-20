CREATE PROCEDURE [dbo].[Insert_FinancialYear]
	@Id INT,
	@StartDate DATE,
	@EndDate DATE,
	@YearNo INT,
	@Remarks VARCHAR(250),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[FinancialYear]
		(
			[StartDate],
			[EndDate],
			[YearNo],
			[Remarks],
			[Status]
		)
		VALUES
		(
			@StartDate,
			@EndDate,
			@YearNo,
			@Remarks,
			@Status
		);
	END

	ELSE
	BEGIN
		UPDATE [dbo].[FinancialYear]
		SET
			[StartDate] = @StartDate,
			[EndDate] = @EndDate,
			[YearNo] = @YearNo,
			[Remarks] = @Remarks,
			[Status] = @Status
		WHERE [Id] = @Id;
	End
END