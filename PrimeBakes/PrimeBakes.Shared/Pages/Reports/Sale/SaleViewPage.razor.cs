using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory;
using PrimeBakesLibrary.Data.Order;
using PrimeBakesLibrary.Data.Sale;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Sale;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Order;
using PrimeBakesLibrary.Models.Product;
using PrimeBakesLibrary.Models.Sale;

namespace PrimeBakes.Shared.Pages.Reports.Sale;

public partial class SaleViewPage
{
	[Parameter] public int SaleId { get; set; }

	private UserModel _user;
	private bool _isLoading = true;
	private bool _isProcessing = false;

	// Data models
	private SaleOverviewModel _saleOverview;
	private readonly List<DetailedSaleProductModel> _detailedSaleProducts = [];

	// Dialog properties
	private bool _showConfirmation = false;
	private string _confirmationTitle = "";
	private string _confirmationMessage = "";
	private string _confirmationDetails = "";
	private string _confirmationButtonText = "";
	private string _confirmationIcon = "";
	private string _currentAction = "";

	#region Load Data
	protected override async Task OnInitializedAsync()
	{
		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Sales);
		_user = authResult.User;

		await LoadSaleData();
		_isLoading = false;
	}

	private async Task LoadSaleData()
	{
		// Load sale overview
		_saleOverview = await SaleData.LoadSaleOverviewBySaleId(SaleId);

		if (_saleOverview == null)
			return;

		// Check access permissions
		if (_user.LocationId != 1 && _user.LocationId != _saleOverview.LocationId)
		{
			NavigationManager.NavigateTo("/Reports/Sale/Detailed");
			return;
		}

		// Load sale details and build detailed product models
		var saleDetails = await SaleData.LoadSaleDetailBySale(SaleId);
		_detailedSaleProducts.Clear();

		foreach (var detail in saleDetails)
		{
			var product = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, detail.ProductId);
			var category = product != null ? await CommonData.LoadTableDataById<ProductCategoryModel>(TableNames.ProductCategory, product.ProductCategoryId) : null;

			_detailedSaleProducts.Add(new DetailedSaleProductModel
			{
				ProductId = detail.ProductId,
				ProductName = product?.Name ?? "Unknown Product",
				ProductCode = product?.Code ?? "N/A",
				CategoryName = category?.Name ?? "Unknown Category",
				Quantity = detail.Quantity,
				Rate = detail.Rate,
				BaseTotal = detail.BaseTotal,
				DiscountPercent = detail.DiscPercent,
				DiscountAmount = detail.DiscAmount,
				AfterDiscount = detail.AfterDiscount,
				CGSTPercent = detail.CGSTPercent,
				CGSTAmount = detail.CGSTAmount,
				SGSTPercent = detail.SGSTPercent,
				SGSTAmount = detail.SGSTAmount,
				IGSTPercent = detail.IGSTPercent,
				IGSTAmount = detail.IGSTAmount,
				TotalTaxAmount = detail.CGSTAmount + detail.SGSTAmount + detail.IGSTAmount,
				Total = detail.Total,
				NetRate = detail.NetRate
			});
		}
	}
	#endregion

	#region Exporting
	private async Task PrintThermalBill()
	{
		_isProcessing = true;
		StateHasChanged();

		var sale = await CommonData.LoadTableDataById<SaleModel>(TableNames.Sale, SaleId);
		var content = await SaleThermalPrint.GenerateThermalBill(sale);
		await JSRuntime.InvokeVoidAsync("printToPrinter", content.ToString());

		_isProcessing = false;
		StateHasChanged();
	}

	private async Task PrintPDF()
	{
		_isProcessing = true;
		StateHasChanged();

		var pdfData = await SaleA4Print.GenerateA4SaleBill(SaleId);
		var fileName = $"Sale_{_saleOverview.BillNo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
		await SaveAndViewService.SaveAndView(fileName, "application/pdf", pdfData);
		VibrationService.VibrateWithTime(100);

		_isProcessing = false;
		StateHasChanged();
	}
	#endregion

	#region Sale Actions
	private void EditSale() =>
		ShowConfirmation(
			"Edit Sale",
			"Are you sure you want to edit this sale?",
			"This will redirect you to the sale editing page where you can modify the sale details. This will Also Delete any Existing Sale Cart.",
			"Edit Sale",
			"fas fa-edit",
			"edit"
		);

	private void DeleteSale() =>
		ShowConfirmation(
			"Delete Sale",
			"Are you sure you want to delete this sale?",
			"This action cannot be undone. All sale data, including products and transactions, will be permanently removed.",
			"Delete Sale",
			"fas fa-trash-alt",
			"delete"
		);

	private void UnlinkOrder() =>
		ShowConfirmation(
			"Unlink Order",
			"Are you sure you want to unlink this sale from the order?",
			"This will remove the connection between this sale and the associated order. The order status may be affected.",
			"Unlink Order",
			"fas fa-unlink",
			"unlink"
		);

	private void ShowConfirmation(string title, string message, string details, string buttonText, string icon, string action)
	{
		_confirmationTitle = title;
		_confirmationMessage = message;
		_confirmationDetails = details;
		_confirmationButtonText = buttonText;
		_confirmationIcon = icon;
		_currentAction = action;
		_showConfirmation = true;
	}

	private async Task ConfirmAction()
	{
		_isProcessing = true;
		StateHasChanged();

		switch (_currentAction)
		{
			case "edit":
				await EditSaleConfirmed();
				break;
			case "delete":
				await DeleteSaleConfirmed();
				break;
			case "unlink":
				await UnlinkOrderConfirmed();
				break;
		}

		_isProcessing = false;
		_showConfirmation = false;
		StateHasChanged();
	}

	private async Task EditSaleConfirmed()
	{
		await DataStorageService.LocalRemove(StorageFileNames.SaleDataFileName);
		await DataStorageService.LocalRemove(StorageFileNames.SaleCartDataFileName);

		var sale = await CommonData.LoadTableDataById<SaleModel>(TableNames.Sale, SaleId);
		var saleDetails = await SaleData.LoadSaleDetailBySale(SaleId);

		await DataStorageService.LocalSaveAsync(StorageFileNames.SaleDataFileName, System.Text.Json.JsonSerializer.Serialize(sale));
		await DataStorageService.LocalSaveAsync(StorageFileNames.SaleCartDataFileName, System.Text.Json.JsonSerializer.Serialize(saleDetails));

		NavigationManager.NavigateTo("/Sale");
	}

	private async Task DeleteSaleConfirmed()
	{
		var sale = await CommonData.LoadTableDataById<SaleModel>(TableNames.Sale, SaleId);
		if (sale is null)
			return;

		sale.Status = false;
		await SaleData.InsertSale(sale);
		await StockData.DeleteProductStockByTransactionNo(sale.BillNo);
		if (sale.LocationId == 1)
		{
			var accounting = await AccountingData.LoadAccountingByReferenceNo(sale.BillNo);
			accounting.Status = false;
			await AccountingData.InsertAccounting(accounting);
		}

		VibrationService.VibrateWithTime(200);
		NavigationManager.NavigateTo("/Reports/Sale/Detailed");
	}

	private async Task UnlinkOrderConfirmed()
	{
		var sale = await CommonData.LoadTableDataById<SaleModel>(TableNames.Sale, SaleId);
		if (sale is null || !sale.OrderId.HasValue)
			return;

		var order = await CommonData.LoadTableDataById<OrderModel>(TableNames.Order, sale.OrderId.Value);
		if (order is null)
			return;

		order.SaleId = null;
		await OrderData.InsertOrder(order);

		sale.OrderId = null;
		await SaleData.InsertSale(sale);

		VibrationService.VibrateWithTime(150);
		// Reload the sale data to reflect the changes
		await LoadSaleData();
	}

	private void CancelAction()
	{
		_showConfirmation = false;
		_currentAction = "";
	}
	#endregion
}

// Model for detailed sale product display
public class DetailedSaleProductModel
{
	public int ProductId { get; set; }
	public string ProductName { get; set; } = "";
	public string ProductCode { get; set; } = "";
	public string CategoryName { get; set; } = "";
	public decimal Quantity { get; set; }
	public decimal Rate { get; set; }
	public decimal BaseTotal { get; set; }
	public decimal DiscountPercent { get; set; }
	public decimal DiscountAmount { get; set; }
	public decimal AfterDiscount { get; set; }
	public decimal CGSTPercent { get; set; }
	public decimal CGSTAmount { get; set; }
	public decimal SGSTPercent { get; set; }
	public decimal SGSTAmount { get; set; }
	public decimal IGSTPercent { get; set; }
	public decimal IGSTAmount { get; set; }
	public decimal TotalTaxAmount { get; set; }
	public decimal Total { get; set; }
	public decimal NetRate { get; set; }
}