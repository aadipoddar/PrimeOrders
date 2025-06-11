CREATE PROCEDURE [dbo].[Insert_KitchenIssueDetail]
	@Id INT,
	@KitchenIssueId INT,
	@RawMaterialId INT,
	@Quantity DECIMAL(7, 3),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[KitchenIssueDetail] (KitchenIssueId, RawMaterialId, Quantity, Status)
		VALUES (@KitchenIssueId, @RawMaterialId, @Quantity, @Status);
	END
	ELSE
	BEGIN
		UPDATE [dbo].[KitchenIssueDetail]
		SET KitchenIssueId = @KitchenIssueId,
			RawMaterialId = @RawMaterialId,
			Quantity = @Quantity,
			Status = @Status
		WHERE Id = @Id;
	END
END