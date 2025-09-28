CREATE PROCEDURE [dbo].[Insert_User]
	@Id INT,
	@Name VARCHAR(50),
	@Passcode SMALLINT,
	@LocationId INT,
	@Sales BIT,
	@Order BIT,
	@Inventory BIT,
	@Accounts BIT,
	@Admin BIT,
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[User] (Name, Passcode, LocationId, [Order], Inventory, Accounts, Admin, Sales, Status)
		VALUES (@Name, @Passcode, @LocationId, @Order, @Inventory, @Accounts, @Admin, @Sales, @Status);
	END

	ELSE
	BEGIN
		UPDATE [dbo].[User]
		SET Name = @Name,
			Passcode = @Passcode,
			LocationId = @LocationId,
			[Order] = @Order,
			Inventory = @Inventory,
			Accounts = @Accounts,
			Admin = @Admin,
			Sales = @Sales,
			Status = @Status
		WHERE Id = @Id;
	END
END;