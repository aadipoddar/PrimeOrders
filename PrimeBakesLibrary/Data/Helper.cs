using System.Globalization;

namespace PrimeBakesLibrary.Data;

public static class Helper
{
	public static string RemoveSpace(this string str) =>
		str.Replace(" ", "");

	public static string FormatIndianCurrency(this decimal rate) =>
		string.Format(new CultureInfo("hi-IN"), "{0:C}", rate);

	public static string FormatIndianCurrency(this decimal? rate)
	{
		rate ??= 0;
		return string.Format(new CultureInfo("hi-IN"), "{0:C}", rate);
	}

	public static string FormatIndianCurrency(this int rate) =>
		string.Format(new CultureInfo("hi-IN"), "{0:C}", rate);

	public static string FormatDecimalWithTwoDigits(this decimal value) =>
		value.ToString("0.00", CultureInfo.InvariantCulture);
}
