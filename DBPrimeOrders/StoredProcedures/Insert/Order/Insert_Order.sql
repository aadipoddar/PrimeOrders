﻿CREATE PROCEDURE [dbo].[Insert_Order]
	@Id INT OUTPUT,
	@OrderNo VARCHAR(50),
	@OrderDate DATE,
	@LocationId INT,
	@UserId INT,
	@Remarks VARCHAR(250),
	@SaleId INT = NULL,
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Order] ([OrderNo], [OrderDate], [LocationId], [UserId], [Remarks], [SaleId], [Status])
		VALUES (@OrderNo, @OrderDate, @LocationId, @UserId, @Remarks, @SaleId, @Status);
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
			SaleId = @SaleId,
			Status = @Status
		WHERE Id = @Id;
	END
	SELECT @Id AS Id;
END