using Microsoft.AspNetCore.Components;

using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Accounts.Masters;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeBakes.Shared.Pages.Admin.Accounts;

public partial class FinancialYearPage
{
	private bool _isLoading = true;
	private bool _isSubmitting = false;
	private List<FinancialYearModel> _financialYears = [];
	private FinancialYearModel _financialYearModel = new();
	private SfGrid<FinancialYearModel> _sfGrid;
	private SfToast _sfToast;
	private SfToast _sfErrorToast;

	protected override async Task OnInitializedAsync()
	{
		await LoadFinancialYears();
		await OnGenerateYearClick();
		_isLoading = false;
	}

	private async Task LoadFinancialYears()
	{
		try
		{
			_financialYears = await CommonData.LoadTableData<FinancialYearModel>(TableNames.FinancialYear);
		}
		catch (Exception ex)
		{
			await ShowErrorToast($"Error loading financial years: {ex.Message}");
		}
	}

	private void OnRowSelected(RowSelectEventArgs<FinancialYearModel> args)
	{
		_financialYearModel = new FinancialYearModel
		{
			Id = args.Data.Id,
			StartDate = args.Data.StartDate,
			EndDate = args.Data.EndDate,
			YearNo = args.Data.YearNo,
			Remarks = args.Data.Remarks,
			Status = args.Data.Status
		};
		StateHasChanged();
	}

	private void OnEditFinancialYear(FinancialYearModel financialYear)
	{
		_financialYearModel = new FinancialYearModel
		{
			Id = financialYear.Id,
			StartDate = financialYear.StartDate,
			EndDate = financialYear.EndDate,
			YearNo = financialYear.YearNo,
			Remarks = financialYear.Remarks,
			Status = financialYear.Status
		};
		StateHasChanged();
	}

	private void OnAddFinancialYear()
	{
		_financialYearModel = new FinancialYearModel();
		StateHasChanged();
	}

	private void OnStartDateChanged()
	{
		UpdateEndDateAndYearNo();
		StateHasChanged();
	}

	private void OnEndDateChanged()
	{
		UpdateYearNoFromDates();
		StateHasChanged();
	}

	private void UpdateEndDateAndYearNo()
	{
		if (_financialYearModel.StartDate != default)
		{
			// Set end date to one year minus one day from start date
			_financialYearModel.EndDate = _financialYearModel.StartDate.AddYears(1).AddDays(-1);
			UpdateYearNoFromDates();
		}
	}

	private void UpdateYearNoFromDates()
	{
		if (_financialYearModel.StartDate != default && _financialYearModel.EndDate != default)
		{
			// Generate year number based on start year
			_financialYearModel.YearNo = _financialYearModel.StartDate.Year;
		}
	}

	private string GetFinancialYearDisplay()
	{
		if (_financialYearModel.StartDate != default && _financialYearModel.EndDate != default)
		{
			return $"{_financialYearModel.StartDate:yyyy}-{_financialYearModel.EndDate:yy}";
		}
		return string.Empty;
	}

	private string GetDurationDisplay()
	{
		if (_financialYearModel.StartDate != default && _financialYearModel.EndDate != default)
		{
			var duration = _financialYearModel.EndDate.DayNumber - _financialYearModel.StartDate.DayNumber + 1;
			return $"{duration} days";
		}
		return string.Empty;
	}

	private async Task OnGenerateYearClick()
	{
		try
		{
			// Find the latest financial year
			var latestYear = _financialYears.OrderByDescending(x => x.YearNo).FirstOrDefault();

			if (latestYear == null)
			{
				// First financial year - start from current year
				var currentYear = DateTime.Now.Year;
				_financialYearModel = new FinancialYearModel
				{
					StartDate = new DateOnly(currentYear, 4, 1), // April 1st
					EndDate = new DateOnly(currentYear + 1, 3, 31), // March 31st next year
					YearNo = currentYear,
					Status = true,
					Remarks = $"Financial Year {currentYear}-{(currentYear + 1).ToString().Substring(2)}"
				};
			}
			else
			{
				// Generate next financial year
				var nextStartDate = latestYear.EndDate.AddDays(1);
				var nextEndDate = nextStartDate.AddYears(1).AddDays(-1);

				_financialYearModel = new FinancialYearModel
				{
					StartDate = nextStartDate,
					EndDate = nextEndDate,
					YearNo = nextStartDate.Year,
					Status = true,
					Remarks = $"Financial Year {nextStartDate:yyyy}-{nextEndDate:yy}"
				};
			}

			await ShowSuccessToast("Financial year auto-generated successfully. Please review and save.");
			StateHasChanged();
		}
		catch (Exception ex)
		{
			await ShowErrorToast($"Error generating financial year: {ex.Message}");
		}
	}

	private async Task SaveFinancialYear()
	{
		if (!await ValidateForm())
			return;

		_isSubmitting = true;
		StateHasChanged();

		try
		{
			// Check for overlapping periods
			if (await HasOverlappingPeriod())
			{
				await ShowErrorToast("This financial year period overlaps with an existing financial year.");
				return;
			}

			// Check for duplicate year numbers (only for new entries or when year number changes)
			if (await HasDuplicateYearNumber())
			{
				await ShowErrorToast("A financial year with this year number already exists.");
				return;
			}

			await FinancialYearData.InsertFinancialYear(_financialYearModel);

			await ShowSuccessToast(_financialYearModel.Id == 0
				? "Financial year added successfully!"
				: "Financial year updated successfully!");

			_financialYearModel = new FinancialYearModel();
			await LoadFinancialYears();

			if (_sfGrid != null)
				await _sfGrid.Refresh();
		}
		catch (Exception ex)
		{
			await ShowErrorToast($"Error saving financial year: {ex.Message}");
		}
		finally
		{
			_isSubmitting = false;
			StateHasChanged();
		}
	}

	private async Task<bool> ValidateForm()
	{
		var errors = new List<string>();

		if (_financialYearModel.StartDate == default)
			errors.Add("Start date is required");

		if (_financialYearModel.EndDate == default)
			errors.Add("End date is required");

		if (_financialYearModel.YearNo <= 0)
			errors.Add("Year number must be greater than 0");

		if (_financialYearModel.StartDate != default && _financialYearModel.EndDate != default)
		{
			if (_financialYearModel.StartDate >= _financialYearModel.EndDate)
				errors.Add("End date must be after start date");

			// Check if the period is reasonable (between 300-400 days)
			var duration = _financialYearModel.EndDate.DayNumber - _financialYearModel.StartDate.DayNumber + 1;
			if (duration < 300 || duration > 400)
				errors.Add("Financial year duration should be approximately one year (300-400 days)");
		}

		if (errors.Any())
		{
			await ShowErrorToast($"Please fix the following errors:\n• {string.Join("\n• ", errors)}");
			return false;
		}

		return true;
	}

	private Task<bool> HasOverlappingPeriod()
	{
		var existingYears = _financialYears.Where(x => x.Id != _financialYearModel.Id).ToList();

		foreach (var existingYear in existingYears)
		{
			// Check if the new period overlaps with existing period
			if (_financialYearModel.StartDate <= existingYear.EndDate &&
				_financialYearModel.EndDate >= existingYear.StartDate)
			{
				return Task.FromResult(true);
			}
		}

		return Task.FromResult(false);
	}

	private Task<bool> HasDuplicateYearNumber()
	{
		return Task.FromResult(_financialYears.Any(x => x.Id != _financialYearModel.Id && x.YearNo == _financialYearModel.YearNo));
	}

	private async Task ShowSuccessToast(string message)
	{
		if (_sfToast != null)
		{
			var toastModel = new ToastModel
			{
				Title = "Success",
				Content = message,
				CssClass = "e-toast-success",
				Icon = "e-success toast-icons",
				ShowCloseButton = true,
				Timeout = 3000
			};
			await _sfToast.ShowAsync(toastModel);
		}
	}

	private async Task ShowErrorToast(string message)
	{
		if (_sfErrorToast != null)
		{
			var toastModel = new ToastModel
			{
				Title = "Error",
				Content = message,
				CssClass = "e-toast-danger",
				Icon = "e-error toast-icons",
				ShowCloseButton = true,
				Timeout = 4000
			};
			await _sfErrorToast.ShowAsync(toastModel);
		}
	}
}