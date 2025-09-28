CREATE PROCEDURE [dbo].[Load_PurchaseDetails_By_Date]
	@FromDate DATE,
	@ToDate DATE
AS
BEGIN
	SELECT
		*
	FROM
		[dbo].[Purchase_Overview] AS po
	WHERE
		po.BillDateTime BETWEEN @FromDate AND @ToDate
END;