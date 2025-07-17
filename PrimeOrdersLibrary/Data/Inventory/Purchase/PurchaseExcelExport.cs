using PrimeOrdersLibrary.Data.Common;
using PrimeOrdersLibrary.Exporting;
using PrimeOrdersLibrary.Models.Inventory;

namespace PrimeOrdersLibrary.Data.Inventory.Purchase;

public static class PurchaseExcelExport
{
	public static async Task<MemoryStream> ExportPurchaseOverviewExcel(List<PurchaseOverviewModel> purchaseOverviewModels, DateOnly startDate, DateOnly endDate, int selectedSupplierId)
	{
		// Create summary items dictionary
		Dictionary<string, object> summaryItems = new()
		{
			{ "Total Purchases", purchaseOverviewModels.Sum(p => p.Total) },
			{ "Total Transactions", purchaseOverviewModels.Count },
			{ "Total Items", purchaseOverviewModels.Sum(p => p.TotalItems) },
			{ "Total Quantity", purchaseOverviewModels.Sum(p => p.TotalQuantity) },
			{ "Total Discount", purchaseOverviewModels.Sum(p => p.DiscountAmount) },
			{ "Total Tax", purchaseOverviewModels.Sum(p => p.TotalTaxAmount) },
			{ "Base Total", purchaseOverviewModels.Sum(p => p.BaseTotal) },
			{ "Sub Total", purchaseOverviewModels.Sum(p => p.SubTotal) }
		};

		// Add supplier filter info if specific supplier is selected
		if (selectedSupplierId > 0)
		{
			var suppliers = await CommonData.LoadTableData<SupplierModel>(TableNames.Supplier);
			var supplierName = suppliers.FirstOrDefault(s => s.Id == selectedSupplierId)?.Name ?? "Unknown";
			summaryItems.Add("Filtered by Supplier", supplierName);
		}

		// Define the column order for better readability
		List<string> columnOrder = [
			nameof(PurchaseOverviewModel.BillNo),
			nameof(PurchaseOverviewModel.BillDate),
			nameof(PurchaseOverviewModel.SupplierName),
			nameof(PurchaseOverviewModel.UserName),
			nameof(PurchaseOverviewModel.TotalItems),
			nameof(PurchaseOverviewModel.TotalQuantity),
			nameof(PurchaseOverviewModel.BaseTotal),
			nameof(PurchaseOverviewModel.DiscountAmount),
			nameof(PurchaseOverviewModel.TotalTaxAmount),
			nameof(PurchaseOverviewModel.SubTotal),
			nameof(PurchaseOverviewModel.CashDiscountAmount),
			nameof(PurchaseOverviewModel.Total),
			nameof(PurchaseOverviewModel.Remarks)
		];

		// Define custom column settings
		Dictionary<string, ExcelExportUtil.ColumnSetting> columnSettings = new()
		{
			[nameof(PurchaseOverviewModel.BillNo)] = new()
			{
				DisplayName = "Invoice No",
				Width = 15,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			},
			[nameof(PurchaseOverviewModel.BillDate)] = new()
			{
				DisplayName = "Date",
				Format = "dd-MMM-yyyy",
				Width = 15
			},
			[nameof(PurchaseOverviewModel.SupplierName)] = new()
			{
				DisplayName = "Supplier",
				Width = 25,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(PurchaseOverviewModel.UserName)] = new()
			{
				DisplayName = "Created By",
				Width = 20,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(PurchaseOverviewModel.TotalItems)] = new()
			{
				DisplayName = "Items",
				Format = "#,##0",
				Width = 12,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(PurchaseOverviewModel.TotalQuantity)] = new()
			{
				DisplayName = "Quantity",
				Format = "#,##0.00",
				Width = 15,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(PurchaseOverviewModel.BaseTotal)] = new()
			{
				DisplayName = "Base Total",
				Format = "₹#,##0.00",
				Width = 15,
				IsCurrency = true,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(PurchaseOverviewModel.DiscountAmount)] = new()
			{
				DisplayName = "Discount",
				Format = "₹#,##0.00",
				Width = 15,
				IsCurrency = true,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(PurchaseOverviewModel.TotalTaxAmount)] = new()
			{
				DisplayName = "Tax",
				Format = "₹#,##0.00",
				Width = 15,
				IsCurrency = true,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(PurchaseOverviewModel.SubTotal)] = new()
			{
				DisplayName = "Sub Total",
				Format = "₹#,##0.00",
				Width = 15,
				IsCurrency = true,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(PurchaseOverviewModel.CashDiscountAmount)] = new()
			{
				DisplayName = "Cash Discount",
				Format = "₹#,##0.00",
				Width = 15,
				IsCurrency = true,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(PurchaseOverviewModel.Total)] = new()
			{
				DisplayName = "Total",
				Format = "₹#,##0.00",
				Width = 15,
				IsCurrency = true,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight,
				FormatCallback = (value) =>
				{
					if (value == null) return null;
					var amount = Convert.ToDecimal(value);
					return new ExcelExportUtil.FormatInfo
					{
						Bold = amount > 10000,
						FontColor = amount > 50000 ? Syncfusion.Drawing.Color.FromArgb(56, 142, 60) : null
					};
				}
			},
			[nameof(PurchaseOverviewModel.Remarks)] = new()
			{
				DisplayName = "Remarks",
				Width = 30,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			}
		};

		// Generate the Excel file
		string reportTitle = "Purchase Report";
		string worksheetName = "Purchase Details";

		return ExcelExportUtil.ExportToExcel(
			purchaseOverviewModels,
			reportTitle,
			worksheetName,
			startDate,
			endDate,
			summaryItems,
			columnSettings,
			columnOrder);
	}
}
