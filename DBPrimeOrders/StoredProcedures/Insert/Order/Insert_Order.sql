CREATE PROCEDURE [dbo].[Insert_Order]
	@Id INT OUTPUT,
	@OrderNo VARCHAR(50),
	@OrderDate DATE,
	@LocationId INT,
	@UserId INT,
	@Remarks VARCHAR(250),
	@Completed BIT,
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Order] ([OrderNo], [OrderDate], [LocationId], [UserId], [Remarks], [Completed], [Status])
		VALUES (@OrderNo, @OrderDate, @LocationId, @UserId, @Remarks, @Completed, @Status);
		SET @Id = SCOPE_IDENTITY();
	END
	ELSE
	BEGIN
		UPDATE [dbo].[Order]
		SET
			OrderNo = @OrderNo,
			OrderDate = @OrderDate,
			LocationId = @LocationId,
			UserId = @UserId,
			Remarks = @Remarks,
			Completed = @Completed,
			Status = @Status
		WHERE Id = @Id;
	END
	SELECT @Id AS Id;
END