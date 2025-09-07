CREATE PROCEDURE [dbo].[Insert_Location]
	@Id INT OUTPUT,
	@Name VARCHAR(50),
	@Discount DECIMAL(5, 2),
	@MainLocation BIT,
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Location] (Name, Discount, MainLocation, Status)
		VALUES (@Name, @Discount, @MainLocation, @Status);
		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[Location]
		SET
			Name = @Name,
			Discount = @Discount,
			MainLocation = @MainLocation,
			Status = @Status
		WHERE Id = @Id;
	END

	SELECT @Id AS Id;
END;