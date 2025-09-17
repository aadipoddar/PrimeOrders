CREATE PROCEDURE [dbo].[Load_KitchenIssueOverview_By_KitchenIssueId]
	@KitchenIssueId INT
AS
BEGIN
	SELECT *
	FROM [dbo].[KitchenIssue_Overview]
	WHERE KitchenIssueId = @KitchenIssueId
END