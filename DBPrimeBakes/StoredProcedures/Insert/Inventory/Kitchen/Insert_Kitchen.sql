CREATE PROCEDURE [dbo].[Insert_Kitchen]
	@Id INT OUTPUT,
	@Name VARCHAR(500),
	@Remarks VARCHAR(MAX) = NULL,
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Kitchen] (Name, Remarks, Status)
		VALUES (@Name, @Remarks, @Status);
		
		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[Kitchen]
		SET Name = @Name, Remarks = @Remarks, Status = @Status
		WHERE Id = @Id;
	END

	SELECT @Id AS Id;
END;