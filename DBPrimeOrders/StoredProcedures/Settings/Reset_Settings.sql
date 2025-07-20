CREATE PROCEDURE [dbo].[Reset_Settings]
AS
BEGIN
	DELETE FROM [Settings]

	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'SalesVoucherId', N'1', N'Voucher type for sales transactions')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'PurchaseVoucherId', N'1', N'Voucher type for purchase transactions')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'SaleLedgerId', N'1', N'Ledger account for sales entries')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'PurchaseLedgerId', N'1', N'Ledger account for purchase entries')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'CashLedgerId', N'1', N'Cash ledger account for cash transactions')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'GSTLedgerId', N'1', N'GST ledger account for tax calculations')

END