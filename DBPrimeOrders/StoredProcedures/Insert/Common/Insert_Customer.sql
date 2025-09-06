CREATE PROCEDURE [dbo].[Insert_Customer]
	@Id INT OUTPUT,
	@Name VARCHAR(250),
	@Number VARCHAR(10)
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Customer] (Name, Number)
		VALUES (@Name, @Number);
		SET @Id = SCOPE_IDENTITY();
	END
	ELSE
	BEGIN
		UPDATE [dbo].[Customer]
		SET Name = @Name,
			Number = @Number
		WHERE Id = @Id;
	END
	SELECT @Id AS Id;
END;