CREATE PROCEDURE [dbo].[Insert_FinancialYear]
	@Id INT OUTPUT,
	@StartDate DATE,
	@EndDate DATE,
	@YearNo INT,
	@Remarks VARCHAR(MAX),
	@Locked BIT = 0,
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
			[Locked],
			[Status]
		)
		VALUES
		(
			@StartDate,
			@EndDate,
			@YearNo,
			@Remarks,
			@Locked,
			@Status
		);
		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[FinancialYear]
		SET
			[StartDate] = @StartDate,
			[EndDate] = @EndDate,
			[YearNo] = @YearNo,
			[Remarks] = @Remarks,
			[Locked] = @Locked,
			[Status] = @Status
		WHERE [Id] = @Id;
	END

	SELECT @Id AS Id;
END