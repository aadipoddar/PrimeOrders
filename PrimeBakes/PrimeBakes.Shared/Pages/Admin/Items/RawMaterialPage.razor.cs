using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Product;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeBakes.Shared.Pages.Admin.Items;

public partial class RawMaterialPage
{
	private bool _isLoading = true;
	private bool _isSubmitting = false;

	private RawMaterialModel _rawMaterialModel = new()
	{
		Id = 0,
		Name = "",
		Code = "",
		RawMaterialCategoryId = 0,
		MeasurementUnit = "KG",
		MRP = 0,
		TaxId = 0,
		Status = true
	};

	private List<RawMaterialModel> _rawMaterials = [];
	private List<RawMaterialCategoryModel> _rawMaterialCategories = [];
	private List<TaxModel> _taxTypes = [];

	private SfGrid<RawMaterialModel> _sfGrid;
	private SfToast _sfToast;
	private SfToast _sfErrorToast;

	// Toast message properties
	private string _successMessage = "Operation completed successfully!";
	private string _errorMessage = "An error occurred. Please try again.";

	protected override async Task OnInitializedAsync()
	{
		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Admin, true);
		await LoadRawMaterials();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadRawMaterials()
	{
		try
		{
			_rawMaterialCategories = await CommonData.LoadTableData<RawMaterialCategoryModel>(TableNames.RawMaterialCategory);
			_rawMaterials = await CommonData.LoadTableData<RawMaterialModel>(TableNames.RawMaterial);
			_taxTypes = await CommonData.LoadTableData<TaxModel>(TableNames.Tax);

			_rawMaterialModel.Code = GenerateCodes.GenerateRawMaterialCode(_rawMaterials.OrderBy(_ => _.Code).LastOrDefault()?.Code);

			if (_sfGrid is not null)
				await _sfGrid.Refresh();
			StateHasChanged();
		}
		catch (Exception ex)
		{
			_errorMessage = $"Failed to load raw materials data: {ex.Message}";
			await ShowErrorToast();
		}
	}

	private void OnAddRawMaterial()
	{
		_rawMaterialModel = new()
		{
			Id = 0,
			Name = "",
			Code = GenerateCodes.GenerateRawMaterialCode(_rawMaterials.OrderBy(_ => _.Code).LastOrDefault()?.Code),
			RawMaterialCategoryId = 0,
			MeasurementUnit = "KG",
			MRP = 0,
			TaxId = 0,
			Status = true
		};
		StateHasChanged();
	}

	private void OnEditRawMaterial(RawMaterialModel rawMaterial)
	{
		_rawMaterialModel = new()
		{
			Id = rawMaterial.Id,
			Name = rawMaterial.Name,
			Code = rawMaterial.Code,
			RawMaterialCategoryId = rawMaterial.RawMaterialCategoryId,
			MeasurementUnit = rawMaterial.MeasurementUnit,
			MRP = rawMaterial.MRP,
			TaxId = rawMaterial.TaxId,
			Status = rawMaterial.Status
		};
		StateHasChanged();
	}

	private async Task ToggleRawMaterialStatus(RawMaterialModel rawMaterial)
	{
		try
		{
			rawMaterial.Status = !rawMaterial.Status;
			await RawMaterialData.InsertRawMaterial(rawMaterial);
			await LoadRawMaterials();

			_successMessage = $"Raw material '{rawMaterial.Name}' has been {(rawMaterial.Status ? "activated" : "deactivated")} successfully.";
			await ShowSuccessToast();

			OnAddRawMaterial();
		}
		catch (Exception ex)
		{
			_errorMessage = $"Failed to update raw material status: {ex.Message}";
			await ShowErrorToast();
		}
	}

	private async Task<bool> ValidateForm()
	{
		// Ensure measurement unit is always uppercase
		_rawMaterialModel.MeasurementUnit = _rawMaterialModel.MeasurementUnit?.ToUpper() ?? "";

		if (string.IsNullOrWhiteSpace(_rawMaterialModel.Name))
		{
			_errorMessage = "Raw material name is required. Please enter a valid name.";
			await ShowErrorToast();
			return false;
		}

		if (string.IsNullOrWhiteSpace(_rawMaterialModel.Code))
		{
			_errorMessage = "Raw material code is required. Please enter a valid code.";
			await ShowErrorToast();
			return false;
		}

		if (_rawMaterialModel.RawMaterialCategoryId <= 0)
		{
			_errorMessage = "Category selection is required. Please select a valid category.";
			await ShowErrorToast();
			return false;
		}

		if (_rawMaterialModel.TaxId <= 0)
		{
			_errorMessage = "Tax selection is required. Please select a valid tax rate.";
			await ShowErrorToast();
			return false;
		}

		if (string.IsNullOrWhiteSpace(_rawMaterialModel.MeasurementUnit))
		{
			_errorMessage = "Measurement unit is required. Please enter a valid unit.";
			await ShowErrorToast();
			return false;
		}

		if (_rawMaterialModel.MRP < 0)
		{
			_errorMessage = $"MRP must be greater than 0. Current value: {_rawMaterialModel.MRP:C}";
			await ShowErrorToast();
			return false;
		}

		// Check for duplicate names and codes
		if (_rawMaterialModel.Id > 0)
		{
			var existingRawMaterial = _rawMaterials.FirstOrDefault(_ => _.Id != _rawMaterialModel.Id && _.Name.Equals(_rawMaterialModel.Name, StringComparison.OrdinalIgnoreCase));
			if (existingRawMaterial is not null)
			{
				_errorMessage = $"Raw material name '{_rawMaterialModel.Name}' already exists. Please choose a different name.";
				await ShowErrorToast();
				return false;
			}

			existingRawMaterial = _rawMaterials.FirstOrDefault(_ => _.Id != _rawMaterialModel.Id && _.Code.Equals(_rawMaterialModel.Code, StringComparison.OrdinalIgnoreCase));
			if (existingRawMaterial is not null)
			{
				_errorMessage = $"Raw material code '{_rawMaterialModel.Code}' is already used by '{existingRawMaterial.Name}'. Please choose a different code.";
				await ShowErrorToast();
				return false;
			}
		}
		else
		{
			var existingRawMaterial = _rawMaterials.FirstOrDefault(_ => _.Name.Equals(_rawMaterialModel.Name, StringComparison.OrdinalIgnoreCase));
			if (existingRawMaterial is not null)
			{
				_errorMessage = $"Raw material name '{_rawMaterialModel.Name}' already exists. Please choose a different name.";
				await ShowErrorToast();
				return false;
			}

			existingRawMaterial = _rawMaterials.FirstOrDefault(_ => _.Code.Equals(_rawMaterialModel.Code, StringComparison.OrdinalIgnoreCase));
			if (existingRawMaterial is not null)
			{
				_errorMessage = $"Raw material code '{_rawMaterialModel.Code}' is already used by '{existingRawMaterial.Name}'. Please choose a different code.";
				await ShowErrorToast();
				return false;
			}
		}

		return true;
	}

	private async Task SaveRawMaterial()
	{
		try
		{
			if (_isSubmitting || !await ValidateForm())
				return;

			_isSubmitting = true;
			StateHasChanged();

			var isNewRawMaterial = _rawMaterialModel.Id == 0;
			var rawMaterialName = _rawMaterialModel.Name;

			await RawMaterialData.InsertRawMaterial(_rawMaterialModel);
			await LoadRawMaterials();

			// Reset form
			OnAddRawMaterial();

			_successMessage = isNewRawMaterial
				? $"Raw material '{rawMaterialName}' has been created successfully!"
				: $"Raw material '{rawMaterialName}' has been updated successfully!";
			await ShowSuccessToast();
		}
		catch (Exception ex)
		{
			_errorMessage = $"Failed to save raw material: {ex.Message}";
			await ShowErrorToast();
		}
		finally
		{
			_isSubmitting = false;
			StateHasChanged();
		}
	}

	public void RowSelectHandler(RowSelectEventArgs<RawMaterialModel> args) =>
		OnEditRawMaterial(args.Data);

	// Helper methods for showing toasts with dynamic content
	private async Task ShowSuccessToast()
	{
		if (_sfToast != null)
		{
			var toastModel = new ToastModel
			{
				Title = "Success",
				Content = _successMessage,
				CssClass = "e-toast-success",
				Icon = "e-success toast-icons"
			};
			await _sfToast.ShowAsync(toastModel);
		}
	}

	private async Task ShowErrorToast()
	{
		if (_sfErrorToast != null)
		{
			var toastModel = new ToastModel
			{
				Title = "Error",
				Content = _errorMessage,
				CssClass = "e-toast-danger",
				Icon = "e-error toast-icons"
			};
			await _sfErrorToast.ShowAsync(toastModel);
		}
	}

	// Helper methods to get display names
	private string GetCategoryName(int categoryId)
	{
		return _rawMaterialCategories.FirstOrDefault(c => c.Id == categoryId)?.Name ?? "Unknown";
	}

	private string GetTaxName(int taxId)
	{
		return _taxTypes.FirstOrDefault(t => t.Id == taxId)?.Code ?? "Unknown";
	}


}