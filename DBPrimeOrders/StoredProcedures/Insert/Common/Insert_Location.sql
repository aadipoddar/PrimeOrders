CREATE PROCEDURE [dbo].[Insert_Location]
	@Id INT,
	@Name VARCHAR(50),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Location] (Name, Status)
		VALUES (@Name, @Status);
	END

	ELSE
	BEGIN
		UPDATE [dbo].[Location]
		SET Name = @Name, Status = @Status
		WHERE Id = @Id;
	END
END;