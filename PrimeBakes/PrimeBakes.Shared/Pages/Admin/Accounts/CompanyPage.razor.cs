using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Accounts.Masters;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Admin.Accounts;

public partial class CompanyPage : IAsyncDisposable
{
	private HotKeysContext _hotKeysContext;
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDeleted = false;

	private CompanyModel _company = new();

	private List<CompanyModel> _companies = [];
	private List<StateUTModel> _stateUTs = [];

	private SfGrid<CompanyModel> _sfGrid;
	private SfDialog _deleteConfirmationDialog;
	private SfDialog _recoverConfirmationDialog;

	private int _deleteCompanyId = 0;
	private string _deleteCompanyName = string.Empty;
	private bool _isDeleteDialogVisible = false;

	private int _recoverCompanyId = 0;
	private string _recoverCompanyName = string.Empty;
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
			.Add(ModCode.Ctrl, Code.S, SaveCompany, "Save", Exclude.None)
			.Add(ModCode.Ctrl, Code.N, () => NavigationManager.NavigateTo(PageRouteNames.AdminCompany, true), "New", Exclude.None)
			.Add(ModCode.Ctrl, Code.E, ExportExcel, "Export Excel", Exclude.None)
			.Add(ModCode.Ctrl, Code.P, ExportPdf, "Export PDF", Exclude.None)
			.Add(ModCode.Ctrl, Code.D, () => NavigationManager.NavigateTo(PageRouteNames.Dashboard), "Dashboard", Exclude.None)
			.Add(ModCode.Ctrl, Code.B, () => NavigationManager.NavigateTo(PageRouteNames.AdminDashboard), "Back", Exclude.None)
			.Add(Code.Insert, EditSelectedItem, "Edit selected", Exclude.None)
			.Add(Code.Delete, DeleteSelectedItem, "Delete selected", Exclude.None);

		_companies = await CommonData.LoadTableData<CompanyModel>(TableNames.Company);
		_stateUTs = await CommonData.LoadTableData<StateUTModel>(TableNames.StateUT);

		if (!_showDeleted)
			_companies = [.. _companies.Where(c => c.Status)];

		if (_sfGrid is not null)
			await _sfGrid.Refresh();
	}
	#endregion

	#region Actions
	private void OnEditCompany(CompanyModel company)
	{
		_company = new()
		{
			Id = company.Id,
			Name = company.Name,
			Code = company.Code,
			StateUTId = company.StateUTId,
			GSTNo = company.GSTNo,
			PANNo = company.PANNo,
			CINNo = company.CINNo,
			Alias = company.Alias,
			Phone = company.Phone,
			Email = company.Email,
			Address = company.Address,
			Remarks = company.Remarks,
			Status = company.Status
		};

		StateHasChanged();
	}

	private void ShowDeleteConfirmation(int id, string name)
	{
		_deleteCompanyId = id;
		_deleteCompanyName = name;
		_isDeleteDialogVisible = true;
		StateHasChanged();
	}

	private void CancelDelete()
	{
		_deleteCompanyId = 0;
		_deleteCompanyName = string.Empty;
		_isDeleteDialogVisible = false;
		StateHasChanged();
	}

	private async Task ConfirmDelete()
	{
		try
		{
			_isProcessing = true;
			_isDeleteDialogVisible = false;

			var company = _companies.FirstOrDefault(c => c.Id == _deleteCompanyId);
			if (company == null)
			{
				await _toastNotification.ShowAsync("Error", "Company not found.", ToastType.Error);
				return;
			}

			company.Status = false;
			await CompanyData.InsertCompany(company);

			await _toastNotification.ShowAsync("Success", $"Company '{company.Name}' has been deleted successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.AdminCompany, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to delete Company: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			_deleteCompanyId = 0;
			_deleteCompanyName = string.Empty;
		}
	}

	private void ShowRecoverConfirmation(int id, string name)
	{
		_recoverCompanyId = id;
		_recoverCompanyName = name;
		_isRecoverDialogVisible = true;
		StateHasChanged();
	}

	private void CancelRecover()
	{
		_recoverCompanyId = 0;
		_recoverCompanyName = string.Empty;
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

			var company = _companies.FirstOrDefault(c => c.Id == _recoverCompanyId);
			if (company == null)
			{
				await _toastNotification.ShowAsync("Error", "Company not found.", ToastType.Error);
				return;
			}

			company.Status = true;
			await CompanyData.InsertCompany(company);

			await _toastNotification.ShowAsync("Success", $"Company '{company.Name}' has been recovered successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.AdminCompany, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to recover Company: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			_recoverCompanyId = 0;
			_recoverCompanyName = string.Empty;
		}
	}
	#endregion

	#region Saving
	private async Task<bool> ValidateForm()
	{
		_company.Name = _company.Name?.Trim() ?? "";
		_company.Name = _company.Name?.ToUpper() ?? "";

		_company.Code = _company.Code?.Trim() ?? "";
		_company.Code = _company.Code?.ToUpper() ?? "";

		_company.GSTNo = _company.GSTNo?.Trim() ?? "";
		_company.GSTNo = _company.GSTNo?.ToUpper() ?? "";

		_company.PANNo = _company.PANNo?.Trim() ?? "";
		_company.PANNo = _company.PANNo?.ToUpper() ?? "";

		_company.CINNo = _company.CINNo?.Trim() ?? "";
		_company.CINNo = _company.CINNo?.ToUpper() ?? "";

		_company.Alias = _company.Alias?.Trim() ?? "";
		_company.Alias = _company.Alias?.ToUpper() ?? "";

		_company.Phone = _company.Phone?.Trim() ?? "";
		_company.Email = _company.Email?.Trim() ?? "";
		_company.Address = _company.Address?.Trim() ?? "";

		_company.Remarks = _company.Remarks?.Trim() ?? "";
		_company.Status = true;

		if (string.IsNullOrWhiteSpace(_company.Name))
		{
			await _toastNotification.ShowAsync("Error", "Company name is required. Please enter a valid company name.", ToastType.Error);
			return false;
		}

		if (string.IsNullOrWhiteSpace(_company.Code))
		{
			await _toastNotification.ShowAsync("Error", "Code is required. Please enter a valid code.", ToastType.Error);
			return false;
		}

		if (_company.StateUTId <= 0)
		{
			await _toastNotification.ShowAsync("Error", "State/UT is required. Please select a valid State/UT.", ToastType.Error);
			return false;
		}

		if (string.IsNullOrWhiteSpace(_company.GSTNo)) _company.GSTNo = null;
		if (string.IsNullOrWhiteSpace(_company.PANNo)) _company.PANNo = null;
		if (string.IsNullOrWhiteSpace(_company.CINNo)) _company.CINNo = null;
		if (string.IsNullOrWhiteSpace(_company.Alias)) _company.Alias = null;
		if (string.IsNullOrWhiteSpace(_company.Phone)) _company.Phone = null;
		if (string.IsNullOrWhiteSpace(_company.Email)) _company.Email = null;
		if (string.IsNullOrWhiteSpace(_company.Address)) _company.Address = null;
		if (string.IsNullOrWhiteSpace(_company.Remarks)) _company.Remarks = null;

		if (_company.Id > 0)
		{
			var existingCompany = _companies.FirstOrDefault(_ => _.Id != _company.Id && _.Name.Equals(_company.Name, StringComparison.OrdinalIgnoreCase));
			if (existingCompany is not null)
			{
				await _toastNotification.ShowAsync("Error", $"Company name '{_company.Name}' already exists. Please choose a different name.", ToastType.Error);
				return false;
			}

			var existingCode = _companies.FirstOrDefault(_ => _.Id != _company.Id && _.Code.Equals(_company.Code, StringComparison.OrdinalIgnoreCase));
			if (existingCode is not null)
			{
				await _toastNotification.ShowAsync("Error", $"Company code '{_company.Code}' already exists. Please choose a different code.", ToastType.Error);
				return false;
			}
		}
		else
		{
			var existingCompany = _companies.FirstOrDefault(_ => _.Name.Equals(_company.Name, StringComparison.OrdinalIgnoreCase));
			if (existingCompany is not null)
			{
				await _toastNotification.ShowAsync("Error", $"Company name '{_company.Name}' already exists. Please choose a different name.", ToastType.Error);
				return false;
			}

			var existingCode = _companies.FirstOrDefault(_ => _.Code.Equals(_company.Code, StringComparison.OrdinalIgnoreCase));
			if (existingCode is not null)
			{
				await _toastNotification.ShowAsync("Error", $"Company code '{_company.Code}' already exists. Please choose a different code.", ToastType.Error);
				return false;
			}
		}

		return true;
	}

	private async Task SaveCompany()
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

			await CompanyData.InsertCompany(_company);

			await _toastNotification.ShowAsync("Success", $"Company '{_company.Name}' has been saved successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.AdminCompany, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to save Company: {ex.Message}", ToastType.Error);
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
			var stream = await CompanyExcelExport.ExportCompany(_companies);

			// Generate file name
			string fileName = "COMPANY_MASTER.xlsx";

			// Save and view the Excel file
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Success", "Company data exported to Excel successfully.", ToastType.Success);
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
			var stream = await CompanyPDFExport.ExportCompany(_companies);

			// Generate file name
			string fileName = "COMPANY_MASTER.pdf";

			// Save and view the PDF file
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Success", "Company data exported to PDF successfully.", ToastType.Success);
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
			OnEditCompany(selectedRecords[0]);
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