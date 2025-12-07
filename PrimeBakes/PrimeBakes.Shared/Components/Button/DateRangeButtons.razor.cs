using Microsoft.AspNetCore.Components;

using PrimeBakes.Shared.Components.Dialog;

using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;

namespace PrimeBakes.Shared.Components.Button;

public partial class DateRangeButtons
{
	[Parameter]
	public bool Disabled { get; set; } = false;

	[Parameter]
	public DateTime FromDate { get; set; }

	[Parameter]
	public DateTime ToDate { get; set; }

	[Parameter]
	public EventCallback<(DateTime FromDate, DateTime ToDate)> OnDatesChanged { get; set; }

	[Parameter]
	public ToastNotification ToastNotification { get; set; }

	private bool _isProcessing = false;

	private async Task HandleDateRangeClick(DateRangeType rangeType)
	{
		if (_isProcessing || Disabled)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			var today = await CommonData.LoadCurrentDateTime();
			var currentYear = today.Year;
			var currentMonth = today.Month;

			DateTime newFromDate = FromDate;
			DateTime newToDate = ToDate;

			switch (rangeType)
			{
				case DateRangeType.Today:
					newFromDate = today;
					newToDate = today;
					break;

				case DateRangeType.Yesterday:
					newFromDate = today.AddDays(-1);
					newToDate = today.AddDays(-1);
					break;

				case DateRangeType.CurrentMonth:
					newFromDate = new DateTime(currentYear, currentMonth, 1);
					newToDate = newFromDate.AddMonths(1).AddDays(-1);
					break;

				case DateRangeType.PreviousMonth:
					newFromDate = new DateTime(newFromDate.Year, newFromDate.Month, 1).AddMonths(-1);
					newToDate = newFromDate.AddMonths(1).AddDays(-1);
					break;

				case DateRangeType.CurrentFinancialYear:
					var currentFY = await FinancialYearData.LoadFinancialYearByDateTime(today);
					newFromDate = currentFY.StartDate.ToDateTime(TimeOnly.MinValue);
					newToDate = currentFY.EndDate.ToDateTime(TimeOnly.MaxValue);
					break;

				case DateRangeType.PreviousFinancialYear:
					var currentFY2 = await FinancialYearData.LoadFinancialYearByDateTime(newFromDate);
					var financialYears = await CommonData.LoadTableDataByStatus<FinancialYearModel>(TableNames.FinancialYear);
					var previousFY = financialYears
						.Where(fy => fy.Id != currentFY2.Id)
						.OrderByDescending(fy => fy.StartDate)
						.FirstOrDefault();

					if (previousFY == null)
					{
						if (ToastNotification != null)
							await ToastNotification.ShowAsync("Warning", "No previous financial year available.", ToastType.Warning);
						return;
					}

					newFromDate = previousFY.StartDate.ToDateTime(TimeOnly.MinValue);
					newToDate = previousFY.EndDate.ToDateTime(TimeOnly.MaxValue);
					break;

				case DateRangeType.AllTime:
					newFromDate = new DateTime(2000, 1, 1);
					newToDate = today;
					break;
			}

			await OnDatesChanged.InvokeAsync((newFromDate, newToDate));
		}
		catch (Exception ex)
		{
			if (ToastNotification != null)
				await ToastNotification.ShowAsync("Error", $"Failed to set date range: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}
}
