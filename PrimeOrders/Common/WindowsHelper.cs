using System.Windows.Controls;

namespace PrimeOrders.Common;

public static class WindowsHelper
{
	public static void ValidateIntegerInput(object sender, System.Windows.Input.TextCompositionEventArgs e) =>
	e.Handled = !int.TryParse(e.Text, out _);

	public static void ValidateDecimalInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
	{
		bool approvedDecimalPoint = false;

		if (e.Text == ".")
		{
			if (!((TextBox)sender).Text.Contains('.'))
				approvedDecimalPoint = true;
		}

		if (!(char.IsDigit(e.Text, e.Text.Length - 1) || approvedDecimalPoint))
			e.Handled = true;
	}
}
