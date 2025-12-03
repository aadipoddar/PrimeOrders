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
	RMS.TransactionId,
	RMS.TransactionNo,
	RMS.TransactionDate AS TransactionDateTime

FROM
	RawMaterialStock AS RMS

INNER JOIN
	RawMaterial AS RM ON RMS.RawMaterialId = RM.Id