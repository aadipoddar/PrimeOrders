using Microsoft.JSInterop;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Accounting;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;

using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Reports.Accounts;

public partial class FinancialAccountingReportPage
{
	private UserModel _user;
	private bool _isLoading = true;
	private bool _showCharts = true;

	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now.AddMonths(-1));
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);
	private int _selectedVoucherId = 0;

	private List<AccountingOverviewModel> _accountingOverviews = [];
	private List<VoucherModel> _vouchers = [];
	private SfGrid<AccountingOverviewModel> _sfGrid;

	protected override async Task OnInitializedAsync()
	{
		_isLoading = true;
		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Accounts, true);
		_user = authResult.User;

		// Only admin users can access financial reports
		if (_user.LocationId != 1)
		{
			NavigationManager.NavigateTo("/Reports-Dashboard");
			return;
		}

		await LoadInitialData();
		await LoadData();
		_isLoading = false;
	}

	private async Task LoadInitialData()
	{
		try
		{
			_vouchers = await CommonData.LoadTableDataByStatus<VoucherModel>(TableNames.Voucher, true);

			// Add "All" option
			_vouchers.Insert(0, new VoucherModel { Id = 0, Name = "All Vouchers" });
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error loading initial data: {ex.Message}");
		}
	}

	private async Task LoadData()
	{
		try
		{
			await LoadAccountingData();
			StateHasChanged();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error loading accounting data: {ex.Message}");
		}
	}

	private async Task LoadAccountingData()
	{
		_accountingOverviews = await AccountingData.LoadAccountingDetailsByDate(_startDate.ToDateTime(TimeOnly.MinValue), _endDate.ToDateTime(TimeOnly.MaxValue));
	}

	#region Event Handlers
	private async Task OnDateRangeChanged(RangePickerEventArgs<DateOnly> args)
	{
		if (args.StartDate != default && args.EndDate != default)
		{
			_startDate = args.StartDate;
			_endDate = args.EndDate;
			await LoadData();
		}
	}

	private async Task OnVoucherChanged(ChangeEventArgs<int, VoucherModel> args)
	{
		_selectedVoucherId = args.Value;
		await LoadData();
	}

	private void ToggleCharts()
	{
		_showCharts = !_showCharts;
		StateHasChanged();
	}

	private async Task ExportToExcel()
	{
		try
		{
			var memoryStream = AccountingExcelExport.ExportAccountingOverviewExcel(
			_accountingOverviews, _startDate, _endDate, _selectedVoucherId, _vouchers);
			var fileName = $"Accounting_Report_{_startDate:yyyy-MM-dd}_to_{_endDate:yyyy-MM-dd}.xlsx";
			await SaveAndViewService.SaveAndView(fileName, memoryStream);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error exporting to Excel: {ex.Message}");
		}
	}

	private async Task ViewAccountingDetails(AccountingOverviewModel accounting)
	{
		// Navigate to Sale Return details
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", $"/Reports/Financial/Accounting/View/{accounting.AccountingId}", "_blank");
		else
			NavigationManager.NavigateTo($"/Reports/Financial/Accounting/View/{accounting.AccountingId}");
	}
	#endregion

	#region Summary Calculations
	private decimal GetTotalDebitAmount()
	{
		return _accountingOverviews.Sum(a => a.TotalDebitAmount);
	}

	private decimal GetTotalCreditAmount()
	{
		return _accountingOverviews.Sum(a => a.TotalCreditAmount);
	}

	private decimal GetTotalAmount()
	{
		return _accountingOverviews.Sum(a => a.TotalAmount);
	}

	private int GetTotalEntries()
	{
		return _accountingOverviews.Count;
	}

	private decimal GetNetBalance()
	{
		return GetTotalDebitAmount() - GetTotalCreditAmount();
	}

	private int GetTotalLedgers()
	{
		return _accountingOverviews.Sum(a => a.TotalLedgers);
	}
	#endregion

	#region Chart Data Methods
	private List<object> GetVoucherTypeChartData()
	{
		return _accountingOverviews
			.GroupBy(a => a.VoucherName)
			.Select(g => new
			{
				VoucherName = g.Key,
				EntryCount = g.Count()
			})
			.Cast<object>()
			.ToList();
	}

	private List<object> GetDebitCreditChartData()
	{
		return
		[
			new { Type = "Debit", Amount = GetTotalDebitAmount() },
			new { Type = "Credit", Amount = GetTotalCreditAmount() }
		];
	}

	private List<object> GetAmountComparisonChartData()
	{
		return
		[
			new { Category = "Total Debit", Amount = GetTotalDebitAmount() },
			new { Category = "Total Credit", Amount = GetTotalCreditAmount() },
			new { Category = "Net Balance", Amount = Math.Abs(GetNetBalance()) }
		];
	}

	private List<object> GetDailyTrendChartData()
	{
		return _accountingOverviews
			.GroupBy(a => a.AccountingDate.ToString("MMM dd"))
			.Select(g => new
			{
				Date = g.Key,
				DebitAmount = g.Sum(x => x.TotalDebitAmount),
				CreditAmount = g.Sum(x => x.TotalCreditAmount)
			})
			.OrderBy(x => x.Date)
			.Cast<object>()
			.ToList();
	}
	#endregion
}