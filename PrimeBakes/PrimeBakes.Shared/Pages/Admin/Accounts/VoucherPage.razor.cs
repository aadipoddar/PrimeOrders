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

public partial class VoucherPage
{
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDeleted = false;

	private VoucherModel _voucher = new();

	private List<VoucherModel> _vouchers = [];

	private SfGrid<VoucherModel> _sfGrid;
	private SfDialog _deleteConfirmationDialog;
	private SfDialog _recoverConfirmationDialog;

	private int _deleteVoucherId = 0;
	private string _deleteVoucherName = string.Empty;
	private bool _isDeleteDialogVisible = false;

	private int _recoverVoucherId = 0;
	private string _recoverVoucherName = string.Empty;
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

		await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Admin, true);
		await LoadData();
		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		_vouchers = await CommonData.LoadTableData<VoucherModel>(TableNames.Voucher);

		if (!_showDeleted)
			_vouchers = [.. _vouchers.Where(v => v.Status)];

		if (_sfGrid is not null)
			await _sfGrid.Refresh();
	}
	#endregion

	#region Actions
	private void OnEditVoucher(VoucherModel voucher)
	{
		_voucher = new()
		{
			Id = voucher.Id,
			Name = voucher.Name,
			PrefixCode = voucher.PrefixCode,
			Remarks = voucher.Remarks,
			Status = voucher.Status
		};

		StateHasChanged();
	}

	private void ShowDeleteConfirmation(int id, string name)
	{
		_deleteVoucherId = id;
		_deleteVoucherName = name;
		_isDeleteDialogVisible = true;
		StateHasChanged();
	}

	private void CancelDelete()
	{
		_deleteVoucherId = 0;
		_deleteVoucherName = string.Empty;
		_isDeleteDialogVisible = false;
		StateHasChanged();
	}

	private async Task ConfirmDelete()
	{
		try
		{
			_isProcessing = true;
			_isDeleteDialogVisible = false;

			var voucher = _vouchers.FirstOrDefault(v => v.Id == _deleteVoucherId);
			if (voucher == null)
			{
				await ShowToast("Error", "Voucher not found.", "error");
				return;
			}

			voucher.Status = false;
			await VoucherData.InsertVoucher(voucher);

			await ShowToast("Success", $"Voucher '{voucher.Name}' has been deleted successfully.", "success");
			NavigationManager.NavigateTo(PageRouteNames.AdminVoucher, true);
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"Failed to delete Voucher: {ex.Message}", "error");
		}
		finally
		{
			_isProcessing = false;
			_deleteVoucherId = 0;
			_deleteVoucherName = string.Empty;
		}
	}

	private void ShowRecoverConfirmation(int id, string name)
	{
		_recoverVoucherId = id;
		_recoverVoucherName = name;
		_isRecoverDialogVisible = true;
		StateHasChanged();
	}

	private void CancelRecover()
	{
		_recoverVoucherId = 0;
		_recoverVoucherName = string.Empty;
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

			var voucher = _vouchers.FirstOrDefault(v => v.Id == _recoverVoucherId);
			if (voucher == null)
			{
				await ShowToast("Error", "Voucher not found.", "error");
				return;
			}

			voucher.Status = true;
			await VoucherData.InsertVoucher(voucher);

			await ShowToast("Success", $"Voucher '{voucher.Name}' has been recovered successfully.", "success");
			NavigationManager.NavigateTo(PageRouteNames.AdminVoucher, true);
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"Failed to recover Voucher: {ex.Message}", "error");
		}
		finally
		{
			_isProcessing = false;
			_recoverVoucherId = 0;
			_recoverVoucherName = string.Empty;
		}
	}
	#endregion

	#region Saving
	private async Task<bool> ValidateForm()
	{
		_voucher.Name = _voucher.Name?.Trim() ?? "";
		_voucher.Name = _voucher.Name?.ToUpper() ?? "";

		_voucher.PrefixCode = _voucher.PrefixCode?.Trim() ?? "";
		_voucher.PrefixCode = _voucher.PrefixCode?.ToUpper() ?? "";

		_voucher.Remarks = _voucher.Remarks?.Trim() ?? "";
		_voucher.Status = true;

		if (string.IsNullOrWhiteSpace(_voucher.Name))
		{
			await ShowToast("Error", "Voucher name is required. Please enter a valid voucher name.", "error");
			return false;
		}

		if (string.IsNullOrWhiteSpace(_voucher.PrefixCode))
		{
			await ShowToast("Error", "Prefix code is required. Please enter a valid prefix code.", "error");
			return false;
		}

		if (string.IsNullOrWhiteSpace(_voucher.Remarks))
			_voucher.Remarks = null;

		if (_voucher.Id > 0)
		{
			var existingVoucher = _vouchers.FirstOrDefault(_ => _.Id != _voucher.Id && _.Name.Equals(_voucher.Name, StringComparison.OrdinalIgnoreCase));
			if (existingVoucher is not null)
			{
				await ShowToast("Error", $"Voucher name '{_voucher.Name}' already exists. Please choose a different name.", "error");
				return false;
			}

			var existingPrefixCode = _vouchers.FirstOrDefault(_ => _.Id != _voucher.Id && _.PrefixCode.Equals(_voucher.PrefixCode, StringComparison.OrdinalIgnoreCase));
			if (existingPrefixCode is not null)
			{
				await ShowToast("Error", $"Prefix code '{_voucher.PrefixCode}' already exists. Please choose a different prefix code.", "error");
				return false;
			}
		}
		else
		{
			var existingVoucher = _vouchers.FirstOrDefault(_ => _.Name.Equals(_voucher.Name, StringComparison.OrdinalIgnoreCase));
			if (existingVoucher is not null)
			{
				await ShowToast("Error", $"Voucher name '{_voucher.Name}' already exists. Please choose a different name.", "error");
				return false;
			}

			var existingPrefixCode = _vouchers.FirstOrDefault(_ => _.PrefixCode.Equals(_voucher.PrefixCode, StringComparison.OrdinalIgnoreCase));
			if (existingPrefixCode is not null)
			{
				await ShowToast("Error", $"Prefix code '{_voucher.PrefixCode}' already exists. Please choose a different prefix code.", "error");
				return false;
			}
		}

		return true;
	}

	private async Task SaveVoucher()
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

			await VoucherData.InsertVoucher(_voucher);

			await ShowToast("Success", $"Voucher '{_voucher.Name}' has been saved successfully.", "success");
			NavigationManager.NavigateTo(PageRouteNames.AdminVoucher, true);
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"Failed to save Voucher: {ex.Message}", "error");
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
			var stream = await VoucherExcelExport.ExportVoucher(_vouchers);

			// Generate file name
			string fileName = "VOUCHER_MASTER.xlsx";

			// Save and view the Excel file
			await SaveAndViewService.SaveAndView(fileName, stream);

			await ShowToast("Success", "Voucher data exported to Excel successfully.", "success");
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
			var stream = await VoucherPDFExport.ExportVoucher(_vouchers);

			// Generate file name
			string fileName = "VOUCHER_MASTER.pdf";

			// Save and view the PDF file
			await SaveAndViewService.SaveAndView(fileName, stream);

			await ShowToast("Success", "Voucher data exported to PDF successfully.", "success");
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