using System.Reflection;
using System.Text.RegularExpressions;

using Syncfusion.Drawing;
using Syncfusion.XlsIO;

namespace PrimeBakesLibrary.Exporting;

/// <summary>
/// Generic Excel exporter for all report types in the application
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
	/// <param name="summaryItems">Optional dictionary of summary values to display</param>
	/// <param name="columnSettings">Optional custom column settings</param>
	/// <param name="columnOrder">Optional custom column display order</param>
	/// <returns>MemoryStream containing the Excel file</returns>
	public static MemoryStream ExportToExcel<T>(
		IEnumerable<T> data,
		string reportTitle,
		string worksheetName,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		Dictionary<string, object> summaryItems = null,
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

				// Add summary section if provided
				if (summaryItems is not null && summaryItems.Count > 0)
					currentRow = AddSummarySection(worksheet, summaryItems, effectiveColumnOrder.Count, currentRow);

				// Add data to worksheet
				currentRow = AddDataSection(worksheet, data, effectiveColumnOrder, columnSettings, currentRow);

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
		/// Whether the column is a currency column
		/// </summary>
		public bool IsCurrency { get; set; }

		/// <summary>
		/// Whether to highlight negative values
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

			// Currency fields detection based on common naming patterns
			if (prop.Name.Contains("Amount") || prop.Name.Contains("Price") ||
				prop.Name.Contains("Cost") || prop.Name.Contains("Value") ||
				prop.Name.Contains("Paid") || prop.Name.Contains("Dues") ||
				prop.Name.Contains("Salary") || prop.Name.Contains("Revenue") ||
				prop.Name.Contains("Total") || prop.Name.Contains("Cash") ||
				prop.Name.Contains("Card") || prop.Name.Contains("UPI") ||
				prop.Name.Contains("Credit") || prop.Name.EndsWith("Fee"))
			{
				setting.Alignment = ExcelHAlign.HAlignRight;
				setting.IncludeInTotal = true;
				setting.IsCurrency = true;
				setting.Format = propType == typeof(int) ? "₹#,##0" : "₹#,##0.00";
				setting.HighlightNegative = true;
			}

			else if (propType == typeof(int) || propType == typeof(long) || propType == typeof(short))
				setting.Alignment = ExcelHAlign.HAlignCenter;

			else if (propType == typeof(decimal) || propType == typeof(double) || propType == typeof(float))
			{
				setting.Alignment = ExcelHAlign.HAlignRight;
				if (prop.Name.Contains("Quantity") || prop.Name.Contains("Qty"))
				{
					setting.Format = "#,##0.00";
					setting.IncludeInTotal = true;
				}
			}

			else if (propType == typeof(DateTime) || propType == typeof(DateOnly))
			{
				setting.Alignment = ExcelHAlign.HAlignCenter;

				if (prop.Name.Contains("DateTime") || prop.Name.EndsWith("Time"))
					setting.Format = "dd-MMM-yyyy hh:mm";
				else
					setting.Format = "dd-MMM-yyyy";
			}

			else if (propType == typeof(TimeOnly) ||
					propType == typeof(DateTime) &&
					 (prop.Name.Contains("Time") || prop.Name.EndsWith("At")))
			{
				setting.Alignment = ExcelHAlign.HAlignCenter;
				setting.Format = "hh:mm";
			}

			else if (propType == typeof(bool))
			{
				setting.Alignment = ExcelHAlign.HAlignCenter;

				// For status-related properties, add conditional formatting
				if (prop.Name.Contains("Status") || prop.Name.Contains("Active"))
				{
					setting.FormatCallback = (value) => new FormatInfo
					{
						Bold = true,
						FontColor = (bool)value ? Color.FromArgb(56, 142, 60) : Color.FromArgb(198, 40, 40),
						FormattedText = (bool)value ? "Active" : "Inactive"
					};
				}
			}

			// Default for strings and other types
			else
				setting.Alignment = ExcelHAlign.HAlignLeft;

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
		headerRange.CellStyle.Font.RGBColor = Color.FromArgb(226, 19, 123); // Updated to app's main color #e2137b
		headerRange.CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;

		int currentRow = 2;

		// Row 2: Company tagline
		IRange taglineRange = worksheet.Range[$"A{currentRow}:{colLetter}{currentRow}"];
		taglineRange.Merge();
		taglineRange.Text = "Celebrating happiness";
		taglineRange.CellStyle.Font.Size = 11;
		taglineRange.CellStyle.Font.FontName = "Calibri";
		taglineRange.CellStyle.Font.Italic = true;
		taglineRange.CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
		taglineRange.CellStyle.Font.RGBColor = Color.FromArgb(193, 14, 105); // Darker variant of primary color
		currentRow++;

		// Row 3: Date range if available
		if (!string.IsNullOrEmpty(dateRangeText))
		{
			IRange dateRange = worksheet.Range[$"A{currentRow}:{colLetter}{currentRow}"];
			dateRange.Merge();
			dateRange.Text = dateRangeText;
			dateRange.CellStyle.Font.Size = 12;
			dateRange.CellStyle.Font.FontName = "Calibri";
			dateRange.CellStyle.Font.Bold = true;
			dateRange.CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
			dateRange.CellStyle.Font.RGBColor = Color.FromArgb(193, 14, 105); // Secondary color
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
		companyRange.CellStyle.Font.RGBColor = Color.FromArgb(33, 150, 243); // Complementary blue

		// Add decorative header background
		IRange headerBackgroundRange = worksheet.Range[$"A1:{colLetter}{currentRow}"];
		headerBackgroundRange.CellStyle.Color = Color.FromArgb(252, 228, 236); // Light pink background matching the theme

		// Add border bottom for header section
		IRange borderRange = worksheet.Range[$"A{currentRow}:{colLetter}{currentRow}"];
		borderRange.CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Medium;
		borderRange.CellStyle.Borders[ExcelBordersIndex.EdgeBottom].ColorRGB = Color.FromArgb(226, 19, 123); // Primary color

		// Space after header
		currentRow++;
		worksheet.Range[$"A{currentRow}:{colLetter}{currentRow}"].RowHeight = 10;

		return currentRow + 1; // Return the next row to use
	}

	/// <summary>
	/// Add summary section to the worksheet
	/// </summary>
	private static int AddSummarySection(
		IWorksheet worksheet,
		Dictionary<string, object> summaryItems,
		int columnCount,
		int startRow)
	{
		// Get the column letter for the spreadsheet width
		string colLetter = GetExcelColumnName(columnCount);

		// Summary Title
		IRange summaryTitleRange = worksheet.Range[$"A{startRow}:{colLetter}{startRow}"];
		summaryTitleRange.Merge();
		summaryTitleRange.Text = "SUMMARY INFORMATION";
		summaryTitleRange.CellStyle.Font.Bold = true;
		summaryTitleRange.CellStyle.Font.Size = 12;
		summaryTitleRange.CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
		summaryTitleRange.CellStyle.Color = Color.FromArgb(252, 228, 236); // Light pink background matching theme
		summaryTitleRange.CellStyle.Font.RGBColor = Color.FromArgb(226, 19, 123); // Primary color text

		startRow++;

		// Determine layout based on number of summary items
		int columns = Math.Min(3, summaryItems.Count);
		int rows = (int)Math.Ceiling(summaryItems.Count / (double)columns);

		// Calculate cell width based on spreadsheet width
		int cellWidth = columnCount / columns;

		// Track current position
		int currentItemIndex = 0;

		// Create summary grid
		for (int row = 0; row < rows; row++)
		{
			for (int col = 0; col < columns && currentItemIndex < summaryItems.Count; col++)
			{
				var item = summaryItems.ElementAt(currentItemIndex);
				currentItemIndex++;

				// Calculate cell coordinates
				int startCol = col * cellWidth + 1;
				int endCol = (col + 1) * cellWidth;
				if (col == columns - 1) endCol = columnCount; // Last column takes remaining space

				string startColLetter = GetExcelColumnName(startCol);
				string endColLetter = GetExcelColumnName(endCol);

				// Label cell
				IRange labelCell = worksheet.Range[$"{startColLetter}{startRow + row * 2}:{endColLetter}{startRow + row * 2}"];
				labelCell.Merge();
				labelCell.Text = item.Key;
				labelCell.CellStyle.Font.Bold = true;
				labelCell.CellStyle.Font.Size = 11;
				labelCell.CellStyle.Color = Color.FromArgb(248, 215, 225); // Lighter pink
				labelCell.CellStyle.Font.RGBColor = Color.FromArgb(226, 19, 123); // Primary color
				labelCell.CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;

				// Value cell
				IRange valueCell = worksheet.Range[$"{startColLetter}{startRow + row * 2 + 1}:{endColLetter}{startRow + row * 2 + 1}"];
				valueCell.Merge();

				// Format value based on type
				if (item.Value is decimal decimalValue)
				{
					valueCell.Number = (double)decimalValue;

					// Check if this might be currency
					if (item.Key.Contains("Revenue") || item.Key.Contains("Salary") ||
						item.Key.Contains("Dues") || item.Key.Contains("Paid") ||
						item.Key.Contains("Amount") || item.Key.Contains("Total") ||
						item.Key.Contains("Cash") || item.Key.Contains("Card") ||
						item.Key.Contains("UPI") || item.Key.Contains("Credit"))
					{
						valueCell.CellStyle.NumberFormat = "₹#,##0.00";
						valueCell.CellStyle.Font.RGBColor = Color.FromArgb(193, 14, 105); // Secondary color for money

						// Highlight negative values in red
						if (decimalValue < 0)
							valueCell.CellStyle.Font.RGBColor = Color.FromArgb(198, 40, 40); // Red
					}
				}

				else if (item.Value is double doubleValue)
				{
					valueCell.Number = doubleValue;
					valueCell.CellStyle.NumberFormat = "#,##0.00";
				}

				else if (item.Value is int intValue)
				{
					valueCell.Number = intValue;
					valueCell.CellStyle.Font.RGBColor = Color.FromArgb(33, 150, 243); // Blue accent color for counts
				}

				else
					valueCell.Text = item.Value?.ToString() ?? "";

				// Common value cell styling
				valueCell.CellStyle.Font.Size = 14;
				valueCell.CellStyle.Font.Bold = true;
				valueCell.CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
			}
		}

		// Calculate the last row used
		int lastRowUsed = startRow + rows * 2 - 1;

		// Add space after summary section
		worksheet.Range[$"A{lastRowUsed + 1}:{colLetter}{lastRowUsed + 1}"].RowHeight = 15;

		return lastRowUsed + 2; // Return the next row to use
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

		// Create table title
		IRange tableTitleRange = worksheet.Range[$"A{startRow}:{colLetter}{startRow}"];
		tableTitleRange.Merge();
		tableTitleRange.Text = "DETAILED DATA";
		tableTitleRange.CellStyle.Font.Bold = true;
		tableTitleRange.CellStyle.Font.Size = 12;
		tableTitleRange.CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
		tableTitleRange.CellStyle.Color = Color.FromArgb(248, 215, 225); // Light pink background
		tableTitleRange.CellStyle.Font.RGBColor = Color.FromArgb(226, 19, 123); // Primary color text

		startRow++;

		// Create header row
		for (int i = 0; i < columnOrder.Count; i++)
		{
			string columnName = columnOrder[i];
			var setting = columnSettings[columnName];

			string cellAddress = GetExcelColumnName(i + 1) + startRow;
			IRange headerCell = worksheet.Range[cellAddress];
			headerCell.Text = setting.DisplayName;
			headerCell.CellStyle.Font.Bold = true;
			headerCell.CellStyle.Color = Color.FromArgb(226, 19, 123); // Primary color background
			headerCell.CellStyle.Font.RGBColor = Color.White;
			headerCell.CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
			headerCell.CellStyle.Borders.ColorRGB = Color.FromArgb(193, 14, 105); // Secondary color border
		}

		startRow++;

		// Data rows
		int rowIndex = startRow;
		var properties = typeof(T).GetProperties().ToDictionary(p => p.Name);

		// Keep track of which columns are valid for totals
		var columnsToTotal = new HashSet<string>();

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
						cell.Text = "";

					else if (value is decimal decimalValue)
					{
						cell.Number = (double)decimalValue;
						if (!string.IsNullOrEmpty(setting.Format))
							cell.CellStyle.NumberFormat = setting.Format;

						// Track this column for totals if appropriate
						if (setting.IncludeInTotal)
							columnsToTotal.Add(columnName);

						// Apply conditional formatting for negative values if needed
						if (setting.HighlightNegative && decimalValue < 0)
						{
							cell.CellStyle.Font.RGBColor = Color.FromArgb(198, 40, 40); // Red
							cell.CellStyle.Font.Bold = true;
						}
					}
					else if (value is double doubleValue)
					{
						cell.Number = doubleValue;
						if (!string.IsNullOrEmpty(setting.Format))
							cell.CellStyle.NumberFormat = setting.Format;

						if (setting.IncludeInTotal)
							columnsToTotal.Add(columnName);
					}

					else if (value is int intValue)
					{
						cell.Number = intValue;
						if (!string.IsNullOrEmpty(setting.Format))
							cell.CellStyle.NumberFormat = setting.Format;

						if (setting.IncludeInTotal)
							columnsToTotal.Add(columnName);
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
						cell.Text = boolValue.ToString();

					else
						cell.Text = value.ToString();

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

		// Add totals row if there are numeric columns
		if (columnsToTotal.Count > 0)
		{
			// Add grand total row
		rowIndex += 1;

			// Find where to start the total label based on the number of columns to total
			int totalLabelColumnCount = Math.Max(1, columnOrder.Count - columnsToTotal.Count);
			string totalLabelStartCol = "A";
			string totalLabelEndCol = GetExcelColumnName(totalLabelColumnCount);

			// Create the total label
			IRange totalLabelRange = worksheet.Range[$"{totalLabelStartCol}{rowIndex}:{totalLabelEndCol}{rowIndex}"];
			totalLabelRange.Merge();
			totalLabelRange.Text = "GRAND TOTAL";
			totalLabelRange.CellStyle.Font.Bold = true;
			totalLabelRange.CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
			totalLabelRange.CellStyle.Color = Color.FromArgb(248, 215, 225); // Light pink background
			totalLabelRange.CellStyle.Font.RGBColor = Color.FromArgb(226, 19, 123); // Primary color text

			// Add the total formulas
			foreach (var columnName in columnsToTotal)
			{
				int colIndex = columnOrder.IndexOf(columnName);
				if (colIndex >= 0)
				{
					colLetter = GetExcelColumnName(colIndex + 1);
					string cellAddress = $"{colLetter}{rowIndex}";

					worksheet.Range[cellAddress].Formula = $"=SUM({colLetter}{startRow}:{colLetter}{rowIndex - 1})";

					// Apply appropriate formatting
					var setting = columnSettings[columnName];
					worksheet.Range[cellAddress].CellStyle.NumberFormat = setting.Format;

					// Style total cells
					worksheet.Range[cellAddress].CellStyle.Font.Bold = true;
					worksheet.Range[cellAddress].CellStyle.Color = Color.FromArgb(248, 215, 225); // Light pink background
					worksheet.Range[cellAddress].CellStyle.Font.RGBColor = Color.FromArgb(226, 19, 123); // Primary color text
				}
			};

			rowIndex++;
		}

		return rowIndex + 1;
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

					else if (width > 40)
						worksheet.Columns[i].ColumnWidth = 40;
				}
				catch
				{
					// Skip this column if there's an issue
					continue;
				}
			}

			// Add footer with date and page numbers
			worksheet.PageSetup.CenterFooter = "&D &T";
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