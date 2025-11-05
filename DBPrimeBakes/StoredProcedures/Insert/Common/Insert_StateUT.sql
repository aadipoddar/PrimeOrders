CREATE PROCEDURE [dbo].[Insert_StateUT]
	@Id INT OUTPUT,
	@Name VARCHAR(250),
	@UnionTerritory BIT,
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[StateUT] ([Name], [UnionTerritory] ,[Status])
		VALUES (@Name, @UnionTerritory, @Status);
		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[StateUT]
		SET [Name] = @Name, 
			[UnionTerritory] = @UnionTerritory,
			[Status] = @Status
		WHERE [Id] = @Id;
	END

	SELECT @Id AS Id;
END