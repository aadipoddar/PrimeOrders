CREATE PROCEDURE [dbo].[Insert_Ledger]
	@Id INT,
	@Name VARCHAR(250),
	@GroupId INT,
	@AccountTypeId INT,
	@Code VARCHAR(10),
	@Phone VARCHAR(20),
	@Address VARCHAR(250),
	@GSTNo VARCHAR(20),
	@Remarks VARCHAR(250),
	@StateId INT,
	@LocationId INT = NULL,
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Ledger]
		(
			[Name],
			[GroupId],
			[AccountTypeId],
			[Code],
			[Phone],
			[Address],
			[GSTNo],
			[Remarks],
			[StateId],
			[LocationId],
			[Status]
		)
		VALUES
		(
			@Name,
			@GroupId,
			@AccountTypeId,
			@Code,
			@Phone,
			@Address,
			@GSTNo,
			@Remarks,
			@StateId,
			@LocationId,
			@Status
		);
	END

	ELSE
	BEGIN
		UPDATE [dbo].[Ledger]
		SET
			[Name] = @Name,
			[GroupId] = @GroupId,
			[AccountTypeId] = @AccountTypeId,
			[Code] = @Code,
			[Phone] = @Phone,
			[Address] = @Address,
			[GSTNo] = @GSTNo,
			[Remarks] = @Remarks,
			[StateId] = @StateId,
			[LocationId] = @LocationId,
			[Status] = @Status
		WHERE [Id] = @Id;
	END
END