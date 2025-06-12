CREATE PROCEDURE [dbo].[Load_OrderDetails_By_Date_LocationId]
	@FromDate DATETIME,
    @ToDate DATETIME,
    @LocationId INT
AS
BEGIN
    IF @LocationId = 0
    BEGIN
        SELECT *
        FROM dbo.Order_Overview v
        WHERE OrderDate BETWEEN @FromDate AND @ToDate;
    END
    ELSE
    BEGIN
        SELECT *
        FROM dbo.Order_Overview v
        WHERE OrderDate BETWEEN @FromDate AND @ToDate
          AND LocationId = @LocationId;
    END
END