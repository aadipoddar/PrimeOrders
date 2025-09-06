CREATE PROCEDURE [dbo].[Load_Customer_By_Number]
	@Number VARCHAR(10)
AS
BEGIN
	SELECT *
	FROM [dbo].[Customer]
	WHERE [Number] = @Number;
END