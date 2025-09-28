CREATE PROCEDURE [dbo].[Insert_AccountType]
	@Id INT,
	@Name VARCHAR(250),
	@Remarks VARCHAR(250),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[AccountType]
		(
			[Name],
			[Remarks],
			[Status]
		)
		VALUES
		(
			@Name,
			@Remarks,
			@Status
		);
	END

	ELSE
	BEGIN
		UPDATE [dbo].[AccountType]
		SET
			[Name] = @Name,
			[Remarks] = @Remarks,
			[Status] = @Status
		WHERE [Id] = @Id;
	END
END