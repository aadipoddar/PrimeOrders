CREATE VIEW [dbo].[RawMaterialStockDetails]
	AS
SELECT
	RMS.Id,
	RMS.RawMaterialId,
	RM.Code AS RawMaterialCode,
	RM.Name AS RawMaterialName,
	RMS.Quantity,
	RMS.NetRate,
	RMS.Type,
	RMS.TransactionNo,
	RMS.TransactionDate,
	RMS.LocationId

FROM
	RawMaterialStock AS RMS

INNER JOIN
	RawMaterial AS RM ON RMS.RawMaterialId = RM.Id
