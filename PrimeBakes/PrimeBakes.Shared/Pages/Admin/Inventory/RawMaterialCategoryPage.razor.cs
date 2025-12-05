using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Inventory.RawMaterial;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Admin.Inventory;

public partial class RawMaterialCategoryPage : IAsyncDisposable
{
	private HotKeysContext _hotKeysContext;
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDeleted = false;

	private RawMaterialCategoryModel _rawMaterialCategory = new();

	private List<RawMaterialCategoryModel> _rawMaterialCategories = [];

	private SfGrid<RawMaterialCategoryModel> _sfGrid;
	private SfDialog _deleteConfirmationDialog;
	private SfDialog _recoverConfirmationDialog;

	private int _deleteRawMaterialCategoryId = 0;
	private string _deleteRawMaterialCategoryName = string.Empty;
	private bool _isDeleteDialogVisible = false;

	private int _recoverRawMaterialCategoryId = 0;
	private string _recoverRawMaterialCategoryName = string.Empty;
	private bool _isRecoverDialogVisible = false;

	private ToastNotification _toastNotification;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Admin, true);
		await LoadData();
		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		_hotKeysContext = HotKeys.CreateContext()
			.Add(ModCode.Ctrl, Code.S, SaveRawMaterialCategory, "Save", Exclude.None)
			.Add(ModCode.Ctrl, Code.N, () => NavigationManager.NavigateTo(PageRouteNames.AdminRawMaterialCategory, true), "New", Exclude.None)
			.Add(ModCode.Ctrl, Code.E, ExportExcel, "Export Excel", Exclude.None)
			.Add(ModCode.Ctrl, Code.P, ExportPdf, "Export PDF", Exclude.None)
			.Add(ModCode.Ctrl, Code.D, () => NavigationManager.NavigateTo(PageRouteNames.Dashboard), "Dashboard", Exclude.None)
			.Add(ModCode.Ctrl, Code.B, () => NavigationManager.NavigateTo(PageRouteNames.AdminDashboard), "Back", Exclude.None)
			.Add(Code.Insert, EditSelectedItem, "Edit selected", Exclude.None)
			.Add(Code.Delete, DeleteSelectedItem, "Delete selected", Exclude.None);

		_rawMaterialCategories = await CommonData.LoadTableData<RawMaterialCategoryModel>(TableNames.RawMaterialCategory);

		if (!_showDeleted)
			_rawMaterialCategories = [.. _rawMaterialCategories.Where(l => l.Status)];

		if (_sfGrid is not null)
			await _sfGrid.Refresh();
	}
	#endregion

	#region Actions
	private void OnEditRawMaterialCategory(RawMaterialCategoryModel rawMaterialCategory)
	{
		_rawMaterialCategory = new()
		{
			Id = rawMaterialCategory.Id,
			Name = rawMaterialCategory.Name,
			Remarks = rawMaterialCategory.Remarks,
			Status = rawMaterialCategory.Status
		};

		StateHasChanged();
	}

	private void ShowDeleteConfirmation(int id, string name)
	{
		_deleteRawMaterialCategoryId = id;
		_deleteRawMaterialCategoryName = name;
		_isDeleteDialogVisible = true;
		StateHasChanged();
	}

	private void CancelDelete()
	{
		_deleteRawMaterialCategoryId = 0;
		_deleteRawMaterialCategoryName = string.Empty;
		_isDeleteDialogVisible = false;
		StateHasChanged();
	}

	private async Task ConfirmDelete()
	{
		try
		{
			_isProcessing = true;
			_isDeleteDialogVisible = false;

			var rawMaterialCategory = _rawMaterialCategories.FirstOrDefault(l => l.Id == _deleteRawMaterialCategoryId);
			if (rawMaterialCategory == null)
			{
				await _toastNotification.ShowAsync("Error", "Raw Material Category not found.", ToastType.Error);
				return;
			}

			rawMaterialCategory.Status = false;
			await RawMaterialData.InsertRawMaterialCategory(rawMaterialCategory);

			await _toastNotification.ShowAsync("Deleted", $"Raw Material Category '{rawMaterialCategory.Name}' has been deleted successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.AdminRawMaterialCategory, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to delete Raw Material Category: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			_deleteRawMaterialCategoryId = 0;
			_deleteRawMaterialCategoryName = string.Empty;
		}
	}

	private void ShowRecoverConfirmation(int id, string name)
	{
		_recoverRawMaterialCategoryId = id;
		_recoverRawMaterialCategoryName = name;
		_isRecoverDialogVisible = true;
		StateHasChanged();
	}

	private void CancelRecover()
	{
		_recoverRawMaterialCategoryId = 0;
		_recoverRawMaterialCategoryName = string.Empty;
		_isRecoverDialogVisible = false;
		StateHasChanged();
	}

	private async Task ToggleDeleted()
	{
		_showDeleted = !_showDeleted;
		await LoadData();
		StateHasChanged();
	}

	private async Task ConfirmRecover()
	{
		try
		{
			_isProcessing = true;
			_isRecoverDialogVisible = false;

			var rawMaterialCategory = _rawMaterialCategories.FirstOrDefault(l => l.Id == _recoverRawMaterialCategoryId);
			if (rawMaterialCategory == null)
			{
				await _toastNotification.ShowAsync("Error", "Raw Material Category not found.", ToastType.Error);
				return;
			}

			rawMaterialCategory.Status = true;
			await RawMaterialData.InsertRawMaterialCategory(rawMaterialCategory);

			await _toastNotification.ShowAsync("Recovered", $"Raw Material Category '{rawMaterialCategory.Name}' has been recovered successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.AdminRawMaterialCategory, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to recover Raw Material Category: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			_recoverRawMaterialCategoryId = 0;
			_recoverRawMaterialCategoryName = string.Empty;
		}
	}
	#endregion

	#region Saving
	private async Task<bool> ValidateForm()
	{
		_rawMaterialCategory.Name = _rawMaterialCategory.Name?.Trim() ?? "";
		_rawMaterialCategory.Name = _rawMaterialCategory.Name?.ToUpper() ?? "";

		_rawMaterialCategory.Remarks = _rawMaterialCategory.Remarks?.Trim() ?? "";
		_rawMaterialCategory.Status = true;

		if (string.IsNullOrWhiteSpace(_rawMaterialCategory.Name))
		{
			await _toastNotification.ShowAsync("Validation", "Category name is required. Please enter a valid name.", ToastType.Warning);
			return false;
		}

		if (string.IsNullOrWhiteSpace(_rawMaterialCategory.Remarks))
			_rawMaterialCategory.Remarks = null;

		if (_rawMaterialCategory.Id > 0)
		{
			var existingRawMaterialCategory = _rawMaterialCategories.FirstOrDefault(_ => _.Id != _rawMaterialCategory.Id && _.Name.Equals(_rawMaterialCategory.Name, StringComparison.OrdinalIgnoreCase));
			if (existingRawMaterialCategory is not null)
			{
				await _toastNotification.ShowAsync("Duplicate", $"Raw Material Category name '{_rawMaterialCategory.Name}' already exists. Please choose a different name.", ToastType.Warning);
				return false;
			}
		}
		else
		{
			var existingRawMaterialCategory = _rawMaterialCategories.FirstOrDefault(_ => _.Name.Equals(_rawMaterialCategory.Name, StringComparison.OrdinalIgnoreCase));
			if (existingRawMaterialCategory is not null)
			{
				await _toastNotification.ShowAsync("Duplicate", $"Raw Material Category name '{_rawMaterialCategory.Name}' already exists. Please choose a different name.", ToastType.Warning);
				return false;
			}
		}

		return true;
	}

	private async Task SaveRawMaterialCategory()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			if (!await ValidateForm())
			{
				_isProcessing = false;
				return;
			}

			await _toastNotification.ShowAsync("Processing", "Please wait while the category is being saved...", ToastType.Info);

			await RawMaterialData.InsertRawMaterialCategory(_rawMaterialCategory);

			await _toastNotification.ShowAsync("Saved", $"Raw Material Category '{_rawMaterialCategory.Name}' has been saved successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.AdminRawMaterialCategory, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to save Raw Material Category: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
		}
	}
	#endregion

	#region Exporting
	private async Task ExportExcel()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Exporting", "Exporting to Excel...", ToastType.Info);

			// Call the Excel export utility
			var stream = await RawMaterialCategoryExcelExport.ExportRawMaterialCategory(_rawMaterialCategories);

			// Generate file name
			string fileName = "RAW_MATERIAL_CATEGORY_MASTER.xlsx";

			// Save and view the Excel file
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Exported", "Raw Material Category data exported to Excel successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"An error occurred while exporting to Excel: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task ExportPdf()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Exporting", "Exporting to PDF...", ToastType.Info);

			// Call the PDF export utility
			var stream = await RawMaterialCategoryPDFExport.ExportRawMaterialCategory(_rawMaterialCategories);

			// Generate file name
			string fileName = "RAW_MATERIAL_CATEGORY_MASTER.pdf";

			// Save and view the PDF file
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Exported", "Raw Material Category data exported to PDF successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"An error occurred while exporting to PDF: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}
	#endregion

	private async Task EditSelectedItem()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count > 0)
			OnEditRawMaterialCategory(selectedRecords[0]);
	}

	private async Task DeleteSelectedItem()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count > 0)
		{
			if (selectedRecords[0].Status)
				ShowDeleteConfirmation(selectedRecords[0].Id, selectedRecords[0].Name);
			else
				ShowRecoverConfirmation(selectedRecords[0].Id, selectedRecords[0].Name);
		}
	}

	public async ValueTask DisposeAsync()
	{
		await _hotKeysContext.DisposeAsync();
	}
}