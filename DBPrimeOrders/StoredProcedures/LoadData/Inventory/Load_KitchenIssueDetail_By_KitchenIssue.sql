CREATE PROCEDURE [dbo].[Load_KitchenIssueDetail_By_KitchenIssue]
	@KitchenIssueId INT
AS
BEGIN
	SELECT
	*
	FROM [dbo].[KitchenIssueDetail] AS kid
	WHERE kid.KitchenIssueId = @KitchenIssueId
	AND kid.Status = 1
END