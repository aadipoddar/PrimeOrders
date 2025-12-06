#nullable enable

using Microsoft.AspNetCore.Components;

namespace PrimeBakes.Shared.Components;

public partial class PageHeader
{
	/// <summary>
	/// The main title displayed in the header center
	/// </summary>
	[Parameter]
	public string Title { get; set; } = string.Empty;

	/// <summary>
	/// The subtitle displayed below the title
	/// </summary>
	[Parameter]
	public string Subtitle { get; set; } = string.Empty;

	/// <summary>
	/// The user name to display (optional, shown before action buttons)
	/// </summary>
	[Parameter]
	public string? UserName { get; set; }

	/// <summary>
	/// Whether to show the logout button
	/// </summary>
	[Parameter]
	public bool ShowLogout { get; set; } = false;

	/// <summary>
	/// Whether to show the home button
	/// </summary>
	[Parameter]
	public bool ShowHome { get; set; } = false;

	/// <summary>
	/// Whether to show the back button
	/// </summary>
	[Parameter]
	public bool ShowBack { get; set; } = false;

	/// <summary>
	/// Callback when logout button is clicked
	/// </summary>
	[Parameter]
	public EventCallback OnLogoutClick { get; set; }

	/// <summary>
	/// Callback when home button is clicked
	/// </summary>
	[Parameter]
	public EventCallback OnHomeClick { get; set; }

	/// <summary>
	/// Callback when back button is clicked
	/// </summary>
	[Parameter]
	public EventCallback OnBackClick { get; set; }

	/// <summary>
	/// Custom content for the right section of the header (replaces default buttons)
	/// Use this for entry pages that need custom action buttons
	/// </summary>
	[Parameter]
	public RenderFragment? RightContent { get; set; }
}
