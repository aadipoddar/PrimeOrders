using Microsoft.AspNetCore.Components;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Stock;
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

	private UserModel _user = default!;
	private bool _isLoading = true;
	private bool _isProcessing = false;

	// Data models
	private SaleReturnOverviewModel? _saleReturnOverview;
	private readonly List<DetailedSaleReturnProductModel> _detailedReturnProducts = [];

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

		await LoadSaleReturnData();
		_isLoading = false;
	}

	private async Task LoadSaleReturnData()
	{
		try
		{
			// Get the sale return details by ID using date range approach
			var saleReturns = await SaleReturnData.LoadSaleReturnDetailsByDateLocationId(
				DateTime.Now.AddYears(-1), // Start date - 1 year back to ensure we capture the return
				DateTime.Now.AddDays(1),   // End date - tomorrow to ensure we capture today's returns
				_user.LocationId
			);

			_saleReturnOverview = saleReturns.FirstOrDefault(sr => sr.SaleReturnId == SaleReturnId);

			// If not found in user's location and user is from primary location (LocationId 1), 
			// try to fetch from all locations
			if (_saleReturnOverview == null && _user.LocationId == 1)
			{
				var allReturns = await SaleReturnData.LoadSaleReturnDetailsByDateLocationId(
					DateTime.Now.AddYears(-1),
					DateTime.Now.AddDays(1),
					0 // 0 to fetch all locations
				);

				_saleReturnOverview = allReturns.FirstOrDefault(sr => sr.SaleReturnId == SaleReturnId);
			}

			if (_saleReturnOverview == null)
				return;

			// Check access permissions
			if (_user.LocationId != 1 && _user.LocationId != _saleReturnOverview.LocationId)
			{
				NavigationManager.NavigateTo("/Reports/Sale/Return");
				return;
			}

			// Load detailed product information
			await LoadDetailedReturnProducts();
		}
		catch (Exception ex)
		{
			// Handle error - could add logging here
			Console.WriteLine($"Error loading sale return details: {ex.Message}");
		}
	}

	private async Task LoadDetailedReturnProducts()
	{
		if (_saleReturnOverview == null) return;

		try
		{
			// Load return details and build detailed product models
			var returnDetails = await SaleReturnData.LoadSaleReturnDetailBySaleReturn(SaleReturnId);
			_detailedReturnProducts.Clear();

			foreach (var detail in returnDetails)
			{
				var product = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, detail.ProductId);
				var category = product != null ? await CommonData.LoadTableDataById<ProductCategoryModel>(TableNames.ProductCategory, product.ProductCategoryId) : null;

				_detailedReturnProducts.Add(new DetailedSaleReturnProductModel
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
		catch (Exception ex)
		{
			Console.WriteLine($"Error loading detailed return products: {ex.Message}");
		}
	}
	#endregion

	#region Exporting
	private async Task PrintPDF()
	{
		_isProcessing = true;
		StateHasChanged();

		try
		{
			// Load the actual SaleReturnModel for PDF generation
			var saleReturn = await CommonData.LoadTableDataById<SaleReturnModel>(TableNames.SaleReturn, SaleReturnId);
			if (saleReturn != null)
			{
				var pdfData = await SaleReturnA4Print.GenerateA4SaleReturnBill(saleReturn.Id);
				var fileName = $"SaleReturn_{_saleReturnOverview.TransactionNo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
				await SaveAndViewService.SaveAndView(fileName, "application/pdf", pdfData);
				VibrationService.VibrateWithTime(100);
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error generating PDF: {ex.Message}");
		}

		_isProcessing = false;
		StateHasChanged();
	}
	#endregion

	#region Sale Return Actions
	private void EditSaleReturn() =>
		ShowConfirmation(
			"Edit Sale Return",
			"Are you sure you want to edit this sale return?",
			"This will redirect you to the sale return editing page where you can modify the return details.",
			"Edit Return",
			"fas fa-edit",
			"edit"
		);

	private void DeleteSaleReturn() =>
		ShowConfirmation(
			"Delete Sale Return",
			"Are you sure you want to delete this sale return?",
			"This action cannot be undone. All return data, including products and transactions, will be permanently removed.",
			"Delete Return",
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
				await EditSaleReturnConfirmed();
				break;
			case "delete":
				await DeleteSaleReturnConfirmed();
				break;
		}

		_isProcessing = false;
		_showConfirmation = false;
		StateHasChanged();
	}

	private async Task EditSaleReturnConfirmed()
	{
		try
		{
			// Clear any existing sale return cart data
			await DataStorageService.LocalRemove(StorageFileNames.SaleReturnDataFileName);

			var saleReturn = await CommonData.LoadTableDataById<SaleReturnModel>(TableNames.SaleReturn, SaleReturnId);
			if (saleReturn != null)
			{
				await DataStorageService.LocalSaveAsync(StorageFileNames.SaleReturnDataFileName, System.Text.Json.JsonSerializer.Serialize(saleReturn));
				NavigationManager.NavigateTo("/SaleReturn");
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error editing sale return: {ex.Message}");
		}
	}

	private async Task DeleteSaleReturnConfirmed()
	{
		try
		{
			var saleReturn = await CommonData.LoadTableDataById<SaleReturnModel>(TableNames.SaleReturn, SaleReturnId);
			if (saleReturn is not null)
			{
				saleReturn.Status = false;
				await SaleReturnData.InsertSaleReturn(saleReturn);
				await ProductStockData.DeleteProductStockByTransactionNo(saleReturn.TransactionNo);
				var accounting = await AccountingData.LoadAccountingByTransactionNo(saleReturn.TransactionNo);
				if (accounting is not null)
				{
					accounting.Status = false;
					await AccountingData.InsertAccounting(accounting);
				}

				VibrationService.VibrateWithTime(200);
				NavigationManager.NavigateTo("/Reports/SaleReturn");
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error deleting sale return: {ex.Message}");
		}
	}

	private void CancelAction()
	{
		_showConfirmation = false;
		_currentAction = "";
	}
	#endregion
}

// Model for detailed sale return product display
public class DetailedSaleReturnProductModel
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