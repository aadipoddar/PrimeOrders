CREATE PROCEDURE [dbo].[Load_Order_By_OrderNo]
	@OrderNo VARCHAR(20)
AS
BEGIN
	SELECT *
	FROM [dbo].[Order]
	WHERE OrderNo = @OrderNo;
END