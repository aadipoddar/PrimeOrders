CREATE PROCEDURE [dbo].[Delete_RawMaterialStock_By_Id]
	@Id INT
AS
BEGIN
	DELETE FROM [dbo].[RawMaterialStock]
	WHERE [Id] = @Id
END