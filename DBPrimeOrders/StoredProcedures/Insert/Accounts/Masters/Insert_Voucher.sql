CREATE PROCEDURE [dbo].[Insert_Voucher]
	@Id INT,
	@Name VARCHAR(250),
	@PrefixCode VARCHAR(10),
	@Remarks VARCHAR(250),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Voucher]
		(
			[Name],
			[PrefixCode],
			[Remarks],
			[Status]
		)
		VALUES
		(
			@Name,
			@PrefixCode,
			@Remarks,
			@Status
		);
	END

	ELSE
	BEGIN
		UPDATE [dbo].[Voucher]
		SET
			[Name] = @Name,
			[PrefixCode] = @PrefixCode,
			[Remarks] = @Remarks,
			[Status] = @Status
		WHERE [Id] = @Id;
	END
END