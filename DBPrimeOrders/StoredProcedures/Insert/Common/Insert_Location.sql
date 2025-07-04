CREATE PROCEDURE [dbo].[Insert_Location]
	@Id INT,
	@Name VARCHAR(50),
	@Discount DECIMAL(5, 2),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Location] (Name, Discount, Status)
		VALUES (@Name, @Discount, @Status);
	END

	ELSE
	BEGIN
		UPDATE [dbo].[Location]
		SET
			Name = @Name,
			Discount = @Discount,
			Status = @Status
		WHERE Id = @Id;
	END
END;