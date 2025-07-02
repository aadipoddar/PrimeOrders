CREATE PROCEDURE [dbo].[Insert_Kitchen]
	@Id INT,
	@Name VARCHAR(50),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Kitchen] (Name, Status)
		VALUES (@Name, @Status);
	END

	ELSE
	BEGIN
		UPDATE [dbo].[Kitchen]
		SET Name = @Name, Status = @Status
		WHERE Id = @Id;
	END
END;