using Syncfusion.XlsIO;

namespace PrimeOrders.Components.Pages.Reports;

/// <summary>
/// Utility for exporting data in formats compatible with Power BI
/// </summary>
public static class PowerBIExportUtil
{
	/// <summary>
	/// Exports report data to Power BI-ready Excel format
	/// </summary>
	/// <typeparam name="T">The type of the main report data</typeparam>
	/// <param name="reportData">The main tabular data for the report</param>
	/// <param name="chartDataSets">Dictionary containing named chart datasets</param>
	/// <param name="reportTitle">Title of the report</param>
	/// <param name="dateRangeStart">Start date for the report period</param>
	/// <param name="dateRangeEnd">End date for the report period</param>
	/// <returns>MemoryStream containing the Excel file</returns>
	public static MemoryStream ExportToPowerBI<T>(
		IEnumerable<T> reportData,
		Dictionary<string, object> chartDataSets,
		string reportTitle,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null)
	{
		MemoryStream ms = new();

		try
		{
			using (ExcelEngine excelEngine = new())
			{
				IApplication application = excelEngine.Excel;
				application.DefaultVersion = ExcelVersion.Xlsx;

				// Create a workbook with multiple worksheets
				IWorkbook workbook = application.Workbooks.Create();

				// First sheet for the main data table
				IWorksheet mainDataSheet = workbook.Worksheets.Create("MainData");
				AddDataTableToSheet(mainDataSheet, reportData);

				// Add metadata worksheet
				IWorksheet metadataSheet = workbook.Worksheets.Create("ReportMetadata");
				AddMetadata(metadataSheet, reportTitle, dateRangeStart, dateRangeEnd);

				// Add chart data worksheets
				if (chartDataSets != null)
				{
					foreach (var chartDataSet in chartDataSets)
					{
						string sheetName = SanitizeSheetName(chartDataSet.Key);
						IWorksheet chartSheet = workbook.Worksheets.Create(sheetName);
						AddChartDataToSheet(chartSheet, chartDataSet.Value);
					}
				}

				// Add Power BI integration hints worksheet
				IWorksheet powerBIHintsSheet = workbook.Worksheets.Create("PowerBIHints");
				AddPowerBIHints(powerBIHintsSheet, chartDataSets);

				// Save workbook to stream
				workbook.SaveAs(ms);
			}

			ms.Position = 0;
			return ms;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error exporting to Power BI format: {ex.Message}");
			ms.Dispose();
			throw;
		}
	}

	/// <summary>
	/// Adds the main data table to a worksheet
	/// </summary>
	private static void AddDataTableToSheet<T>(IWorksheet sheet, IEnumerable<T> data)
	{
		if (data == null || !data.Any())
			return;

		var properties = typeof(T).GetProperties();

		// Set headers
		for (int i = 0; i < properties.Length; i++)
		{
			sheet.Range[1, i + 1].Text = properties[i].Name;
			sheet.Range[1, i + 1].CellStyle.Font.Bold = true;
		}

		// Set data
		int rowIndex = 2;
		foreach (var item in data)
		{
			for (int i = 0; i < properties.Length; i++)
			{
				var value = properties[i].GetValue(item);
				if (value != null)
				{
					// Handle different data types appropriately
					if (value is DateTime dateTime)
					{
						sheet.Range[rowIndex, i + 1].DateTime = dateTime;
						sheet.Range[rowIndex, i + 1].CellStyle.NumberFormat = "yyyy-MM-dd HH:mm:ss";
					}
					else if (value is DateOnly dateOnly)
					{
						sheet.Range[rowIndex, i + 1].DateTime = dateOnly.ToDateTime(TimeOnly.MinValue);
						sheet.Range[rowIndex, i + 1].CellStyle.NumberFormat = "yyyy-MM-dd";
					}
					else if (value is decimal decimalValue)
					{
						sheet.Range[rowIndex, i + 1].Number = (double)decimalValue;
						// Use appropriate numeric format
						sheet.Range[rowIndex, i + 1].CellStyle.NumberFormat = "#,##0.00";
					}
					else if (value is double doubleValue)
					{
						sheet.Range[rowIndex, i + 1].Number = doubleValue;
						sheet.Range[rowIndex, i + 1].CellStyle.NumberFormat = "#,##0.00";
					}
					else if (value is int || value is long)
					{
						sheet.Range[rowIndex, i + 1].Number = Convert.ToDouble(value);
						sheet.Range[rowIndex, i + 1].CellStyle.NumberFormat = "#,##0";
					}
					else
					{
						sheet.Range[rowIndex, i + 1].Text = value.ToString();
					}
				}
			}
			rowIndex++;
		}

		// Format as a data table for Power BI
		var tableRange = sheet.Range[$"A1:{GetExcelColumnName(properties.Length)}{rowIndex - 1}"];
		var table = sheet.ListObjects.Create("MainDataTable", tableRange);
		table.BuiltInTableStyle = TableBuiltInStyles.TableStyleMedium2;

		// Autofit columns
		sheet.UsedRange.AutofitColumns();
	}

	/// <summary>
	/// Adds chart data to a worksheet
	/// </summary>
	private static void AddChartDataToSheet(IWorksheet sheet, object chartData)
	{
		if (chartData == null)
			return;

		// Different handling based on type of chart data
		if (chartData is IEnumerable<object> genericList)
		{
			// Handle generic list of objects
			if (!genericList.Any())
				return;

			var firstItem = genericList.First();
			var properties = firstItem.GetType().GetProperties();

			// Set headers
			for (int i = 0; i < properties.Length; i++)
			{
				sheet.Range[1, i + 1].Text = properties[i].Name;
				sheet.Range[1, i + 1].CellStyle.Font.Bold = true;
			}

			// Set data
			int rowIndex = 2;
			foreach (var item in genericList)
			{
				for (int i = 0; i < properties.Length; i++)
				{
					var value = properties[i].GetValue(item);
					if (value != null)
					{
						if (value is decimal decimalValue)
						{
							sheet.Range[rowIndex, i + 1].Number = (double)decimalValue;
							sheet.Range[rowIndex, i + 1].CellStyle.NumberFormat = "#,##0.00";
						}
						else if (value is double doubleValue)
						{
							sheet.Range[rowIndex, i + 1].Number = doubleValue;
							sheet.Range[rowIndex, i + 1].CellStyle.NumberFormat = "#,##0.00";
						}
						else if (value is int || value is long)
						{
							sheet.Range[rowIndex, i + 1].Number = Convert.ToDouble(value);
							sheet.Range[rowIndex, i + 1].CellStyle.NumberFormat = "#,##0";
						}
						else if (value is DateTime dt)
						{
							sheet.Range[rowIndex, i + 1].DateTime = dt;
							sheet.Range[rowIndex, i + 1].CellStyle.NumberFormat = "yyyy-MM-dd";
						}
						else if (value is DateOnly dateOnly)
						{
							sheet.Range[rowIndex, i + 1].DateTime = dateOnly.ToDateTime(TimeOnly.MinValue);
							sheet.Range[rowIndex, i + 1].CellStyle.NumberFormat = "yyyy-MM-dd";
						}
						else
						{
							sheet.Range[rowIndex, i + 1].Text = value.ToString();
						}
					}
				}
				rowIndex++;
			}

			// Format as a data table for Power BI
			var tableRange = sheet.Range[$"A1:{GetExcelColumnName(properties.Length)}{rowIndex - 1}"];
			var table = sheet.ListObjects.Create($"{sheet.Name}Table", tableRange);
			table.BuiltInTableStyle = TableBuiltInStyles.TableStyleMedium9;
		}

		// Autofit columns
		sheet.UsedRange.AutofitColumns();
	}

	/// <summary>
	/// Adds metadata information to the metadata worksheet
	/// </summary>
	private static void AddMetadata(IWorksheet sheet, string reportTitle, DateOnly? dateRangeStart, DateOnly? dateRangeEnd)
	{
		// Set title
		sheet.Range["A1"].Text = "Report Properties";
		sheet.Range["A1"].CellStyle.Font.Bold = true;
		sheet.Range["A1"].CellStyle.Font.Size = 14;

		// Add metadata rows
		sheet.Range["A3"].Text = "Report Title";
		sheet.Range["B3"].Text = reportTitle;

		sheet.Range["A4"].Text = "Export Date";
		sheet.Range["B4"].DateTime = DateTime.Now;
		sheet.Range["B4"].CellStyle.NumberFormat = "yyyy-MM-dd HH:mm:ss";

		if (dateRangeStart.HasValue && dateRangeEnd.HasValue)
		{
			sheet.Range["A5"].Text = "Start Date";
			sheet.Range["B5"].DateTime = dateRangeStart.Value.ToDateTime(TimeOnly.MinValue);
			sheet.Range["B5"].CellStyle.NumberFormat = "yyyy-MM-dd";

			sheet.Range["A6"].Text = "End Date";
			sheet.Range["B6"].DateTime = dateRangeEnd.Value.ToDateTime(TimeOnly.MinValue);
			sheet.Range["B6"].CellStyle.NumberFormat = "yyyy-MM-dd";

			sheet.Range["A7"].Text = "Date Period";
			sheet.Range["B7"].Formula = $"=TEXT(B5,\"dd-MMM-yyyy\") & \" to \" & TEXT(B6,\"dd-MMM-yyyy\")";
		}

		// Add some Power BI related info
		sheet.Range["A9"].Text = "Power BI Information";
		sheet.Range["A9"].CellStyle.Font.Bold = true;

		sheet.Range["A10"].Text = "Data Sheets";
		sheet.Range["B10"].Text = "MainData and named chart datasets";

		sheet.Range["A11"].Text = "Refresh Date";
		sheet.Range["B11"].Formula = "=TODAY()";
		sheet.Range["B11"].CellStyle.NumberFormat = "yyyy-MM-dd";

		// Autofit columns
		sheet.UsedRange.AutofitColumns();
	}

	/// <summary>
	/// Adds Power BI visualization hints to the hints worksheet
	/// </summary>
	private static void AddPowerBIHints(IWorksheet sheet, Dictionary<string, object> chartDataSets)
	{
		sheet.Range["A1"].Text = "Power BI Visualization Hints";
		sheet.Range["A1"].CellStyle.Font.Bold = true;
		sheet.Range["A1"].CellStyle.Font.Size = 14;

		int row = 3;

		// Add general hints
		sheet.Range[$"A{row}"].Text = "1. Main Data Table";
		sheet.Range[$"B{row}"].Text = "Use 'MainData' sheet for detailed analytics";
		row++;

		// Add hints for each chart dataset
		if (chartDataSets != null)
		{
			foreach (var dataset in chartDataSets)
			{
				string sheetName = SanitizeSheetName(dataset.Key);
				sheet.Range[$"A{row}"].Text = $"2. {sheetName}";

				// Generate hints based on dataset type
				string hint = "Use for visualization";
				if (dataset.Key.Contains("daily", StringComparison.OrdinalIgnoreCase) ||
					dataset.Key.Contains("trend", StringComparison.OrdinalIgnoreCase) ||
					dataset.Key.Contains("over time", StringComparison.OrdinalIgnoreCase))
				{
					hint = "Recommended for Line or Area chart showing trends over time";
				}
				else if (dataset.Key.Contains("distribution", StringComparison.OrdinalIgnoreCase) ||
						 dataset.Key.Contains("payment", StringComparison.OrdinalIgnoreCase) ||
						 dataset.Key.Contains("method", StringComparison.OrdinalIgnoreCase) ||
						 dataset.Key.Contains("category", StringComparison.OrdinalIgnoreCase))
				{
					hint = "Recommended for Pie or Donut chart showing distribution";
				}
				else if (dataset.Key.Contains("comparison", StringComparison.OrdinalIgnoreCase) ||
						 dataset.Key.Contains("location", StringComparison.OrdinalIgnoreCase))
				{
					hint = "Recommended for Column or Bar chart for comparison";
				}

				sheet.Range[$"B{row}"].Text = hint;
				row++;
			}
		}

		// Add some Power BI specific guidance
		row += 2;
		sheet.Range[$"A{row}"].Text = "Power BI Dashboard Creation Tips:";
		sheet.Range[$"A{row}"].CellStyle.Font.Bold = true;
		row++;

		sheet.Range[$"A{row}"].Text = "1. Data Relationships";
		sheet.Range[$"B{row}"].Text = "All sheets use consistent IDs and naming - no need to create relationships";
		row++;

		sheet.Range[$"A{row}"].Text = "2. Date Hierarchies";
		sheet.Range[$"B{row}"].Text = "Create date hierarchies from any date columns for drill-down analysis";
		row++;

		sheet.Range[$"A{row}"].Text = "3. Measures";
		sheet.Range[$"B{row}"].Text = "Create sum measures for financial columns and count measures for transactional data";
		row++;

		sheet.Range[$"A{row}"].Text = "4. Visualizations";
		sheet.Range[$"B{row}"].Text = "Use matrix for detailed data, cards for KPIs, and appropriate charts for each dataset";

		// Autofit columns
		sheet.UsedRange.AutofitColumns();
	}

	/// <summary>
	/// Convert a column number to Excel column letter
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
	/// Sanitizes a string to be used as an Excel worksheet name
	/// </summary>
	private static string SanitizeSheetName(string name)
	{
		// Remove invalid characters
		string sanitized = string.Join("", name.Select(c =>
			c is '\\' or '/' or '*' or '?' or ':' or '[' or ']' ? '_' : c));

		// Limit to 31 characters (Excel limit)
		if (sanitized.Length > 31)
		{
			sanitized = sanitized.Substring(0, 31);
		}

		return sanitized;
	}
}