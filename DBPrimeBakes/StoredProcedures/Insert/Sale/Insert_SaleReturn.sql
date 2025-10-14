CREATE PROCEDURE [dbo].[Insert_SaleReturn]
	@Id INT OUTPUT,
	@BillNo VARCHAR(20),
	@DiscPercent DECIMAL(5, 2),
	@DiscReason VARCHAR(250),
	@RoundOff MONEY,
	@Remarks VARCHAR(250),
	@UserId INT,
	@LocationId INT,
	@SaleReturnDateTime DATETIME,
	@PartyId INT = NULL,
	@Cash MONEY = 0,
	@Card MONEY = 0,
	@UPI MONEY = 0,
	@Credit MONEY = 0,
	@CustomerId INT = NULL,
	@CreatedAt DATETIME,
	@Status BIT = 1
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[SaleReturn]
			([BillNo], [DiscPercent], DiscReason, RoundOff, Remarks, UserId, LocationId, SaleReturnDateTime, PartyId, Cash, Card, UPI, Credit, CustomerId, Status)
		VALUES 
			(@BillNo, @DiscPercent, @DiscReason, @RoundOff, @Remarks, @UserId, @LocationId, @SaleReturnDateTime, @PartyId, @Cash, @Card, @UPI, @Credit, @CustomerId, @Status);
		SET @Id = SCOPE_IDENTITY();
	END
	ELSE
	BEGIN
		UPDATE [dbo].[SaleReturn]
		SET
			BillNo = @BillNo,
			DiscPercent = @DiscPercent,
			DiscReason = @DiscReason,
			RoundOff = @RoundOff,
			Remarks = @Remarks,
			UserId = @UserId,
			LocationId = @LocationId,
			SaleReturnDateTime = @SaleReturnDateTime,
			PartyId = @PartyId,
			Cash = @Cash,
			Card = @Card,
			UPI = @UPI,
			Credit = @Credit,
			CustomerId = @CustomerId,
			Status = @Status
		WHERE Id = @Id;
	END
	SELECT @Id AS Id;
END