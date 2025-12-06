using Microsoft.AspNetCore.Components;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Components;

public partial class DeleteConfirmationDialog
{
    private SfDialog _dialog;
    private bool _isVisible;

    [Parameter]
    public string EntityName { get; set; } = "Item";

    [Parameter]
    public string IdentifierLabel { get; set; } = "Name";

    [Parameter]
    public string IdentifierValue { get; set; } = "";

    [Parameter]
    public bool IsPermanentDelete { get; set; } = false;

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public EventCallback OnConfirm { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    public async Task ShowAsync()
    {
        _isVisible = true;
        StateHasChanged();
        await Task.CompletedTask;
    }

    public async Task HideAsync()
    {
        _isVisible = false;
        StateHasChanged();
        await Task.CompletedTask;
    }

    private async Task HandleConfirm()
    {
        await OnConfirm.InvokeAsync();
    }

    private async Task HandleCancel()
    {
        _isVisible = false;
        await OnCancel.InvokeAsync();
    }
}
