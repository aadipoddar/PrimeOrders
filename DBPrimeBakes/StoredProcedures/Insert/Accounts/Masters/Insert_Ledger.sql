CREATE PROCEDURE [dbo].[Insert_Ledger]
	@Id INT OUTPUT,
	@Name VARCHAR(500),
	@GroupId INT,
	@AccountTypeId INT,
	@Code VARCHAR(50),
	@StateUTId INT = NULL,
	@GSTNo VARCHAR(MAX) = NULL,
	@PANNo VARCHAR(MAX) = NULL,
	@CINNo VARCHAR(MAX) = NULL,
	@Alias VARCHAR(MAX) = NULL,
	@Phone VARCHAR(20) = NULL,
	@Email VARCHAR(MAX) = NULL,
	@Address VARCHAR(MAX) = NULL,
	@Remarks VARCHAR(MAX) = NULL,
	@LocationId INT = NULL,
	@Status BIT = 1
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
			[StateUTId],
			[GSTNo],
			[PANNo],
			[CINNo],
			[Alias],
			[Phone],
			[Email],
			[Address],
			[Remarks],
			[LocationId],
			[Status]
		)
		VALUES
		(
			@Name,
			@GroupId,
			@AccountTypeId,
			@Code,
			@StateUTId,
			@GSTNo,
			@PANNo,
			@CINNo,
			@Alias,
			@Phone,
			@Email,
			@Address,
			@Remarks,
			@LocationId,
			@Status
		);
		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[Ledger]
		SET
			[Name] = @Name,
			[GroupId] = @GroupId,
			[AccountTypeId] = @AccountTypeId,
			[Code] = @Code,
			[StateUTId] = @StateUTId,
			[GSTNo] = @GSTNo,
			[PANNo] = @PANNo,
			[CINNo] = @CINNo,
			[Alias] = @Alias,
			[Phone] = @Phone,
			[Email] = @Email,
			[Address] = @Address,
			[Remarks] = @Remarks,
			[LocationId] = @LocationId,
			[Status] = @Status
		WHERE
			[Id] = @Id;
	END
	SELECT @Id AS Id;
END