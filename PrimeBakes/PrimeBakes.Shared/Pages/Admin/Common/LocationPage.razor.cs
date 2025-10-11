using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeBakes.Shared.Pages.Admin.Common;

public partial class LocationPage
{
	private bool _isLoading = true;
	private bool _isSubmitting = false;

	private LocationModel _locationModel = new()
	{
		Name = "",
		PrefixCode = "",
		Discount = 0,
		Status = true
	};

	private List<LocationModel> _locations = [];

	private SfGrid<LocationModel> _sfGrid;
	private SfToast _sfToast;
	private SfToast _sfErrorToast;

	// Toast message properties
	private string _successMessage = "Operation completed successfully!";
	private string _errorMessage = "An error occurred. Please try again.";

	protected override async Task OnInitializedAsync()
	{
		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Admin, true);
		await LoadLocations();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadLocations()
	{
		_locations = await CommonData.LoadTableData<LocationModel>(TableNames.Location);
		if (_sfGrid is not null)
			await _sfGrid.Refresh();
		StateHasChanged();
	}

	private void OnAddLocation()
	{
		_locationModel = new()
		{
			Id = 0,
			Name = "",
			PrefixCode = "",
			Discount = 0,
			Status = true
		};
		StateHasChanged();
	}

	private void OnEditLocation(LocationModel location)
	{
		_locationModel = new()
		{
			Id = location.Id,
			Name = location.Name,
			PrefixCode = location.PrefixCode,
			Discount = location.Discount,
			Status = location.Status
		};
		StateHasChanged();
	}

	private async Task ToggleLocationStatus(LocationModel location)
	{
		try
		{
			if (location.Id == 1 && location.Status)
			{
				_errorMessage = "Cannot deactivate the main location. It must remain active for system operations.";
				await ShowErrorToast();
				return;
			}

			location.Status = !location.Status;
			await LocationData.InsertLocation(location);
			await LoadLocations();

			_successMessage = $"Location '{location.Name}' has been {(location.Status ? "activated" : "deactivated")} successfully.";
			await ShowSuccessToast();

			OnAddLocation();
		}
		catch (Exception ex)
		{
			_errorMessage = $"Failed to update location status: {ex.Message}";
			await ShowErrorToast();
		}
	}

	private async Task<bool> ValidateForm()
	{
		_locationModel.PrefixCode = _locationModel.PrefixCode?.ToUpper() ?? "";

		if (string.IsNullOrWhiteSpace(_locationModel.Name))
		{
			_errorMessage = "Location name is required. Please enter a valid location name.";
			await ShowErrorToast();
			return false;
		}

		if (string.IsNullOrWhiteSpace(_locationModel.PrefixCode))
		{
			_errorMessage = "Prefix code is required. Please enter a valid prefix code for the location.";
			await ShowErrorToast();
			return false;
		}

		if (_locationModel.Id == 1 && _locationModel.Status == false)
		{
			_errorMessage = "Cannot deactivate the main location. The primary location must remain active.";
			await ShowErrorToast();
			return false;
		}

		if (_locationModel.Id > 0)
		{
			var existingLocation = _locations.FirstOrDefault(_ => _.Id != _locationModel.Id && _.PrefixCode.Equals(_locationModel.PrefixCode, StringComparison.OrdinalIgnoreCase));
			if (existingLocation is not null)
			{
				_errorMessage = $"Prefix code '{_locationModel.PrefixCode}' is already used by location '{existingLocation.Name}'. Please choose a different prefix code.";
				await ShowErrorToast();
				return false;
			}

			existingLocation = _locations.FirstOrDefault(_ => _.Id != _locationModel.Id && _.Name.Equals(_locationModel.Name, StringComparison.OrdinalIgnoreCase));
			if (existingLocation is not null)
			{
				_errorMessage = $"Location name '{_locationModel.Name}' already exists. Please choose a different name.";
				await ShowErrorToast();
				return false;
			}
		}
		else
		{
			var existingLocation = _locations.FirstOrDefault(_ => _.PrefixCode.Equals(_locationModel.PrefixCode, StringComparison.OrdinalIgnoreCase));
			if (existingLocation is not null)
			{
				_errorMessage = $"Prefix code '{_locationModel.PrefixCode}' is already used by location '{existingLocation.Name}'. Please choose a different prefix code.";
				await ShowErrorToast();
				return false;
			}

			existingLocation = _locations.FirstOrDefault(_ => _.Name.Equals(_locationModel.Name, StringComparison.OrdinalIgnoreCase));
			if (existingLocation is not null)
			{
				_errorMessage = $"Location name '{_locationModel.Name}' already exists. Please choose a different name.";
				await ShowErrorToast();
				return false;
			}
		}

		if (_locationModel.Discount < 0 || _locationModel.Discount > 100)
		{
			_errorMessage = $"Discount must be between 0% and 100%. Current value: {_locationModel.Discount}%";
			await ShowErrorToast();
			return false;
		}

		return true;
	}

	private async Task SaveLocation()
	{
		try
		{
			if (_isSubmitting || !await ValidateForm())
				return;

			_isSubmitting = true;
			StateHasChanged();

			var isNewLocation = _locationModel.Id == 0;
			var locationName = _locationModel.Name;

			_locationModel.Id = await LocationData.InsertLocation(_locationModel);
			await InsertLedger();
			await LoadLocations();

			// Reset form
			OnAddLocation();

			_successMessage = isNewLocation
				? $"Location '{locationName}' has been created successfully!"
				: $"Location '{locationName}' has been updated successfully!";
			await ShowSuccessToast();
		}
		catch (Exception ex)
		{
			_errorMessage = $"Failed to save location: {ex.Message}";
			await ShowErrorToast();
		}
		finally
		{
			_isSubmitting = false;
			StateHasChanged();
		}
	}

	public void RowSelectHandler(RowSelectEventArgs<LocationModel> args) =>
		OnEditLocation(args.Data);

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

	private async Task InsertLedger()
	{
		try
		{
			var ledgers = await CommonData.LoadTableData<LedgerModel>(TableNames.Ledger);
			var ledger = await LedgerData.LoadLedgerByLocation(_locationModel.Id);

			await LedgerData.InsertLedger(new()
			{
				Id = ledger?.Id ?? 0,
				Name = _locationModel.Name,
				LocationId = _locationModel.Id,
				Code = ledger?.Code ?? GenerateCodes.GenerateLedgerCode(ledgers.OrderBy(_ => _.Code).LastOrDefault()?.Code),
				AccountTypeId = 3,
				GroupId = 1,
				Alias = "",
				Email = "",
				Remarks = "",
				Address = "",
				GSTNo = "",
				Phone = "",
				StateId = 2,
				Status = true
			});
		}
		catch (Exception ex)
		{
			// Log the error but don't fail the location creation
			// The ledger creation is supplementary
			throw new Exception($"Location saved but ledger creation failed: {ex.Message}", ex);
		}
	}
}