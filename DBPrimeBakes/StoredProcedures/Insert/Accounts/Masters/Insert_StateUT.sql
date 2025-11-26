CREATE PROCEDURE [dbo].[Insert_StateUT]
	@Id INT OUTPUT,
	@Name VARCHAR(500),
	@UnionTerritory BIT,
	@Remarks VARCHAR(MAX) = NULL,
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[StateUT] ([Name], [UnionTerritory], [Remarks], [Status])
		VALUES (@Name, @UnionTerritory, @Remarks, @Status);
		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[StateUT]
		SET [Name] = @Name, 
			[UnionTerritory] = @UnionTerritory,
			[Remarks] = @Remarks,
			[Status] = @Status
		WHERE [Id] = @Id;
	END

	SELECT @Id AS Id;
END