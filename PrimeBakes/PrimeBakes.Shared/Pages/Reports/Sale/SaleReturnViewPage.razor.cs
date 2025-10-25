using Microsoft.AspNetCore.Components;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Sale;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Sale;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Product;
using PrimeBakesLibrary.Models.Sale;

namespace PrimeBakes.Shared.Pages.Reports.Sale;

public partial class SaleReturnViewPage
{
	[Parameter] public int SaleReturnId { get; set; }

	private UserModel _user;
	private bool _isLoading = true;
	private bool _isProcessing = false;

	// Data models
	private SaleReturnOverviewModel _saleReturnOverview;
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
		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Sales, true);
		_user = authResult.User;

		await LoadSaleData();
		_isLoading = false;
	}

	private async Task LoadSaleData()
	{
		// Load sale overview
		_saleReturnOverview = await SaleReturnData.LoadSaleReturnOverviewBySaleReturnId(SaleReturnId);

		if (_saleReturnOverview is null)
			return;

		// Load sale details and build detailed product models
		var saleReturnDetails = await SaleReturnData.LoadSaleReturnDetailBySaleReturn(SaleReturnId);
		_detailedSaleProducts.Clear();

		foreach (var detail in saleReturnDetails)
		{
			var product = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, detail.ProductId);
			var category = product != null ? await CommonData.LoadTableDataById<ProductCategoryModel>(TableNames.ProductCategory, product.ProductCategoryId) : null;

			_detailedSaleProducts.Add(new()
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
	private async Task PrintPDF()
	{
		_isProcessing = true;
		StateHasChanged();

		var pdfData = await SaleReturnA4Print.GenerateA4SaleReturnBill(SaleReturnId);
		var fileName = $"SaleReturn_{_saleReturnOverview.BillNo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
		await SaveAndViewService.SaveAndView(fileName, "application/pdf", pdfData);
		VibrationService.VibrateWithTime(100);

		_isProcessing = false;
		StateHasChanged();
	}
	#endregion

	#region Sale Actions
	private void EditSale() =>
		ShowConfirmation(
			"Edit Sale Return",
			"Are you sure you want to edit this sale return?",
			"This will redirect you to the sale return editing page where you can modify the sale return details. This will Also Delete any Existing Sale Cart.",
			"Edit Sale Return",
			"fas fa-edit",
			"edit"
		);

	private void DeleteSale() =>
		ShowConfirmation(
			"Delete Sale Return",
			"Are you sure you want to delete this sale return?",
			"This action cannot be undone. All sale return data, including products and transactions, will be permanently removed.",
			"Delete Sale Return",
			"fas fa-trash-alt",
			"delete"
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
		}

		_isProcessing = false;
		_showConfirmation = false;
		StateHasChanged();
	}

	private async Task EditSaleConfirmed()
	{
		await DataStorageService.LocalRemove(StorageFileNames.SaleReturnDataFileName);
		await DataStorageService.LocalRemove(StorageFileNames.SaleReturnCartDataFileName);

		var saleReturn = await CommonData.LoadTableDataById<SaleReturnModel>(TableNames.SaleReturn, SaleReturnId);
		var saleReturnDetails = await SaleReturnData.LoadSaleReturnDetailBySaleReturn(SaleReturnId);

		List<SaleReturnProductCartModel> cart = [];
		foreach (var detail in saleReturnDetails)
		{
			var product = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, detail.ProductId);

			cart.Add(new()
			{
				ProductId = detail.ProductId,
				ProductName = product.Name,
				ProductCategoryId = product.ProductCategoryId,
				Quantity = detail.Quantity,
				Rate = detail.Rate,
				BaseTotal = detail.BaseTotal,
				DiscPercent = detail.DiscPercent,
				DiscAmount = detail.DiscAmount,
				AfterDiscount = detail.AfterDiscount,
				CGSTPercent = detail.CGSTPercent,
				CGSTAmount = detail.CGSTAmount,
				SGSTPercent = detail.SGSTPercent,
				SGSTAmount = detail.SGSTAmount,
				IGSTPercent = detail.IGSTPercent,
				IGSTAmount = detail.IGSTAmount,
				Total = detail.Total,
				NetRate = detail.NetRate
			});
		}

		await DataStorageService.LocalSaveAsync(StorageFileNames.SaleReturnDataFileName, System.Text.Json.JsonSerializer.Serialize(saleReturn));
		await DataStorageService.LocalSaveAsync(StorageFileNames.SaleReturnCartDataFileName, System.Text.Json.JsonSerializer.Serialize(cart));

		NavigationManager.NavigateTo("/SaleReturn");
	}

	private async Task DeleteSaleConfirmed()
	{
		var saleReturn = await CommonData.LoadTableDataById<SaleReturnModel>(TableNames.SaleReturn, SaleReturnId);
		if (saleReturn is null)
			return;

		await SaleReturnData.DeleteSaleReturn(saleReturn);

		VibrationService.VibrateWithTime(200);
		NavigationManager.NavigateTo("/Reports/SaleReturn");
	}

	private void CancelAction()
	{
		_showConfirmation = false;
		_currentAction = "";
	}
	#endregion
}