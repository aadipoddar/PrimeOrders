using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Models.Accounts.Masters;

using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeOrders.Components.Pages.Accounts.Masters;

public partial class FinancialYearPage
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] public IJSRuntime JS { get; set; }

	private UserModel _user;
	private bool _isLoading = true;

	private FinancialYearModel _financialYearModel = new()
	{
		StartDate = DateOnly.FromDateTime(DateTime.Now),
		EndDate = DateOnly.FromDateTime(DateTime.Now.AddYears(1).AddDays(-1)),
		YearNo = 1,
		Remarks = "",
		Status = true
	};

	private List<FinancialYearModel> _financialYears = [];

	private SfGrid<FinancialYearModel> _sfGrid;
	private SfToast _sfToast;
	private SfToast _sfUpdateToast;
	private SfToast _sfErrorToast;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_isLoading = true;

		if (!((_user = (await AuthService.ValidateUser(JS, NavManager, UserRoles.Accounts, true)).User) is not null))
			return;

		await LoadData();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		_financialYears = await CommonData.LoadTableData<FinancialYearModel>(TableNames.FinancialYear);

		// Set default values for new financial year
		if (_financialYears.Count > 0)
		{
			var lastYear = _financialYears.OrderByDescending(fy => fy.YearNo).FirstOrDefault();
			if (lastYear is not null)
			{
				_financialYearModel.YearNo = lastYear.YearNo + 1;
				_financialYearModel.StartDate = lastYear.EndDate.AddDays(1);
				_financialYearModel.EndDate = lastYear.EndDate.AddYears(1);
			}
		}

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		StateHasChanged();
	}

	public async Task RowSelectHandler(RowSelectEventArgs<FinancialYearModel> args)
	{
		_financialYearModel = args.Data;
		await _sfUpdateToast.ShowAsync();
		StateHasChanged();
	}

	private void OnStartDateChanged(ChangedEventArgs<DateOnly> args)
	{
		_financialYearModel.StartDate = args.Value;
		_financialYearModel.EndDate = args.Value.AddYears(1).AddDays(-1);

		StateHasChanged();
	}

	private void OnEndDateChanged(ChangedEventArgs<DateOnly> args)
	{
		_financialYearModel.EndDate = args.Value;
		StateHasChanged();
	}

	private string GetFinancialYearDisplay()
	{
		if (_financialYearModel.StartDate != default && _financialYearModel.EndDate != default)
			return $"{_financialYearModel.StartDate:yyyy}-{_financialYearModel.EndDate:yy}";

		return "";
	}

	private async Task<bool> ValidateForm()
	{
		if (_financialYearModel.StartDate == default)
		{
			_sfErrorToast.Content = "Start date is required.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (_financialYearModel.EndDate == default)
		{
			_sfErrorToast.Content = "End date is required.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (_financialYearModel.StartDate >= _financialYearModel.EndDate)
		{
			_sfErrorToast.Content = "End date must be after start date.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (_financialYearModel.YearNo <= 0)
		{
			_sfErrorToast.Content = "Year number must be greater than 0.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		// Check for overlapping financial years
		var overlappingYear = _financialYears.FirstOrDefault(fy =>
			fy.Id != _financialYearModel.Id &&
			fy.Status &&
			((fy.StartDate <= _financialYearModel.StartDate && fy.EndDate >= _financialYearModel.StartDate) ||
			 (fy.StartDate <= _financialYearModel.EndDate && fy.EndDate >= _financialYearModel.EndDate) ||
			 (fy.StartDate >= _financialYearModel.StartDate && fy.EndDate <= _financialYearModel.EndDate)));

		if (overlappingYear != null)
		{
			_sfErrorToast.Content = $"Date range overlaps with existing financial year {overlappingYear.StartDate:yyyy}-{overlappingYear.EndDate:yy}.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		// Check for duplicate year numbers
		var duplicateYearNo = _financialYears.FirstOrDefault(fy =>
			fy.Id != _financialYearModel.Id &&
			fy.YearNo == _financialYearModel.YearNo);

		if (duplicateYearNo != null)
		{
			_sfErrorToast.Content = "A financial year with this year number already exists.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		return true;
	}

	private async Task OnSaveClick()
	{
		if (!await ValidateForm())
			return;

		await FinancialYearData.InsertFinancialYear(_financialYearModel);
		await _sfToast.ShowAsync();
	}

	private void OnGenerateYearClick()
	{
		var currentDate = DateTime.Now;
		var currentYear = currentDate.Year;

		// Determine financial year based on current date
		// Assuming financial year starts from April 1st
		DateOnly startDate, endDate;

		if (currentDate.Month >= 4) // April to December
		{
			startDate = new DateOnly(currentYear, 4, 1);
			endDate = new DateOnly(currentYear + 1, 3, 31);
		}
		else // January to March
		{
			startDate = new DateOnly(currentYear - 1, 4, 1);
			endDate = new DateOnly(currentYear, 3, 31);
		}

		var nextYearNo = 1;
		if (_financialYears.Count > 0)
		{
			var lastYear = _financialYears.OrderByDescending(fy => fy.YearNo).FirstOrDefault();
			if (lastYear != null)
			{
				nextYearNo = lastYear.YearNo + 1;
			}
		}

		_financialYearModel.StartDate = startDate;
		_financialYearModel.EndDate = endDate;
		_financialYearModel.YearNo = nextYearNo;
		_financialYearModel.Remarks = $"Financial Year {startDate:yyyy}-{endDate:yy} (Auto Generated)";

		StateHasChanged();
	}
}