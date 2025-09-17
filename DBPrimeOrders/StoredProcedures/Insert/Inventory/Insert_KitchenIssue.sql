CREATE PROCEDURE [dbo].[Insert_KitchenIssue]
	@Id INT OUTPUT,
	@KitchenId INT,
	@LocationId INT,
	@UserId INT,
	@TransactionNo VARCHAR(20),
	@IssueDate DATETIME,
	@Remarks VARCHAR(250),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[KitchenIssue] (KitchenId, LocationId, UserId, TransactionNo, IssueDate, Remarks, Status)
		VALUES (@KitchenId, @LocationId, @UserId, @TransactionNo, @IssueDate, @Remarks, @Status);

		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[KitchenIssue]
		SET KitchenId = @KitchenId,
			LocationId = @LocationId,
			UserId = @UserId,
			TransactionNo = @TransactionNo,
			IssueDate = @IssueDate,
			Remarks = @Remarks,
			Status = @Status
		WHERE Id = @Id;
	END

	SELECT @Id AS Id;
END