CREATE VIEW [dbo].[KitchenIssue_Overview]
	AS
	SELECT
		ki.Id AS KitchenIssueId,
		ki.TransactionNo,
		ki.KitchenId,
		k.Name AS KitchenName,
		ki.IssueDate,
		ki.UserId,
		u.Name AS UserName,

		COUNT(DISTINCT ki.Id) AS TotalProducts,
		SUM(kid.Quantity) AS TotalQuantity

	FROM
		dbo.KitchenIssue ki

	INNER JOIN
		dbo.Kitchen k ON ki.KitchenId = k.Id
	INNER JOIN
		dbo.[User] u ON ki.UserId = u.Id
	INNER JOIN
		dbo.KitchenIssueDetail kid ON ki.Id = kid.KitchenIssueId

	WHERE
		ki.Status = 1
		AND kid.Status = 1

	GROUP BY
		ki.Id,
		ki.TransactionNo,
		ki.KitchenId,
		k.Name,
		ki.IssueDate,
		ki.UserId,
		u.Name