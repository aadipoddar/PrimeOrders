using Microsoft.AspNetCore.Components;
using PrimeBakes.Shared.Services;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeBakes.Shared.Pages.Admin.Accounts;

public partial class GroupPage
{
    private UserModel _userModel;
    private bool _isLoading = true;
    private bool _isSubmitting = false;
    private string _successMessage = "Operation completed successfully!";
    private string _errorMessage = "An error occurred. Please try again.";

    private GroupModel _groupModel = new()
    {
        Name = "",
        Remarks = "",
        Status = true
    };

    private List<GroupModel> _groups = [];

    private SfGrid<GroupModel> _sfGrid;
    private SfToast _sfToast;
    private SfToast _sfErrorToast;

    protected override async Task OnInitializedAsync()
    {
        var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Admin, true);
        _userModel = authResult.User;
        await LoadData();
        _isLoading = false;
    }

    private async Task LoadData()
    {
        _groups = await CommonData.LoadTableData<GroupModel>(TableNames.Group);

        if (_sfGrid is not null)
            await _sfGrid.Refresh();

        StateHasChanged();
    }

    private void OnRowSelected(RowSelectEventArgs<GroupModel> args)
    {
        _groupModel = new GroupModel
        {
            Id = args.Data.Id,
            Name = args.Data.Name,
            Remarks = args.Data.Remarks ?? "",
            Status = args.Data.Status
        };

        StateHasChanged();
    }

    private async Task<bool> ValidateForm()
    {
        if (string.IsNullOrWhiteSpace(_groupModel.Name))
        {
            _errorMessage = "Group name is required. Please enter a valid group name.";
            await ShowErrorToast();
            return false;
        }

        // Check for duplicate group names
        if (_groupModel.Id <= 0)
        {
            var existingGroup = _groups.FirstOrDefault(g =>
                g.Name.Equals(_groupModel.Name, StringComparison.OrdinalIgnoreCase));

            if (existingGroup != null)
            {
                _errorMessage = $"Group name '{_groupModel.Name}' already exists. Please choose a different name.";
                await ShowErrorToast();
                return false;
            }
        }
        else
        {
            var existingGroup = _groups.FirstOrDefault(g =>
                g.Id != _groupModel.Id &&
                g.Name.Equals(_groupModel.Name, StringComparison.OrdinalIgnoreCase));

            if (existingGroup != null)
            {
                _errorMessage = $"Group name '{_groupModel.Name}' already exists. Please choose a different name.";
                await ShowErrorToast();
                return false;
            }
        }

        return true;
    }

    private async Task OnSaveClick()
    {
        if (!await ValidateForm())
            return;

        _isSubmitting = true;
        StateHasChanged();

        try
        {
            bool isNewGroup = _groupModel.Id <= 0;
            await GroupData.InsertGroup(_groupModel);

            _successMessage = isNewGroup
                ? $"Group '{_groupModel.Name}' has been created successfully."
                : $"Group '{_groupModel.Name}' has been updated successfully.";

            await ShowSuccessToast();
            await LoadData();
            ResetForm();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to save group: {ex.Message}";
            await ShowErrorToast();
            Console.WriteLine(ex.Message);
        }
        finally
        {
            _isSubmitting = false;
            StateHasChanged();
        }
    }

    private void ResetForm()
    {
        _groupModel = new()
        {
            Name = "",
            Remarks = "",
            Status = true
        };
    }

    private void OnEditGroup(GroupModel group)
    {
        _groupModel = new GroupModel
        {
            Id = group.Id,
            Name = group.Name,
            Remarks = group.Remarks ?? "",
            Status = group.Status
        };
        StateHasChanged();
    }

    private void OnAddGroup()
    {
        ResetForm();
        StateHasChanged();
    }

    private async Task SaveGroup()
    {
        if (!await ValidateForm())
            return;

        _isSubmitting = true;
        StateHasChanged();

        try
        {
            bool isNewGroup = _groupModel.Id <= 0;
            await GroupData.InsertGroup(_groupModel);

            _successMessage = isNewGroup
                ? $"Group '{_groupModel.Name}' has been created successfully."
                : $"Group '{_groupModel.Name}' has been updated successfully.";

            await ShowSuccessToast();
            await LoadData();
            ResetForm();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to save group: {ex.Message}";
            await ShowErrorToast();
            Console.WriteLine(ex.Message);
        }
        finally
        {
            _isSubmitting = false;
            StateHasChanged();
        }
    }

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