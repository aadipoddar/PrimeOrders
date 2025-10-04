CREATE PROCEDURE [dbo].[Insert_State]
	@Id INT,
	@Name VARCHAR(100),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[State] ([Name], [Status])
		VALUES (@Name, @Status);
	END

	ELSE
	BEGIN
		UPDATE [dbo].[State]
		SET [Name] = @Name, 
			[Status] = @Status
		WHERE [Id] = @Id;
	END
END