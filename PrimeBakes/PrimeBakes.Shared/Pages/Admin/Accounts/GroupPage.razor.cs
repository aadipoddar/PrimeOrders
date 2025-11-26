using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Accounts.Masters;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Admin.Accounts;

public partial class GroupPage
{
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

	private string _errorTitle = string.Empty;
	private string _errorMessage = string.Empty;

	private string _successTitle = string.Empty;
	private string _successMessage = string.Empty;

	private SfToast _sfSuccessToast;
	private SfToast _sfErrorToast;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Admin, true);
		await LoadData();
		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
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
				await ShowToast("Error", "Group not found.", "error");
				return;
			}

			group.Status = false;
			await GroupData.InsertGroup(group);

			await ShowToast("Success", $"Group '{group.Name}' has been deleted successfully.", "success");
			NavigationManager.NavigateTo(PageRouteNames.AdminGroup, true);
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"Failed to delete Group: {ex.Message}", "error");
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
				await ShowToast("Error", "Group not found.", "error");
				return;
			}

			group.Status = true;
			await GroupData.InsertGroup(group);

			await ShowToast("Success", $"Group '{group.Name}' has been recovered successfully.", "success");
			NavigationManager.NavigateTo(PageRouteNames.AdminGroup, true);
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"Failed to recover Group: {ex.Message}", "error");
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
			await ShowToast("Error", "Group name is required. Please enter a valid group name.", "error");
			return false;
		}

		if (string.IsNullOrWhiteSpace(_group.Remarks))
			_group.Remarks = null;

		if (_group.Id > 0)
		{
			var existingGroup = _groups.FirstOrDefault(_ => _.Id != _group.Id && _.Name.Equals(_group.Name, StringComparison.OrdinalIgnoreCase));
			if (existingGroup is not null)
			{
				await ShowToast("Error", $"Group name '{_group.Name}' already exists. Please choose a different name.", "error");
				return false;
			}
		}
		else
		{
			var existingGroup = _groups.FirstOrDefault(_ => _.Name.Equals(_group.Name, StringComparison.OrdinalIgnoreCase));
			if (existingGroup is not null)
			{
				await ShowToast("Error", $"Group name '{_group.Name}' already exists. Please choose a different name.", "error");
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

			if (!await ValidateForm())
			{
				_isProcessing = false;
				return;
			}

			await GroupData.InsertGroup(_group);

			await ShowToast("Success", $"Group '{_group.Name}' has been saved successfully.", "success");
			NavigationManager.NavigateTo(PageRouteNames.AdminGroup, true);
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"Failed to save Group: {ex.Message}", "error");
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

			// Call the Excel export utility
			var stream = await Task.Run(() => GroupExcelExport.ExportGroup(_groups));

			// Generate file name
			string fileName = "GROUP_MASTER.xlsx";

			// Save and view the Excel file
			await SaveAndViewService.SaveAndView(fileName, stream);

			await ShowToast("Success", "Group data exported to Excel successfully.", "success");
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"An error occurred while exporting to Excel: {ex.Message}", "error");
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

			// Call the PDF export utility
			var stream = await Task.Run(() => GroupPDFExport.ExportGroup(_groups));

			// Generate file name
			string fileName = "GROUP_MASTER.pdf";

			// Save and view the PDF file
			await SaveAndViewService.SaveAndView(fileName, stream);

			await ShowToast("Success", "Group data exported to PDF successfully.", "success");
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"An error occurred while exporting to PDF: {ex.Message}", "error");
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}
	#endregion

	#region Utilities
	private async Task ShowToast(string title, string message, string type)
	{
		VibrationService.VibrateWithTime(200);

		if (type == "error")
		{
			_errorTitle = title;
			_errorMessage = message;
			await _sfErrorToast.ShowAsync(new()
			{
				Title = _errorTitle,
				Content = _errorMessage
			});
		}

		else if (type == "success")
		{
			_successTitle = title;
			_successMessage = message;
			await _sfSuccessToast.ShowAsync(new()
			{
				Title = _successTitle,
				Content = _successMessage
			});
		}
	}
	#endregion
}
