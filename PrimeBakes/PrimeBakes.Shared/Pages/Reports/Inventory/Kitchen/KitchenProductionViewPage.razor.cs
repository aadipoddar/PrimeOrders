using Microsoft.AspNetCore.Components;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Kitchen;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Kitchen;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Kitchen;
using PrimeBakesLibrary.Models.Product;

using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Reports.Inventory.Kitchen;

public partial class KitchenProductionViewPage
{
	[Parameter] public int ProductionId { get; set; }

	private UserModel _user;

	private KitchenProductionOverviewModel _kitchenProductionOverview;
	private readonly List<DetailedKitchenProductionProductModel> _detailedKitchenProductionProducts = [];
	private bool _isLoading = true;
	private bool _isProcessing = false;

	// Delete confirmation dialog
	private SfDialog _deleteConfirmDialog;
	private bool _showDeleteConfirm = false;

	protected override async Task OnInitializedAsync()
	{
		_isLoading = true;

		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Inventory, true);
		_user = authResult.User;

		await LoadKitchenProductionDetails();
		_isLoading = false;
	}

	private async Task LoadKitchenProductionDetails()
	{
		_kitchenProductionOverview = await KitchenProductionData.LoadKitchenProductionOverviewByKitchenProductionId(ProductionId);

		// Check location access based on user permissions
		// Only admin (LocationId = 1) can view all productions
		if (_user.LocationId != 1)
		{
			// Load the actual kitchen production to check LocationId
			var kitchenProductionModel = await CommonData.LoadTableDataById<KitchenProductionModel>(TableNames.KitchenProduction, ProductionId);
			if (kitchenProductionModel?.LocationId != _user.LocationId)
			{
				NavigationManager.NavigateTo("/Reports/Kitchen-Production");
				return;
			}
		}

		if (_kitchenProductionOverview is not null)
		{
			// Load detailed kitchen production products
			var productionDetails = await KitchenProductionData.LoadKitchenProductionDetailByKitchenProduction(ProductionId);
			var products = await CommonData.LoadTableDataByStatus<ProductModel>(TableNames.Product);

			// Load product information for each production detail
			foreach (var detail in productionDetails.Where(d => d.Status))
			{
				var product = products.FirstOrDefault(p => p.Id == detail.ProductId);
				if (product is not null)
				{
					// Load category information
					var category = await CommonData.LoadTableDataById<ProductCategoryModel>(TableNames.ProductCategory, product.ProductCategoryId);

					_detailedKitchenProductionProducts.Add(new()
					{
						ProductCode = product.Code ?? "N/A",
						ProductName = product.Name,
						CategoryName = category?.Name ?? "N/A",
						Quantity = detail.Quantity,
						Rate = detail.Rate,
						Total = detail.Total,
					});
				}
			}
		}

		StateHasChanged();
	}

	#region Actions
	private async Task EditKitchenProduction()
	{
		if (_kitchenProductionOverview is null || _isProcessing || _user.LocationId != 1 || !_user.Admin)
			return;

		await DataStorageService.LocalRemove(StorageFileNames.KitchenProductionDataFileName);
		await DataStorageService.LocalRemove(StorageFileNames.KitchenProductionCartDataFileName);

		var kitchenProduction = await CommonData.LoadTableDataById<KitchenProductionModel>(TableNames.KitchenProduction, _kitchenProductionOverview.KitchenProductionId);
		var kitchenProductionDetails = await KitchenProductionData.LoadKitchenProductionDetailByKitchenProduction(_kitchenProductionOverview.KitchenProductionId);

		await DataStorageService.LocalSaveAsync(StorageFileNames.KitchenProductionDataFileName, System.Text.Json.JsonSerializer.Serialize(kitchenProduction));
		await DataStorageService.LocalSaveAsync(StorageFileNames.KitchenProductionCartDataFileName, System.Text.Json.JsonSerializer.Serialize(kitchenProductionDetails));

		NavigationManager.NavigateTo("/Inventory/Kitchen-Production");
	}

	private void DeleteKitchenProduction()
	{
		if (_kitchenProductionOverview is null || _isProcessing || _user.LocationId != 1 || !_user.Admin)
			return;

		_showDeleteConfirm = true;
		StateHasChanged();
	}

	private async Task ConfirmDeleteKitchenProduction()
	{
		_showDeleteConfirm = false;
		_isProcessing = true;
		StateHasChanged();

		var kitchenProduction = await CommonData.LoadTableDataById<KitchenProductionModel>(TableNames.KitchenProduction, _kitchenProductionOverview.KitchenProductionId);
		if (kitchenProduction is null)
		{
			NavigationManager.NavigateTo("/Reports/Kitchen-Production");
			return;
		}

		kitchenProduction.Status = false;
		await KitchenProductionData.InsertKitchenProduction(kitchenProduction);
		await ProductStockData.DeleteProductStockByTransactionNo(kitchenProduction.TransactionNo);

		VibrationService.VibrateWithTime(500);
		NavigationManager.NavigateTo("/Reports/Kitchen-Production");

		_isProcessing = false;
		StateHasChanged();
	}

	private void CancelDeleteKitchenProduction()
	{
		_showDeleteConfirm = false;
		StateHasChanged();
	}
	#endregion

	private async Task PrintPDF()
	{
		if (_kitchenProductionOverview == null || _isProcessing)
			return;

		_isProcessing = true;
		StateHasChanged();

		var memoryStream = await KitchenProductionA4Print.GenerateA4KitchenProductionBill(_kitchenProductionOverview.KitchenProductionId);
		var fileName = $"Kitchen_Production_{_kitchenProductionOverview.TransactionNo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
		await SaveAndViewService.SaveAndView(fileName, "application/pdf", memoryStream);

		_isProcessing = false;
		StateHasChanged();
	}

	// Model class for detailed kitchen production products
	public class DetailedKitchenProductionProductModel
	{
		public string ProductCode { get; set; } = string.Empty;
		public string ProductName { get; set; } = string.Empty;
		public string CategoryName { get; set; } = string.Empty;
		public decimal Quantity { get; set; }
		public string Unit { get; set; } = string.Empty;
		public decimal Rate { get; set; }
		public decimal Total { get; set; }
	}
}