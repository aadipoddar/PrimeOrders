CREATE PROCEDURE [dbo].[Load_KitchenIssueDetails_By_Date]
	@FromDate DATETIME,
	@ToDate DATETIME
AS
BEGIN
	SELECT *
	FROM dbo.KitchenIssue_Overview v
	WHERE IssueDate BETWEEN @FromDate AND @ToDate;
END