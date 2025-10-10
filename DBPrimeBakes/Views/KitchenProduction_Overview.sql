CREATE VIEW [dbo].[KitchenProduction_Overview]
	AS
	SELECT
		kp.Id AS KitchenProductionId,
		kp.TransactionNo,
		kp.KitchenId,
		k.Name AS KitchenName,
		kp.ProductionDate,
		kp.UserId,
		u.Name AS UserName,
		kp.Remarks,

		COUNT(DISTINCT kp.Id) AS TotalProducts,
		SUM(kpd.Quantity) AS TotalQuantity,
		SUM(kpd.Total) AS TotalAmount,

		kp.CreatedAt

	FROM
		dbo.KitchenProduction kp

	INNER JOIN
		dbo.Kitchen k ON kp.KitchenId = k.Id
	INNER JOIN
		dbo.[User] u ON kp.UserId = u.Id
	INNER JOIN
		dbo.KitchenProductionDetail kpd ON kp.Id = kpd.KitchenProductionId

	WHERE
		kp.Status = 1
		AND kpd.Status = 1

	GROUP BY
		kp.Id,
		kp.TransactionNo,
		kp.KitchenId,
		k.Name,
		kp.ProductionDate,
		kp.UserId,
		kp.Remarks,
		kp.CreatedAt,
		u.Name