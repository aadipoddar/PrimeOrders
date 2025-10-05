using Microsoft.AspNetCore.Components;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Kitchen;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Kitchen;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Inventory.Kitchen;

using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Reports.Inventory.Kitchen;

public partial class KitchenIssueViewPage
{
	[Parameter] public int KitchenIssueId { get; set; }

	private UserModel _user;

	private KitchenIssueOverviewModel _kitchenIssueOverview;
	private readonly List<DetailedKitchenIssueMaterialModel> _detailedKitchenIssueMaterials = [];
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

		await LoadKitchenIssueDetails();
		_isLoading = false;
	}

	private async Task LoadKitchenIssueDetails()
	{
		_kitchenIssueOverview = await KitchenIssueData.LoadKitchenIssueOverviewByKitchenIssueId(KitchenIssueId);

		// Check location access based on user permissions
		// Only admin (LocationId = 1) can view all issues
		if (_user.LocationId != 1)
		{
			// Load the actual kitchen issue to check LocationId
			var kitchenIssueModel = await CommonData.LoadTableDataById<KitchenIssueModel>(TableNames.KitchenIssue, KitchenIssueId);
			if (kitchenIssueModel?.LocationId != _user.LocationId)
			{
				NavigationManager.NavigateTo("/Reports/Kitchen-Issue");
				return;
			}
		}

		if (_kitchenIssueOverview is not null)
		{
			// Load detailed kitchen issue materials
			var issueDetails = await KitchenIssueData.LoadKitchenIssueDetailByKitchenIssue(KitchenIssueId);
			var rawMaterials = await CommonData.LoadTableDataByStatus<RawMaterialModel>(TableNames.RawMaterial);

			// Load material information for each issue detail
			foreach (var detail in issueDetails.Where(d => d.Status))
			{
				var rawMaterial = rawMaterials.FirstOrDefault(rm => rm.Id == detail.RawMaterialId);
				if (rawMaterial is not null)
				{
					// Load category information
					var category = await CommonData.LoadTableDataById<RawMaterialCategoryModel>(TableNames.RawMaterialCategory, rawMaterial.RawMaterialCategoryId);

					_detailedKitchenIssueMaterials.Add(new DetailedKitchenIssueMaterialModel
					{
						MaterialCode = rawMaterial.Code ?? "N/A",
						MaterialName = rawMaterial.Name,
						CategoryName = category?.Name ?? "N/A",
						Quantity = detail.Quantity,
						Unit = detail.MeasurementUnit ?? rawMaterial.MeasurementUnit ?? "Unit",
						Rate = detail.Rate,
						Total = detail.Total
					});
				}
			}
		}

		StateHasChanged();
	}

	#region Actions
	private async Task EditKitchenIssue()
	{
		if (_kitchenIssueOverview is null || _isProcessing || _user.LocationId != 1 || !_user.Admin)
			return;

		await DataStorageService.LocalRemove(StorageFileNames.KitchenIssueDataFileName);
		await DataStorageService.LocalRemove(StorageFileNames.KitchenIssueCartDataFileName);

		var kitchenIssue = await CommonData.LoadTableDataById<KitchenIssueModel>(TableNames.KitchenIssue, _kitchenIssueOverview.KitchenIssueId);
		var kitchenIssueDetails = await KitchenIssueData.LoadKitchenIssueDetailByKitchenIssue(_kitchenIssueOverview.KitchenIssueId);

		await DataStorageService.LocalSaveAsync(StorageFileNames.KitchenIssueDataFileName, System.Text.Json.JsonSerializer.Serialize(kitchenIssue));
		await DataStorageService.LocalSaveAsync(StorageFileNames.KitchenIssueCartDataFileName, System.Text.Json.JsonSerializer.Serialize(kitchenIssueDetails));

		NavigationManager.NavigateTo("/Inventory/Kitchen-Issue");
	}

	private void DeleteKitchenIssue()
	{
		if (_kitchenIssueOverview is null || _isProcessing || _user.LocationId != 1 || !_user.Admin)
			return;

		_showDeleteConfirm = true;
		StateHasChanged();
	}

	private async Task ConfirmDeleteKitchenIssue()
	{
		_showDeleteConfirm = false;
		_isProcessing = true;
		StateHasChanged();

		var kitchenIssue = await CommonData.LoadTableDataById<KitchenIssueModel>(TableNames.KitchenIssue, _kitchenIssueOverview.KitchenIssueId);
		if (kitchenIssue is null)
		{
			NavigationManager.NavigateTo("/Reports/Kitchen-Issue");
			return;
		}

		kitchenIssue.Status = false;
		await KitchenIssueData.InsertKitchenIssue(kitchenIssue);
		await RawMaterialStockData.DeleteRawMaterialStockByTransactionNo(kitchenIssue.TransactionNo);
		VibrationService.VibrateWithTime(500);
		NavigationManager.NavigateTo("/Reports/Kitchen-Issue");

		_isProcessing = false;
		StateHasChanged();
	}

	private void CancelDeleteKitchenIssue()
	{
		_showDeleteConfirm = false;
		StateHasChanged();
	}
	#endregion

	private async Task PrintPDF()
	{
		if (_kitchenIssueOverview == null || _isProcessing)
			return;

		_isProcessing = true;
		StateHasChanged();

		var memoryStream = await KitchenIssueA4Print.GenerateA4KitchenIssueBill(_kitchenIssueOverview.KitchenIssueId);
		var fileName = $"Kitchen_Issue_{_kitchenIssueOverview.TransactionNo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
		await SaveAndViewService.SaveAndView(fileName, "application/pdf", memoryStream);

		_isProcessing = false;
		StateHasChanged();
	}

	// Model class for detailed kitchen issue materials
	public class DetailedKitchenIssueMaterialModel
	{
		public string MaterialCode { get; set; } = string.Empty;
		public string MaterialName { get; set; } = string.Empty;
		public string CategoryName { get; set; } = string.Empty;
		public decimal Quantity { get; set; }
		public string Unit { get; set; } = string.Empty;
		public decimal Rate { get; set; }
		public decimal Total { get; set; }
	}
}