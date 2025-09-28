CREATE PROCEDURE [dbo].[Insert_Purchase]
	@Id INT OUTPUT,
	@BillNo VARCHAR(50),
	@SupplierId INT,
	@BillDateTime DATETIME,
	@CDPercent DECIMAL(5, 2),
	@CDAmount MONEY,
	@Remarks VARCHAR(250),
	@UserId INT,
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Purchase] ([BillNo], [SupplierId], [BillDateTime], [CDPercent], [CDAmount], [Remarks], [UserId], [Status])
		VALUES (@BillNo, @SupplierId, @BillDateTime, @CDPercent, @CDAmount, @Remarks, @UserId, @Status);

		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[Purchase]
		SET
			BillNo = @BillNo,
			SupplierId = @SupplierId,
			BillDateTime = @BillDateTime,
			CDPercent = @CDPercent,
			CDAmount = @CDAmount,
			Remarks = @Remarks,
			UserId = @UserId,
			Status = @Status
		WHERE Id = @Id;
	END

	SELECT @Id AS Id;
END