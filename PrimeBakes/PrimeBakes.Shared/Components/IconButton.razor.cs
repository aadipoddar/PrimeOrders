using Microsoft.AspNetCore.Components;

namespace PrimeBakes.Shared.Components;

public partial class IconButton
{
	[Parameter]
	public IconType Icon { get; set; }

	[Parameter]
	public string Title { get; set; } = string.Empty;

	[Parameter]
	public bool Disabled { get; set; } = false;

	[Parameter]
	public EventCallback OnClick { get; set; }

	[Parameter]
	public ButtonVariant Variant { get; set; } = ButtonVariant.Default;

	[Parameter]
	public ButtonSize Size { get; set; } = ButtonSize.Medium;

	[Parameter]
	public string CssClass { get; set; } = string.Empty;

	[Parameter]
	public string Text { get; set; } = string.Empty;

	private int IconSize => Size switch
	{
		ButtonSize.Small => 16,
		ButtonSize.Medium => 20,
		ButtonSize.Large => 24,
		_ => 20
	};

	private string GetCssClass()
	{
		var classes = new List<string>
        {
            "icon-btn",
            // Add variant class
            Variant switch
            {
                ButtonVariant.Save => "icon-btn-save",
                ButtonVariant.Excel => "icon-btn-excel",
                ButtonVariant.Pdf => "icon-btn-pdf",
                ButtonVariant.View => "icon-btn-view",
                ButtonVariant.Edit => "icon-btn-edit",
                ButtonVariant.Delete => "icon-btn-delete",
                ButtonVariant.Add => "icon-btn-add",
                ButtonVariant.Recover => "icon-btn-recover",
                _ => string.Empty
            },

            // Add size class
            Size switch
            {
                ButtonSize.Small => "icon-btn-small",
                ButtonSize.Grid => "icon-btn-grid",
                _ => string.Empty
            }
        };

		// Add text class if text is provided
		if (!string.IsNullOrEmpty(Text))
			classes.Add("icon-btn-with-text");

		// Add custom CSS class
		if (!string.IsNullOrEmpty(CssClass))
			classes.Add(CssClass);

		return string.Join(" ", classes.Where(c => !string.IsNullOrEmpty(c)));
	}
}

public enum IconType
{
	Save,
	Excel,
	Pdf,
	View,
	History,
	Report,
	New,
	Refresh,
	Edit,
	Delete,
	Home,
	Back,
	Logout,
	Print,
	Settings,
	Search,
	Filter,
	Download,
	Add,
	Cart,
	Upload,
	TrialBalance,
	Generate,
	Reset,
	Recover
}

public enum ButtonVariant
{
	Default,
	Save,
	Excel,
	Pdf,
	View,
	Edit,
	Delete,
	Add,
	Recover
}

public enum ButtonSize
{
	Small,
	Medium,
	Large,
	Grid
}
