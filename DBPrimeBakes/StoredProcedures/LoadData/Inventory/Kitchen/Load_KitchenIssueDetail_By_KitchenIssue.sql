CREATE PROCEDURE [dbo].[Load_KitchenIssueDetail_By_KitchenIssue]
	@KitchenIssueId INT
AS
BEGIN
	SELECT *
	FROM [dbo].[KitchenIssueDetail]
	WHERE [KitchenIssueId] = @KitchenIssueId
	AND [Status] = 1
END