using PrimeBakes.Shared.Components;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Accounts.Masters;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Admin.Accounts;

public partial class GroupPage : IAsyncDisposable
{
	private HotKeysContext _hotKeysContext;
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDeleted = false;

	private GroupModel _group = new();

	private List<GroupModel> _groups = [];

	private SfGrid<GroupModel> _sfGrid;
	private SfDialog _deleteConfirmationDialog;
	private SfDialog _recoverConfirmationDialog;

	private int _deleteGroupId = 0;
	private string _deleteGroupName = string.Empty;
	private bool _isDeleteDialogVisible = false;

	private int _recoverGroupId = 0;
	private string _recoverGroupName = string.Empty;
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
			.Add(ModCode.Ctrl, Code.S, SaveGroup, "Save", Exclude.None)
			.Add(ModCode.Ctrl, Code.N, () => NavigationManager.NavigateTo(PageRouteNames.AdminGroup, true), "New", Exclude.None)
			.Add(ModCode.Ctrl, Code.E, ExportExcel, "Export Excel", Exclude.None)
			.Add(ModCode.Ctrl, Code.P, ExportPdf, "Export PDF", Exclude.None)
			.Add(ModCode.Ctrl, Code.D, () => NavigationManager.NavigateTo(PageRouteNames.Dashboard), "Dashboard", Exclude.None)
			.Add(ModCode.Ctrl, Code.B, () => NavigationManager.NavigateTo(PageRouteNames.AdminDashboard), "Back", Exclude.None)
			.Add(Code.Insert, EditSelectedItem, "Edit selected", Exclude.None)
			.Add(Code.Delete, DeleteSelectedItem, "Delete selected", Exclude.None);

		_groups = await CommonData.LoadTableData<GroupModel>(TableNames.Group);

		if (!_showDeleted)
			_groups = [.. _groups.Where(g => g.Status)];

		if (_sfGrid is not null)
			await _sfGrid.Refresh();
	}
	#endregion

	#region Actions
	private void OnEditGroup(GroupModel group)
	{
		_group = new()
		{
			Id = group.Id,
			Name = group.Name,
			Remarks = group.Remarks,
			Status = group.Status
		};

		StateHasChanged();
	}

	private void ShowDeleteConfirmation(int id, string name)
	{
		_deleteGroupId = id;
		_deleteGroupName = name;
		_isDeleteDialogVisible = true;
		StateHasChanged();
	}

	private void CancelDelete()
	{
		_deleteGroupId = 0;
		_deleteGroupName = string.Empty;
		_isDeleteDialogVisible = false;
		StateHasChanged();
	}

	private async Task ConfirmDelete()
	{
		try
		{
			_isProcessing = true;
			_isDeleteDialogVisible = false;

			var group = _groups.FirstOrDefault(g => g.Id == _deleteGroupId);
			if (group == null)
			{
				await _toastNotification.ShowAsync("Error", "Group not found.", ToastType.Error);
				return;
			}

			group.Status = false;
			await GroupData.InsertGroup(group);

			await _toastNotification.ShowAsync("Success", $"Group '{group.Name}' has been deleted successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.AdminGroup, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to delete Group: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			_deleteGroupId = 0;
			_deleteGroupName = string.Empty;
		}
	}

	private void ShowRecoverConfirmation(int id, string name)
	{
		_recoverGroupId = id;
		_recoverGroupName = name;
		_isRecoverDialogVisible = true;
		StateHasChanged();
	}

	private void CancelRecover()
	{
		_recoverGroupId = 0;
		_recoverGroupName = string.Empty;
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

			var group = _groups.FirstOrDefault(g => g.Id == _recoverGroupId);
			if (group == null)
			{
				await _toastNotification.ShowAsync("Error", "Group not found.", ToastType.Error);
				return;
			}

			group.Status = true;
			await GroupData.InsertGroup(group);

			await _toastNotification.ShowAsync("Success", $"Group '{group.Name}' has been recovered successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.AdminGroup, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to recover Group: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			_recoverGroupId = 0;
			_recoverGroupName = string.Empty;
		}
	}
	#endregion

	#region Saving
	private async Task<bool> ValidateForm()
	{
		_group.Name = _group.Name?.Trim() ?? "";
		_group.Name = _group.Name?.ToUpper() ?? "";

		_group.Remarks = _group.Remarks?.Trim() ?? "";
		_group.Status = true;

		if (string.IsNullOrWhiteSpace(_group.Name))
		{
			await _toastNotification.ShowAsync("Error", "Group name is required. Please enter a valid group name.", ToastType.Error);
			return false;
		}

		if (string.IsNullOrWhiteSpace(_group.Remarks))
			_group.Remarks = null;

		if (_group.Id > 0)
		{
			var existingGroup = _groups.FirstOrDefault(_ => _.Id != _group.Id && _.Name.Equals(_group.Name, StringComparison.OrdinalIgnoreCase));
			if (existingGroup is not null)
			{
				await _toastNotification.ShowAsync("Error", $"Group name '{_group.Name}' already exists. Please choose a different name.", ToastType.Error);
				return false;
			}
		}
		else
		{
			var existingGroup = _groups.FirstOrDefault(_ => _.Name.Equals(_group.Name, StringComparison.OrdinalIgnoreCase));
			if (existingGroup is not null)
			{
				await _toastNotification.ShowAsync("Error", $"Group name '{_group.Name}' already exists. Please choose a different name.", ToastType.Error);
				return false;
			}
		}

		return true;
	}

	private async Task SaveGroup()
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

			await _toastNotification.ShowAsync("Processing Transaction", "Please wait while the transaction is being saved...", ToastType.Info);

			await GroupData.InsertGroup(_group);

			await _toastNotification.ShowAsync("Success", $"Group '{_group.Name}' has been saved successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.AdminGroup, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to save Group: {ex.Message}", ToastType.Error);
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
			await _toastNotification.ShowAsync("Processing", "Exporting to Excel...", ToastType.Info);

			// Call the Excel export utility
			var stream = await GroupExcelExport.ExportGroup(_groups);

			// Generate file name
			string fileName = "GROUP_MASTER.xlsx";

			// Save and view the Excel file
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Success", "Group data exported to Excel successfully.", ToastType.Success);
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
			await _toastNotification.ShowAsync("Processing", "Exporting to PDF...", ToastType.Info);

			// Call the PDF export utility
			var stream = await GroupPDFExport.ExportGroup(_groups);

			// Generate file name
			string fileName = "GROUP_MASTER.pdf";

			// Save and view the PDF file
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Success", "Group data exported to PDF successfully.", ToastType.Success);
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
			OnEditGroup(selectedRecords[0]);
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
