CREATE PROCEDURE [dbo].[Insert_Sale]
	@Id INT OUTPUT,
	@DiscPercent DECIMAL(5, 2),
	@DiscReason VARCHAR(250),
	@Remarks VARCHAR(250),
	@UserId INT,
	@LocationId INT,
	@SaleDateTime DATETIME,
	@PartyId INT = NULL,
	@OrderId INT = NULL,
	@Cash MONEY = 0,
	@Card MONEY = 0,
	@UPI MONEY = 0,
	@Credit MONEY = 0,
	@Status BIT = 1
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Sale] 
			([DiscPercent], DiscReason, Remarks, UserId, LocationId, PartyId, OrderId, Cash, Card, UPI, Credit, Status)
		VALUES 
			(@DiscPercent, @DiscReason, @Remarks, @UserId, @LocationId, @PartyId, @OrderId, @Cash, @Card, @UPI, @Credit, @Status);
		SET @Id = SCOPE_IDENTITY();
	END
	ELSE
	BEGIN
		UPDATE [dbo].[Sale]
		SET
			[DiscPercent] = @DiscPercent,
			DiscReason = @DiscReason,
			Remarks = @Remarks,
			UserId = @UserId,
			LocationId = @LocationId,
			PartyId = @PartyId,
			OrderId = @OrderId,
			Cash = @Cash,
			Card = @Card,
			UPI = @UPI,
			Credit = @Credit,
			Status = @Status
		WHERE Id = @Id;
	END
	SELECT @Id AS Id;
END