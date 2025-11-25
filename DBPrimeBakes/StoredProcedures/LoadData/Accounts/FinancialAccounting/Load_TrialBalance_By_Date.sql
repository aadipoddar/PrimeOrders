CREATE PROCEDURE [dbo].[Load_TrialBalance_By_Date]
	@StartDate DATE,
	@EndDate DATE
AS
BEGIN
	SET NOCOUNT ON;

	-- Calculate opening balance (all transactions before @FromDate)
	WITH OpeningBalance AS (
		SELECT 
			ad.LedgerId,
			SUM(ISNULL(ad.Debit, 0)) AS OpeningDebit,
			SUM(ISNULL(ad.Credit, 0)) AS OpeningCredit
		FROM 
			AccountingDetail ad
		INNER JOIN 
			Accounting a ON ad.AccountingId = a.Id
		WHERE 
			a.TransactionDateTime < @StartDate
			AND a.Status = 1
			AND ad.Status = 1
		GROUP BY 
			ad.LedgerId
	),
	-- Calculate transactions within the date range
	PeriodTransactions AS (
		SELECT 
			ad.LedgerId,
			SUM(ISNULL(ad.Debit, 0)) AS PeriodDebit,
			SUM(ISNULL(ad.Credit, 0)) AS PeriodCredit
		FROM 
			AccountingDetail ad
		INNER JOIN 
			Accounting a ON ad.AccountingId = a.Id
		WHERE 
			a.TransactionDateTime >= @StartDate
			AND a.TransactionDateTime <= @EndDate
			AND a.Status = 1
			AND ad.Status = 1
		GROUP BY 
			ad.LedgerId
	)
	-- Combine all data
	SELECT 
		l.Id AS LedgerId,
		l.Code AS LedgerCode,
		l.Name AS LedgerName,
		g.Id AS GroupId,
		g.Name AS GroupName,
		at.Id AS AccountTypeId,
		at.Name AS AccountTypeName,
		
		-- Opening Balance
		ISNULL(ob.OpeningDebit, 0) AS OpeningDebit,
		ISNULL(ob.OpeningCredit, 0) AS OpeningCredit,
		(ISNULL(ob.OpeningDebit, 0) - ISNULL(ob.OpeningCredit, 0)) AS OpeningBalance,
		
		-- Period Transactions
		ISNULL(pt.PeriodDebit, 0) AS Debit,
		ISNULL(pt.PeriodCredit, 0) AS Credit,
		
		-- Closing Balance
		(ISNULL(ob.OpeningDebit, 0) + ISNULL(pt.PeriodDebit, 0)) AS ClosingDebit,
		(ISNULL(ob.OpeningCredit, 0) + ISNULL(pt.PeriodCredit, 0)) AS ClosingCredit,
		((ISNULL(ob.OpeningDebit, 0) + ISNULL(pt.PeriodDebit, 0)) - 
		 (ISNULL(ob.OpeningCredit, 0) + ISNULL(pt.PeriodCredit, 0))) AS ClosingBalance
		
	FROM 
		Ledger l
	INNER JOIN 
		[Group] g ON l.GroupId = g.Id
	INNER JOIN 
		AccountType at ON l.AccountTypeId = at.Id
	LEFT JOIN 
		OpeningBalance ob ON l.Id = ob.LedgerId
	LEFT JOIN 
		PeriodTransactions pt ON l.Id = pt.LedgerId
	WHERE 
		l.Status = 1
		AND (ob.LedgerId IS NOT NULL OR pt.LedgerId IS NOT NULL) -- Only show ledgers with transactions
	ORDER BY 
		g.Name, l.Name

END
