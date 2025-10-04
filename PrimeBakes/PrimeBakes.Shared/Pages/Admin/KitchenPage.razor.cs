using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Kitchen;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Kitchen;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeBakes.Shared.Pages.Admin;

public partial class KitchenPage
{
	private bool _isLoading = true;
	private bool _isSubmitting = false;

	private KitchenModel _kitchenModel = new()
	{
		Id = 0,
		Name = "",
		Status = true
	};

	private List<KitchenModel> _kitchens = [];

	private SfGrid<KitchenModel> _sfGrid;
	private SfToast _sfToast;
	private SfToast _sfErrorToast;

	// Toast message properties
	private string _successMessage = "Operation completed successfully!";
	private string _errorMessage = "An error occurred. Please try again.";

	protected override async Task OnInitializedAsync()
	{
		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Admin, true);
		await LoadKitchens();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadKitchens()
	{
		_kitchens = await CommonData.LoadTableData<KitchenModel>(TableNames.Kitchen);
		if (_sfGrid is not null)
			await _sfGrid.Refresh();
		StateHasChanged();
	}

	private void OnAddKitchen()
	{
		_kitchenModel = new()
		{
			Id = 0,
			Name = "",
			Status = true
		};
		StateHasChanged();
	}

	private void OnEditKitchen(KitchenModel kitchen)
	{
		_kitchenModel = new()
		{
			Id = kitchen.Id,
			Name = kitchen.Name,
			Status = kitchen.Status
		};
		StateHasChanged();
	}

	private async Task ToggleKitchenStatus(KitchenModel kitchen)
	{
		try
		{
			kitchen.Status = !kitchen.Status;
			await KitchenData.InsertKitchen(kitchen);
			await LoadKitchens();

			_successMessage = $"Kitchen '{kitchen.Name}' has been {(kitchen.Status ? "activated" : "deactivated")} successfully.";
			await ShowSuccessToast();

			OnAddKitchen();
		}
		catch (Exception ex)
		{
			_errorMessage = $"Failed to update kitchen status: {ex.Message}";
			await ShowErrorToast();
		}
	}

	private async Task<bool> ValidateForm()
	{
		if (string.IsNullOrWhiteSpace(_kitchenModel.Name))
		{
			_errorMessage = "Kitchen name is required. Please enter a valid kitchen name.";
			await ShowErrorToast();
			return false;
		}

		// Check for duplicate names
		if (_kitchenModel.Id > 0)
		{
			var existingKitchen = _kitchens.FirstOrDefault(_ => _.Id != _kitchenModel.Id && _.Name.Equals(_kitchenModel.Name, StringComparison.OrdinalIgnoreCase));
			if (existingKitchen is not null)
			{
				_errorMessage = $"Kitchen name '{_kitchenModel.Name}' already exists. Please choose a different name.";
				await ShowErrorToast();
				return false;
			}
		}
		else
		{
			var existingKitchen = _kitchens.FirstOrDefault(_ => _.Name.Equals(_kitchenModel.Name, StringComparison.OrdinalIgnoreCase));
			if (existingKitchen is not null)
			{
				_errorMessage = $"Kitchen name '{_kitchenModel.Name}' already exists. Please choose a different name.";
				await ShowErrorToast();
				return false;
			}
		}

		return true;
	}

	private async Task SaveKitchen()
	{
		try
		{
			if (_isSubmitting || !await ValidateForm())
				return;

			_isSubmitting = true;
			StateHasChanged();

			var isNewKitchen = _kitchenModel.Id == 0;
			var kitchenName = _kitchenModel.Name;

			await KitchenData.InsertKitchen(_kitchenModel);
			await LoadKitchens();

			// Reset form
			OnAddKitchen();

			_successMessage = isNewKitchen
				? $"Kitchen '{kitchenName}' has been created successfully!"
				: $"Kitchen '{kitchenName}' has been updated successfully!";
			await ShowSuccessToast();
		}
		catch (Exception ex)
		{
			_errorMessage = $"Failed to save kitchen: {ex.Message}";
			await ShowErrorToast();
		}
		finally
		{
			_isSubmitting = false;
			StateHasChanged();
		}
	}

	public void RowSelectHandler(RowSelectEventArgs<KitchenModel> args) =>
		OnEditKitchen(args.Data);

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
}