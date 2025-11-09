CREATE VIEW [dbo].[ProductStockDetails]
	AS
SELECT
	PS.Id,
	PS.ProductId,
	P.Code AS ProductCode,
	P.Name AS ProductName,
	PS.Quantity,
	PS.NetRate,
	PS.Type,
	PS.TransactionId,
	PS.TransactionNo,
	PS.TransactionDate,
	PS.LocationId,
	L.Name AS LocationName

FROM
	ProductStock AS PS

INNER JOIN
	Product AS P ON PS.ProductId = P.Id
INNER JOIN
	Location AS L ON PS.LocationId = L.Id