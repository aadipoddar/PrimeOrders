CREATE PROCEDURE [dbo].[Insert_Location]
	@Id INT,
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
END;