using Syncfusion.Blazor.Notifications;

namespace PrimeBakes.Shared.Components;

/// <summary>
/// Defines the types of toast notifications available
/// </summary>
public enum ToastType
{
	/// <summary>Success toast (green) - for successful operations</summary>
	Success,
	/// <summary>Error toast (red) - for errors and failures</summary>
	Error,
	/// <summary>Warning toast (amber) - for warnings</summary>
	Warning,
	/// <summary>Info toast (blue) - for informational messages</summary>
	Info
}

public partial class ToastNotification
{
	private SfToast _sfToast = null!;

	/// <summary>
	/// Shows a toast notification with the specified type
	/// </summary>
	/// <param name="title">The title of the toast</param>
	/// <param name="message">The content message of the toast</param>
	/// <param name="type">The type of toast (Success, Error, Warning, Info)</param>
	public async Task ShowAsync(string title, string message, ToastType type)
	{
		var cssClass = type switch
		{
			ToastType.Success => "e-toast-success",
			ToastType.Error => "e-toast-danger",
			ToastType.Warning => "e-toast-warning",
			ToastType.Info => "e-toast-info",
			_ => "e-toast-success"
		};

		await _sfToast.ShowAsync(new ToastModel
		{
			Title = title,
			Content = message,
			CssClass = cssClass
		});
	}

	/// <summary>
	/// Shows a success toast notification (green)
	/// </summary>
	public async Task ShowSuccessAsync(string title, string message) =>
		await ShowAsync(title, message, ToastType.Success);

	/// <summary>
	/// Shows an error toast notification (red)
	/// </summary>
	public async Task ShowErrorAsync(string title, string message) =>
		await ShowAsync(title, message, ToastType.Error);

	/// <summary>
	/// Shows a warning toast notification (amber)
	/// </summary>
	public async Task ShowWarningAsync(string title, string message) =>
		await ShowAsync(title, message, ToastType.Warning);

	/// <summary>
	/// Shows an info toast notification (blue)
	/// </summary>
	public async Task ShowInfoAsync(string title, string message) =>
		await ShowAsync(title, message, ToastType.Info);

	/// <summary>
	/// Hides all currently displayed toasts
	/// </summary>
	public async Task HideAllAsync() =>
		await _sfToast.HideAsync("All");
}