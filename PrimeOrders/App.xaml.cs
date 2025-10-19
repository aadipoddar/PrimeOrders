using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using Microsoft.Win32;

using PrimeBakesLibrary.DataAccess;

namespace PrimeOrders;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
	public static bool IsLightTheme { get; private set; } = GetIsLightTheme();
	public static string FooterVersionText { get; set; } = $"Version: {Assembly.GetExecutingAssembly().GetName().Version}";

	protected override void OnStartup(StartupEventArgs e)
	{
		Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(Secrets.SyncfusionLicense);
		Dapper.SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

		EventManager.RegisterClassHandler(typeof(TextBox), UIElement.PreviewMouseLeftButtonDownEvent,
			new MouseButtonEventHandler(SelectivelyIgnoreMouseButton));
		EventManager.RegisterClassHandler(typeof(TextBox), UIElement.GotKeyboardFocusEvent,
			new RoutedEventHandler(SelectAllText));
		EventManager.RegisterClassHandler(typeof(TextBox), Control.MouseDoubleClickEvent,
			new RoutedEventHandler(SelectAllText));

		base.OnStartup(e);
	}

	void SelectivelyIgnoreMouseButton(object sender, MouseButtonEventArgs e)
	{
		// Find the TextBox
		DependencyObject parent = e.OriginalSource as UIElement;
		while (parent != null && parent is not TextBox)
			parent = VisualTreeHelper.GetParent(parent);

		if (parent != null)
		{
			var textBox = (TextBox)parent;
			if (!textBox.IsKeyboardFocusWithin)
			{
				// If the text box is not yet focused, give it the focus and
				// stop further processing of this click event.
				textBox.Focus();
				e.Handled = true;
			}
		}
	}

	void SelectAllText(object sender, RoutedEventArgs e)
	{
		if (e.OriginalSource is TextBox textBox)
			textBox.SelectAll();
	}

	private static bool GetIsLightTheme()
	{
		try
		{
			using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
			var value = key?.GetValue("AppsUseLightTheme");
			return value is int i && i > 0;
		}
		catch
		{
			return true;
		}
	}
}
