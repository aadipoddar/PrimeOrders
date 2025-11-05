CREATE PROCEDURE [dbo].[Load_TrialBalance_By_Date]
	@FromDate DATETIME,
	@ToDate DATETIME
AS
BEGIN
	SELECT
		ad.LedgerId,
		l.Name AS LedgerName,
		l.Code AS LedgerCode,
		l.GroupId,
		g.Name AS GroupName,
		l.AccountTypeId,
		at.Name AS AccountTypeName,
		l.Phone AS Phone,
		l.Address,
		l.GSTNo,
		l.[StateUTId],
		s.Name AS StateName,
		l.LocationId,
		loc.Name AS LocationName,
	
		ISNULL
	        (
	           (SELECT ISNULL(SUM (Debit), 0) + ISNULL(SUM (Credit), 0)
					FROM AccountingDetails
				INNER JOIN Accounting a ON a.Id = AccountingDetails.AccountingId
					WHERE	LedgerId = ad.LedgerId
							AND AccountingDate < @FromDate
							AND a.Status = 1
							AND AccountingDetails.Status = 1
				GROUP BY LedgerId),
	           0) AS OpeningBalance,
	
		ISNULL
	        (
	           (SELECT ISNULL(SUM (Debit), 0)
					FROM AccountingDetails
				INNER JOIN Accounting a ON a.Id = AccountingDetails.AccountingId
					WHERE	LedgerId = ad.LedgerId
							AND AccountingDate >= @FromDate
							AND AccountingDate <= @ToDate
							AND a.Status = 1
							AND AccountingDetails.Status = 1
				GROUP BY LedgerId),
	           0) AS TotalDebit,
	
		ISNULL
	        (
	           (SELECT ISNULL(SUM (Credit), 0)
					FROM AccountingDetails
				INNER JOIN Accounting a ON a.Id = AccountingDetails.AccountingId
					WHERE	LedgerId = ad.LedgerId
							AND AccountingDate >= @FromDate
							AND AccountingDate <= @ToDate
							AND a.Status = 1
							AND AccountingDetails.Status = 1
				GROUP BY LedgerId),
	           0) AS TotalCredit,
	
		ISNULL
	        (
	           (SELECT ISNULL(SUM (Debit), 0) + ISNULL(SUM (Credit), 0)
					FROM AccountingDetails
				INNER JOIN Accounting a ON a.Id = AccountingDetails.AccountingId
					WHERE	LedgerId = ad.LedgerId
							AND AccountingDate <= @ToDate
							AND a.Status = 1
							AND AccountingDetails.Status = 1
				GROUP BY LedgerId),
	           0) AS ClosingBalance
	
	FROM
		[dbo].[AccountingDetails] ad
	
	INNER JOIN
		[dbo].[Accounting] a ON a.Id = ad.AccountingId
	INNER JOIN
		[dbo].[Ledger] l ON l.Id = ad.LedgerId
	INNER JOIN
		[dbo].[Group] g ON g.Id = l.GroupId
	INNER JOIN
		[dbo].[AccountType] at ON at.Id = l.AccountTypeId
	INNER JOIN
		[dbo].[StateUT] s ON s.Id = l.[StateUTId]
	LEFT JOIN
		[dbo].[Location] loc ON loc.Id = l.LocationId
	
	WHERE 
		a.Status = 1 AND
		ad.Status = 1
	
	GROUP BY
		ad.LedgerId,
		l.Name,
		l.Code,
		l.GroupId,
		g.Name,
		l.AccountTypeId,
		at.Name,
		l.Phone,
		l.Address,
		l.GSTNo,
		l.[StateUTId],
		s.Name,
		l.LocationId,
		loc.Name
END