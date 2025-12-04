using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace PrimeBakes.Shared.Components;

public partial class ButtonCard
{
    /// <summary>
    /// The title displayed on the card
    /// </summary>
    [Parameter]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The description text displayed below the title
    /// </summary>
    [Parameter]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The color theme for the card (blue, green, red, orange, purple, teal, indigo, amber, emerald, cyan, rose, sky, violet)
    /// </summary>
    [Parameter]
    public string Color { get; set; } = "blue";

    /// <summary>
    /// The SVG icon content to display
    /// </summary>
    [Parameter]
    public RenderFragment? IconContent { get; set; }

    /// <summary>
    /// The click event callback
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Whether the button is disabled
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; } = false;

    private string CssClasses => $"dashboard-card card-{Color.ToLower()}";
}