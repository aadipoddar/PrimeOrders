using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Accounts.Masters;

using Syncfusion.Blazor.Grids;
using PrimeBakes.Shared.Components.Dialog;

namespace PrimeBakes.Shared.Pages.Admin.Accounts;

public partial class VoucherPage : IAsyncDisposable
{
	private HotKeysContext _hotKeysContext;
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDeleted = false;

	private VoucherModel _voucher = new();

	private List<VoucherModel> _vouchers = [];

	private SfGrid<VoucherModel> _sfGrid;
	private DeleteConfirmationDialog _deleteConfirmationDialog;
	private RecoverConfirmationDialog _recoverConfirmationDialog;

	private int _deleteVoucherId = 0;
	private string _deleteVoucherName = string.Empty;

	private int _recoverVoucherId = 0;
	private string _recoverVoucherName = string.Empty;

	ToastNotification _toastNotification;

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
			.Add(ModCode.Ctrl, Code.S, SaveVoucher, "Save", Exclude.None)
			.Add(ModCode.Ctrl, Code.N, () => NavigationManager.NavigateTo(PageRouteNames.AdminVoucher, true), "New", Exclude.None)
			.Add(ModCode.Ctrl, Code.E, ExportExcel, "Export Excel", Exclude.None)
			.Add(ModCode.Ctrl, Code.P, ExportPdf, "Export PDF", Exclude.None)
			.Add(ModCode.Ctrl, Code.D, () => NavigationManager.NavigateTo(PageRouteNames.Dashboard), "Dashboard", Exclude.None)
			.Add(ModCode.Ctrl, Code.B, () => NavigationManager.NavigateTo(PageRouteNames.AdminDashboard), "Back", Exclude.None)
			.Add(Code.Insert, EditSelectedItem, "Edit selected", Exclude.None)
			.Add(Code.Delete, DeleteSelectedItem, "Delete selected", Exclude.None);

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

	private async Task ShowDeleteConfirmation(int id, string name)
	{
		_deleteVoucherId = id;
		_deleteVoucherName = name;
		await _deleteConfirmationDialog.ShowAsync();
	}

	private async Task CancelDelete()
	{
		_deleteVoucherId = 0;
		_deleteVoucherName = string.Empty;
		await _deleteConfirmationDialog.HideAsync();
	}

	private async Task ConfirmDelete()
	{
		try
		{
			_isProcessing = true;
			await _deleteConfirmationDialog.HideAsync();

			var voucher = _vouchers.FirstOrDefault(v => v.Id == _deleteVoucherId);
			if (voucher == null)
			{
				await _toastNotification.ShowAsync("Error", "Voucher not found.", ToastType.Error);
				return;
			}

			voucher.Status = false;
			await VoucherData.InsertVoucher(voucher);

			await _toastNotification.ShowAsync("Success", $"Voucher '{voucher.Name}' has been deleted successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.AdminVoucher, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to delete Voucher: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			_deleteVoucherId = 0;
			_deleteVoucherName = string.Empty;
		}
	}

	private async Task ShowRecoverConfirmation(int id, string name)
	{
		_recoverVoucherId = id;
		_recoverVoucherName = name;
		await _recoverConfirmationDialog.ShowAsync();
	}

	private async Task CancelRecover()
	{
		_recoverVoucherId = 0;
		_recoverVoucherName = string.Empty;
		await _recoverConfirmationDialog.HideAsync();
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
			await _recoverConfirmationDialog.HideAsync();

			var voucher = _vouchers.FirstOrDefault(v => v.Id == _recoverVoucherId);
			if (voucher == null)
			{
				await _toastNotification.ShowAsync("Error", "Voucher not found.", ToastType.Error);
				return;
			}

			voucher.Status = true;
			await VoucherData.InsertVoucher(voucher);

			await _toastNotification.ShowAsync("Success", $"Voucher '{voucher.Name}' has been recovered successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.AdminVoucher, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to recover Voucher: {ex.Message}", ToastType.Error);
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
			await _toastNotification.ShowAsync("Error", "Voucher name is required. Please enter a valid voucher name.", ToastType.Error);
			return false;
		}

		if (string.IsNullOrWhiteSpace(_voucher.PrefixCode))
		{
			await _toastNotification.ShowAsync("Error", "Prefix code is required. Please enter a valid prefix code.", ToastType.Error);
			return false;
		}

		if (string.IsNullOrWhiteSpace(_voucher.Remarks))
			_voucher.Remarks = null;

		if (_voucher.Id > 0)
		{
			var existingVoucher = _vouchers.FirstOrDefault(_ => _.Id != _voucher.Id && _.Name.Equals(_voucher.Name, StringComparison.OrdinalIgnoreCase));
			if (existingVoucher is not null)
			{
				await _toastNotification.ShowAsync("Error", $"Voucher name '{_voucher.Name}' already exists. Please choose a different name.", ToastType.Error);
				return false;
			}

			var existingPrefixCode = _vouchers.FirstOrDefault(_ => _.Id != _voucher.Id && _.PrefixCode.Equals(_voucher.PrefixCode, StringComparison.OrdinalIgnoreCase));
			if (existingPrefixCode is not null)
			{
				await _toastNotification.ShowAsync("Error", $"Prefix code '{_voucher.PrefixCode}' already exists. Please choose a different prefix code.", ToastType.Error);
				return false;
			}
		}
		else
		{
			var existingVoucher = _vouchers.FirstOrDefault(_ => _.Name.Equals(_voucher.Name, StringComparison.OrdinalIgnoreCase));
			if (existingVoucher is not null)
			{
				await _toastNotification.ShowAsync("Error", $"Voucher name '{_voucher.Name}' already exists. Please choose a different name.", ToastType.Error);
				return false;
			}

			var existingPrefixCode = _vouchers.FirstOrDefault(_ => _.PrefixCode.Equals(_voucher.PrefixCode, StringComparison.OrdinalIgnoreCase));
			if (existingPrefixCode is not null)
			{
				await _toastNotification.ShowAsync("Error", $"Prefix code '{_voucher.PrefixCode}' already exists. Please choose a different prefix code.", ToastType.Error);
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
			StateHasChanged();

			if (!await ValidateForm())
			{
				_isProcessing = false;
				return;
			}

			await _toastNotification.ShowAsync("Processing Transaction", "Please wait while the transaction is being saved...", ToastType.Info);

			await VoucherData.InsertVoucher(_voucher);

			await _toastNotification.ShowAsync("Success", $"Voucher '{_voucher.Name}' has been saved successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.AdminVoucher, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to save Voucher: {ex.Message}", ToastType.Error);
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
			var stream = await VoucherExcelExport.ExportVoucher(_vouchers);

			// Generate file name
			string fileName = "VOUCHER_MASTER.xlsx";

			// Save and view the Excel file
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Success", "Voucher data exported to Excel successfully.", ToastType.Success);
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
			var stream = await VoucherPDFExport.ExportVoucher(_vouchers);

			// Generate file name
			string fileName = "VOUCHER_MASTER.pdf";

			// Save and view the PDF file
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Success", "Voucher data exported to PDF successfully.", ToastType.Success);
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
			OnEditVoucher(selectedRecords[0]);
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