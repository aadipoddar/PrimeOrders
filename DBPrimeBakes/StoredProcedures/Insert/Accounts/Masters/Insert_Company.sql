CREATE PROCEDURE [dbo].[Insert_Company]
	@Id INT OUTPUT,
	@Name VARCHAR(500),
	@Code VARCHAR(10),
	@StateUTId INT,
	@GSTNo VARCHAR(MAX) = NULL,
	@PANNo VARCHAR(MAX) = NULL,
	@CINNo VARCHAR(MAX) = NULL,
	@Alias VARCHAR(MAX) = NULL,
	@Phone VARCHAR(10) = NULL,
	@Email VARCHAR(MAX) = NULL,
	@Address VARCHAR(MAX) = NULL,
	@Remarks VARCHAR(MAX) = NULL,
	@Status BIT = 1
AS
BEGIN

	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Company]
		(
			[Name],
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
			[Status]
		)
		VALUES
		(
			@Name,
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
			@Status
		);
		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[Company]
		SET
			[Name] = @Name,
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
			[Status] = @Status
		WHERE
			[Id] = @Id;
	END

	SELECT @Id AS Id;
END