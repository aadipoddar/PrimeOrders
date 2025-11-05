using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Common;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeBakes.Shared.Pages.Admin.Common;

public partial class StatePage
{
	// Services are injected via _Imports.razor, not as properties

	private bool _isLoading = true;
	private bool _isSubmitting = false;

	private StateUTModel _stateModel = new()
	{
		Id = 0,
		Name = "",
		Status = true
	};

	private List<StateUTModel> _states = [];

	private SfGrid<StateUTModel> _sfGrid;
	private SfToast _sfToast;
	private SfToast _sfErrorToast;

	// Toast message properties
	private string _successMessage = "Operation completed successfully!";
	private string _errorMessage = "An error occurred. Please try again.";

	protected override async Task OnInitializedAsync()
	{
		try
		{
			var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Admin, true);
			await LoadStates();
		}
		catch (Exception ex)
		{
			_errorMessage = $"Failed to load data: {ex.Message}";
			await ShowErrorToast();
		}
		finally
		{
			_isLoading = false;
			StateHasChanged();
		}
	}

	private async Task LoadStates()
	{
		_states = await CommonData.LoadTableData<StateUTModel>(TableNames.StateUT);
		if (_sfGrid is not null)
			await _sfGrid.Refresh();
		StateHasChanged();
	}

	private void OnAddState()
	{
		_stateModel = new()
		{
			Id = 0,
			Name = "",
			Status = true
		};
		StateHasChanged();
	}

	private void OnEditState(StateUTModel state)
	{
		_stateModel = new()
		{
			Id = state.Id,
			Name = state.Name,
			Status = state.Status
		};
		StateHasChanged();
	}

	private async Task SaveState()
	{
		if (_isSubmitting) return;

		try
		{
			_isSubmitting = true;
			StateHasChanged();

			// Validation
			if (string.IsNullOrWhiteSpace(_stateModel.Name))
			{
				_errorMessage = "State name is required.";
				await ShowErrorToast();
				return;
			}

			// Trim and format the name
			_stateModel.Name = _stateModel.Name.Trim();

			// Check for duplicate names (excluding current state if editing)
			var existingState = _states.FirstOrDefault(s =>
				s.Name.Equals(_stateModel.Name, StringComparison.OrdinalIgnoreCase) &&
				s.Id != _stateModel.Id);

			if (existingState != null)
			{
				_errorMessage = $"A state with the name '{_stateModel.Name}' already exists.";
				await ShowErrorToast();
				return;
			}

			bool success;
			var isNewState = _stateModel.Id == 0;
			var stateName = _stateModel.Name;

			await StateUTData.InsertStateUT(_stateModel);
			_successMessage = isNewState
				? $"State '{stateName}' has been added successfully!"
				: $"State '{stateName}' has been updated successfully!";
			success = true;

			if (success)
			{
				await LoadStates();
				OnAddState(); // Reset form
				await ShowSuccessToast();
				await _sfGrid.Refresh();
			}
		}
		catch (Exception ex)
		{
			_errorMessage = $"An error occurred while saving: {ex.Message}";
			await ShowErrorToast();
		}
		finally
		{
			_isSubmitting = false;
			StateHasChanged();
		}
	}

	private async Task ToggleStateStatus(StateUTModel state)
	{
		try
		{
			var previousStatus = state.Status;
			state.Status = !state.Status;

			await StateUTData.InsertStateUT(state);
			_successMessage = $"State '{state.Name}' has been {(state.Status ? "activated" : "deactivated")} successfully!";
			await ShowSuccessToast();
			await LoadStates();
			await _sfGrid.Refresh();
		}
		catch (Exception ex)
		{
			// Revert the status change
			state.Status = !state.Status;
			_errorMessage = $"An error occurred while updating status: {ex.Message}";
			await ShowErrorToast();
		}
	}

	private void RowSelectHandler(RowSelectEventArgs<StateUTModel> args)
	{
		OnEditState(args.Data);
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