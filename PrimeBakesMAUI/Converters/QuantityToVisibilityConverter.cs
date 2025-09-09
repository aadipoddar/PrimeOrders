using System.Globalization;

namespace PrimeBakes.Converters;

public class QuantityToVisibilityConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is decimal quantity)
		{
			// If parameter is "inverse", show when quantity is 0 (for Add button)
			// If parameter is null or "normal", show when quantity > 0 (for grid)
			string param = parameter?.ToString()?.ToLower();

			if (param == "inverse")
				return quantity == 0;
			else
				return quantity > 0;
		}

		return false;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
