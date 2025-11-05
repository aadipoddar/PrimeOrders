using System.Reflection;
using System.Text.RegularExpressions;

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
	#region Public Methods

	/// <summary>
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
		bool useLandscape = false)
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

				// Setup header
				float currentY = SetupHeader(page, reportTitle, dateRangeStart, dateRangeEnd, logoPath);

				// Add data grid
				currentY = AddDataGrid(page, pdfDocument, data, effectiveColumnOrder, columnSettings, currentY,
					useBuiltInStyle, autoAdjustColumnWidth);

				// Add branding footer
				AddBrandingFooter(pdfDocument);

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
	/// Setup PDF header with title and date range
	/// </summary>
	private static float SetupHeader(
		PdfPage page,
		string reportTitle,
		DateOnly? dateRangeStart,
		DateOnly? dateRangeEnd,
		string customLogoPath = null)
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

				// Calculate logo dimensions (max height 30, maintain aspect ratio)
				float maxLogoHeight = 30;
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

				currentY += logoHeight + 3; // Move down past logo with small spacing
			}

			// Draw company name at left margin (always shown, whether logo exists or not)
			PdfStandardFont companyFont = new(PdfFontFamily.Helvetica, 14, PdfFontStyle.Bold);
			PdfBrush companyBrush = new PdfSolidBrush(new PdfColor(59, 130, 246)); // Blue
			page.Graphics.DrawString("Prime Bakes", companyFont, companyBrush, new PointF(leftMargin, currentY));
			currentY += 15;
		}
		catch (Exception ex)
		{
			// Fallback: Just show company name if logo loading fails
			Console.WriteLine($"Logo loading failed: {ex.Message}");
			PdfStandardFont companyFont = new(PdfFontFamily.Helvetica, 14, PdfFontStyle.Bold);
			PdfBrush companyBrush = new PdfSolidBrush(new PdfColor(59, 130, 246)); // Blue
			page.Graphics.DrawString("Prime Bakes", companyFont, companyBrush, new PointF(leftMargin, currentY));
			currentY += 15;
		}

		// Report title
		PdfStandardFont titleFont = new(PdfFontFamily.Helvetica, 11, PdfFontStyle.Bold);
		PdfBrush titleBrush = new PdfSolidBrush(new PdfColor(31, 41, 55)); // Dark gray
		page.Graphics.DrawString(reportTitle, titleFont, titleBrush, new PointF(leftMargin, currentY));
		currentY += 13;

		// Date range
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
		{
			string dateRange = $"Period: {dateRangeStart?.ToString("dd-MMM-yyyy") ?? "START"} to {dateRangeEnd?.ToString("dd-MMM-yyyy") ?? "END"}";
			PdfStandardFont dateFont = new(PdfFontFamily.Helvetica, 8);
			PdfBrush dateBrush = new PdfSolidBrush(new PdfColor(107, 114, 128)); // Gray
			page.Graphics.DrawString(dateRange, dateFont, dateBrush, new PointF(leftMargin, currentY));
			currentY += 10;
		}

		// Separator line
		PdfPen separatorPen = new(new PdfColor(59, 130, 246), 1); // Blue
		page.Graphics.DrawLine(separatorPen, new PointF(leftMargin, currentY), new PointF(pageWidth - rightMargin, currentY));
		currentY += 5;

		return currentY;
	}

	/// <summary>
	/// Add data grid to PDF
	/// </summary>
	private static float AddDataGrid<T>(
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
		pdfGrid.AllowRowBreakAcrossPages = false; // Keep rows together

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
				headerRow.Cells[i].Style.Font = new PdfStandardFont(PdfFontFamily.Helvetica, 5, PdfFontStyle.Bold);
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
					row.Cells[i].Style.Font = new PdfStandardFont(PdfFontFamily.Helvetica, 4);
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
			totalRow.Cells[0].Style.Font = new PdfStandardFont(PdfFontFamily.Helvetica, 5, PdfFontStyle.Bold);
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
					totalRow.Cells[i].Style.Font = new PdfStandardFont(PdfFontFamily.Helvetica, 5, PdfFontStyle.Bold);
					totalRow.Cells[i].Style.TextBrush = new PdfSolidBrush(new PdfColor(30, 64, 175)); // Dark blue
					totalRow.Cells[i].Style.StringFormat = setting.StringFormat;
				}
			}
		}

		// Grid styling
		pdfGrid.Style.CellPadding = new PdfPaddings(2, 1, 2, 1);
		pdfGrid.Style.Font = new PdfStandardFont(PdfFontFamily.Helvetica, 4);

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

		// Draw grid with improved layout
		float footerHeight = 50; // Height of the footer template
		float pageHeight = page.GetClientSize().Height;
		RectangleF paginateBounds = new(
			15, // left margin
			0, // top
			pageWidth - 30, // width
			pageHeight - footerHeight // height, leaving space for footer
		);

		PdfGridLayoutFormat layoutFormat = new()
		{
			Layout = PdfLayoutType.Paginate,
			Break = PdfLayoutBreakType.FitPage,
			PaginateBounds = paginateBounds
		};

		PdfGridLayoutResult result = pdfGrid.Draw(page, new PointF(15, startY), layoutFormat);

		return result.Bounds.Bottom + 10;
	}

	/// <summary>
	/// Add branding footer to all pages
	/// </summary>
	private static void AddBrandingFooter(PdfDocument document)
	{
		try
		{
			// Create footer template
			PdfPageTemplateElement footer = new(new RectangleF(0, 0, document.Pages[0].GetClientSize().Width, 50));

			PdfStandardFont footerFont = new(PdfFontFamily.Helvetica, 7, PdfFontStyle.Italic);
			PdfBrush footerBrush = new PdfSolidBrush(new PdfColor(107, 114, 128)); // Gray

			// Left: AadiSoft branding
			string branding = $"© {DateTime.Now.Year} A Product By aadisoft.vercel.app";
			footer.Graphics.DrawString(branding, footerFont, footerBrush, new PointF(15, 10));

			// Center: Export date
			string exportDate = $"Exported on: {DateTime.Now:dd-MMM-yyyy hh:mm tt}";
			SizeF exportDateSize = footerFont.MeasureString(exportDate);
			float centerX = (document.Pages[0].GetClientSize().Width - exportDateSize.Width) / 2;
			footer.Graphics.DrawString(exportDate, footerFont, footerBrush, new PointF(centerX, 10));

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
			pageInfo.Draw(footer.Graphics, new PointF(rightX, 10));

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
