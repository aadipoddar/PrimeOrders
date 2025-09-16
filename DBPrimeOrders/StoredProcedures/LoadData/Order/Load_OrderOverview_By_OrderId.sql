CREATE PROCEDURE [dbo].[Load_OrderOverview_By_OrderId]
	@OrderId INT
AS
BEGIN
	SELECT *
	FROM [dbo].[Order_Overview]
	WHERE OrderId = @OrderId
END