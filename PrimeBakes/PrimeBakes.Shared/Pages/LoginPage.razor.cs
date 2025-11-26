using Microsoft.AspNetCore.Components;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Common;

using Syncfusion.Blazor.Inputs;

namespace PrimeBakes.Shared.Pages;

public partial class LoginPage
{
    [Inject] public NavigationManager NavManager { get; set; }

    private string _passcode = "";
    private bool _isVerifying = false;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        await DataStorageService.SecureRemoveAll();
    }

    protected override async Task OnInitializedAsync() =>
        await DataStorageService.SecureRemoveAll();

    private async Task CheckPasscode(OtpInputEventArgs e)
    {
        _passcode = e.Value?.ToString() ?? string.Empty;
        if (_passcode.Length != 4 || _isVerifying)
            return;

        _isVerifying = true;
        StateHasChanged();

        var user = await UserData.LoadUserByPasscode(int.Parse(_passcode));

        if (user is null || !user.Status)
        {
            _isVerifying = false;
            StateHasChanged();
            return;
        }

        await DataStorageService.SecureSaveAsync(StorageFileNames.UserDataFileName, System.Text.Json.JsonSerializer.Serialize(user));
        VibrationService.VibrateWithTime(500);
        NavManager.NavigateTo("/");
    }
}