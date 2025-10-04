CREATE PROCEDURE [dbo].[Insert_Ledger]
	@Id INT,
	@Name VARCHAR(250),
	@Alias VARCHAR(250),
	@GroupId INT,
	@AccountTypeId INT,
	@Code VARCHAR(10),
	@Phone VARCHAR(20),
	@Email VARCHAR(100),
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
			[Alias],
			[GroupId],
			[AccountTypeId],
			[Code],
			[Phone],
			[Email],
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
			@Alias,
			@GroupId,
			@AccountTypeId,
			@Code,
			@Phone,
			@Email,
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
			[Alias] = @Alias,
			[GroupId] = @GroupId,
			[AccountTypeId] = @AccountTypeId,
			[Code] = @Code,
			[Phone] = @Phone,
			[Email] = @Email,
			[Address] = @Address,
			[GSTNo] = @GSTNo,
			[Remarks] = @Remarks,
			[StateId] = @StateId,
			[LocationId] = @LocationId,
			[Status] = @Status
		WHERE [Id] = @Id;
	END
END