CREATE PROCEDURE [dbo].[Reset_Settings]
AS
BEGIN
	DELETE FROM [Settings]

	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'PrimaryCompanyLinkingId'			, N'1'		, N'Company Id for the Primary Company Account')

	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'LedgerCodePrefix'				, N'LD'		, N'Prefix for Ledger Codes')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'PurchaseTransactionPrefix'		, N'PUR'	, N'Prefix for Purchase Transaction Numbers')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'PurchaseReturnTransactionPrefix'	, N'PURRET'	, N'Prefix for Purchase Return Transaction Numbers')

	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'UpdateRawMaterialMasterRateOnPurchase'	, N'true'	, N'Update Raw Material Master Rate on Purchase Transactions')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'UpdateRawMaterialMasterUOMOnPurchase'	, N'true'	, N'Update Raw Material Master Unit of Measurement on Purchase Transactions')

	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'SalesVoucherId', N'1', N'Voucher type for Sales transactions')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'SaleReturnVoucherId', N'1', N'Voucher type for Sale Return transactions')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'PurchaseVoucherId', N'1', N'Voucher type for Purchase transactions')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'PurchaseReturnVoucherId', N'1', N'Voucher type for Purchase Return transactions')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'SaleLedgerId', N'1', N'Ledger account for Sales entries')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'PurchaseLedgerId', N'1', N'Ledger account for Purchase entries')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'CashLedgerId', N'1', N'Cash ledger account for Cash Entries')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'GSTLedgerId', N'1', N'GST ledger account for GST Tax Entries')

END