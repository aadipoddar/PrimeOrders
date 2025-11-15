using Microsoft.AspNetCore.Components;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Accounting;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Reports.Accounts;

public partial class FinancialAccountingViewPage
{
	[Parameter] public int AccountingId { get; set; }

	private UserModel _user;
	private bool _isLoading = true;
	private bool _isProcessing = false;

	// Data models
	private AccountingOverviewModel _accountingOverview;
	private List<LedgerOverviewModel> _accountingDetails = [];

	// Grid reference
	private SfGrid<LedgerOverviewModel> _sfGrid;

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
		_isLoading = true;
		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Accounts, true);
		_user = authResult.User;

		await LoadAccountingData();
		_isLoading = false;
	}

	private async Task LoadAccountingData()
	{
		try
		{
			_accountingOverview = await AccountingData.LoadAccountingOverviewByAccountingId(AccountingId);

			if (_accountingOverview == null)
				return;

			// Load accounting details (ledger entries)
			await LoadAccountingDetails();

			StateHasChanged();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error loading accounting data: {ex.Message}");
		}
	}

	private async Task LoadAccountingDetails()
	{
		try
		{
			_accountingDetails.Clear();
			_accountingDetails = await AccountingData.LoadLedgerOverviewByAccountingId(AccountingId);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error loading accounting details: {ex.Message}");
		}
	}
	#endregion

	#region Accounting Actions
	private void EditAccounting() =>
		ShowConfirmation(
			"Edit Accounting",
			"Are you sure you want to edit this accounting record?",
			"This will redirect you to the accounting editing page where you can modify the record details. Any unsaved changes will be lost.",
			"Edit Accounting",
			"fas fa-edit",
			"edit"
		);

	private void DeleteAccounting() =>
		ShowConfirmation(
			"Delete Accounting",
			"Are you sure you want to delete this accounting record?",
			"This action cannot be undone. All accounting data, including ledger entries and transactions, will be permanently removed.",
			"Delete Accounting",
			"fas fa-trash-alt",
			"delete"
		);

	private void PrintAccounting() =>
		ShowConfirmation(
			"Print Accounting",
			"Are you sure you want to print this accounting record?",
			"This will generate a PDF document with all the accounting details and open it for printing or download.",
			"Print Accounting",
			"fas fa-print",
			"print"
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
		VibrationService.VibrateHapticClick();
		StateHasChanged();
	}

	private async Task ConfirmAction()
	{
		_isProcessing = true;
		VibrationService.VibrateWithTime(500);

		try
		{
			switch (_currentAction)
			{
				case "edit":
					await EditAccountingConfirmed();
					break;
				case "delete":
					await DeleteAccountingConfirmed();
					break;
				case "print":
					await PrintAccountingConfirmed();
					break;
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error executing action {_currentAction}: {ex.Message}");
		}
		finally
		{
			_isProcessing = false;
			_showConfirmation = false;
			StateHasChanged();
		}
	}

	private async Task EditAccountingConfirmed()
	{
		await DataStorageService.LocalRemove(StorageFileNames.FinancialAccountingDataFileName);
		await DataStorageService.LocalRemove(StorageFileNames.FinancialAccountingCartDataFileName);

		var accounting = await CommonData.LoadTableDataById<AccountingModel>(TableNames.Accounting, AccountingId);
		var accountingDetails = await AccountingData.LoadLedgerOverviewByAccountingId(accounting.Id);

		List<AccountingCartModel> cart = [];
		foreach (var item in accountingDetails)
		{
			cart.Add(new()
			{
				Id = item.LedgerId,
				GroupId = item.GroupId,
				AccountTypeId = item.AccountTypeId,
				Name = item.LedgerName,
				ReferenceId = item.ReferenceId,
				ReferenceNo = item.ReferenceNo,
				ReferenceType = item.ReferenceType,
				Remarks = item.Remarks,
				Debit = item.Debit,
				Credit = item.Credit
			});
		}

		await DataStorageService.LocalSaveAsync(StorageFileNames.FinancialAccountingDataFileName, System.Text.Json.JsonSerializer.Serialize(accounting));
		await DataStorageService.LocalSaveAsync(StorageFileNames.FinancialAccountingCartDataFileName, System.Text.Json.JsonSerializer.Serialize(cart));

		NavigationManager.NavigateTo("/Accounting");
	}

	private async Task DeleteAccountingConfirmed()
	{
		try
		{
			var accounting = await CommonData.LoadTableDataById<AccountingModel>(TableNames.Accounting, AccountingId);
			accounting.Status = false;
			await AccountingData.InsertAccounting(accounting);
			NavigationManager.NavigateTo("/Reports/Financial/Accounting");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error deleting accounting record: {ex.Message}");
		}
	}

	private async Task PrintAccountingConfirmed()
	{
		try
		{
			// Generate PDF for accounting record
			var memoryStream = await AccountingA4Print.GenerateA4AccountingVoucher(AccountingId);
			var fileName = $"Accounting_{_accountingOverview.TransactionNo}_{DateTime.Now:yyyy-MM-dd}.pdf";
			await SaveAndViewService.SaveAndView(fileName,	memoryStream);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error generating PDF: {ex.Message}");
		}
	}
	#endregion
}