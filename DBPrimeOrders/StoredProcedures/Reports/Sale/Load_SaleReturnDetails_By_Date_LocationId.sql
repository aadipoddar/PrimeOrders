CREATE PROCEDURE [dbo].[Load_SaleReturnDetails_By_Date_LocationId]
	@FromDate DATETIME,
    @ToDate DATETIME,
    @LocationId INT
AS
BEGIN
    IF @LocationId = 0
    BEGIN
        SELECT *
        FROM dbo.SaleReturn_Overview v
        WHERE ReturnDateTime BETWEEN @FromDate AND @ToDate;
    END

    ELSE
    BEGIN
        SELECT *
        FROM dbo.SaleReturn_Overview v
        WHERE ReturnDateTime BETWEEN @FromDate AND @ToDate
          AND LocationId = @LocationId;
    END
END
