CREATE PROCEDURE [dbo].[Insert_RawMaterialCategory]
	@Id INT OUTPUT,
	@Name VARCHAR(500),
	@Remarks VARCHAR(MAX) = NULL,
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[RawMaterialCategory] (Name, Remarks, Status)
		VALUES (@Name, @Remarks, @Status);
		
		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[RawMaterialCategory]
		SET Name = @Name, Remarks = @Remarks, Status = @Status
		WHERE Id = @Id;
	END

	SELECT @Id AS Id;
END;