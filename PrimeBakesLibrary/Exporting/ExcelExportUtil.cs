using System.Reflection;
using System.Text.RegularExpressions;

using Syncfusion.Drawing;
using Syncfusion.XlsIO;

namespace PrimeBakesLibrary.Exporting;

/// <summary>
/// Generic Excel exporter for all report types in the Prime Bakes application
/// </summary>
public static class ExcelExportUtil
{
	#region Public Methods

	/// <summary>
	/// Exports any data collection to an Excel file with professional formatting
	/// </summary>
	/// <typeparam name="T">The type of data being exported</typeparam>
	/// <param name="data">The collection of data to export</param>
	/// <param name="reportTitle">The title of the report</param>
	/// <param name="worksheetName">The name of the worksheet</param>
	/// <param name="dateRangeStart">Optional start date for date range reports</param>
	/// <param name="dateRangeEnd">Optional end date for date range reports</param>
	/// <param name="columnSettings">Optional custom column settings</param>
	/// <param name="columnOrder">Optional custom column display order</param>
	/// <returns>MemoryStream containing the Excel file</returns>
	public static MemoryStream ExportToExcel<T>(
		IEnumerable<T> data,
		string reportTitle,
		string worksheetName,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		Dictionary<string, ColumnSetting> columnSettings = null,
		List<string> columnOrder = null)
	{
		MemoryStream ms = new();

		try
		{
			using (ExcelEngine excelEngine = new())
			{
				IApplication application = excelEngine.Excel;
				application.DefaultVersion = ExcelVersion.Xlsx;

				// Create a workbook with a worksheet
				IWorkbook workbook = application.Workbooks.Create(1);
				IWorksheet worksheet = workbook.Worksheets[0];
				worksheet.Name = worksheetName;

				// Apply document properties
				workbook.BuiltInDocumentProperties.Title = reportTitle;
				workbook.BuiltInDocumentProperties.Subject = worksheetName;
				workbook.BuiltInDocumentProperties.Author = "Prime Bakes";

				// Get column info from data type if not provided
				columnSettings ??= GetDefaultColumnSettings<T>();

				// Determine column order
				List<string> effectiveColumnOrder = DetermineColumnOrder<T>(columnSettings, columnOrder);

				// Setup the worksheet
				int currentRow = SetupHeader(worksheet, reportTitle, effectiveColumnOrder.Count, dateRangeStart, dateRangeEnd);

				// Add data to worksheet
				currentRow = AddDataSection(worksheet, data, effectiveColumnOrder, columnSettings, currentRow);

				// Add branding footer
				AddBrandingFooter(worksheet, currentRow, effectiveColumnOrder.Count);

				// Apply final formatting
				ApplyFinalFormatting(worksheet, effectiveColumnOrder.Count);

				// Save workbook to stream
				workbook.SaveAs(ms);
			}

			ms.Position = 0;
			return ms;
		}
		catch (Exception ex)
		{
			// Log exception
			Console.WriteLine($"Error exporting Excel: {ex.Message}");

			// Clean up stream on error
			ms.Dispose();
			throw;
		}
	}

	/// <summary>
	/// Column setting information for customizing Excel export
	/// </summary>
	public class ColumnSetting
	{
		/// <summary>
		/// Display name for the column header
		/// </summary>
		public string DisplayName { get; set; }

		/// <summary>
		/// Format string for the cell data
		/// </summary>
		public string Format { get; set; }

		/// <summary>
		/// Width of the column in Excel
		/// </summary>
		public double Width { get; set; } = 15;

		/// <summary>
		/// Horizontal alignment of the cell content
		/// </summary>
		public ExcelHAlign Alignment { get; set; } = ExcelHAlign.HAlignCenter;

		/// <summary>
		/// Whether to highlight negative values in red
		/// </summary>
		public bool HighlightNegative { get; set; }

		/// <summary>
		/// Whether the column should be included in totals
		/// </summary>
		public bool IncludeInTotal { get; set; }

		/// <summary>
		/// Custom validation function
		/// </summary>
		public Func<object, FormatInfo> FormatCallback { get; set; }
	}

	/// <summary>
	/// Format information returned by format callbacks
	/// </summary>
	public class FormatInfo
	{
		public Color? FontColor { get; set; }
		public bool Bold { get; set; }
		public string FormattedText { get; set; }
	}

	#endregion

	#region Private Methods

	/// <summary>
	/// Get default column settings from type T
	/// </summary>
	private static Dictionary<string, ColumnSetting> GetDefaultColumnSettings<T>()
	{
		var settings = new Dictionary<string, ColumnSetting>();

		// Use reflection to get properties of T
		var properties = typeof(T).GetProperties();

		foreach (var prop in properties)
		{
			// Skip collections and complex types that don't make sense in Excel
			if (prop.PropertyType == typeof(byte[]) ||
				typeof(IEnumerable<object>).IsAssignableFrom(prop.PropertyType) &&
				prop.PropertyType != typeof(string))
				continue;

			var setting = new ColumnSetting
			{
				DisplayName = SplitCamelCase(prop.Name),
			};

			// Set appropriate format and alignment based on property type
			var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

			// Numeric types - all should be included in totals
			if (propType == typeof(decimal) || propType == typeof(double) || propType == typeof(float))
			{
				setting.Alignment = ExcelHAlign.HAlignRight;
				setting.Format = "#,##0.00";
				setting.IncludeInTotal = true;
				setting.HighlightNegative = true;
			}
			else if (propType == typeof(int) || propType == typeof(long) || propType == typeof(short))
			{
				// Check if it's likely an ID field (skip totals for IDs)
				if (prop.Name.EndsWith("Id") || prop.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
				{
					setting.Alignment = ExcelHAlign.HAlignCenter;
					setting.IncludeInTotal = false;
				}
				else
				{
					setting.Alignment = ExcelHAlign.HAlignRight;
					setting.Format = "#,##0";
					setting.IncludeInTotal = true;
				}
			}
			// DateTime types
			else if (propType == typeof(DateTime) || propType == typeof(DateOnly))
			{
				setting.Alignment = ExcelHAlign.HAlignCenter;

				if (prop.Name.Contains("DateTime") || prop.Name.EndsWith("Time"))
					setting.Format = "dd-MMM-yyyy hh:mm";
				else
					setting.Format = "dd-MMM-yyyy";
			}
			// TimeOnly type
			else if (propType == typeof(TimeOnly))
			{
				setting.Alignment = ExcelHAlign.HAlignCenter;
				setting.Format = "hh:mm";
			}
			// Boolean type
			else if (propType == typeof(bool))
			{
				setting.Alignment = ExcelHAlign.HAlignCenter;

				// For status-related properties, add conditional formatting
				if (prop.Name.Contains("Status") || prop.Name.Contains("Active") || prop.Name.Contains("Is"))
				{
					setting.FormatCallback = (value) =>
					{
						if (value == null) return null;
						bool boolValue = (bool)value;
						return new FormatInfo
						{
							Bold = true,
							FontColor = boolValue ? Color.FromArgb(22, 163, 74) : Color.FromArgb(220, 38, 38),
							FormattedText = boolValue ? "Yes" : "No"
						};
					};
				}
			}
			// Default for strings and other types
			else
			{
				setting.Alignment = ExcelHAlign.HAlignLeft;
			}

			// Add to collection
			settings[prop.Name] = setting;
		}

		return settings;
	}

	/// <summary>
	/// Determine effective column order for the report
	/// </summary>
	private static List<string> DetermineColumnOrder<T>(
		Dictionary<string, ColumnSetting> columnSettings,
		List<string> columnOrder)
	{
		// If explicit column order is provided, use it
		if (columnOrder is not null && columnOrder.Count > 0)
			return columnOrder;

		// Otherwise use all available columns in natural order
		return [.. columnSettings.Keys];
	}

	/// <summary>
	/// Set up the header section of the worksheet
	/// </summary>
	private static int SetupHeader(
		IWorksheet worksheet,
		string reportTitle,
		int columnCount,
		DateOnly? dateRangeStart,
		DateOnly? dateRangeEnd)
	{
		// Set column range based on data width
		string colLetter = GetExcelColumnName(columnCount);

		// Build date range string if dates are provided
		string dateRangeText = "";
		if (dateRangeStart.HasValue && dateRangeEnd.HasValue)
			dateRangeText = $"{dateRangeStart:dd MMM yyyy} - {dateRangeEnd:dd MMM yyyy}";

		// Main header with report title
		IRange headerRange = worksheet.Range[$"A1:{colLetter}1"];
		headerRange.Merge();
		headerRange.Text = reportTitle.ToUpper();
		headerRange.CellStyle.Font.Bold = true;
		headerRange.CellStyle.Font.Size = 20;
		headerRange.CellStyle.Font.FontName = "Calibri";
		headerRange.CellStyle.Font.RGBColor = Color.FromArgb(59, 130, 246); // Blue theme color
		headerRange.CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;

		int currentRow = 2;

		// Row 2: Date range if available
		if (!string.IsNullOrEmpty(dateRangeText))
		{
			IRange dateRange = worksheet.Range[$"A{currentRow}:{colLetter}{currentRow}"];
			dateRange.Merge();
			dateRange.Text = dateRangeText;
			dateRange.CellStyle.Font.Size = 12;
			dateRange.CellStyle.Font.FontName = "Calibri";
			dateRange.CellStyle.Font.Bold = true;
			dateRange.CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
			dateRange.CellStyle.Font.RGBColor = Color.FromArgb(71, 85, 105); // Slate gray
			currentRow++;
		}

		// Company Name
		IRange companyRange = worksheet.Range[$"A{currentRow}:{colLetter}{currentRow}"];
		companyRange.Merge();
		companyRange.Text = "Prime Bakes";
		companyRange.CellStyle.Font.Size = 14;
		companyRange.CellStyle.Font.FontName = "Calibri";
		companyRange.CellStyle.Font.Bold = true;
		companyRange.CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
		companyRange.CellStyle.Font.RGBColor = Color.FromArgb(15, 23, 42); // Dark slate

		// Add decorative header background
		IRange headerBackgroundRange = worksheet.Range[$"A1:{colLetter}{currentRow}"];
		headerBackgroundRange.CellStyle.Color = Color.FromArgb(239, 246, 255); // Light blue background

		// Add border bottom for header section
		IRange borderRange = worksheet.Range[$"A{currentRow}:{colLetter}{currentRow}"];
		borderRange.CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Medium;
		borderRange.CellStyle.Borders[ExcelBordersIndex.EdgeBottom].ColorRGB = Color.FromArgb(59, 130, 246); // Blue

		// Space after header
		currentRow++;
		worksheet.Range[$"A{currentRow}:{colLetter}{currentRow}"].RowHeight = 10;

		return currentRow + 1; // Return the next row to use
	}

	/// <summary>
	/// Add data section to the worksheet
	/// </summary>
	private static int AddDataSection<T>(
		IWorksheet worksheet,
		IEnumerable<T> data,
		List<string> columnOrder,
		Dictionary<string, ColumnSetting> columnSettings,
		int startRow)
	{
		if (data == null || !data.Any())
			return startRow + 1;

		string colLetter = GetExcelColumnName(columnOrder.Count);

		// Create header row
		for (int i = 0; i < columnOrder.Count; i++)
		{
			string columnName = columnOrder[i];
			var setting = columnSettings[columnName];

			string cellAddress = GetExcelColumnName(i + 1) + startRow;
			IRange headerCell = worksheet.Range[cellAddress];
			headerCell.Text = setting.DisplayName;
			headerCell.CellStyle.Font.Bold = true;
			headerCell.CellStyle.Font.Size = 11;
			headerCell.CellStyle.Color = Color.FromArgb(59, 130, 246); // Blue background
			headerCell.CellStyle.Font.RGBColor = Color.White;
			headerCell.CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
			headerCell.CellStyle.VerticalAlignment = ExcelVAlign.VAlignCenter;
			headerCell.CellStyle.Borders.ColorRGB = Color.FromArgb(37, 99, 235); // Darker blue border
			worksheet.SetRowHeight(startRow, 25);
		}

		startRow++;

		// Data rows
		int rowIndex = startRow;
		var properties = typeof(T).GetProperties().ToDictionary(p => p.Name);

		// Keep track of which columns have numeric data for totals
		var columnsWithData = new HashSet<string>();

		foreach (var item in data)
		{
			for (int i = 0; i < columnOrder.Count; i++)
			{
				string columnName = columnOrder[i];
				string cellAddress = GetExcelColumnName(i + 1) + rowIndex;
				IRange cell = worksheet.Range[cellAddress];

				if (properties.TryGetValue(columnName, out PropertyInfo property))
				{
					var setting = columnSettings[columnName];
					object value = property.GetValue(item);

					// Apply the value to the cell based on its type
					if (value is null)
					{
						cell.Text = "";
					}
					else if (value is decimal decimalValue)
					{
						cell.Number = (double)decimalValue;
						if (!string.IsNullOrEmpty(setting.Format))
							cell.CellStyle.NumberFormat = setting.Format;

						// Track this column for totals
						if (setting.IncludeInTotal)
							columnsWithData.Add(columnName);

						// Apply conditional formatting for negative values if needed
						if (setting.HighlightNegative && decimalValue < 0)
						{
							cell.CellStyle.Font.RGBColor = Color.FromArgb(220, 38, 38); // Red
							cell.CellStyle.Font.Bold = true;
						}
					}
					else if (value is double doubleValue)
					{
						cell.Number = doubleValue;
						if (!string.IsNullOrEmpty(setting.Format))
							cell.CellStyle.NumberFormat = setting.Format;

						if (setting.IncludeInTotal)
							columnsWithData.Add(columnName);
					}
					else if (value is float floatValue)
					{
						cell.Number = floatValue;
						if (!string.IsNullOrEmpty(setting.Format))
							cell.CellStyle.NumberFormat = setting.Format;

						if (setting.IncludeInTotal)
							columnsWithData.Add(columnName);
					}
					else if (value is int intValue)
					{
						cell.Number = intValue;
						if (!string.IsNullOrEmpty(setting.Format))
							cell.CellStyle.NumberFormat = setting.Format;

						if (setting.IncludeInTotal)
							columnsWithData.Add(columnName);
					}
					else if (value is long longValue)
					{
						cell.Number = longValue;
						if (!string.IsNullOrEmpty(setting.Format))
							cell.CellStyle.NumberFormat = setting.Format;

						if (setting.IncludeInTotal)
							columnsWithData.Add(columnName);
					}
					else if (value is short shortValue)
					{
						cell.Number = shortValue;
						if (!string.IsNullOrEmpty(setting.Format))
							cell.CellStyle.NumberFormat = setting.Format;

						if (setting.IncludeInTotal)
							columnsWithData.Add(columnName);
					}
					else if (value is DateTime dateTimeValue)
					{
						cell.DateTime = dateTimeValue;
						cell.CellStyle.NumberFormat = setting.Format ?? "dd-MMM-yyyy";
					}
					else if (value is DateOnly dateOnlyValue)
					{
						cell.DateTime = dateOnlyValue.ToDateTime(TimeOnly.MinValue);
						cell.CellStyle.NumberFormat = setting.Format ?? "dd-MMM-yyyy";
					}
					else if (value is TimeOnly timeOnlyValue)
					{
						cell.DateTime = DateTime.Today.Add(timeOnlyValue.ToTimeSpan());
						cell.CellStyle.NumberFormat = setting.Format ?? "hh:mm";
					}
					else if (value is bool boolValue)
					{
						cell.Text = boolValue.ToString();
					}
					else
					{
						cell.Text = value.ToString();
					}

					// Apply formatting callback if available
					if (setting.FormatCallback != null)
					{
						var formatInfo = setting.FormatCallback(value);
						if (formatInfo != null)
						{
							if (formatInfo.FontColor.HasValue)
								cell.CellStyle.Font.RGBColor = formatInfo.FontColor.Value;

							if (formatInfo.Bold)
								cell.CellStyle.Font.Bold = true;

							if (!string.IsNullOrEmpty(formatInfo.FormattedText))
								cell.Text = formatInfo.FormattedText;
						}
					}

					// Apply alignment
					cell.CellStyle.HorizontalAlignment = setting.Alignment;
				}
			}

			// Style data row
			IRange dataRow = worksheet.Range[$"A{rowIndex}:{colLetter}{rowIndex}"];
			dataRow.CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
			dataRow.CellStyle.Borders[ExcelBordersIndex.EdgeBottom].ColorRGB = Color.LightGray;

			// Alternate row colors
			if (rowIndex % 2 == 0)
				dataRow.CellStyle.Color = Color.FromArgb(248, 249, 250);

			rowIndex++;
		}

		// Create table with borders
		if (rowIndex > startRow)
		{
			IRange tableRange = worksheet.Range[$"A{startRow - 1}:{colLetter}{rowIndex - 1}"];
			tableRange.CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Thin;
			tableRange.CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Thin;
			tableRange.CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
			tableRange.CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
		}

		// Add totals row for all numeric columns with data
		if (columnsWithData.Count > 0)
		{
			rowIndex += 1;

			// Add the total label in the first column
			worksheet.Range[$"A{rowIndex}"].Text = "TOTAL";
			worksheet.Range[$"A{rowIndex}"].CellStyle.Font.Bold = true;
			worksheet.Range[$"A{rowIndex}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignLeft;
			worksheet.Range[$"A{rowIndex}"].CellStyle.Color = Color.FromArgb(219, 234, 254); // Light blue background
			worksheet.Range[$"A{rowIndex}"].CellStyle.Font.RGBColor = Color.FromArgb(30, 64, 175); // Dark blue text
			worksheet.Range[$"A{rowIndex}"].CellStyle.Font.Size = 11;

			// Add the total formulas for numeric columns
			for (int i = 0; i < columnOrder.Count; i++)
			{
				string columnName = columnOrder[i];

				if (columnsWithData.Contains(columnName))
				{
					string colLtr = GetExcelColumnName(i + 1);
					string cellAddress = $"{colLtr}{rowIndex}";

					worksheet.Range[cellAddress].Formula = $"=SUM({colLtr}{startRow}:{colLtr}{rowIndex - 1})";

					// Apply appropriate formatting
					var setting = columnSettings[columnName];
					worksheet.Range[cellAddress].CellStyle.NumberFormat = setting.Format;
					worksheet.Range[cellAddress].CellStyle.Font.Bold = true;
					worksheet.Range[cellAddress].CellStyle.Color = Color.FromArgb(219, 234, 254); // Light blue background
					worksheet.Range[cellAddress].CellStyle.Font.RGBColor = Color.FromArgb(30, 64, 175); // Dark blue text
					worksheet.Range[cellAddress].CellStyle.HorizontalAlignment = setting.Alignment;
					worksheet.Range[cellAddress].CellStyle.Font.Size = 11;
				}
				else
				{
					// Empty cell for non-numeric columns
					string colLtr = GetExcelColumnName(i + 1);
					string cellAddress = $"{colLtr}{rowIndex}";
					worksheet.Range[cellAddress].CellStyle.Color = Color.FromArgb(219, 234, 254); // Light blue background
				}
			}

			// Add border to total row
			IRange totalRow = worksheet.Range[$"A{rowIndex}:{colLetter}{rowIndex}"];
			totalRow.CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Medium;
			totalRow.CellStyle.Borders[ExcelBordersIndex.EdgeTop].ColorRGB = Color.FromArgb(59, 130, 246); // Blue
			totalRow.CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Medium;
			totalRow.CellStyle.Borders[ExcelBordersIndex.EdgeBottom].ColorRGB = Color.FromArgb(59, 130, 246); // Blue
			worksheet.SetRowHeight(rowIndex, 25);

			rowIndex++;
		}

		return rowIndex + 1;
	}

	/// <summary>
	/// Add branding footer to the worksheet
	/// </summary>
	private static void AddBrandingFooter(IWorksheet worksheet, int startRow, int columnCount)
	{
		try
		{
			// Add empty row for spacing
			startRow += 1;

			// Add branding row
			string colLetter = GetExcelColumnName(columnCount);
			IRange brandingRange = worksheet.Range[$"A{startRow}:{colLetter}{startRow}"];
			brandingRange.Merge();
			brandingRange.Text = $"© {DateTime.Now.Year} A Product By AadiSoft | www.aadisoft.vercel.app";
			brandingRange.CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
			brandingRange.CellStyle.VerticalAlignment = ExcelVAlign.VAlignCenter;
			brandingRange.CellStyle.Font.Size = 10;
			brandingRange.CellStyle.Font.Italic = true;
			brandingRange.CellStyle.Font.RGBColor = Color.FromArgb(107, 114, 128); // Gray-500
			brandingRange.CellStyle.Color = Color.FromArgb(249, 250, 251); // Gray-50 background
			worksheet.SetRowHeight(startRow, 22);
		}
		catch (Exception ex)
		{
			// Log error but continue - don't let branding issues prevent export
			Console.WriteLine($"Error in AddBrandingFooter: {ex.Message}");
		}
	}

	/// <summary>
	/// Apply final formatting to the worksheet
	/// </summary>
	private static void ApplyFinalFormatting(IWorksheet worksheet, int columnCount)
	{
		try
		{
			// AutoFit columns for better readability
			worksheet.UsedRange?.AutofitColumns();

			// Apply column width limits
			for (int i = 1; i <= columnCount; i++)
			{
				try
				{
					double width = worksheet.Columns[i].ColumnWidth;

					if (width < 8)
						worksheet.Columns[i].ColumnWidth = 8;
					else if (width > 50)
						worksheet.Columns[i].ColumnWidth = 50;
				}
				catch
				{
					// Skip this column if there's an issue
					continue;
				}
			}

			// Add footer with AadiSoft branding, date and page numbers
			worksheet.PageSetup.LeftFooter = $"© {DateTime.Now.Year} A Product By AadiSoft";
			worksheet.PageSetup.CenterFooter = $"Exported on: {DateTime.Now:dd-MMM-yyyy hh:mm tt}";
			worksheet.PageSetup.RightFooter = "Page &P of &N";

			// Set print options for better presentation
			worksheet.PageSetup.Orientation = ExcelPageOrientation.Landscape;
			worksheet.PageSetup.FitToPagesTall = 0;
			worksheet.PageSetup.FitToPagesWide = 1;
			worksheet.PageSetup.LeftMargin = 0.25;
			worksheet.PageSetup.RightMargin = 0.25;
			worksheet.PageSetup.TopMargin = 0.5;
			worksheet.PageSetup.BottomMargin = 0.5;
			worksheet.PageSetup.HeaderMargin = 0.3;
			worksheet.PageSetup.FooterMargin = 0.3;
		}
		catch (Exception ex)
		{
			// Log error but continue - don't let formatting issues prevent export
			Console.WriteLine($"Error in ApplyFinalFormatting: {ex.Message}");
		}
	}

	/// <summary>
	/// Convert a column number to Excel column letter (A, B, C, ..., Z, AA, AB, ...)
	/// </summary>
	private static string GetExcelColumnName(int columnNumber)
	{
		string columnName = "";

		while (columnNumber > 0)
		{
			int remainder = (columnNumber - 1) % 26;
			char columnLetter = (char)('A' + remainder);
			columnName = columnLetter + columnName;
			columnNumber = (columnNumber - 1) / 26;
		}

		return columnName;
	}

	/// <summary>
	/// Split camel case text into readable format
	/// </summary>
	private static string SplitCamelCase(string input)
	{
		if (string.IsNullOrEmpty(input))
			return input;

		// Handle special cases
		if (input.Equals("ID", StringComparison.OrdinalIgnoreCase))
			return "ID";

		// Replace common abbreviations
		input = Regex.Replace(input, "Id$", "ID");

		// Split by capital letters
		return Regex.Replace(input,
			"([a-z])([A-Z])",
			"$1 $2",
			RegexOptions.Compiled);
	}

	#endregion
}