using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Product;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Product;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeBakes.Shared.Pages.Admin.Common;

public partial class TaxPage
{
	private bool _isLoading = true;
	private bool _isEdit;
	private bool _isSaving;

	private TaxModel _taxModel = new()
	{
		Id = 0,
		Code = "",
		CGST = 0,
		SGST = 0,
		IGST = 0,
		Extra = false,
		Inclusive = true,
		Status = true
	};

	private List<TaxModel> _taxes = [];

	private SfGrid<TaxModel> _sfGrid;
	private SfToast _sfToast;
	private SfToast _sfErrorToast;

	private string _successMessage = "";
	private string _errorMessage = "";

	protected override async Task OnInitializedAsync()
	{
		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Admin, true);
		await LoadTaxes();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadTaxes()
	{
		try
		{
			_taxes = await CommonData.LoadTableData<TaxModel>(TableNames.Tax);

			if (_sfGrid is not null)
				await _sfGrid.Refresh();
			StateHasChanged();
		}
		catch (Exception ex)
		{
			_errorMessage = $"Failed to load taxes: {ex.Message}";
			await ShowErrorToast();
		}
	}

	private async Task OnToolbarClickAsync(Syncfusion.Blazor.Navigations.ClickEventArgs args)
	{
		if (args.Item.Id == "add")
		{
			_taxModel = new TaxModel
			{
				Status = true,
				Inclusive = true,
				Extra = false
			};
			_isEdit = false;
		}
		else if (args.Item.Id == "edit")
		{
			var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
			if (selectedRecords.Count > 0)
			{
				_taxModel = new TaxModel
				{
					Id = selectedRecords[0].Id,
					Code = selectedRecords[0].Code,
					CGST = selectedRecords[0].CGST,
					SGST = selectedRecords[0].SGST,
					IGST = selectedRecords[0].IGST,
					Inclusive = selectedRecords[0].Inclusive,
					Extra = selectedRecords[0].Extra,
					Status = selectedRecords[0].Status
				};
				_isEdit = true;
			}
			else
			{
				_errorMessage = "Please select a tax to edit.";
				await ShowErrorToast();
			}
		}
		else if (args.Item.Id == "delete")
		{
			var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
			if (selectedRecords.Count > 0)
			{
				await DeleteTax(selectedRecords[0]);
			}
			else
			{
				_errorMessage = "Please select a tax to delete.";
				await ShowErrorToast();
			}
		}
	}

	private async Task DeleteTax(TaxModel tax)
	{
		try
		{
			_isSaving = true;
			tax.Status = false;
			await TaxData.InsertTax(tax);
			await LoadTaxes();

			_successMessage = "Tax deleted successfully.";
			await ShowSuccessToast();
		}
		catch (Exception ex)
		{
			_errorMessage = $"Failed to delete tax: {ex.Message}";
			await ShowErrorToast();
		}
		finally
		{
			_isSaving = false;
		}
	}

	private async Task SaveTax()
	{
		if (!ValidateTax())
			return;

		try
		{
			_isSaving = true;
			await TaxData.InsertTax(_taxModel);
			await LoadTaxes();

			_successMessage = _isEdit ? "Tax updated successfully." : "Tax created successfully.";
			await ShowSuccessToast();

			_taxModel = new TaxModel
			{
				Status = true,
				Inclusive = true,
				Extra = false
			};
			_isEdit = false;
		}
		catch (Exception ex)
		{
			_errorMessage = $"Failed to save tax: {ex.Message}";
			await ShowErrorToast();
		}
		finally
		{
			_isSaving = false;
		}
	}

	private bool ValidateTax()
	{
		// Check if code is provided
		if (string.IsNullOrWhiteSpace(_taxModel.Code))
		{
			ShowValidationError("Tax code is required.");
			return false;
		}

		// Check for duplicate code (excluding current record if editing)
		var duplicateCode = _taxes.Any(t => t.Code.Equals(_taxModel.Code, StringComparison.OrdinalIgnoreCase) && t.Id != _taxModel.Id);
		if (duplicateCode)
		{
			ShowValidationError("Tax code already exists.");
			return false;
		}

		// Check that all three percentages cannot be more than 0
		var allPercentagesGreaterThanZero = _taxModel.CGST > 0 && _taxModel.SGST > 0 && _taxModel.IGST > 0;
		if (allPercentagesGreaterThanZero)
		{
			ShowValidationError("All three tax percentages cannot be greater than 0. Use either (CGST + SGST) OR IGST.");
			return false;
		}

		// Check that either (SGST + CGST) OR IGST is used, not both
		var hasCGSTSGST = _taxModel.CGST > 0 || _taxModel.SGST > 0;
		var hasIGST = _taxModel.IGST > 0;

		if (hasCGSTSGST && hasIGST)
		{
			ShowValidationError("Cannot use both (CGST/SGST) and IGST together. Use either (CGST + SGST) OR IGST.");
			return false;
		}

		// At least one tax percentage should be provided
		if (!hasCGSTSGST && !hasIGST)
		{
			ShowValidationError("At least one tax percentage (CGST, SGST, or IGST) must be greater than 0.");
			return false;
		}

		// If using CGST or SGST, typically both should be used together for domestic transactions
		if ((_taxModel.CGST > 0 && _taxModel.SGST == 0 || _taxModel.CGST == 0 && _taxModel.SGST > 0) && _taxModel.IGST == 0)
		{
			ShowValidationError("For domestic transactions, both CGST and SGST should be used together.");
			return false;
		}

		// Tax should be either inclusive or exclusive (both cannot be true)
		if (_taxModel.Inclusive && _taxModel.Extra)
		{
			ShowValidationError("Tax cannot be both Inclusive and Extra. Please select either Inclusive or Extra.");
			return false;
		}

		// At least one of Inclusive or Extra should be selected
		if (!_taxModel.Inclusive && !_taxModel.Extra)
		{
			ShowValidationError("Tax must be either Inclusive or Extra. Please select one option.");
			return false;
		}

		// Validate percentage ranges (0-100)
		if (_taxModel.CGST < 0 || _taxModel.CGST > 100)
		{
			ShowValidationError("CGST percentage must be between 0 and 100.");
			return false;
		}

		if (_taxModel.SGST < 0 || _taxModel.SGST > 100)
		{
			ShowValidationError("SGST percentage must be between 0 and 100.");
			return false;
		}

		if (_taxModel.IGST < 0 || _taxModel.IGST > 100)
		{
			ShowValidationError("IGST percentage must be between 0 and 100.");
			return false;
		}

		return true;
	}

	private async void ShowValidationError(string message)
	{
		_errorMessage = message;
		await ShowErrorToast();
	}

	private void OnCGSTChanged(decimal value)
	{
		_taxModel.CGST = value;
		ValidateTaxPercentages();
	}

	private void OnSGSTChanged(decimal value)
	{
		_taxModel.SGST = value;
		ValidateTaxPercentages();
	}

	private void OnIGSTChanged(decimal value)
	{
		_taxModel.IGST = value;
		ValidateTaxPercentages();
	}

	private void ValidateTaxPercentages()
	{
		// Auto-correct conflicting combinations during input
		if (_taxModel.IGST > 0)
		{
			// If IGST is entered, clear CGST and SGST
			if (_taxModel.CGST > 0 || _taxModel.SGST > 0)
			{
				_taxModel.CGST = 0;
				_taxModel.SGST = 0;
			}
		}
		else if (_taxModel.CGST > 0 || _taxModel.SGST > 0)
		{
			// If CGST or SGST is entered, clear IGST
			if (_taxModel.IGST > 0)
			{
				_taxModel.IGST = 0;
			}
		}
	}

	private void OnInclusiveChanged(bool value)
	{
		_taxModel.Inclusive = value;
		if (value)
		{
			_taxModel.Extra = false; // Ensure only one is selected
		}
	}

	private void OnExtraChanged(bool value)
	{
		_taxModel.Extra = value;
		if (value)
		{
			_taxModel.Inclusive = false; // Ensure only one is selected
		}
	}

	private void OnEditTax(TaxModel tax)
	{
		_taxModel = new TaxModel
		{
			Id = tax.Id,
			Code = tax.Code,
			CGST = tax.CGST,
			SGST = tax.SGST,
			IGST = tax.IGST,
			Inclusive = tax.Inclusive,
			Extra = tax.Extra,
			Status = tax.Status
		};
		_isEdit = true;
		StateHasChanged();
	}

	private async Task ToggleTaxStatus(TaxModel tax)
	{
		try
		{
			// Toggle the status locally first
			var originalStatus = tax.Status;
			tax.Status = !tax.Status;

			// Update in database using the existing Insert_Tax procedure
			await TaxData.InsertTax(tax);

			_successMessage = $"Tax '{tax.Code}' {(tax.Status ? "activated" : "deactivated")} successfully!";
			await ShowSuccessToast();
			await LoadTaxes(); // Refresh the grid
		}
		catch (Exception ex)
		{
			// Revert the change if exception occurred
			tax.Status = !tax.Status;
			_errorMessage = $"Error updating tax status: {ex.Message}";
			await ShowErrorToast();
		}
	}

	private void CancelEdit()
	{
		_taxModel = new TaxModel
		{
			Status = true,
			Inclusive = true,
			Extra = false
		};
		_isEdit = false;
	}

	private async Task ShowSuccessToast()
	{
		var toastModel = new ToastModel
		{
			Title = "Success!",
			Content = _successMessage,
			CssClass = "e-toast-success",
			Icon = "e-success toast-icons",
			ShowCloseButton = true,
			ShowProgressBar = true
		};

		await _sfToast.ShowAsync(toastModel);
	}

	private async Task ShowErrorToast()
	{
		var toastModel = new ToastModel
		{
			Title = "Error!",
			Content = _errorMessage,
			CssClass = "e-toast-danger",
			Icon = "e-error toast-icons",
			ShowCloseButton = true,
			ShowProgressBar = true
		};

		await _sfErrorToast.ShowAsync(toastModel);
	}
}