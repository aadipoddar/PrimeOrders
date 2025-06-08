CREATE PROCEDURE [dbo].[Load_SaleDetails_By_Date_LocationId]
    @FromDate DATETIME,
    @ToDate DATETIME,
    @LocationId INT
AS
BEGIN
    IF @LocationId = 0
    BEGIN
        SELECT *
        FROM dbo.Sale_Overview v
        WHERE SaleDateTime BETWEEN @FromDate AND @ToDate;
    END

    ELSE
    BEGIN
        SELECT *
        FROM dbo.Sale_Overview v
        WHERE SaleDateTime BETWEEN @FromDate AND @ToDate
          AND LocationId = @LocationId;
    END
END