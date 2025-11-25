using System.Reflection;
using System.Text.RegularExpressions;

using PrimeBakesLibrary.Data;

using NumericWordsConversion;

using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;

namespace PrimeBakesLibrary.Exporting;

/// <summary>
/// Generic PDF export utility for generating professional PDF reports
/// </summary>
public static class PDFReportExportUtil
{
	private static PdfLayoutFormat _layoutFormat;

	#region Public Methods	/// <summary>
	/// Export data to PDF with automatic column detection and formatting
	/// </summary>
	/// <typeparam name="T">Type of data to export</typeparam>
	/// <param name="data">Collection of data to export</param>
	/// <param name="reportTitle">Title of the report</param>
	/// <param name="dateRangeStart">Optional start date for report</param>
	/// <param name="dateRangeEnd">Optional end date for report</param>
	/// <param name="columnSettings">Optional custom column settings</param>
	/// <param name="columnOrder">Optional custom column order</param>
	/// <param name="useBuiltInStyle">Optional: Use Syncfusion built-in table styles</param>
	/// <param name="autoAdjustColumnWidth">Optional: Automatically adjust column widths</param>
	/// <param name="logoPath">Optional: Custom path to company logo image (PNG, JPG, etc.)</param>
	/// <param name="useLandscape">Optional: Use landscape orientation for wide tables</param>
	/// <param name="locationName">Optional: Location name to display in header</param>
	/// <param name="partyName">Optional: Party/ledger name to display in header</param>
	/// <param name="customSummaryFields">Optional: Custom fields to display in summary section (key=label, value=formatted value)</param>
	/// <returns>MemoryStream containing the PDF file</returns>
	public static MemoryStream ExportToPdf<T>(
		IEnumerable<T> data,
		string reportTitle,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		Dictionary<string, ColumnSetting> columnSettings = null,
		List<string> columnOrder = null,
		bool useBuiltInStyle = false,
		bool autoAdjustColumnWidth = true,
		string logoPath = null,
		bool useLandscape = false,
		string locationName = null,
		string partyName = null,
		Dictionary<string, string> customSummaryFields = null)
	{
		MemoryStream ms = new();

		try
		{
			using (PdfDocument pdfDocument = new())
			{
				// Get column info from data type if not provided
				columnSettings ??= GetDefaultColumnSettings<T>();

				// Determine column order
				List<string> effectiveColumnOrder = DetermineColumnOrder<T>(columnSettings, columnOrder);

				// Filter out columns that have no data (all null or zero)
				effectiveColumnOrder = FilterEmptyColumns(data, effectiveColumnOrder, columnSettings);

				// Add page to the PDF document with orientation
				PdfPage page;
				if (useLandscape)
				{
					// Create landscape page (A4 landscape dimensions: 842 x 595 points)
					PdfSection section = pdfDocument.Sections.Add();
					section.PageSettings.Size = new SizeF(842, 595); // Landscape A4
					page = section.Pages.Add();
				}
				else
				{
					page = pdfDocument.Pages.Add();
				}

				// Add footer template first
				AddBrandingFooter(pdfDocument);

				// Initialize layout format for proper pagination
				_layoutFormat = new()
				{
					Layout = PdfLayoutType.Paginate,
					Break = PdfLayoutBreakType.FitPage
				};

				// Setup header
				float currentY = SetupHeader(page, reportTitle, dateRangeStart, dateRangeEnd, logoPath, locationName, partyName);

				// Add data grid
				PdfLayoutResult gridResult = AddDataGrid(page, pdfDocument, data, effectiveColumnOrder, columnSettings, currentY,
					useBuiltInStyle, autoAdjustColumnWidth);

				// Get the last page where the grid ended
				PdfPage lastPage = gridResult.Page;
				PdfGraphics lastPageGraphics = lastPage.Graphics;
				currentY = gridResult.Bounds.Bottom + 8;

				// Add summary section
				currentY = AddSummarySection(lastPageGraphics, data, columnSettings, effectiveColumnOrder, 15, lastPage.GetClientSize().Width, currentY, customSummaryFields);

				// Save PDF document to stream
				pdfDocument.Save(ms);
				pdfDocument.Close(true);
			}

			ms.Position = 0;
			return ms;
		}
		catch (Exception ex)
		{
			// Log exception
			Console.WriteLine($"Error exporting PDF: {ex.Message}");

			// Clean up stream on error
			ms.Dispose();
			throw;
		}
	}

	/// <summary>
	/// Column setting configuration class
	/// </summary>
	public class ColumnSetting
	{
		public string DisplayName { get; set; }
		public PdfStringFormat StringFormat { get; set; }
		public bool IncludeInTotal { get; set; } = true;
		public bool HighlightNegative { get; set; } = false;
		public string Format { get; set; }
		public bool IsRequired { get; set; }
		public bool IsGrandTotal { get; set; }

		public ColumnSetting()
		{
			StringFormat = new PdfStringFormat
			{
				Alignment = PdfTextAlignment.Left,
				LineAlignment = PdfVerticalAlignment.Middle
			};
		}
	}

	#endregion

	#region Private Methods

	/// <summary>
	/// Get default column settings based on data type properties
	/// </summary>
	private static Dictionary<string, ColumnSetting> GetDefaultColumnSettings<T>()
	{
		var settings = new Dictionary<string, ColumnSetting>();
		var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

		foreach (var prop in properties)
		{
			var setting = new ColumnSetting
			{
				DisplayName = SplitCamelCase(prop.Name)
			};

			// Configure based on property type
			Type propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

			if (propType == typeof(int) || propType == typeof(long) || propType == typeof(short))
			{
				// Integer types - right align
				setting.StringFormat.Alignment = PdfTextAlignment.Right;
				setting.Format = "#,##0";
				setting.IncludeInTotal = !prop.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase);
			}
			else if (propType == typeof(decimal) || propType == typeof(double) || propType == typeof(float))
			{
				// Decimal types - right align with 2 decimals
				setting.StringFormat.Alignment = PdfTextAlignment.Right;
				setting.Format = "#,##0.00";
				setting.IncludeInTotal = true;
			}
			else if (propType == typeof(DateTime) || propType == typeof(DateOnly))
			{
				// Date types - center align
				setting.StringFormat.Alignment = PdfTextAlignment.Center;
				setting.Format = "dd-MMM-yyyy hh:mm";
				setting.IncludeInTotal = false;
			}
			else if (propType == typeof(bool))
			{
				// Boolean - center align
				setting.StringFormat.Alignment = PdfTextAlignment.Center;
				setting.IncludeInTotal = false;
			}
			else
			{
				// String and others - left align
				setting.StringFormat.Alignment = PdfTextAlignment.Left;
				setting.IncludeInTotal = false;
			}

			settings[prop.Name] = setting;
		}

		return settings;
	}

	/// <summary>
	/// Determine the effective column order
	/// </summary>
	private static List<string> DetermineColumnOrder<T>(
		Dictionary<string, ColumnSetting> columnSettings,
		List<string> columnOrder)
	{
		if (columnOrder != null && columnOrder.Count > 0)
		{
			return columnOrder;
		}

		// Use all properties if no order specified
		var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
		return [.. properties.Select(p => p.Name)];
	}

	/// <summary>
	/// Filter out columns that have all null or zero values
	/// </summary>
	private static List<string> FilterEmptyColumns<T>(
		IEnumerable<T> data,
		List<string> columnOrder,
		Dictionary<string, ColumnSetting> columnSettings = null)
	{
		if (data == null || !data.Any())
			return columnOrder;

		var filteredColumns = new List<string>();

		foreach (var columnName in columnOrder)
		{
			var propInfo = typeof(T).GetProperty(columnName);
			if (propInfo == null)
			{
				// Keep column if property not found (shouldn't happen)
				filteredColumns.Add(columnName);
				continue;
			}

			// Check if column is marked as required in columnSettings
			if (columnSettings != null && columnSettings.TryGetValue(columnName, out var setting) && setting.IsRequired)
			{
				filteredColumns.Add(columnName);
				continue;
			}

			bool hasNonEmptyValue = false;

			foreach (var item in data)
			{
				var value = propInfo.GetValue(item);

				// Check if value is not null and not zero
				if (value != null)
				{
					// For numeric types, check if not zero
					if (value is decimal decValue && decValue != 0)
					{
						hasNonEmptyValue = true;
						break;
					}
					else if (value is double dblValue && dblValue != 0)
					{
						hasNonEmptyValue = true;
						break;
					}
					else if (value is float fltValue && fltValue != 0)
					{
						hasNonEmptyValue = true;
						break;
					}
					else if (value is int intValue && intValue != 0)
					{
						hasNonEmptyValue = true;
						break;
					}
					else if (value is long lngValue && lngValue != 0)
					{
						hasNonEmptyValue = true;
						break;
					}
					// For non-numeric types, if it's not null, it has a value
					else if (!(value is decimal || value is double || value is float || value is int || value is long))
					{
						// For strings, check if not empty
						if (value is string strValue)
						{
							if (!string.IsNullOrWhiteSpace(strValue))
							{
								hasNonEmptyValue = true;
								break;
							}
						}
						else
						{
							// Other types - if not null, consider it has a value
							hasNonEmptyValue = true;
							break;
						}
					}
				}
			}

			// Only add column if it has at least one non-empty/non-zero value
			if (hasNonEmptyValue)
			{
				filteredColumns.Add(columnName);
			}
		}

		return filteredColumns;
	}

	/// <summary>
	/// Setup PDF header with title and date range
	/// </summary>
	private static float SetupHeader(
		PdfPage page,
		string reportTitle,
		DateOnly? dateRangeStart,
		DateOnly? dateRangeEnd,
		string customLogoPath = null,
		string locationName = null,
		string partyName = null)
	{
		float currentY = 10;
		float leftMargin = 15;
		float rightMargin = 15;
		float pageWidth = page.GetClientSize().Width;

		// Try to load and draw company logo
		try
		{
			// Use custom logo path if provided, otherwise try default locations
			string[] possibleLogoPaths;

			if (!string.IsNullOrEmpty(customLogoPath) && File.Exists(customLogoPath))
			{
				possibleLogoPaths = [customLogoPath];
			}
			else
			{
				// Try multiple possible logo paths
				possibleLogoPaths =
				[
					Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "images", "logo_full.png"),
					Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "wwwroot", "images", "logo_full.png"),
					Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logo_full.png"),
					Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "PrimeBakes", "PrimeBakes", "wwwroot", "images", "logo_full.png"),
					Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "PrimeBakes", "PrimeBakes.Web", "wwwroot", "images", "logo_full.png")
				];
			}

			string logoPath = possibleLogoPaths.FirstOrDefault(File.Exists);

			if (!string.IsNullOrEmpty(logoPath))
			{
				// Load the logo image
				using FileStream imageStream = new(logoPath, FileMode.Open, FileAccess.Read);
				PdfBitmap logoBitmap = new(imageStream);

				// Calculate logo dimensions (compact for better space utilization)
				float maxLogoHeight = 42;
				float logoWidth = logoBitmap.Width;
				float logoHeight = logoBitmap.Height;
				float aspectRatio = logoWidth / logoHeight;

				if (logoHeight > maxLogoHeight)
				{
					logoHeight = maxLogoHeight;
					logoWidth = logoHeight * aspectRatio;
				}

				// Center the logo horizontally
				float logoX = (pageWidth - logoWidth) / 2;
				page.Graphics.DrawImage(logoBitmap, new PointF(logoX, currentY), new SizeF(logoWidth, logoHeight));

				currentY += logoHeight + 5; // Move down past logo with spacing
			}
		}
		catch (Exception ex)
		{
			// Log error but continue without logo
			Console.WriteLine($"Logo loading failed: {ex.Message}");
		}

		// Draw a separator line (like invoice)
		PdfPen separatorPen = new(new PdfColor(59, 130, 246), 2f);
		page.Graphics.DrawLine(separatorPen, new PointF(leftMargin, currentY), new PointF(pageWidth - rightMargin, currentY));
		currentY += 4;

		// Report title (in blue like invoice)
		PdfStandardFont titleFont = new(PdfFontFamily.Helvetica, 14, PdfFontStyle.Bold);
		PdfBrush titleBrush = new PdfSolidBrush(new PdfColor(59, 130, 246)); // Blue
		page.Graphics.DrawString(reportTitle.ToUpper(), titleFont, titleBrush, new PointF(leftMargin, currentY));
		currentY += 16;

		// Location (left side, like outlet in invoice)
		if (!string.IsNullOrEmpty(locationName))
		{
			PdfStandardFont locationFont = new(PdfFontFamily.Helvetica, 10, PdfFontStyle.Bold);
			PdfBrush locationBrush = new PdfSolidBrush(new PdfColor(100, 100, 100)); // Gray
			string locationText = $"Location: {locationName}";
			page.Graphics.DrawString(locationText, locationFont, locationBrush, new PointF(leftMargin, currentY));

			// Only add space if party name is also present
			if (!string.IsNullOrEmpty(partyName))
				currentY += 14;
		}

		// Party name (left side, below location if exists)
		if (!string.IsNullOrEmpty(partyName))
		{
			PdfStandardFont partyFont = new(PdfFontFamily.Helvetica, 10, PdfFontStyle.Bold);
			PdfBrush partyBrush = new PdfSolidBrush(new PdfColor(100, 100, 100)); // Gray
			string partyText = $"Party: {partyName}";
			page.Graphics.DrawString(partyText, partyFont, partyBrush, new PointF(leftMargin, currentY));
		}

		// Date range (right side, like invoice date)
		bool hasPeriod = dateRangeStart.HasValue || dateRangeEnd.HasValue;
		if (hasPeriod)
		{
			string dateRange = $"Period: {dateRangeStart?.ToString("dd-MMM-yyyy") ?? "START"} to {dateRangeEnd?.ToString("dd-MMM-yyyy") ?? "END"}";
			PdfStandardFont dateFont = new(PdfFontFamily.Helvetica, 10);
			PdfBrush dateBrush = PdfBrushes.Black;
			SizeF dateSize = dateFont.MeasureString(dateRange);
			page.Graphics.DrawString(dateRange, dateFont, dateBrush, new PointF(pageWidth - rightMargin - dateSize.Width, currentY));
		}

		// Add final spacing if location, party, or period was displayed
		if (!string.IsNullOrEmpty(locationName) || !string.IsNullOrEmpty(partyName) || hasPeriod)
			currentY += 14; return currentY;
	}

	/// <summary>
	/// Add data grid to PDF
	/// </summary>
	private static PdfLayoutResult AddDataGrid<T>(
		PdfPage page,
		PdfDocument document,
		IEnumerable<T> data,
		List<string> columnOrder,
		Dictionary<string, ColumnSetting> columnSettings,
		float startY,
		bool useBuiltInStyle = false,
		bool autoAdjustColumnWidth = true)
	{
		// Create PdfGrid
		PdfGrid pdfGrid = new();

		// Add columns (PdfGrid requires int count, not Add method with names)
		pdfGrid.Columns.Add(columnOrder.Count);

		// Enable features
		pdfGrid.RepeatHeader = true; // Repeat headers on each page
		pdfGrid.AllowRowBreakAcrossPages = true; // Allow pagination for large datasets

		// Calculate available width and set fixed column widths to prevent overflow
		float pageWidth = page.GetClientSize().Width;
		float availableWidth = pageWidth - 30; // Account for margins (15px each side)
		float columnWidth = availableWidth / columnOrder.Count;

		// Set equal width for all columns to ensure they fit on one page
		for (int i = 0; i < columnOrder.Count; i++)
		{
			pdfGrid.Columns[i].Width = columnWidth;
		}

		// Disable horizontal overflow to force content to fit within column width
		pdfGrid.Style.AllowHorizontalOverflow = false;

		// Apply built-in style if requested
		if (useBuiltInStyle)
		{
			PdfGridBuiltinStyleSettings styleSettings = new()
			{
				ApplyStyleForBandedRows = true,
				ApplyStyleForHeaderRow = true
			};
			pdfGrid.ApplyBuiltinStyle(PdfGridBuiltinStyle.GridTable4Accent1, styleSettings);
		}

		// Add header row
		PdfGridRow headerRow = pdfGrid.Headers.Add(1)[0];
		for (int i = 0; i < columnOrder.Count; i++)
		{
			string columnName = columnOrder[i];
			var setting = columnSettings[columnName];
			headerRow.Cells[i].Value = setting.DisplayName;

			// Header style (only apply if not using built-in style)
			if (!useBuiltInStyle)
			{
				headerRow.Cells[i].Style.BackgroundBrush = new PdfSolidBrush(new PdfColor(59, 130, 246)); // Blue
				headerRow.Cells[i].Style.TextBrush = PdfBrushes.White;
				headerRow.Cells[i].Style.Font = new PdfStandardFont(PdfFontFamily.Helvetica, 7, PdfFontStyle.Bold);
				headerRow.Cells[i].Style.Borders.All = new PdfPen(Color.White, 0.5f);
				headerRow.Cells[i].Style.StringFormat = new PdfStringFormat
				{
					Alignment = PdfTextAlignment.Center,
					LineAlignment = PdfVerticalAlignment.Middle,
					WordWrap = PdfWordWrapType.Word
				};
			}
		}

		// Add data rows
		var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.ToDictionary(p => p.Name, p => p);

		foreach (var item in data)
		{
			PdfGridRow row = pdfGrid.Rows.Add();

			for (int i = 0; i < columnOrder.Count; i++)
			{
				string columnName = columnOrder[i];
				if (!properties.ContainsKey(columnName))
					continue;

				var property = properties[columnName];
				var value = property.GetValue(item);
				var setting = columnSettings[columnName];

				// Format value
				string displayValue = FormatValue(value, setting.Format);
				row.Cells[i].Value = displayValue;

				// Apply cell styling (only if not using built-in style)
				if (!useBuiltInStyle)
				{
					row.Cells[i].Style.Font = new PdfStandardFont(PdfFontFamily.Helvetica, 8);
					row.Cells[i].Style.Borders.All = new PdfPen(new PdfColor(229, 231, 235), 0.5f); // Light gray border
				}

				// Apply string format with word wrap
				if (setting.StringFormat != null)
				{
					// Clone the existing format and ensure word wrap is enabled
					row.Cells[i].Style.StringFormat = new PdfStringFormat
					{
						Alignment = setting.StringFormat.Alignment,
						LineAlignment = setting.StringFormat.LineAlignment,
						WordWrap = PdfWordWrapType.Word
					};
				}
				else
				{
					// Default format with word wrap
					row.Cells[i].Style.StringFormat = new PdfStringFormat
					{
						WordWrap = PdfWordWrapType.Word
					};
				}

				row.Cells[i].Style.CellPadding = new PdfPaddings(2, 1, 2, 1);

				// Highlight negative values
				if (setting.HighlightNegative && value != null)
				{
					if (decimal.TryParse(value.ToString(), out decimal numValue) && numValue < 0)
					{
						row.Cells[i].Style.TextBrush = new PdfSolidBrush(new PdfColor(220, 38, 38)); // Red
					}
				}
			}
		}

		// Add totals row if needed
		var columnsWithTotals = columnOrder
			.Where(col => columnSettings.ContainsKey(col) && columnSettings[col].IncludeInTotal)
			.ToList();

		if (columnsWithTotals.Count > 0)
		{
			PdfGridRow totalRow = pdfGrid.Rows.Add();

			// Add "TOTAL" label in first cell
			totalRow.Cells[0].Value = "TOTAL";
			totalRow.Cells[0].Style.Font = new PdfStandardFont(PdfFontFamily.Helvetica, 7, PdfFontStyle.Bold);
			totalRow.Cells[0].Style.BackgroundBrush = new PdfSolidBrush(new PdfColor(219, 234, 254)); // Light blue
			totalRow.Cells[0].Style.TextBrush = new PdfSolidBrush(new PdfColor(30, 64, 175)); // Dark blue
			totalRow.Cells[0].Style.Borders.All = new PdfPen(new PdfColor(59, 130, 246), 1f);
			totalRow.Cells[0].Style.CellPadding = new PdfPaddings(2, 1, 2, 1);

			// Calculate and add totals
			for (int i = 1; i < columnOrder.Count; i++)
			{
				string columnName = columnOrder[i];
				var setting = columnSettings[columnName];

				totalRow.Cells[i].Style.BackgroundBrush = new PdfSolidBrush(new PdfColor(219, 234, 254)); // Light blue
				totalRow.Cells[i].Style.Borders.All = new PdfPen(new PdfColor(59, 130, 246), 1f);
				totalRow.Cells[i].Style.CellPadding = new PdfPaddings(2, 1, 2, 1);

				if (setting.IncludeInTotal && properties.ContainsKey(columnName))
				{
					var property = properties[columnName];
					decimal total = 0;

					foreach (var item in data)
					{
						var value = property.GetValue(item);
						if (value != null && decimal.TryParse(value.ToString(), out decimal numValue))
						{
							total += numValue;
						}
					}

					string displayValue = FormatValue(total, setting.Format);
					totalRow.Cells[i].Value = displayValue;
					totalRow.Cells[i].Style.Font = new PdfStandardFont(PdfFontFamily.Helvetica, 7, PdfFontStyle.Bold);
					totalRow.Cells[i].Style.TextBrush = new PdfSolidBrush(new PdfColor(30, 64, 175)); // Dark blue
					totalRow.Cells[i].Style.StringFormat = setting.StringFormat;
				}
			}
		}

		// Grid styling
		pdfGrid.Style.CellPadding = new PdfPaddings(2, 1, 2, 1);
		pdfGrid.Style.Font = new PdfStandardFont(PdfFontFamily.Helvetica, 8);

		// Alternating row colors (only if not using built-in style)
		if (!useBuiltInStyle)
		{
			for (int i = 0; i < pdfGrid.Rows.Count; i++)
			{
				// Don't apply to totals row (last row if totals exist)
				bool isTotalsRow = columnsWithTotals.Count > 0 && i == pdfGrid.Rows.Count - 1;

				if (i % 2 == 0 && !isTotalsRow)
				{
					for (int j = 0; j < pdfGrid.Columns.Count; j++)
					{
						pdfGrid.Rows[i].Cells[j].Style.BackgroundBrush = new PdfSolidBrush(new PdfColor(249, 250, 251)); // Light gray
					}
				}
			}
		}

		// Draw grid with proper pagination layout similar to reference code
		// The footer template bounds are automatically handled by the document template
		PdfLayoutResult result = pdfGrid.Draw(page, new PointF(15, startY), _layoutFormat);

		return result;
	}

	/// <summary>
	/// Add summary section with totals and amount in words
	/// </summary>
	private static float AddSummarySection<T>(
		PdfGraphics graphics,
		IEnumerable<T> data,
		Dictionary<string, ColumnSetting> columnSettings,
		List<string> columnOrder,
		float leftMargin,
		float pageWidth,
		float startY,
		Dictionary<string, string> customSummaryFields = null)
	{
		float currentY = startY;
		float summaryColumnX = pageWidth - 200; // Right side for summary
		float rightMargin = 20;

		PdfStandardFont labelFont = new(PdfFontFamily.Helvetica, 8, PdfFontStyle.Regular);
		PdfStandardFont valueFont = new(PdfFontFamily.Helvetica, 8, PdfFontStyle.Regular);
		PdfStandardFont totalFont = new(PdfFontFamily.Helvetica, 10, PdfFontStyle.Bold);
		PdfBrush labelBrush = new PdfSolidBrush(new PdfColor(80, 80, 80));
		PdfBrush valueBrush = new PdfSolidBrush(new PdfColor(0, 0, 0));

		// Calculate totals for numeric columns
		var totals = new Dictionary<string, decimal>();
		decimal grandTotal = 0;

		foreach (var columnName in columnOrder)
		{
			if (columnSettings.TryGetValue(columnName, out var setting) && setting.IncludeInTotal)
			{
				decimal columnTotal = 0;
				foreach (var item in data)
				{
					var propInfo = typeof(T).GetProperty(columnName);
					if (propInfo != null)
					{
						var value = propInfo.GetValue(item);
						if (value != null && decimal.TryParse(value.ToString(), out decimal numValue))
						{
							columnTotal += numValue;
						}
					}
				}
				totals[columnName] = columnTotal;

				// Use column marked as IsGrandTotal, or fall back to TotalAmount, or last numeric column
				if (setting.IsGrandTotal)
					grandTotal = columnTotal;
				else if (columnName == "TotalAmount" && !columnSettings.Values.Any(s => s.IsGrandTotal))
					grandTotal = columnTotal;
				else if (!totals.ContainsKey("TotalAmount") && !columnSettings.Values.Any(s => s.IsGrandTotal) && columnTotal != 0)
					grandTotal = columnTotal;
			}
		}       // Draw custom summary fields first (if any)
		if (customSummaryFields != null && customSummaryFields.Count > 0)
		{
			PdfStandardFont customFont = new(PdfFontFamily.Helvetica, 8, PdfFontStyle.Bold);
			PdfBrush customBrush = new PdfSolidBrush(new PdfColor(59, 130, 246));

			foreach (var field in customSummaryFields)
			{
				graphics.DrawString($"{field.Key}:", customFont, customBrush, new PointF(summaryColumnX, currentY));
				SizeF fieldSize = customFont.MeasureString(field.Value);
				graphics.DrawString(field.Value, customFont, customBrush, new PointF(pageWidth - rightMargin - fieldSize.Width, currentY));
				currentY += 12;
			}

			// Add separator line after custom fields
			if (totals.Count > 0)
			{
				PdfPen separatorPen = new(new PdfColor(200, 200, 200), 0.5f);
				graphics.DrawLine(separatorPen, new PointF(summaryColumnX - 10, currentY), new PointF(pageWidth - 20, currentY));
				currentY += 8;
			}
		}

		// Draw summary on the right side
		if (totals.Count > 0)
		{
			// Draw each total
			foreach (var kvp in totals)
			{
				if (kvp.Value == 0) continue;

				var setting = columnSettings[kvp.Key];
				graphics.DrawString($"{setting.DisplayName}:", labelFont, labelBrush, new PointF(summaryColumnX, currentY));
				string totalText = FormatValue(kvp.Value, setting.Format);
				SizeF totalSize = valueFont.MeasureString(totalText);
				graphics.DrawString(totalText, valueFont, valueBrush, new PointF(pageWidth - rightMargin - totalSize.Width, currentY));
				currentY += 10;
			}

			// Draw line above grand total
			PdfPen linePen = new(new PdfColor(59, 130, 246), 1f);
			graphics.DrawLine(linePen, new PointF(summaryColumnX - 10, currentY), new PointF(pageWidth - 20, currentY));
			currentY += 4;

			// Grand Total
			PdfBrush totalBrush = new PdfSolidBrush(new PdfColor(59, 130, 246));
			graphics.DrawString("GRAND TOTAL:", totalFont, totalBrush, new PointF(summaryColumnX, currentY));
			string grandTotalText = grandTotal.FormatIndianCurrency();
			SizeF grandTotalSize = totalFont.MeasureString(grandTotalText);
			graphics.DrawString(grandTotalText, totalFont, totalBrush, new PointF(pageWidth - rightMargin - grandTotalSize.Width, currentY));
			currentY += 15;

			// Amount in Words
			if (grandTotal > 0)
			{
				PdfStandardFont amountLabelFont = new(PdfFontFamily.Helvetica, 8, PdfFontStyle.Bold);
				PdfStandardFont amountValueFont = new(PdfFontFamily.Helvetica, 8, PdfFontStyle.Italic);

				graphics.DrawString("Amount in Words:", amountLabelFont, valueBrush, new PointF(leftMargin, currentY));
				currentY += 10;

				string amountInWords = ConvertAmountToWords(grandTotal);
				float leftColumnWidth = (pageWidth - 40) * 0.6f;
				DrawWrappedText(graphics, amountInWords, amountValueFont, valueBrush,
					new RectangleF(leftMargin, currentY, leftColumnWidth, 20));
				currentY += 18;
			}
		}

		return currentY;
	}

	/// <summary>
	/// Helper method to draw wrapped text within a rectangle
	/// </summary>
	private static void DrawWrappedText(PdfGraphics graphics, string text, PdfFont font, PdfBrush brush, RectangleF bounds)
	{
		if (string.IsNullOrEmpty(text))
			return;

		PdfStringFormat format = new()
		{
			LineAlignment = PdfVerticalAlignment.Top,
			Alignment = PdfTextAlignment.Left,
			WordWrap = PdfWordWrapType.Word
		};

		graphics.DrawString(text, font, brush, bounds, format);
	}

	/// <summary>
	/// Convert amount to words (Indian format)
	/// </summary>
	private static string ConvertAmountToWords(decimal amount)
	{
		try
		{
			// Use NumericWordsConversion package for Indian currency
			var converter = new CurrencyWordsConverter(new CurrencyWordsConversionOptions
			{
				Culture = Culture.Hindi,
				CurrencyUnit = "Rupee",
				SubCurrencyUnit = "Paise",
				EndOfWordsMarker = "Only"
			});

			string words = converter.ToWords(amount);
			return words;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error converting amount to words: {ex.Message}");
			return "Amount in Words Not Available";
		}
	}

	/// <summary>
	/// Add branding footer to all pages
	/// </summary>
	private static void AddBrandingFooter(PdfDocument document)
	{
		try
		{
			// Create footer template with proper bounds
			RectangleF footerRect = new(0, 0, document.Pages[0].GetClientSize().Width, 30);
			PdfPageTemplateElement footer = new(footerRect);

			PdfStandardFont footerFont = new(PdfFontFamily.Helvetica, 7, PdfFontStyle.Italic);
			PdfBrush footerBrush = new PdfSolidBrush(new PdfColor(107, 114, 128)); // Gray

			// Draw separator line at top
			PdfPen separatorPen = new(new PdfColor(59, 130, 246), 0.5f);
			footer.Graphics.DrawLine(separatorPen, new PointF(0, 0), new PointF(footer.Width, 0));

			// Left: AadiSoft branding
			string branding = $"© {DateTime.Now.Year} A Product By aadisoft.vercel.app";
			footer.Graphics.DrawString(branding, footerFont, footerBrush, new PointF(15, 8));

			// Center: Export date
			string exportDate = $"Exported on: {DateTime.Now:dd-MMM-yyyy hh:mm tt}";
			SizeF exportDateSize = footerFont.MeasureString(exportDate);
			float centerX = (document.Pages[0].GetClientSize().Width - exportDateSize.Width) / 2;
			footer.Graphics.DrawString(exportDate, footerFont, footerBrush, new PointF(centerX, 8));

			// Right: Page numbers
			PdfPageNumberField pageNumber = new();
			PdfPageCountField pageCount = new();
			PdfCompositeField pageInfo = new(
				footerFont,
				footerBrush,
				"Page {0} of {1}",
				pageNumber,
				pageCount);

			string pageText = "Page 999 of 999"; // Max width for alignment
			SizeF pageInfoSize = footerFont.MeasureString(pageText);
			float rightX = document.Pages[0].GetClientSize().Width - pageInfoSize.Width - 15;
			pageInfo.Draw(footer.Graphics, new PointF(rightX, 8));

			// Add footer to document
			document.Template.Bottom = footer;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error adding branding footer: {ex.Message}");
		}
	}

	/// <summary>
	/// Format a value based on the specified format string
	/// </summary>
	private static string FormatValue(object value, string format)
	{
		if (value == null)
			return string.Empty;

		if (string.IsNullOrEmpty(format))
			return value.ToString();

		try
		{
			if (value is DateTime dt)
			{
				return dt.ToString(format);
			}
			else if (value is DateOnly dateOnly)
			{
				return dateOnly.ToString(format);
			}
			else if (value is decimal || value is double || value is float || value is int || value is long)
			{
				if (decimal.TryParse(value.ToString(), out decimal numValue))
				{
					return numValue.ToString(format);
				}
			}
			else if (value is bool boolValue)
			{
				return boolValue ? "Yes" : "No";
			}

			return value.ToString();
		}
		catch
		{
			return value.ToString();
		}
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
