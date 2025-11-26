CREATE PROCEDURE [dbo].[Insert_Location]
	@Id INT OUTPUT,
	@Name VARCHAR(500),
	@PrefixCode VARCHAR(10),
	@Discount DECIMAL(5, 2),
	@Remarks VARCHAR(MAX),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Location] (Name, PrefixCode, Discount, Remarks, Status)
		VALUES (@Name, @PrefixCode, @Discount, @Remarks, @Status);
		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[Location]
		SET
			Name = @Name,
			PrefixCode = @PrefixCode,
			Discount = @Discount,
			Remarks = @Remarks,
			Status = @Status
		WHERE Id = @Id;
	END

	SELECT @Id AS Id;
END;