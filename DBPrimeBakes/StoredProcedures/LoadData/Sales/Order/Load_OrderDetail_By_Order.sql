CREATE PROCEDURE [dbo].[Load_OrderDetail_By_Order]
	@OrderId INT
AS
BEGIN
	SELECT *
	FROM [dbo].[OrderDetail]
	WHERE [OrderId] = @OrderId
	AND [Status] = 1
END