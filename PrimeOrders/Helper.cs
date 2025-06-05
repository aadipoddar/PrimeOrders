using System.Globalization;

namespace PrimeOrders;

public static class Helper
{
	public static string RemoveSpace(this string str) =>
		str.Replace(" ", "");

	public static string FormatIndianCurrency(this decimal rate)
	{
		var hindi = new CultureInfo("hi-IN");
		return string.Format(hindi, "{0:C}", rate);
	}

	public static string FormatIndianCurrency(this decimal? rate)
	{
		rate ??= 0;

		var hindi = new CultureInfo("hi-IN");
		return string.Format(hindi, "{0:C}", rate);
	}

	public static string FormatIndianCurrency(this int rate)
	{
		var hindi = new CultureInfo("hi-IN");
		return string.Format(hindi, "{0:C}", rate);
	}
}
